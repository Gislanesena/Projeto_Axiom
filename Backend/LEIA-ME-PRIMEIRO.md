# Guia simples – MySQL e criar o admin

Você vai fazer **só 3 coisas**: (1) rodar um arquivo no MySQL, (2) rodar o programa no computador, (3) criar o admin no navegador. **Não precisa criar o banco na mão** nem adicionar usuário admin pelo MySQL.

---

## Onde está cada coisa

| O que | Onde está no seu PC |
|-------|----------------------|
| Arquivo do banco (tabelas) | `C:\Users\gisla\OneDrive\Documentos\TreinamentosSite\Backend\Schema.sql` |
| Programa (Backend) | Pasta: `C:\Users\gisla\OneDrive\Documentos\TreinamentosSite\Backend` |

No **Explorador de Arquivos** do Windows:
1. Abra **Documentos** → **OneDrive** → **TreinamentosSite**.
2. Entre na pasta **Backend**.
3. O arquivo que você vai abrir no MySQL é o **Schema.sql** (ícone de folha/script).

---

## PARTE 1 – Rodar o Schema no MySQL (só uma vez)

Isso **cria o banco** `axiomcode` e **todas as tabelas**. Se o banco já existir, não quebra nada.

### Se você usa **MySQL Workbench**

1. Abra o **MySQL Workbench** e conecte no seu servidor (clique na conexão que você usa, geralmente “Local” ou “localhost”).
2. No menu: **File** → **Open SQL Script** (ou **Abrir script SQL**).
3. Navegue até:
   ```
   C:\Users\gisla\OneDrive\Documentos\TreinamentosSite\Backend
   ```
4. Selecione o arquivo **Schema.sql** e clique em **Abrir**.
5. O script vai aparecer na tela. Clique no **ícone do raio** (Execute) ou use **Ctrl+Shift+Enter**.
6. Espere rodar. No final deve aparecer algo como “X rows affected” ou mensagem de sucesso. Pronto.

### Se você usa **HeidiSQL** ou outro programa

1. Conecte no MySQL.
2. Procure a opção de **abrir arquivo SQL** ou **executar script**.
3. Abra o arquivo:
   ```
   C:\Users\gisla\OneDrive\Documentos\TreinamentosSite\Backend\Schema.sql
   ```
4. Execute o script todo. Pronto.

Depois disso você **não precisa criar o banco de novo** no app do MySQL. O próprio script já cria o banco `axiomcode` e as tabelas.

---

## PARTE 2 – Rodar o programa (Backend)

1. Abra o **PowerShell** ou o **Prompt de Comando** (CMD).
2. Digite e aperte Enter (uma linha por vez):
   ```bash
   cd C:\Users\gisla\OneDrive\Documentos\TreinamentosSite\Backend
   dotnet run
   ```
3. Espere até aparecer uma caixa com as linhas:
   ```text
   ========================================
   AxiomCode - Servidor iniciado!
   ========================================
   Abra o navegador e acesse:
     http://localhost:5000
   ...
   ```
4. **Deixe essa janela aberta** (se fechar, o site para).

**Se não aparecer nenhuma mensagem com "localhost":**
- Tente abrir o navegador mesmo assim e digite: **http://localhost:5000**
- Se a janela do PowerShell/CMD fechar sozinha logo após rodar `dotnet run`, provavelmente deu erro. Rode de novo e **leia o que aparecer em vermelho** (a mensagem de erro). Anote e peça ajuda com essa mensagem.
- Confira se você está na pasta certa: `C:\Users\gisla\OneDrive\Documentos\TreinamentosSite\Backend` antes de rodar `dotnet run`.

Se der erro de “senha” ou “conexão”, veja a seção **“Se der erro de conexão”** no final.

---

## PARTE 3 – Criar o admin (pelo navegador)

O admin **não se cria no MySQL**. Você cria **no site**, numa página especial.

1. Abra o **navegador** (Chrome, Edge, etc.).
2. Na barra de endereço digite **exatamente**:
   ```text
   http://localhost:5000/setup
   ```
   (Se o programa mostrou outra porta, troque o 5000 por ela.)
3. Vai abrir uma página **“Criar primeiro administrador”**.
4. Preencha:
   - **Nome de usuário:** o que você quiser para logar (ex.: `admin`).
   - **Senha:** a senha que você quiser para o admin.
5. Clique em **“Criar admin”**.
6. Você será levado para a tela de **Login**. Use o **mesmo nome** e **mesma senha** que acabou de criar.
7. Depois do login, você cai na **área do administrador** (/admin).

Só isso. Esse usuário **já é o admin**; não precisa adicionar nada no MySQL.

---

## Resumindo

| Passo | O que fazer |
|-------|-------------|
| 1 | No app do MySQL: abrir e **executar** o arquivo `Backend\Schema.sql` (cria o banco e tabelas). |
| 2 | No PC: abrir terminal, ir na pasta `Backend` e rodar `dotnet run`. |
| 3 | No navegador: abrir `http://localhost:5000/setup` e **criar o usuário e senha do admin**. |
| 4 | Fazer **login** com esse usuário e senha → você está como admin. |

Você **não** precisa:
- Criar o banco de novo no app do MySQL (o Schema.sql já faz isso).
- Adicionar o usuário admin no MySQL (você adiciona na página /setup do site).

---

## Se der erro de conexão com o MySQL

Se ao rodar `dotnet run` aparecer erro de “cannot connect” ou “access denied”:

1. Lembre da **senha** que você usa para entrar no MySQL (no Workbench ou outro app).
2. No PowerShell, **antes** de rodar `dotnet run`, digite (trocando `SUA_SENHA` pela senha real):
   ```powershell
   $env:AXIOM_DB = "Server=localhost;Database=axiomcode;User=root;Password=SUA_SENHA"
   ```
3. Depois rode de novo:
   ```powershell
   dotnet run
   ```

Se ainda der erro, diga qual app do MySQL você usa (Workbench, XAMPP, etc.) e qual mensagem aparece, que a gente ajusta o próximo passo.
