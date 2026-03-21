-- Execute no banco axiomcode (PostgreSQL), após o Schema.sql base.
-- Recuperação de senha por código enviado por e-mail

CREATE TABLE IF NOT EXISTS recuperacao_senha (
    id SERIAL PRIMARY KEY,
    usuario_id INT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    codigo VARCHAR(8) NOT NULL,
    expira_em TIMESTAMP NOT NULL,
    usado BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_recuperacao_usuario ON recuperacao_senha(usuario_id);
CREATE INDEX IF NOT EXISTS idx_recuperacao_codigo ON recuperacao_senha(codigo);

-- Garantir e-mail único quando preenchido (para login / esqueci senha)
CREATE UNIQUE INDEX IF NOT EXISTS idx_usuarios_email_lower
    ON usuarios (LOWER(email))
    WHERE email IS NOT NULL AND TRIM(email) <> '';

-- Vincular e-mail ao primeiro usuário (normalmente o admin criado no /setup)
UPDATE usuarios SET email = 'gislane.sena1@aluno.unip.br'
WHERE id = (SELECT MIN(id) FROM usuarios);
