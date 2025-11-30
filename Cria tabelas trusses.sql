-- 1. Cria o Banco de Dados
CREATE DATABASE Trusses;
GO

USE Trusses;
GO

-- 2. Tabela de Treliças (O projeto/simulação em si)
CREATE TABLE Trusses (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Nome NVARCHAR(MAX) NOT NULL
);
GO

-- 3. Tabela de Nós
-- Armazena a posição (X, Y) e os Resultados das Reações (ReactionX, ReactionY)
CREATE TABLE Nodes (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    X FLOAT NOT NULL,
    Y FLOAT NOT NULL,
    ReactionX FLOAT NOT NULL DEFAULT 0,
    ReactionY FLOAT NOT NULL DEFAULT 0,
    TrussId BIGINT NOT NULL,
    CONSTRAINT FK_Nodes_Trusses FOREIGN KEY (TrussId) REFERENCES Trusses(Id) ON DELETE CASCADE
);
GO

-- 4. Tabela de Suportes (Apoios)
-- Relação 1 para 1 com Nós (Um nó pode ter um suporte)
CREATE TABLE Supports (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    RestrainX BIT NOT NULL, -- 1 = Travado (Pino), 0 = Livre (Rolo se Y for 1)
    RestrainY BIT NOT NULL,
    NodeId BIGINT NOT NULL UNIQUE,
    CONSTRAINT FK_Supports_Nodes FOREIGN KEY (NodeId) REFERENCES Nodes(Id) ON DELETE CASCADE
);
GO

-- 5. Tabela de Cargas (Forças aplicadas)
-- Relação 1 para 1 com Nós (Simplificação do seu modelo atual)
CREATE TABLE Loads (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Magnitude FLOAT NOT NULL,
    Angle FLOAT NOT NULL,
    NodeId BIGINT NOT NULL UNIQUE,
    CONSTRAINT FK_Loads_Nodes FOREIGN KEY (NodeId) REFERENCES Nodes(Id) ON DELETE CASCADE
);
GO

-- 6. Tabela de Membros (Barras)
-- Liga dois nós e armazena o Resultado da Força Interna (Force)
CREATE TABLE Members (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Force] FLOAT NULL, -- Pode ser NULL se ainda não foi calculado
    StartNodeId BIGINT NOT NULL,
    EndNodeId BIGINT NOT NULL,
    TrussId BIGINT NOT NULL,
    
    -- Foreign Keys
    CONSTRAINT FK_Members_Trusses FOREIGN KEY (TrussId) REFERENCES Trusses(Id) ON DELETE CASCADE,
    -- Nota: Usamos NO ACTION nas FKs de nós para evitar o erro de "múltiplos caminhos em cascata" do SQL Server
    CONSTRAINT FK_Members_Nodes_Start FOREIGN KEY (StartNodeId) REFERENCES Nodes(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Members_Nodes_End FOREIGN KEY (EndNodeId) REFERENCES Nodes(Id) ON DELETE NO ACTION
);
GO