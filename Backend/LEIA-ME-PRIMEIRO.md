# Guia simples – PostgreSQL (Docker) e criar o admin

Você vai fazer **só 3 coisas**: (1) subir o banco PostgreSQL e rodar o **Schema.sql**, (2) rodar o programa no computador, (3) criar o admin no navegador. **Não precisa** criar usuário de banco na mão se usar o Docker Compose do projeto — usuário **`admin`** e senha **`123456789`** já estão definidos.

---

## Credenciais padrão do PostgreSQL (este projeto)

| Campo    | Valor        |
|----------|--------------|
| Usuário  | `admin`      |
| Senha    | `123456789`  |
| Banco    | `axiomcode`  |
| Porta    | `5432`       |
| Host     | `localhost`  |

A connection string do backend é a mesma ideia (veja **README.md** e variável **AXIOM_DB**).

---

## PARTE 1 – Subir o PostgreSQL com Docker (só uma vez)

1. Instale o [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows).
2. Abra um terminal na pasta do projeto (onde está o arquivo **`docker-compose.yml`**).
3. Execute:
   ```bash
   docker compose up -d
   ```
4. Crie as tabelas executando o script **`Backend\Schema.sql`** no banco **`axiomcode`**:
   - **Com `psql` instalado** (Cliente PostgreSQL):
     ```bash
     psql "postgresql://admin:123456789@localhost:5432/axiomcode" -f Backend/Schema.sql
     ```
   - **Com DBeaver / pgAdmin / outra ferramenta:** conecte com usuário `admin`, senha `123456789`, banco `axiomcode`, e execute o arquivo `Schema.sql`.

> Sem Docker: instale PostgreSQL, crie o banco `axiomcode` e um usuário (pode ser `admin` / `123456789`), depois rode o mesmo `Schema.sql`.

---

## PARTE 2 – Rodar o backend

1. Abra o PowerShell ou CMD e vá até a pasta **Backend**:
   ```bash
   cd caminho\para\DesenvolvimentoWeb\Backend
   dotnet run
   ```
2. Quando o servidor estiver em **http://localhost:5000**, siga para a Parte 3.

Se der erro de conexão com o banco, confira se o container está rodando (`docker compose ps`) e se a porta **5432** está livre.

---

## PARTE 3 – Criar o usuário administrador do site

O admin **não** se cria dentro do PostgreSQL com SQL manual para o dia a dia. Você usa uma página do site:

1. No navegador, acesse: **http://localhost:5000/setup**
2. Informe **nome de usuário** e **senha** e confirme para criar o **primeiro** administrador.

Só isso. Esse usuário **já é o admin** do sistema (campo `eh_admin` no PostgreSQL).

---

## Resumo rápido

| Passo | O que fazer |
|-------|-------------|
| 1 | `docker compose up -d` e executar `Backend/Schema.sql` no banco `axiomcode` |
| 2 | `dotnet run` na pasta **Backend** |
| 3 | Abrir **http://localhost:5000/setup** e criar o primeiro admin |

---

## Se der erro de conexão com o PostgreSQL

1. Confirme usuário **`admin`**, senha **`123456789`**, banco **`axiomcode`**, host **`localhost`**, porta **`5432`**.
2. Ajuste a variável **AXIOM_DB** se usar outra senha ou host, por exemplo:
   ```powershell
   $env:AXIOM_DB = "Host=localhost;Port=5432;Database=axiomcode;Username=admin;Password=123456789"
   ```
3. No Docker, veja os logs: `docker compose logs postgres`.
