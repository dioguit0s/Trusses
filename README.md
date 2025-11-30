# Trusses - Análise de Treliças (C# Port)

![Status](https://img.shields.io/badge/Status-Em_Desenvolvimento-yellow)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![EF Core](https://img.shields.io/badge/EF_Core-9.0-blue)

O aplicativo permite desenhar estruturas, aplicar cargas e suportes, e calcular automaticamente os esforços internos (tração/compressão) e reações de apoio através do Método da Rigidez Direta.

## 🚀 Funcionalidades

* **Modelagem Gráfica**:
  * **Nós**: Criação livre ou alinhada à grade (grid de 50px).
  * **Membros**: Conexão intuitiva entre nós.
  * **Suportes**: Inserção de Pinos (restrição X/Y) e Rolos (restrição Y) através de gestos do mouse.
  * **Cargas**: Aplicação de vetores de força com magnitude e ângulo personalizados.
* **Motor de Cálculo**:
  * Resolução de sistemas estáticos determinados e indeterminados.
  * **Visualização Colorida**: Membros em **Azul** (Tração) e **Vermelho** (Compressão).
  * Exibição numérica das reações de apoio e forças internas.
* **Persistência e Histórico**:
  * **Banco de Dados**: Salva e carrega simulações completas via SQL Server.
  * **Histórico Rápido**: Painel lateral com acesso imediato às últimas 10 simulações trabalhadas.
* **Ferramentas de Edição**:
  * **Borracha**: Modo de exclusão rápida de nós e membros.
  * **Menu de Contexto**: Clique com o botão direito para remover itens específicos (cargas, suportes) sem apagar a geometria.

## 🛠️ Tecnologias

* **Linguagem**: C# (.NET 8.0)
* **Frontend**: Windows Forms (GDI+ para renderização).
* **Banco de Dados**: Microsoft SQL Server.
* **ORM**: Entity Framework Core 9.0.0
* **Matemática**: [MathNet.Numerics](https://numerics.mathdotnet.com/) (v5.0.0) para álgebra linear.

## 📂 Estrutura do Repositório

```
📁 Trusses
├── 📂 Trusses.App       # Interface Gráfica (Windows Forms)
├── 📂 Trusses.Core      # Regras de Negócio, Modelos e Solver
├── 📄 Cria tabelas trusses.sql  # Script para criação do Banco de Dados
└── 📄 Trusses.sln       # Solução do Visual Studio
```

⚙️ Configuração e Execução
1. Pré-requisitos
Visual Studio 2022 com carga de trabalho para Desktop .NET.

SQL Server (LocalDB, Express ou Docker).

2. Configurar o Banco de Dados
O projeto requer um banco de dados SQL Server. Você pode configurá-lo de duas formas:

Opção A (Manual - Recomendada):

Abra o seu gerenciador de banco de dados (SSMS, Azure Data Studio, etc).

Localize o arquivo Cria tabelas trusses.sql na raiz deste repositório.

Execute o script para criar o banco Trusses e todas as tabelas necessárias (Nodes, Members, Loads, etc).

Opção B (Automática): O Entity Framework Core está configurado para tentar criar o banco automaticamente na inicialização (db.Database.EnsureCreated()), caso ele não exista e as permissões do usuário permitam.

3. Ajustar a String de Conexão (Importante)
O código atual vem com uma senha padrão (123456) configurada. Você deve alterar isso para corresponder ao seu ambiente local.

Abra o arquivo Trusses.Core/Data/AppDbContext.cs e localize o método OnConfiguring:

C#

protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    // ⚠️ Configure sua string de conexão aqui.
    // Exemplo atual no código (altere User Id e Password conforme necessário):
    options.UseSqlServer("Server=LOCALHOST;Database=Trusses;User Id=sa;Password=123456;TrustServerCertificate=True;");
}
4. Compilar e Rodar
Clone o repositório:

Bash

git clone [https://github.com/dioguit0s/trusses.git](https://github.com/dioguit0s/trusses.git)
Abra o arquivo Trusses.sln no Visual Studio.

Defina o projeto Trusses.App como Startup Project (clique com o botão direito no projeto -> Set as Startup Project).

Pressione F5 para iniciar.

📖 Guia Rápido de Uso
Desenhar: Use as ferramentas da barra superior para criar a geometria da treliça.

Definir: Adicione suportes (arraste no nó) e cargas (arraste a partir do nó).

Calcular: Clique no botão verde CALCULAR para ver os resultados.

Salvar: Clique em "Salvar", dê um nome à simulação e ela aparecerá no histórico à direita.

🤝 Contribuição
Sinta-se à vontade para enviar Pull Requests ou abrir Issues para melhorar o solver, adicionar novos tipos de suporte ou otimizar a interface.

📄 Licença
Este projeto é de cunho educacional. Baseado no software MDSolids por Timothy A. Philpot.
