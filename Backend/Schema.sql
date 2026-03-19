-- ============================================
-- AxiomCode - PostgreSQL
-- ============================================

CREATE DATABASE axiomcode;

-- conectar no banco
\c axiomcode;

CREATE TABLE usuarios (
    id SERIAL PRIMARY KEY,
    nome_usuario VARCHAR(100) NOT NULL UNIQUE,
    senha_hash VARCHAR(255) NOT NULL,
    email VARCHAR(255),
    eh_admin BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE eventos (
    id SERIAL PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    data_evento DATE NOT NULL,
    horario TIME NOT NULL,
    endereco VARCHAR(500) NOT NULL,
    descricao TEXT,
    foto_url VARCHAR(500),
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE artigos_blog (
    id SERIAL PRIMARY KEY,
    titulo VARCHAR(200) NOT NULL,
    data_publicacao DATE NOT NULL,
    horario TIME NOT NULL,
    descricao TEXT,
    foto_url VARCHAR(500),
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE contatos (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(200) NOT NULL,
    telefone VARCHAR(50) NOT NULL,
    email VARCHAR(255) NOT NULL,
    assunto VARCHAR(200),
    mensagem TEXT NOT NULL,
    enviado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE inscricoes_eventos (
    id SERIAL PRIMARY KEY,
    evento_id INT NOT NULL,
    usuario_id INT NOT NULL,
    inscrito_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_evento_usuario UNIQUE (evento_id, usuario_id),
    FOREIGN KEY (evento_id) REFERENCES eventos(id) ON DELETE CASCADE,
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE
);

CREATE INDEX idx_eventos_data ON eventos(data_evento);
CREATE INDEX idx_artigos_blog_data ON artigos_blog(data_publicacao);
CREATE INDEX idx_inscricoes_evento ON inscricoes_eventos(evento_id);