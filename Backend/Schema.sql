-- ============================================
-- AxiomCode - PostgreSQL
-- ============================================
-- Com Docker (recomendado): o banco `axiomcode` já é criado pelo container.
--   Execute só as tabelas abaixo, conectado ao banco axiomcode:
--   psql "postgresql://admin:123456789@localhost:5432/axiomcode" -f Schema.sql
--
-- Sem Docker: crie o banco como superusuário (ex.: postgres) e depois rode este arquivo:
--   CREATE DATABASE axiomcode ENCODING 'UTF8';
--   \\c axiomcode
--   (cole o restante deste arquivo a partir de CREATE TABLE)
-- ============================================

-- Usuários: admin e usuários comuns (login com nome de usuário e senha)
CREATE TABLE IF NOT EXISTS usuarios (
    id              SERIAL PRIMARY KEY,
    nome_usuario    VARCHAR(100) NOT NULL UNIQUE,
    senha_hash      VARCHAR(255) NOT NULL,
    email           VARCHAR(255),
    eh_admin        BOOLEAN NOT NULL DEFAULT FALSE,
    criado_em       TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Eventos (criados pelo admin)
CREATE TABLE IF NOT EXISTS eventos (
    id          SERIAL PRIMARY KEY,
    titulo      VARCHAR(200) NOT NULL,
    data_evento DATE NOT NULL,
    horario     TIME NOT NULL,
    endereco    VARCHAR(500) NOT NULL DEFAULT '',
    descricao   TEXT,
    foto_url    VARCHAR(500),
    criado_em   TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Artigos do blog (criados pelo admin)
CREATE TABLE IF NOT EXISTS artigos_blog (
    id               SERIAL PRIMARY KEY,
    titulo           VARCHAR(200) NOT NULL,
    data_publicacao  DATE NOT NULL,
    horario          TIME NOT NULL,
    descricao        TEXT,
    foto_url         VARCHAR(500),
    criado_em        TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Mensagens do formulário de contato
CREATE TABLE IF NOT EXISTS contatos (
    id          SERIAL PRIMARY KEY,
    nome        VARCHAR(200) NOT NULL,
    telefone    VARCHAR(50) NOT NULL,
    email       VARCHAR(255) NOT NULL,
    assunto     VARCHAR(200),
    mensagem    TEXT NOT NULL,
    enviado_em  TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Inscrições em eventos (qual usuário se inscreveu em qual evento)
CREATE TABLE IF NOT EXISTS inscricoes_eventos (
    id          SERIAL PRIMARY KEY,
    evento_id   INT NOT NULL REFERENCES eventos(id) ON DELETE CASCADE,
    usuario_id  INT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    inscrito_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (evento_id, usuario_id)
);

-- Índices para buscas
CREATE INDEX IF NOT EXISTS idx_eventos_data ON eventos(data_evento);
CREATE INDEX IF NOT EXISTS idx_artigos_blog_data ON artigos_blog(data_publicacao);
CREATE INDEX IF NOT EXISTS idx_inscricoes_evento ON inscricoes_eventos(evento_id);

-- Primeiro administrador: após rodar a aplicação, acesse /setup no navegador e crie usuário e senha.
