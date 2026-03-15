# AxiomCode - Backend (C# + MySQL)

## Arquivos principais

- **Schema.sql** – Script do banco MySQL (tabelas: usuarios, eventos, artigos_blog, contatos, inscricoes_eventos).
- **Program.cs** – Toda a aplicação em um arquivo: classes, rotas, login, contato, eventos, blog e admin.

O arquivo **AxiomCode.Backend.csproj** é necessário para o `dotnet run` funcionar.

## Como rodar

1. **MySQL**: tenha o MySQL instalado. Crie um banco (ex.: `axiomcode`) e execute o **Schema.sql** nele (MySQL Workbench, phpMyAdmin ou linha de comando).

2. **Conexão**: por padrão a app usa  
   `Server=localhost;Database=axiomcode;User=root;Password=;`  
   Se sua senha do MySQL for diferente, defina a variável de ambiente **AXIOM_DB**, por exemplo:
   ```powershell
   $env:AXIOM_DB = "Server=localhost;Database=axiomcode;User=root;Password=SUA_SENHA"
   ```

3. **Rodar a aplicação** (na pasta Backend):
   ```bash
   dotnet restore
   dotnet run
   ```

4. Acesse no navegador **http://localhost:5000**. O site (HTML/CSS) é servido da pasta **SiteUni**; a página de contato tem formulário integrado ao backend.

5. **Primeiro admin**: acesse **http://localhost:5000/setup** e crie o primeiro usuário (será administrador). Depois use **/login** para entrar.

## Formulário de contato

- A página **contato.html** (ou **http://localhost:5000/contato.html**) contém o formulário estilizado.
- Campos obrigatórios: Nome, Telefone, E-mail, Mensagem. Assunto é opcional.
- O envio é feito por POST para **/contato** e os dados são salvos na tabela **contatos** do MySQL.

## Rotas

| Rota | Descrição |
|------|-----------|
| / | Redireciona para /eventos |
| /contato.html | Página de contato do site (formulário) |
| /contato | POST recebe o formulário de contato |
| /login | Login (admin ou usuário) |
| /logout | Sair |
| /setup | Criar primeiro admin (só se não existir usuário) |
| /registro | Criar conta (usuário comum) |
| /eventos | Lista de eventos; usuário logado pode se inscrever |
| /blog | Lista de artigos (só leitura) |
| /admin | Área do administrador |
| /admin/eventos | Criar evento |
| /admin/blog | Criar artigo |
