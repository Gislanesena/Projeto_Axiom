# AxiomCode - Backend (C# + PostgreSQL)

## Arquivos principais

- **Schema.sql** – Script do banco PostgreSQL (tabelas: usuarios, eventos, artigos_blog, contatos, inscricoes_eventos).
- **Program.cs** – Aplicação com login, eventos, meus eventos, contato e admin.

O arquivo **AxiomCode.Backend.csproj** é necessário para o `dotnet run` funcionar.

## Como rodar

1. **PostgreSQL**: tenha o PostgreSQL instalado. Crie o banco `axiomcode` e execute o **Schema.sql** nele.  
   Em seguida execute **Migration-dashboard-auth.sql** (tabela `recuperacao_senha`, índice de e-mail e atualização do e-mail do primeiro usuário para testes).

2. **Conexão**: por padrão a app usa  
   `Host=localhost;Port=5432;Database=axiomcode;Username=postgres;Password=123456`  
   Se for diferente no seu ambiente, defina **AXIOM_DB**, por exemplo:
   ```powershell
   $env:AXIOM_DB = "Host=localhost;Port=5432;Database=axiomcode;Username=postgres;Password=SUA_SENHA"
   ```

3. **Rodar a aplicação** (na pasta Backend):
   ```bash
   dotnet restore
   dotnet run
   ```

4. Acesse no navegador **http://localhost:5000**. O site (HTML/CSS) é servido da pasta **SiteAxiom**; a página de contato envia para banco e e-mail.

5. **Primeiro admin**: acesse **http://localhost:5000/setup** e crie o primeiro usuário com **e-mail** (será administrador). Depois use o botão **Login** no site (modal) ou **/login**.

6. **Dashboard**: após login válido, você é levado a **http://localhost:5000/dashboard.html** (só abre se o cookie de sessão existir; use sempre a URL do servidor, não `file://`).

7. **Front + API**: o login e cadastro usam `fetch` para `/api/auth/*` com `credentials: same-origin`. O site precisa ser aberto em **http://localhost:5000/...** (mesma origem do backend).

## Formulário de contato

- A página **contato.html** (ou **http://localhost:5000/contato.html**) contém o formulário estilizado.
- Campos obrigatórios: Nome, Telefone, E-mail, Mensagem. Assunto é opcional.
- O envio é feito por POST para **/contato** e os dados são salvos na tabela **contatos**.
- Depois de salvar no banco, o backend tenta enviar e-mail para **gislane.sena1@aluno.unip.br**.

### Configuração SMTP (obrigatória para envio de e-mail)

Defina estas variáveis no PowerShell antes de rodar `dotnet run`:

```powershell
$env:AXIOM_SMTP_HOST = "smtp.office365.com"
$env:AXIOM_SMTP_PORT = "587"
$env:AXIOM_SMTP_USER = "SEU_EMAIL@dominio.com"
$env:AXIOM_SMTP_PASS = "SUA_SENHA_OU_APP_PASSWORD"
$env:AXIOM_SMTP_FROM = "SEU_EMAIL@dominio.com"
$env:AXIOM_SMTP_SSL  = "true"
```

Sem SMTP configurado, o contato **continua sendo gravado no banco**; o envio por e-mail falha silenciosamente para o usuário (veja o console do backend: linha `[SMTP]`).

## Rotas

| Rota | Descrição |
|------|-----------|
| / | Redireciona para /index.html |
| /contato.html | Página de contato do site (formulário) |
| /contato | POST recebe o formulário de contato |
| /login | Login (admin ou usuário) |
| /logout | Sair |
| /setup | Criar primeiro admin (só se não existir usuário) |
| /registro | Criar conta (usuário comum) |
| /eventos | Lista de eventos; usuário logado pode se inscrever |
| /eventos/inscrever-por-titulo | Inscreve no evento enviado pelo front |
| /meus-eventos | Lista os eventos inscritos do usuário logado |
| /dashboard.html | Dashboard estilizado (protegido por cookie) |
| /api/auth/login | POST JSON — login (define cookies) |
| /api/auth/me | JSON — sessão atual |
| /api/auth/registro | POST JSON — criar usuário |
| /api/auth/recuperar-senha | POST JSON — envia código por e-mail |
| /api/auth/redefinir-senha | POST JSON — nova senha com código |
| /api/admin/contatos | JSON — mensagens de contato (só admin) |
| /admin | Área do administrador |
| /admin/eventos | Criar evento |
| /admin/blog | Criar artigo |
