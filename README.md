Trusses - Análise de Treliças (C# Port)
Aplicativo desktop para análise estrutural que permite desenhar treliças, aplicar cargas e suportes, e calcular automaticamente os esforços internos (tração/compressão) e reações de apoio utilizando o Método da Rigidez Direta.

🚀 Funcionalidades
Modelagem Gráfica Interativa:

Nós: Criação livre ou alinhada à grade (grid de 50px).

Membros: Conexão intuitiva entre nós.

Suportes: Inserção rápida de Pinos (restrição X/Y) e Rolos (restrição Y).

Cargas: Aplicação de vetores de força com magnitude e ângulo personalizados.

Motor de Cálculo:

Resolução de sistemas estáticos determinados e indeterminados.

Visualização Colorida: Membros em Azul (Tração) e Vermelho (Compressão).

Cálculo imediato de reações de apoio e forças axiais.

Persistência e Histórico:

Salva e carrega simulações completas via SQL Server.

Painel lateral com histórico rápido das últimas 10 simulações.

Ferramentas de Edição:

Borracha para exclusão rápida.

Menu de contexto (botão direito) para remoção cirúrgica de cargas ou suportes.

🛠️ Tecnologias Utilizadas
Linguagem: C# (.NET 8.0)

Frontend: Windows Forms (Renderização via GDI+).

Banco de Dados: Microsoft SQL Server.

ORM: Entity Framework Core.

Matemática: MathNet.Numerics para álgebra linear e resolução matricial.

📂 Estrutura do Projeto
Plaintext

📁 Trusses
├── 📂 Trusses.App       # Interface Gráfica e ponto de entrada (Windows Forms)
├── 📂 Trusses.Core      # Regras de Negócio, Modelos (Entities) e Lógica do Solver
├── 📄 Cria tabelas trusses.sql  # Script SQL para criação manual do banco
└── 📄 Trusses.sln       # Solução do Visual Studio
⚙️ Configuração e Execução
1. Pré-requisitos
Visual Studio 2022 (com suporte para desenvolvimento Desktop .NET).

SQL Server.

2. Configurar o Banco de Dados
O projeto necessita de uma instância SQL Server rodando.

Opção A (Manual - Recomendada):

Abra seu gerenciador de banco de dados (SSMS, Azure Data Studio).

Execute o script Cria tabelas trusses.sql localizado na raiz do repositório.

Isso criará o banco Trusses e as tabelas necessárias (Nodes, Members, etc.).

Opção B (Automática via EF Core): O Entity Framework tentará criar o banco automaticamente na inicialização se ele não existir, contanto que a string de conexão seja válida e o usuário tenha permissão de CREATE DATABASE.

3. Configurar Conexão (Importante)
Antes de rodar, você deve apontar o projeto para o seu banco de dados local.

Abra o arquivo Trusses.Core/Data/AppDbContext.cs.

Localize o método OnConfiguring.

Altere a string de conexão para corresponder ao seu ambiente (ex: altere Server, User Id e Password).

C#

// Localização: Trusses.Core/Data/AppDbContext.cs
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    // ⚠️ ATENÇÃO: Altere abaixo conforme seu SQL Server local
    options.UseSqlServer("Server=LOCALHOST;Database=Trusses;User Id=sa;Password=SUA_SENHA_AQUI;TrustServerCertificate=True;");
}

4. Compilar e Rodar
Clone o repositório:

Bash

git clone https://github.com/dioguit0s/trusses.git
Abra o arquivo Trusses.sln no Visual Studio.

Defina o projeto Trusses.App como Startup Project (botão direito -> Set as Startup Project).

Pressione F5 para iniciar.

📖 Guia Rápido de Uso
Desenhar: Selecione "Nó" ou "Membro" na barra superior para desenhar a treliça.

Definir: Adicione suportes (arraste sobre um nó, na horizontal para pino e na vertinal para rolo) e cargas (clique e arraste a partir de um nó para definir a direção da força).

Calcular: Clique no botão verde CALCULAR para visualizar as forças coloridas e os valores numéricos.

Salvar: Utilize o botão "Salvar" para persistir o projeto no banco SQL.

🤝 Contribuição
Sinta-se à vontade para enviar Pull Requests ou abrir Issues. Melhorias no algoritmo do solver ou na interface UI/UX são bem-vindas.

📄 Licença
Este projeto é de cunho educacional. Baseado na funcionalidade do software MDSolids por Timothy A. Philpot.
