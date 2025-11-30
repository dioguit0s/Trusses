using Microsoft.EntityFrameworkCore;
using Trusses.Core.Data;
using Trusses.Core.Logic;
using Trusses.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing; // GDI+
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Trusses.App
{
    public partial class MainForm : Form
    {
        // Estado da Aplicação
        private List<Node> nodes = new List<Node>();
        private List<Member> members = new List<Member>();
        private string mode = "node";

        // Variáveis de Interação
        private Node interactionStartNode = null;
        private Node selectedNode = null;
        private PointF currentDragPos;
        private bool isDragging = false;

        // Configuração Gráfica
        private int gridCols = 24;
        private int gridRows = 20;
        private float gridSpacing = 30.0f;
        private float offsetX = 40.0f;
        private float offsetY = 40.0f;

        // Cores
        private readonly Pen penGrid = new Pen(Color.DarkGray, 1);
        private readonly Pen penMemberDefault = new Pen(Color.Gray, 3);
        private readonly Pen penMemberTension = new Pen(Color.Blue, 3);
        private readonly Pen penMemberCompression = new Pen(Color.Red, 3);
        private readonly Pen penMemberZero = new Pen(Color.Thistle, 3);
        private readonly Pen penReaction = new Pen(Color.DarkBlue, 2);
        private readonly Pen penLoad = new Pen(Color.DarkRed, 2);
        private readonly Pen penSupport = new Pen(Color.Green, 2);
        private readonly Font fontLabels = new Font("Arial", 8);
        private readonly Font fontForce = new Font("Arial", 9, FontStyle.Bold);

        // Controles de UI
        private Panel canvasPanel;
        private ToolStripStatusLabel statusLabel;
        private ToolStripStatusLabel coordLabel;

        //controles historico
        private ListBox historyList;
        private GroupBox historyGroup;

        public MainForm()
        {

            // Configuração manual da Janela
            this.Text = "Trusses";
            this.Size = new Size(1100, 750);

            // Garante que o banco existe ao iniciar
            using (var db = new AppDbContext())
            {
                db.Database.EnsureCreated();
            }

            InitializeUI();
            RefreshHistory();
        }

        private void InitializeUI()
        {
            // 1. Toolbar Superior
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5),
                BackColor = SystemColors.ControlLight
            };

            toolbar.Controls.Add(CreateButton("Nós", () => SetMode("node", "Clique para criar nós")));
            toolbar.Controls.Add(CreateButton("Membros", () => SetMode("member", "Arraste entre nós para criar membros")));
            toolbar.Controls.Add(CreateButton("Suportes", () => SetMode("support", "Arraste em um nó (CIMA=Rolo, LADO=Pino)")));
            toolbar.Controls.Add(CreateButton("Cargas", () => SetMode("load", "Arraste a partir de um nó para definir carga")));

            var btnCompute = CreateButton("CALCULAR", CalculateTruss);
            btnCompute.BackColor = Color.LightGreen;
            toolbar.Controls.Add(btnCompute);

            var btnClear = CreateButton("Limpar", ClearAll);
            toolbar.Controls.Add(btnClear);

            var btnDelete = CreateButton("Borracha", () => SetMode("delete", "Clique em um item para apagá-lo"));
            btnDelete.BackColor = Color.LightCoral;
            toolbar.Controls.Add(btnDelete);

            var btnSave = CreateButton("Salvar DB", SaveProjectToDb);
            btnSave.BackColor = Color.LightBlue;
            toolbar.Controls.Add(btnSave);

            var btnLoad = CreateButton("Carregar DB", LoadProjectFromDb);
            btnLoad.BackColor = Color.LightBlue;
            toolbar.Controls.Add(btnLoad);

            this.Controls.Add(toolbar);

            // 2. Barra de Status Inferior
            var statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Modo: Adicionar Nós");
            coordLabel = new ToolStripStatusLabel("Pos: (0,0)") { Spring = true, TextAlign = ContentAlignment.MiddleRight };
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(coordLabel);
            this.Controls.Add(statusStrip);


            // 3. Painel de Histórico à Direita
            historyGroup = new GroupBox
            {
                Text = "Histórico (Últimos 10)",
                Dock = DockStyle.Right,
                Width = 200,
                Padding = new Padding(10)
            };

            historyList = new ListBox { Dock = DockStyle.Fill };
            historyList.DoubleClick += (s, e) =>
            {
                if (historyList.SelectedItem is TrussItem item)
                    LoadSimulation(item.Id);
            };

            var lblInfo = new Label { Text = "Duplo-clique para carregar", Dock = DockStyle.Bottom, Height = 20, ForeColor = Color.Gray };

            historyGroup.Controls.Add(historyList);
            historyGroup.Controls.Add(lblInfo);
            this.Controls.Add(historyGroup);

            // 4. Canvas de Desenho
            canvasPanel = new DoubleBufferedPanel();
            canvasPanel.Dock = DockStyle.Fill;
            canvasPanel.BackColor = Color.White;

            canvasPanel.MouseMove += OnCanvasMouseMove;
            canvasPanel.MouseDown += OnCanvasMouseDown;
            canvasPanel.MouseUp += OnCanvasMouseUp;
            canvasPanel.Paint += OnCanvasPaint;

            this.Controls.Add(canvasPanel);
            toolbar.BringToFront();
        }

        //logica de histórico de simulações salvas
        // Classe auxiliar para exibir no ListBox
        private class TrussItem
        {
            public long Id { get; set; }
            public string Nome { get; set; }
            public override string ToString() => $"[{Id}] {Nome}";
        }

        private void RefreshHistory()
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var recents = db.Trusses
                        .OrderByDescending(t => t.Id)
                        .Take(10)
                        .Select(t => new TrussItem { Id = t.Id, Nome = t.Nome })
                        .ToList();

                    historyList.Items.Clear();
                    foreach (var item in recents)
                    {
                        historyList.Items.Add(item);
                    }
                }
            }
            catch { /* Ignora erros de conexão ao iniciar */ }
        }

        //Logica de Salvamento e Carregamento do Banco de Dados
        private void SaveProjectToDb()
        {
            if (nodes.Count == 0) { MessageBox.Show("Nada para salvar!"); return; }

            string nome = Microsoft.VisualBasic.Interaction.InputBox("Nome da Simulação:", "Salvar", "Minha Treliça " + DateTime.Now.ToShortTimeString());
            if (string.IsNullOrWhiteSpace(nome)) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Cria uma nova Treliça
                    var truss = new Truss { Nome = nome };

                    // Mapeia os nós da tela (Memória) para novos nós do Banco
                    // Precisamos de um Dicionário para reconstruir os membros depois
                    var mapOldToNew = new Dictionary<Node, Node>();

                    foreach (var n in nodes)
                    {
                        var newNode = new Node
                        {
                            X = n.X,
                            Y = n.Y,
                            ReactionX = n.ReactionX, // Salva o resultado calculado
                            ReactionY = n.ReactionY
                        };

                        if (n.Support != null)
                        {
                            newNode.Support = new Support { RestrainX = n.Support.RestrainX, RestrainY = n.Support.RestrainY };
                        }

                        if (n.Load != null)
                        {
                            newNode.Load = new Load { Magnitude = n.Load.Magnitude, Angle = n.Load.Angle };
                        }

                        truss.Nodes.Add(newNode);
                        mapOldToNew[n] = newNode;
                    }

                    // Recria os membros usando os novos nós mapeados
                    foreach (var m in members)
                    {
                        if (mapOldToNew.ContainsKey(m.StartNode) && mapOldToNew.ContainsKey(m.EndNode))
                        {
                            var newMember = new Member
                            {
                                StartNode = mapOldToNew[m.StartNode],
                                EndNode = mapOldToNew[m.EndNode],
                                Force = m.Force // Salva o resultado calculado
                            };
                            truss.Members.Add(newMember);
                        }
                    }

                    db.Trusses.Add(truss);
                    db.SaveChanges();
                    MessageBox.Show($"Simulação '{nome}' salva com sucesso! ID: {truss.Id}");
                    RefreshHistory();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar no banco: " + ex.Message);
            }
        }

        //carrega a simulacao direto do historico
        private void LoadSimulation(long id)
        {
            try
            {
                using (var db = new AppDbContext())
                {
                    var truss = db.Trusses
                        .Include(t => t.Nodes).ThenInclude(n => n.Support)
                        .Include(t => t.Nodes).ThenInclude(n => n.Load)
                        .Include(t => t.Members)
                        .FirstOrDefault(t => t.Id == id);

                    if (truss == null) return;

                    this.nodes.Clear();
                    this.members.Clear();

                    var nodeMap = new Dictionary<long, Node>();
                    foreach (var dbNode in truss.Nodes)
                    {
                        this.nodes.Add(dbNode);
                        nodeMap[dbNode.Id] = dbNode;
                    }

                    foreach (var dbMember in truss.Members)
                    {
                        if (nodeMap.ContainsKey(dbMember.StartNodeId)) dbMember.StartNode = nodeMap[dbMember.StartNodeId];
                        if (nodeMap.ContainsKey(dbMember.EndNodeId)) dbMember.EndNode = nodeMap[dbMember.EndNodeId];
                        if (dbMember.StartNode != null && dbMember.EndNode != null) this.members.Add(dbMember);
                    }

                    canvasPanel.Invalidate();
                    statusLabel.Text = $"Carregado: {truss.Nome}";
                }
            }
            catch (Exception ex) { MessageBox.Show("Erro ao carregar: " + ex.Message); }
        }

        //abre uma simulação especifica pedindo o ID
        private void LoadProjectFromDb()
        {
            string idStr = Microsoft.VisualBasic.Interaction.InputBox("Digite o ID da Simulação para carregar:", "Carregar");
            if (!long.TryParse(idStr, out long id)) return;

            try
            {
                using (var db = new AppDbContext())
                {
                    // Carrega a Treliça com todos os relacionamentos (Eager Loading)
                    var truss = db.Trusses
                        .Include(t => t.Nodes).ThenInclude(n => n.Support)
                        .Include(t => t.Nodes).ThenInclude(n => n.Load)
                        .Include(t => t.Members) // Carrega membros, mas StartNode/EndNode vem como ID
                        .FirstOrDefault(t => t.Id == id);

                    if (truss == null)
                    {
                        MessageBox.Show("Simulação não encontrada!");
                        return;
                    }

                    // --- RECONSTRUÇÃO DO ESTADO NA TELA ---

                    // 1. Limpa a tela atual
                    this.nodes.Clear();
                    this.members.Clear();

                    // 2. Carrega os Nós
                    // Importante: Membros no banco guardam IDs. Precisamos mapear ID -> Objeto Node carregado
                    var nodeMap = new Dictionary<long, Node>();

                    foreach (var dbNode in truss.Nodes)
                    {
                        // O EF Core já preencheu dbNode.Support e dbNode.Load graças aos Includes
                        // Como estamos usando as mesmas classes de modelo para a UI, podemos usar os objetos diretamente
                        // ou cloná-los se quisermos desconectar do contexto. Usar direto é ok aqui.
                        this.nodes.Add(dbNode);
                        nodeMap[dbNode.Id] = dbNode;
                    }

                    // 3. Carrega os Membros e reconecta as referências de objeto
                    foreach (var dbMember in truss.Members)
                    {
                        // O EF Core pode não ter preenchido as propriedades de navegação StartNode/EndNode automaticamente
                        // se não incluirmos explicitamente, mas temos os IDs.
                        if (nodeMap.ContainsKey(dbMember.StartNodeId))
                            dbMember.StartNode = nodeMap[dbMember.StartNodeId];

                        if (nodeMap.ContainsKey(dbMember.EndNodeId))
                            dbMember.EndNode = nodeMap[dbMember.EndNodeId];

                        if (dbMember.StartNode != null && dbMember.EndNode != null)
                        {
                            this.members.Add(dbMember);
                        }
                    }

                    canvasPanel.Invalidate();
                    statusLabel.Text = $"Carregado: {truss.Nome}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar: " + ex.Message + "\n" + ex.InnerException?.Message);
            }
        }


        // Métodos auxiliares necessários para o código acima compilar:
        private Button CreateButton(string text, Action onClick)
        {
            var btn = new Button { Text = text, AutoSize = true };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private void SetMode(string newMode, string message)
        {
            this.mode = newMode;
            statusLabel.Text = message;
            interactionStartNode = null;
        }

        // --- Lógica de Desenho (Paint) ---

        private void OnCanvasPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawGrid(g);
            DrawMembers(g);
            DrawNodesAndDetails(g);
            DrawInteractionLine(g);
        }

        private void DrawGrid(Graphics g)
        {
            float width = gridCols * gridSpacing;
            float height = gridRows * gridSpacing;

            for (int i = 0; i <= gridCols; i++)
            {
                float x = offsetX + i * gridSpacing;
                g.DrawLine(penGrid, x, offsetY, x, offsetY + height);
            }
            for (int j = 0; j <= gridRows; j++)
            {
                float y = offsetY + j * gridSpacing;
                g.DrawLine(penGrid, offsetX, y, offsetX + width, y);
            }
        }

        private void DrawMembers(Graphics g)
        {
            foreach (var m in members)
            {
                Pen p = penMemberDefault;
                string text = "";

                if (m.Force.HasValue)
                {
                    double f = m.Force.Value;
                    if (Math.Abs(f) < 0.001) { p = penMemberZero; text = "0.0"; }
                    else if (f > 0) { p = penMemberTension; text = $"{f:F1} (T)"; }
                    else { p = penMemberCompression; text = $"{Math.Abs(f):F1} (C)"; }
                }

                g.DrawLine(p, (float)m.StartNode.X, (float)m.StartNode.Y, (float)m.EndNode.X, (float)m.EndNode.Y);

                if (!string.IsNullOrEmpty(text))
                {
                    float mx = (float)(m.StartNode.X + m.EndNode.X) / 2;
                    float my = (float)(m.StartNode.Y + m.EndNode.Y) / 2;
                    g.FillRectangle(Brushes.White, mx - 15, my - 8, 40, 16);
                    g.DrawString(text, fontLabels, Brushes.Black, mx - 15, my - 8);
                }
            }
        }

        private void DrawNodesAndDetails(Graphics g)
        {
            int index = 0;
            foreach (var n in nodes)
            {
                float x = (float)n.X;
                float y = (float)n.Y;

                // Suportes
                if (n.Support != null)
                {
                    if (n.Support.RestrainX) // Pino (Triângulo)
                    {
                        g.DrawPolygon(penSupport, new PointF[] {
                            new PointF(x, y), new PointF(x-8, y+15), new PointF(x+8, y+15)
                        });
                        g.DrawLine(penSupport, x - 12, y + 18, x + 12, y + 18);
                    }
                    else // Rolo (Círculo)
                    {
                        g.DrawEllipse(penSupport, x - 8, y + 2, 16, 16);
                        g.DrawLine(penSupport, x - 12, y + 20, x + 12, y + 20);
                    }
                }

                // Cargas
                if (n.Load != null)
                {
                    DrawArrow(g, penLoad, x, y, n.Load.Angle, true);
                    g.DrawString($"{n.Load.Magnitude:F1}", fontForce, Brushes.Black, x, y - 40);
                }

                // Reações
                if (Math.Abs(n.ReactionY) > 0.01)
                {
                    float angle = (n.ReactionY > 0) ? 270 : 90; // 270 aponta pra cima
                    DrawArrow(g, penReaction, x, y, angle, true);
                    float txtY = (n.ReactionY > 0) ? y + 35 : y - 35;
                    g.DrawString($"Ry {Math.Abs(n.ReactionY):F1}", fontLabels, Brushes.DarkBlue, x, txtY);
                }
                if (Math.Abs(n.ReactionX) > 0.01)
                {
                    float angle = (n.ReactionX > 0) ? 0 : 180;
                    DrawArrow(g, penReaction, x, y, angle, true);
                    g.DrawString($"Rx {Math.Abs(n.ReactionX):F1}", fontLabels, Brushes.DarkBlue, x, y + 20);
                }

                // O Nó em si
                g.FillEllipse(Brushes.Black, x - 4, y - 4, 8, 8);
                string label = ((char)('A' + index++)).ToString();
                g.DrawString(label, fontLabels, Brushes.Red, x - 15, y - 15);
            }
        }

        private void DrawInteractionLine(Graphics g)
        {
            if (isDragging && interactionStartNode != null)
            {
                using (Pen dashPen = new Pen(Color.Gray))
                {
                    dashPen.DashStyle = DashStyle.Dash;
                    g.DrawLine(dashPen, (float)interactionStartNode.X, (float)interactionStartNode.Y, currentDragPos.X, currentDragPos.Y);
                }
            }
        }

        private void DrawArrow(Graphics g, Pen pen, float tx, float ty, double angleDeg, bool pointToNode)
        {
            double rad = angleDeg * (Math.PI / 180.0);
            float len = 30;

            // Calcula a cauda da seta
            float tailX = tx - len * (float)Math.Cos(rad);
            float tailY = ty - len * (float)Math.Sin(rad);

            if (pointToNode)
            {
                // Desenha linha da cauda até o nó
                g.DrawLine(pen, tailX, tailY, tx, ty);

                // Desenha ponta na extremidade tx, ty
                DrawArrowHead(g, pen, tx, ty, (float)Math.Atan2(tailY - ty, tailX - tx));
            }
        }

        private void DrawArrowHead(Graphics g, Pen pen, float tipX, float tipY, float angleRad)
        {
            float arrowSize = 7;
            float x1 = tipX + arrowSize * (float)Math.Cos(angleRad + 0.5);
            float y1 = tipY + arrowSize * (float)Math.Sin(angleRad + 0.5);
            float x2 = tipX + arrowSize * (float)Math.Cos(angleRad - 0.5);
            float y2 = tipY + arrowSize * (float)Math.Sin(angleRad - 0.5);

            g.DrawLine(pen, tipX, tipY, x1, y1);
            g.DrawLine(pen, tipX, tipY, x2, y2);
        }

        // --- Lógica de Mouse ---

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            int gx = (int)Math.Round((e.X - offsetX) / gridSpacing);
            int gy = (int)Math.Round((e.Y - offsetY) / gridSpacing);
            coordLabel.Text = $"Grid: ({gx}, {gy})";

            if (isDragging)
            {
                currentDragPos = e.Location;
                canvasPanel.Invalidate();
            }
        }

        private void OnCanvasMouseDown(object sender, MouseEventArgs e)
        {
            PointF cursor = e.Location;

            // --- LÓGICA DE APAGAR (Botão Esquerdo com Modo Borracha) ---
            if (e.Button == MouseButtons.Left && mode == "delete")
            {
                // Tenta apagar Nó
                var clickedNode = FindNodeNear(cursor);
                if (clickedNode != null)
                {
                    DeleteNode(clickedNode);
                    canvasPanel.Invalidate();
                    return;
                }

                // Tenta apagar Membro
                var clickedMember = FindMemberNear(cursor);
                if (clickedMember != null)
                {
                    members.Remove(clickedMember);
                    canvasPanel.Invalidate();
                    return;
                }
                return;
            }

            // --- LÓGICA DE MENU DE CONTEXTO (Botão Direito) ---
            if (e.Button == MouseButtons.Right)
            {
                ShowContextMenu(cursor);
                return;
            }

            // --- LÓGICA NORMAL (Criação) ---
            if (e.Button == MouseButtons.Left)
            {
                PointF snapped = Snap(e.X, e.Y);
                var clickedNode = FindNodeNear(snapped);

                if (mode == "node" && clickedNode == null && IsInsideGrid(snapped))
                {
                    var n = new Node { Id = DateTime.Now.Ticks, X = snapped.X, Y = snapped.Y };
                    nodes.Add(n);
                    canvasPanel.Invalidate();
                }
                else if (clickedNode != null)
                {
                    interactionStartNode = clickedNode;
                    selectedNode = clickedNode;
                    currentDragPos = e.Location;
                    isDragging = true;
                }
            }
        }

        private void OnCanvasMouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging && interactionStartNode != null)
            {
                PointF pos = e.Location;
                PointF snapPos = Snap(pos.X, pos.Y);

                if (mode == "member")
                {
                    var endNode = FindNodeNear(snapPos);
                    if (endNode != null && endNode != interactionStartNode)
                    {
                        // Evita duplicatas
                        if (!members.Any(m => (m.StartNode == interactionStartNode && m.EndNode == endNode) || (m.StartNode == endNode && m.EndNode == interactionStartNode)))
                        {
                            members.Add(new Member { StartNode = interactionStartNode, EndNode = endNode });
                        }
                    }
                }
                else if (mode == "support") { ApplySupportGesture(interactionStartNode, pos); }
                else if (mode == "load") { ApplyLoad(interactionStartNode, pos); }
            }

            isDragging = false;
            interactionStartNode = null;
            canvasPanel.Invalidate();
        }

        // --- NOVAS FUNÇÕES DE SUPORTE PARA DELEÇÃO ---

        private void ShowContextMenu(PointF cursor)
        {
            var clickedNode = FindNodeNear(cursor);
            var clickedMember = FindMemberNear(cursor);

            ContextMenuStrip menu = new ContextMenuStrip();

            if (clickedNode != null)
            {
                // Opções para Nó
                var itemDelNode = menu.Items.Add("Excluir Nó");
                itemDelNode.Click += (s, ev) => { DeleteNode(clickedNode); canvasPanel.Invalidate(); };

                if (clickedNode.Load != null)
                {
                    var itemDelLoad = menu.Items.Add("Remover Carga");
                    itemDelLoad.Click += (s, ev) => { clickedNode.Load = null; canvasPanel.Invalidate(); };
                }

                if (clickedNode.Support != null)
                {
                    var itemDelSupport = menu.Items.Add("Remover Suporte");
                    itemDelSupport.Click += (s, ev) => { clickedNode.Support = null; canvasPanel.Invalidate(); };
                }
            }
            else if (clickedMember != null)
            {
                // Opções para Membro
                var itemDelMember = menu.Items.Add("Excluir Membro");
                itemDelMember.Click += (s, ev) => { members.Remove(clickedMember); canvasPanel.Invalidate(); };
            }

            if (menu.Items.Count > 0)
            {
                menu.Show(canvasPanel, Point.Round(cursor));
            }
        }

        private void DeleteNode(Node n)
        {
            // Remove o nó e todos os membros conectados a ele
            members.RemoveAll(m => m.StartNode == n || m.EndNode == n);
            nodes.Remove(n);
        }

        private Member FindMemberNear(PointF p)
        {
            // Distância de ponto a segmento de reta
            foreach (var m in members)
            {
                double dist = PointToSegmentDistance(p.X, p.Y, m.StartNode.X, m.StartNode.Y, m.EndNode.X, m.EndNode.Y);
                if (dist < 5.0) return m;
            }
            return null;
        }

        private double PointToSegmentDistance(double px, double py, double x1, double y1, double x2, double y2)
        {
            double A = px - x1; double B = py - y1;
            double C = x2 - x1; double D = y2 - y1;

            double dot = A * C + B * D;
            double len_sq = C * C + D * D;
            double param = -1;
            if (len_sq != 0) param = dot / len_sq;

            double xx, yy;

            if (param < 0) { xx = x1; yy = y1; }
            else if (param > 1) { xx = x2; yy = y2; }
            else { xx = x1 + param * C; yy = y1 + param * D; }

            double dx = px - xx; double dy = py - yy;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // --- Lógica de Negócio Auxiliar ---

        private void ApplySupportGesture(Node n, PointF mouseUpPos)
        {
            double dy = mouseUpPos.Y - n.Y;
            double dx = mouseUpPos.X - n.X;

            // Se arrastou pouco, ignora
            if (Math.Abs(dy) < 10 && Math.Abs(dx) < 10) return;

            var s = new Support { Node = n, RestrainY = true }; // Default Y travado

            // Se arrastou pra baixo/cima significativamente, é Rolo (só trava Y)
            // Se arrastou pro lado, é Pino (trava X e Y)
            if (Math.Abs(dx) > Math.Abs(dy)) // Movimento horizontal
            {
                s.RestrainX = true; // Pino
            }
            else
            {
                s.RestrainX = false; // Rolo
            }
            n.Support = s;
        }

        private void ApplyLoad(Node n, PointF mousePos)
        {
            double dx = mousePos.X - n.X;
            double dy = mousePos.Y - n.Y;
            if (Math.Sqrt(dx * dx + dy * dy) < 10) return;

            double angle = Math.Atan2(dy, dx) * (180.0 / Math.PI);

            // Input simples via DialogBox (em C# puro é chato, vamos usar InputBox do VB ou um Form customizado rápido)
            string val = Microsoft.VisualBasic.Interaction.InputBox("Magnitude da Carga:", "Definir Carga", "50");
            if (double.TryParse(val, out double mag))
            {
                n.Load = new Load { Magnitude = mag, Angle = angle, Node = n };
            }
        }

        private void CalculateTruss()
        {
            if (nodes.Count == 0 || members.Count == 0) return;
            var truss = new Truss { Nodes = nodes, Members = members };
            try
            {
                TrussSolver.Resolver(truss);
                statusLabel.Text = "Cálculo Concluído.";
                canvasPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro: " + ex.Message);
            }
        }

        private void ClearAll() { 
            nodes.Clear(); 
            members.Clear(); 
            interactionStartNode = null; 
            canvasPanel.Invalidate(); 
        }

        // --- Utilitários ---

        private PointF Snap(float x, float y)
        {
            float sx = (float)Math.Round((x - offsetX) / gridSpacing) * gridSpacing + offsetX;
            float sy = (float)Math.Round((y - offsetY) / gridSpacing) * gridSpacing + offsetY;
            return new PointF(sx, sy);
        }

        private Node FindNodeNear(PointF p)
        {
            return nodes.FirstOrDefault(n => Math.Sqrt(Math.Pow(n.X - p.X, 2) + Math.Pow(n.Y - p.Y, 2)) < 10);
        }

        private bool IsInsideGrid(PointF p)
        {
            return p.X >= offsetX && p.X <= offsetX + gridCols * gridSpacing &&
                   p.Y >= offsetY && p.Y <= offsetY + gridRows * gridSpacing;
        }

        // Classe interna para evitar flicker (Double Buffer)
        public class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
                this.UpdateStyles();
            }
        }
    }
}
