# AxiomCode - Backend (C# + PostgreSQL)

## Arquivos principais

- **Schema.sql** – Script das tabelas no **PostgreSQL** (`usuarios`, `eventos`, `artigos_blog`, `contatos`, `inscricoes_eventos`).
- **Program.cs** – Aplicação: rotas, login, contato, eventos, blog e admin.
- **docker-compose.yml** (na raiz do repositório) – Sobe o PostgreSQL em container com usuário **`admin`** e senha **`123456789`**.

O arquivo **AxiomCode.Backend.csproj** referencia o pacote **Npgsql** (driver PostgreSQL para .NET).

## Banco de dados (PostgreSQL)

### Opção A — Docker (recomendado)

Na pasta do repositório (onde está `docker-compose.yml`):

```bash
docker compose up -d
```

Aguarde o container ficar saudável e crie as tabelas:

```bash
# Windows PowerShell (com psql no PATH) ou use pgAdmin / DBeaver
psql "postgresql://admin:123456789@localhost:5432/axiomcode" -f Backend/Schema.sql
```

### Opção B — PostgreSQL instalado localmente

1. Crie o banco `axiomcode` e um usuário com senha (ou use `admin` / `123456789`).
2. Conecte ao banco `axiomcode` e execute o conteúdo de **Schema.sql**.

### Conexão padrão da aplicação

A string correta está em **`appsettings.json`** (`ConnectionStrings:Default`) e corresponde ao **docker-compose.yml**:

- Banco: **`axiomcode`**
- Usuário: **`admin`**
- Senha: **`123456789`**
- `SSL Mode=Disable` (evita erro de SSL em PostgreSQL local)

Se algo não conectar, abra **http://localhost:5000/api/health/db** — devolve JSON com `ok: true` ou a mensagem de erro.

Para outro servidor ou credenciais, defina a variável de ambiente **AXIOM_DB** (ela **substitui** o appsettings):

```powershell
$env:AXIOM_DB = "Host=localhost;Port=5432;Database=axiomcode;Username=admin;Password=SUA_SENHA"
```

## Como rodar o backend

Na pasta **Backend**:

```bash
dotnet restore
dotnet run
```

Acesse **http://localhost:5000**. O site estático é servido da pasta configurada no projeto (ex.: **SiteUni** ou **SiteAxiom**).

**Primeiro administrador:** acesse **http://localhost:5000/setup** e crie o primeiro usuário (será administrador). Depois use **/login**.

## Formulário de contato

- Os dados são gravados na tabela **contatos** no PostgreSQL.

## Rotas (resumo)

| Rota | Descrição |
|------|-----------|
| /api/health/db | Testa conexão com PostgreSQL (JSON) |
| / | Redireciona conforme `Program.cs` |
| /contato | POST do formulário de contato |
| /login | Login |
| /logout | Sair |
| /setup | Criar primeiro admin (só se não existir usuário) |
| /admin | Área do administrador |
| /admin/eventos | Criar evento |
| /admin/blog | Criar artigo |
