using MathNet.Numerics.LinearAlgebra;
using Trusses.Core.Models; 
using System;
using System.Collections.Generic;
using System.Linq;

namespace Trusses.Core.Logic
{
    public static class TrussSolver
    {
        public static Truss Resolver(Truss truss)
        {
            var nodes = truss.Nodes;
            var members = truss.Members;

            int dof = nodes.Count * 2; // Graus de liberdade (2 por nó)

            // 1. Inicializa Matrizes e Vetores (Dense Matrix do MathNet)
            var K = Matrix<double>.Build.Dense(dof, dof);
            var F = Vector<double>.Build.Dense(dof);
            bool[] restrained = new bool[dof];

            // Mapa para localizar índice do nó rapidamente
            var nodeIndexMap = new Dictionary<long, int>();
            for (int i = 0; i < nodes.Count; i++)
            {
                nodeIndexMap[nodes[i].Id] = i;
                // Reseta reações anteriores
                nodes[i].ReactionX = 0;
                nodes[i].ReactionY = 0;
            }

            // 2. Monta a Matriz de Rigidez Global (Stiffness Matrix)
            foreach (var m in members)
            {
                AddMemberStiffness(K, m, nodeIndexMap);
            }

            // Pequeno epsilon para estabilidade numérica (evita singularidade se houver nós soltos)
            for (int i = 0; i < dof; i++)
            {
                K[i, i] += 1e-9;
            }

            // 3. Aplica as Cargas (Loads) no Vetor de Forças F
            foreach (var n in nodes)
            {
                if (n.Load != null)
                {
                    int idx = nodeIndexMap[n.Id];
                    int xi = 2 * idx;
                    int yi = 2 * idx + 1;

                    double mag = n.Load.Magnitude;
                    double angRad = n.Load.Angle * (Math.PI / 180.0); // Graus para Radianos

                    F[xi] += mag * Math.Cos(angRad);
                    F[yi] += mag * Math.Sin(angRad);
                }
            }

            // 4. Identifica Restrições (Supports)
            foreach (var n in nodes)
            {
                int idx = nodeIndexMap[n.Id];
                int xi = 2 * idx;
                int yi = 2 * idx + 1;

                if (n.Support != null)
                {
                    if (n.Support.RestrainX) restrained[xi] = true;
                    if (n.Support.RestrainY) restrained[yi] = true;
                }
            }

            // 5. Resolve o Sistema Linear (K * U = F) considerando restrições
            var displacements = SolveWithConstraints(K, F, restrained);

            // 6. Pós-Processamento: Calcular Forças nos Membros
            foreach (var m in members)
            {
                double force = CalcularForcaNoMembro(m, displacements, nodeIndexMap);
                m.Force = force;
            }

            // 7. Pós-Processamento: Calcular Reações de Apoio
            // Força Global = K * Deslocamentos
            var GlobalForces = K * displacements;

            foreach (var n in nodes)
            {
                if (n.Support != null)
                {
                    int idx = nodeIndexMap[n.Id];
                    int xi = 2 * idx;
                    int yi = 2 * idx + 1;

                    double totalFx = GlobalForces[xi];
                    double totalFy = GlobalForces[yi];

                    // Subtrai a carga aplicada para achar apenas a reação do apoio
                    double loadX = 0;
                    double loadY = 0;
                    if (n.Load != null)
                    {
                        double mag = n.Load.Magnitude;
                        double angRad = n.Load.Angle * (Math.PI / 180.0);
                        loadX = mag * Math.Cos(angRad);
                        loadY = mag * Math.Sin(angRad);
                    }

                    double rx = totalFx - loadX;
                    double ry = totalFy - loadY;

                    // Inverte sinal de Y (convenção gráfica vs matemática) se necessário, 
                    // mas matematicamente Reação + Carga = ForçaElastica. 
                    ry = -ry;

                    // Limpeza de ruído numérico
                    if (Math.Abs(rx) < 1e-5) rx = 0.0;
                    if (Math.Abs(ry) < 1e-5) ry = 0.0;

                    if (n.Support.RestrainX) n.ReactionX = rx;
                    if (n.Support.RestrainY) n.ReactionY = ry;
                }
            }

            return truss;
        }

        private static void AddMemberStiffness(Matrix<double> K, Member m, Dictionary<long, int> nodeIndexMap)
        {
            var n1 = m.StartNode;
            var n2 = m.EndNode;

            int i = nodeIndexMap[n1.Id];
            int j = nodeIndexMap[n2.Id];

            double dx = n2.X - n1.X;
            double dy = n2.Y - n1.Y;
            double L = Math.Sqrt(dx * dx + dy * dy);

            // Cossenos diretores
            double c = dx / L;
            double s = dy / L;

            double rigidity = 1.0; // EA/L simplificado (assumindo EA constante relativo)

            // Matriz de rigidez local do elemento (4x4)
            double[,] kLocal = {
                {  c*c,  c*s, -c*c, -c*s },
                {  c*s,  s*s, -c*s, -s*s },
                { -c*c, -c*s,  c*c,  c*s },
                { -c*s, -s*s,  c*s,  s*s }
            };

            // Mapeamento para os índices globais (2*i, 2*i+1...)
            int[] indices = { 2 * i, 2 * i + 1, 2 * j, 2 * j + 1 };

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    // Acumula na matriz global
                    K[indices[row], indices[col]] += kLocal[row, col] * rigidity;
                }
            }
        }

        private static Vector<double> SolveWithConstraints(Matrix<double> K, Vector<double> F, bool[] restrained)
        {
            int n = F.Count;
            // Lista de índices livres (não restritos)
            var freeIndices = new List<int>();
            for (int i = 0; i < n; i++)
            {
                if (!restrained[i]) freeIndices.Add(i);
            }

            int m = freeIndices.Count;
            if (m == 0) return Vector<double>.Build.Dense(n);

            // Cria submatrizes apenas com os graus de liberdade livres
            var Kf = Matrix<double>.Build.Dense(m, m);
            var Ff = Vector<double>.Build.Dense(m);

            for (int i = 0; i < m; i++)
            {
                int row = freeIndices[i];
                Ff[i] = F[row];
                for (int j = 0; j < m; j++)
                {
                    int col = freeIndices[j];
                    Kf[i, j] = K[row, col];
                }
            }

            // Resolve o sistema reduzido: Kf * Uf = Ff
            var Uf = Kf.Solve(Ff);

            // Reconstrói o vetor de deslocamentos global preenchendo zeros onde está travado
            var U = Vector<double>.Build.Dense(n);
            for (int i = 0; i < m; i++)
            {
                U[freeIndices[i]] = Uf[i];
            }

            return U;
        }

        private static double CalcularForcaNoMembro(Member m, Vector<double> u, Dictionary<long, int> nodeIndexMap)
        {
            var n1 = m.StartNode;
            var n2 = m.EndNode;
            int i = nodeIndexMap[n1.Id];
            int j = nodeIndexMap[n2.Id];

            double dx = n2.X - n1.X;
            double dy = n2.Y - n1.Y;
            double L = Math.Sqrt(dx * dx + dy * dy);
            double c = dx / L;
            double s = dy / L;

            // Deslocamentos nodais
            double u1 = u[2 * i];
            double v1 = u[2 * i + 1];
            double u2 = u[2 * j];
            double v2 = u[2 * j + 1];

            // Cálculo da deformação axial
            double deformation = (u2 - u1) * c + (v2 - v1) * s;

            // Força = Deformação * Rigidez (k=1.0 aqui)
            return deformation * 1.0;
        }
    }
}