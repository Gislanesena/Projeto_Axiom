using Npgsql;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

// ============== CONFIGURAÇÃO ==============
var connectionString = Environment.GetEnvironmentVariable("AXIOM_DB")
    ?? "Host=localhost;Port=5432;Database=axiomcode;Username=postgres;Password=123456";
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddAntiforgery();
var app = builder.Build();

// Páginas restritas antes dos arquivos estáticos
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    var temUid = context.Request.Cookies.TryGetValue("uid", out var uid) && int.TryParse(uid, out var idOk) && idOk > 0;
    var isAdm = context.Request.Cookies.TryGetValue("adm", out var adm) && adm == "1";

    if (string.Equals(path, "/dashboard.html", StringComparison.OrdinalIgnoreCase))
    {
        if (!temUid)
        {
            context.Response.Redirect("/index.html?precisa_login=1");
            return;
        }
    }
    else if (string.Equals(path, "/admin-criar-evento.html", StringComparison.OrdinalIgnoreCase))
    {
        if (!temUid)
        {
            context.Response.Redirect("/index.html?precisa_login=1");
            return;
        }
        if (!isAdm)
        {
            context.Response.Redirect("/index.html?acesso_negado=1");
            return;
        }
    }
    else if (string.Equals(path, "/meus-eventos.html", StringComparison.OrdinalIgnoreCase))
    {
        if (!temUid)
        {
            context.Response.Redirect("/index.html?precisa_login=1");
            return;
        }
    }
    await next();
});

// Servir arquivos estáticos do site (HTML, CSS, JS, imagens).
// Primeiro tenta a pasta atual do front (SiteAxiom) e mantém SiteUni como fallback.
var sitePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SiteAxiom");
if (!Directory.Exists(sitePath))
{
    sitePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SiteUni");
}
if (Directory.Exists(sitePath))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(sitePath);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
}

// ============== HELPERS (senha e cookie) ==============
static string HashSenha(string senha)
{
    var bytes = Encoding.UTF8.GetBytes(senha);
    var hash = SHA256.HashData(bytes);
    return Convert.ToBase64String(hash);
}
static bool VerificarSenha(string senha, string hash) => HashSenha(senha) == hash;
static (int? userId, bool isAdmin) LerUsuarioCookie(HttpRequest req)
{
    if (!req.Cookies.TryGetValue("uid", out var uid) || !req.Cookies.TryGetValue("adm", out var adm))
        return (null, false);
    return (int.TryParse(uid, out var id) ? id : null, adm == "1");
}
/// <summary>Sem login → precisa_login; logado sem admin → acesso_negado.</summary>
static string? RedirectSePrecisaLoginOuNaoAdmin(int? userId, bool isAdmin)
{
    if (!userId.HasValue)
        return "/index.html?precisa_login=1";
    if (!isAdmin)
        return "/index.html?acesso_negado=1";
    return null;
}
static CookieOptions CriarCookieAuth() => new CookieOptions
{
    HttpOnly = true,
    SameSite = SameSiteMode.Lax,
    MaxAge = TimeSpan.FromHours(24),
    Path = "/"
};
static string Layout(string titulo, string body, bool isAdmin = false)
{
    var nav = @"<nav><a href=""/"">Home</a> | <a href=""/eventos"">Eventos</a> | <a href=""/meus-eventos"">Meus eventos</a> | <a href=""/contato"">Contato</a> | ";
    if (isAdmin) nav += @"<a href=""/admin"">Admin</a> | ";
    nav += @"<a href=""/login"">Login</a></nav>";
    return $@"<!DOCTYPE html><html lang=""pt-br""><head><meta charset=""utf-8""/><title>{titulo}</title></head><body>{nav}<hr/>{body}</body></html>";
}
static async Task EnviarEmailAsync(string destinatario, string assuntoEmail, string corpoTexto, string? replyTo = null)
{
    var smtpHost = Environment.GetEnvironmentVariable("AXIOM_SMTP_HOST");
    if (string.IsNullOrWhiteSpace(smtpHost))
        throw new InvalidOperationException("SMTP não configurado: defina AXIOM_SMTP_HOST.");

    var smtpPortStr = Environment.GetEnvironmentVariable("AXIOM_SMTP_PORT");
    var smtpUser = Environment.GetEnvironmentVariable("AXIOM_SMTP_USER");
    var smtpPass = Environment.GetEnvironmentVariable("AXIOM_SMTP_PASS");
    var smtpFrom = Environment.GetEnvironmentVariable("AXIOM_SMTP_FROM") ?? smtpUser;
    var smtpSslStr = Environment.GetEnvironmentVariable("AXIOM_SMTP_SSL") ?? "true";

    if (string.IsNullOrWhiteSpace(smtpFrom))
        throw new InvalidOperationException("Remetente SMTP inválido. Defina AXIOM_SMTP_FROM ou AXIOM_SMTP_USER.");

    var smtpPort = int.TryParse(smtpPortStr, out var parsedPort) ? parsedPort : 587;
    var enableSsl = !smtpSslStr.Equals("false", StringComparison.OrdinalIgnoreCase);

    using var message = new MailMessage(smtpFrom, destinatario)
    {
        Subject = assuntoEmail,
        Body = corpoTexto,
        IsBodyHtml = false
    };
    if (!string.IsNullOrWhiteSpace(replyTo))
        message.ReplyToList.Add(new MailAddress(replyTo));

    using var client = new SmtpClient(smtpHost, smtpPort)
    {
        EnableSsl = enableSsl,
        DeliveryMethod = SmtpDeliveryMethod.Network
    };
    if (!string.IsNullOrWhiteSpace(smtpUser))
        client.Credentials = new NetworkCredential(smtpUser, smtpPass ?? "");

    await client.SendMailAsync(message);
}

static Task EnviarEmailContatoAsync(string nome, string telefone, string email, string? assunto, string mensagem)
{
    var assuntoFinal = string.IsNullOrWhiteSpace(assunto) ? "Contato pelo site AxiomCode" : assunto;
    var corpo = $"""
Nome: {nome}
Telefone: {telefone}
Email: {email}

Mensagem:
{mensagem}
""";
    return EnviarEmailAsync("gislane.sena1@aluno.unip.br", $"[AxiomCode] {assuntoFinal}", corpo, replyTo: email);
}

static async Task EnviarEmailConfirmacaoInscricaoAsync(string? emailUsuario, string nomeUsuario, string titulo, DateOnly data, TimeOnly hora, string endereco)
{
    if (string.IsNullOrWhiteSpace(emailUsuario)) return;
    var corpo = $"""
Olá, {nomeUsuario}!

Sua inscrição foi registrada no evento:
{titulo}
Data: {data:dd/MM/yyyy} às {hora:hh\:mm}
Local: {(string.IsNullOrWhiteSpace(endereco) ? "(não informado)" : endereco)}

— AxiomCode
""";
    try
    {
        await EnviarEmailAsync(emailUsuario.Trim(), "[AxiomCode] Confirmação de inscrição no evento", corpo);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SMTP] Falha e-mail inscrição: {ex.Message}");
    }
}

// ============== ROTAS ==============

// Página inicial do front (mantém o layout/CSS principal no localhost)
app.MapGet("/", () => Results.Redirect("/index.html"));

// ----- Login (admin e usuários: usuário e senha)
app.MapGet("/login", () =>
{
    var html = Layout("Login", @"
        <h1>Login</h1>
        <form method=""post"" action=""/login"">
            <label>Usuário: <input name=""nome_usuario"" required /></label><br/>
            <label>Senha: <input name=""senha"" type=""password"" required /></label><br/>
            <button type=""submit"">Entrar</button>
        </form>
        <p><a href=""/registro"">Criar conta (usuário)</a></p>");
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapPost("/login", async (HttpContext ctx, HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var user = form["nome_usuario"].ToString();
    var senha = form["senha"].ToString();
    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(senha))
        return Results.Redirect("/login?erro=1");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT id, nome_usuario, senha_hash, eh_admin FROM usuarios WHERE nome_usuario = @u", conn);
    cmd.Parameters.AddWithValue("u", user);
    await using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync())
        return Results.Redirect("/login?erro=1");
    var id = r.GetInt32(0);
    var hash = r.GetString(2);
    var ehAdmin = r.GetBoolean(3);
    if (!VerificarSenha(senha, hash))
        return Results.Redirect("/login?erro=1");

    ctx.Response.Cookies.Append("uid", id.ToString(), CriarCookieAuth());
    ctx.Response.Cookies.Append("adm", ehAdmin ? "1" : "0", CriarCookieAuth());
    return Results.Redirect("/dashboard.html");
});

app.MapGet("/dashboard", (HttpRequest req) =>
{
    if (!LerUsuarioCookie(req).userId.HasValue)
        return Results.Redirect("/index.html?precisa_login=1");
    return Results.Redirect("/dashboard.html");
});

app.MapGet("/logout", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("uid", CriarCookieAuth());
    ctx.Response.Cookies.Delete("adm", CriarCookieAuth());
    return Results.Redirect("/");
});

// ----- Setup: criar primeiro usuário admin (só funciona se ainda não existir nenhum usuário)
app.MapGet("/setup", async () =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", conn);
    var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
    if (count > 0)
        return Results.Content(Layout("Setup", "<p>Já existem usuários. Use o login normal.</p><a href=\"/login\">Login</a>"), "text/html; charset=utf-8");
    var html = Layout("Criar primeiro admin", @"
        <h1>Criar primeiro administrador</h1>
        <form method=""post"" action=""/setup"">
            <label>Nome de usuário: <input name=""nome_usuario"" required /></label><br/>
            <label>E-mail: <input name=""email"" type=""email"" required placeholder=""gislane.sena1@aluno.unip.br"" /></label><br/>
            <label>Senha: <input name=""senha"" type=""password"" required /></label><br/>
            <button type=""submit"">Criar admin</button>
        </form>");
    return Results.Content(html, "text/html; charset=utf-8");
});

// Registrar novo usuário (não admin) – para se inscrever em eventos
app.MapGet("/registro", () =>
{
    var html = Layout("Criar conta", @"
        <h1>Criar conta</h1>
        <form method=""post"" action=""/registro"">
            <label>Nome de usuário: <input name=""nome_usuario"" required /></label><br/>
            <label>E-mail: <input name=""email"" type=""email"" required /></label><br/>
            <label>Senha: <input name=""senha"" type=""password"" required /></label><br/>
            <button type=""submit"">Criar conta</button>
        </form>");
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapPost("/registro", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var user = form["nome_usuario"].ToString()?.Trim() ?? "";
    var email = form["email"].ToString()?.Trim() ?? "";
    var senha = form["senha"].ToString() ?? "";
    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(email))
        return Results.Redirect("/registro?erro=1");
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("INSERT INTO usuarios (nome_usuario, senha_hash, email, eh_admin) VALUES (@u, @h, @e, FALSE)", conn);
    cmd.Parameters.AddWithValue("u", user);
    cmd.Parameters.AddWithValue("h", HashSenha(senha));
    cmd.Parameters.AddWithValue("e", email);
    try
    {
        var n = await cmd.ExecuteNonQueryAsync();
        return Results.Redirect(n > 0 ? "/login" : "/registro?erro=2");
    }
    catch (PostgresException px) when (px.SqlState == "23505")
    {
        return Results.Redirect("/registro?erro=3");
    }
});

app.MapPost("/setup", async (HttpContext ctx, HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var user = form["nome_usuario"].ToString()?.Trim() ?? "";
    var email = form["email"].ToString()?.Trim() ?? "";
    var senha = form["senha"].ToString() ?? "";
    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(email))
        return Results.Redirect("/setup?erro=1");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var c = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", conn);
    var count = (long)(await c.ExecuteScalarAsync() ?? 0);
    if (count > 0)
        return Results.Redirect("/login");

    await using var cmd = new NpgsqlCommand("INSERT INTO usuarios (nome_usuario, senha_hash, email, eh_admin) VALUES (@u, @h, @e, TRUE)", conn);
    cmd.Parameters.AddWithValue("u", user);
    cmd.Parameters.AddWithValue("h", HashSenha(senha));
    cmd.Parameters.AddWithValue("e", email);
    await cmd.ExecuteNonQueryAsync();
    return Results.Redirect("/login");
});

// Redireciona /contato para a página do site (contato.html com formulário estilizado)
app.MapGet("/contato", () => Results.Redirect("/contato.html"));

app.MapPost("/contato", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var nome = form["nome"].ToString()?.Trim() ?? "";
    var telefone = form["telefone"].ToString()?.Trim() ?? "";
    var email = form["email"].ToString()?.Trim() ?? "";
    var assunto = form["assunto"].ToString()?.Trim();
    var mensagem = form["mensagem"].ToString()?.Trim() ?? "";
    if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(telefone) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(mensagem))
        return Results.Redirect("/contato.html?erro=1");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("INSERT INTO contatos (nome, telefone, email, assunto, mensagem) VALUES (@n,@t,@e,@a,@m)", conn);
    cmd.Parameters.AddWithValue("n", nome);
    cmd.Parameters.AddWithValue("t", telefone);
    cmd.Parameters.AddWithValue("e", email);
    cmd.Parameters.AddWithValue("a", (object?)assunto ?? DBNull.Value);
    cmd.Parameters.AddWithValue("m", mensagem);
    await cmd.ExecuteNonQueryAsync();
    try
    {
        await EnviarEmailContatoAsync(nome, telefone, email, assunto, mensagem);
    }
    catch (Exception ex)
    {
        // Mesmo com falha no SMTP, mantém feedback de sucesso no front.
        Console.WriteLine($"[SMTP] Falha ao enviar contato para gislane.sena1@aluno.unip.br: {ex.Message}");
        return Results.Redirect("/contato.html?ok=1");
    }
    return Results.Redirect("/contato.html?ok=1");
});

async Task RegistrarInscricaoComEmailAsync(int usuarioId, int eventoId)
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var ins = new NpgsqlCommand(
        "INSERT INTO inscricoes_eventos (evento_id, usuario_id) VALUES (@e,@u) ON CONFLICT (evento_id, usuario_id) DO NOTHING", conn);
    ins.Parameters.AddWithValue("e", eventoId);
    ins.Parameters.AddWithValue("u", usuarioId);
    var rows = await ins.ExecuteNonQueryAsync();
    if (rows == 0) return;

    string? emailUser = null;
    var nomeUser = "";
    await using (var uq = new NpgsqlCommand("SELECT email, nome_usuario FROM usuarios WHERE id = @id", conn))
    {
        uq.Parameters.AddWithValue("id", usuarioId);
        await using var ur = await uq.ExecuteReaderAsync();
        if (await ur.ReadAsync())
        {
            if (!ur.IsDBNull(0) && !string.IsNullOrWhiteSpace(ur.GetString(0)))
                emailUser = ur.GetString(0);
            nomeUser = ur.GetString(1);
        }
    }

    await using (var eq = new NpgsqlCommand("SELECT titulo, data_evento, horario, endereco FROM eventos WHERE id = @id", conn))
    {
        eq.Parameters.AddWithValue("id", eventoId);
        await using var er = await eq.ExecuteReaderAsync();
        if (await er.ReadAsync())
        {
            var tit = er.GetString(0);
            var d = DateOnly.FromDateTime(er.GetDateTime(1));
            var h = TimeOnly.FromTimeSpan(er.GetTimeSpan(2));
            var end = er.GetString(3);
            await EnviarEmailConfirmacaoInscricaoAsync(emailUser, nomeUser, tit, d, h, end);
        }
    }
}

// ----- Eventos (lista pública; usuário pode se inscrever)
app.MapGet("/eventos", async (HttpRequest req) =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT id, titulo, data_evento, horario, endereco, descricao, foto_url FROM eventos ORDER BY data_evento, horario", conn);
    var sb = new StringBuilder();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        var id = r.GetInt32(0);
        var titulo = r.GetString(1);
        var data = r.GetDateTime(2);
        var horario = r.GetTimeSpan(3);
        var endereco = r.GetString(4);
        var desc = r.IsDBNull(5) ? "" : r.GetString(5);
        var foto = r.IsDBNull(6) ? "" : r.GetString(6);
        sb.Append($@"<div class=""card"" style=""border:1px solid #ccc;padding:1em;margin:1em 0;"">
            <h3>{titulo}</h3><p>Data: {data:dd/MM/yyyy} às {horario:hh\:mm}</p><p>Endereço: {endereco}</p>
            {(string.IsNullOrEmpty(desc) ? "" : $"<p>{desc}</p>")}{(string.IsNullOrEmpty(foto) ? "" : $"<img src=\"{foto}\" alt=\"\" style=\"max-width:200px;\"/>")}
            <form method=""post"" action=""/eventos/inscrever"" style=""display:inline;""><input type=""hidden"" name=""evento_id"" value=""{id}""/><button type=""submit"">Inscrever-se</button></form>
            </div>");
    }
    var (userId, _) = LerUsuarioCookie(req);
    var body = $"<h1>Eventos</h1>{(userId.HasValue ? "<p>Você está logado. Pode se inscrever nos eventos.</p>" : "<p>Faça <a href=\"/login\">login</a> para se inscrever.</p>")}" + sb.ToString();
    return Results.Content(Layout("Eventos", body), "text/html; charset=utf-8");
});

app.MapPost("/eventos/inscrever", async (HttpContext ctx, HttpRequest req) =>
{
    var (userId, _) = LerUsuarioCookie(req);
    if (!userId.HasValue)
        return Results.Redirect("/eventos.html?precisa_login=1");
    var form = await req.ReadFormAsync();
    if (!int.TryParse(form["evento_id"].ToString(), out var eventoId))
        return Results.Redirect("/eventos.html");
    try
    {
        await RegistrarInscricaoComEmailAsync(userId.Value, eventoId);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Inscrição] Erro: {ex.Message}");
    }
    return Results.Redirect("/eventos.html?inscrito=1");
});

app.MapPost("/eventos/inscrever-por-titulo", async (HttpRequest req) =>
{
    var (userId, _) = LerUsuarioCookie(req);
    if (!userId.HasValue)
        return Results.Redirect("/eventos.html?precisa_login=1");

    var form = await req.ReadFormAsync();
    var tituloEvento = form["titulo_evento"].ToString();
    if (string.IsNullOrWhiteSpace(tituloEvento))
        return Results.Redirect("/eventos.html");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    await using var busca = new NpgsqlCommand("SELECT id FROM eventos WHERE titulo = @t LIMIT 1", conn);
    busca.Parameters.AddWithValue("t", tituloEvento);
    var idObj = await busca.ExecuteScalarAsync();
    if (idObj is null)
        return Results.Redirect("/eventos.html?erro=evento");

    var eventoId = Convert.ToInt32(idObj);
    try
    {
        await RegistrarInscricaoComEmailAsync(userId.Value, eventoId);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Inscrição] Erro: {ex.Message}");
    }
    return Results.Redirect("/dashboard.html?inscrito=1");
});

app.MapGet("/meus-eventos", (HttpRequest req) =>
{
    if (!LerUsuarioCookie(req).userId.HasValue)
        return Results.Redirect("/index.html?precisa_login=1");
    return Results.Redirect("/meus-eventos.html");
});

// ----- Blog (só leitura)
app.MapGet("/blog", async () =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT id, titulo, data_publicacao, horario, descricao, foto_url FROM artigos_blog ORDER BY data_publicacao DESC, horario DESC", conn);
    var sb = new StringBuilder();
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        var id = r.GetInt32(0);
        var titulo = r.GetString(1);
        var data = r.GetDateTime(2);
        var horario = r.GetTimeSpan(3);
        var desc = r.IsDBNull(4) ? "" : r.GetString(4);
        var foto = r.IsDBNull(5) ? "" : r.GetString(5);
        sb.Append($@"<div style=""border:1px solid #ccc;padding:1em;margin:1em 0;""><h3><a href=""/blog/{id}"">{titulo}</a></h3><p>Publicado em {data:dd/MM/yyyy} às {horario:hh\:mm}</p></div>");
    }
    return Results.Content(Layout("Blog", "<h1>Blog</h1>" + sb.ToString()), "text/html; charset=utf-8");
});

app.MapGet("/blog/{id:int}", async (int id) =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT titulo, data_publicacao, horario, descricao, foto_url FROM artigos_blog WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    await using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync())
        return Results.NotFound();
    var titulo = r.GetString(0);
    var data = r.GetDateTime(1);
    var horario = r.GetTimeSpan(2);
    var desc = r.IsDBNull(3) ? "" : r.GetString(3);
    var foto = r.IsDBNull(4) ? "" : r.GetString(4);
    var body = $@"<h1>{titulo}</h1><p>Publicado em {data:dd/MM/yyyy} às {horario:hh\:mm}</p>{(string.IsNullOrEmpty(desc) ? "" : $"<div>{desc}</div>")}{(string.IsNullOrEmpty(foto) ? "" : $"<img src=\"{foto}\" alt=\"\" style=\"max-width:300px;\"/>")}<p><a href=""/blog"">Voltar ao blog</a></p>";
    return Results.Content(Layout(titulo, body), "text/html; charset=utf-8");
});

// ----- Área admin (somente administradores)
app.MapGet("/admin", async (HttpRequest req) =>
{
    var (userId, isAdmin) = LerUsuarioCookie(req);
    if (!userId.HasValue || !isAdmin)
        return Results.Redirect("/login?voltar=/admin");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    var sbEventos = new StringBuilder();
    await using (var cmd = new NpgsqlCommand("SELECT id, titulo, data_evento, horario, endereco FROM eventos ORDER BY data_evento DESC", conn))
    await using (var r = await cmd.ExecuteReaderAsync())
        while (await r.ReadAsync())
            sbEventos.AppendLine($"<li>{r.GetString(1)} - {r.GetDateTime(2):dd/MM/yyyy} - <a href=\"/admin/eventos\">Ver</a></li>");

    var sbBlog = new StringBuilder();
    await using (var cmd = new NpgsqlCommand("SELECT id, titulo, data_publicacao FROM artigos_blog ORDER BY data_publicacao DESC", conn))
    await using (var r = await cmd.ExecuteReaderAsync())
        while (await r.ReadAsync())
            sbBlog.AppendLine($"<li>{r.GetString(1)} - <a href=\"/blog/{r.GetInt32(0)}\">Ver</a></li>");

    var sbContatos = new StringBuilder();
    await using (var cmd = new NpgsqlCommand("SELECT nome, email, telefone, assunto, mensagem, enviado_em FROM contatos ORDER BY enviado_em DESC LIMIT 50", conn))
    await using (var r = await cmd.ExecuteReaderAsync())
    {
        while (await r.ReadAsync())
        {
            var nome = r.GetString(0);
            var email = r.GetString(1);
            var telefone = r.GetString(2);
            var assunto = r.IsDBNull(3) ? "(sem assunto)" : r.GetString(3);
            var mensagem = r.GetString(4);
            var enviadoEm = r.GetDateTime(5);
            sbContatos.AppendLine($@"<div style=""border:1px solid #ddd;padding:10px;margin:10px 0;"">
                <strong>{nome}</strong> ({email}) - {telefone}<br/>
                <em>{assunto}</em><br/>
                <div>{mensagem}</div>
                <small>Enviado em: {enviadoEm:dd/MM/yyyy HH:mm}</small>
            </div>");
        }
    }

    var body = $@"
        <h1>Área do administrador</h1>
        <p><a href=""/admin/eventos"">Lançar evento</a> | <a href=""/admin/blog"">Lançar artigo (blog)</a></p>
        <h2>Eventos publicados</h2><ul>{sbEventos}</ul>
        <h2>Artigos do blog</h2><ul>{sbBlog}</ul>
        <h2>Respostas do formulário de contato</h2>{(sbContatos.Length == 0 ? "<p>Nenhuma resposta ainda.</p>" : sbContatos.ToString())}";
    return Results.Content(Layout("Admin", body, true), "text/html; charset=utf-8");
});

// Admin: criar evento — página estilizada no front (admin-criar-evento.html)
app.MapGet("/admin/eventos", (HttpRequest req) =>
{
    var (userId, isAdmin) = LerUsuarioCookie(req);
    var redir = RedirectSePrecisaLoginOuNaoAdmin(userId, isAdmin);
    if (redir != null) return Results.Redirect(redir);
    return Results.Redirect("/admin-criar-evento.html");
});

app.MapPost("/admin/eventos", async (HttpContext ctx, HttpRequest req) =>
{
    var (userId, isAdmin) = LerUsuarioCookie(req);
    var redir = RedirectSePrecisaLoginOuNaoAdmin(userId, isAdmin);
    if (redir != null) return Results.Redirect(redir);
    var form = await req.ReadFormAsync();
    var titulo = form["titulo"].ToString()?.Trim() ?? "";
    var dataStr = form["data_evento"].ToString();
    var horarioStr = form["horario"].ToString();
    var endereco = form["endereco"].ToString()?.Trim() ?? "";
    var descricao = form["descricao"].ToString()?.Trim();
    var fotoUrl = form["foto_url"].ToString()?.Trim();
    if (string.IsNullOrEmpty(titulo) || string.IsNullOrEmpty(dataStr) || string.IsNullOrEmpty(horarioStr))
        return Results.Redirect("/admin-criar-evento.html?erro=1");
    if (!DateOnly.TryParse(dataStr, out var data) || !TimeOnly.TryParse(horarioStr, out var horario))
        return Results.Redirect("/admin-criar-evento.html?erro=1");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("INSERT INTO eventos (titulo, data_evento, horario, endereco, descricao, foto_url) VALUES (@t,@d,@h,@e,@desc,@f)", conn);
    cmd.Parameters.AddWithValue("t", titulo);
    cmd.Parameters.AddWithValue("d", data);
    cmd.Parameters.AddWithValue("h", horario);
    cmd.Parameters.AddWithValue("e", endereco);
    cmd.Parameters.AddWithValue("desc", (object?)descricao ?? DBNull.Value);
    cmd.Parameters.AddWithValue("f", (object?)fotoUrl ?? DBNull.Value);
    await cmd.ExecuteNonQueryAsync();
    return Results.Redirect("/dashboard.html?evento_criado=1");
});

// Admin: criar artigo blog (título, data, horário obrigatórios; descrição e foto opcionais)
app.MapGet("/admin/blog", (HttpRequest req) =>
{
    var (_, isAdmin) = LerUsuarioCookie(req);
    if (!isAdmin) return Results.Redirect("/login");
    var html = Layout("Novo artigo", @"
        <h1>Lançar artigo (blog)</h1>
        <form method=""post"" action=""/admin/blog"">
            <label>Título *: <input name=""titulo"" required /></label><br/>
            <label>Data publicação *: <input name=""data_publicacao"" type=""date"" required /></label><br/>
            <label>Horário *: <input name=""horario"" type=""time"" required /></label><br/>
            <label>Descrição: <textarea name=""descricao""></textarea></label><br/>
            <label>URL da foto: <input name=""foto_url"" /></label><br/>
            <button type=""submit"">Publicar</button>
        </form>", true);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapPost("/admin/blog", async (HttpContext ctx, HttpRequest req) =>
{
    var (_, isAdmin) = LerUsuarioCookie(req);
    if (!isAdmin) return Results.Redirect("/login");
    var form = await req.ReadFormAsync();
    var titulo = form["titulo"].ToString()?.Trim() ?? "";
    var dataStr = form["data_publicacao"].ToString();
    var horarioStr = form["horario"].ToString();
    var descricao = form["descricao"].ToString()?.Trim();
    var fotoUrl = form["foto_url"].ToString()?.Trim();
    if (string.IsNullOrEmpty(titulo) || string.IsNullOrEmpty(dataStr) || string.IsNullOrEmpty(horarioStr))
        return Results.Redirect("/admin/blog?erro=1");
    if (!DateOnly.TryParse(dataStr, out var data) || !TimeOnly.TryParse(horarioStr, out var horario))
        return Results.Redirect("/admin/blog?erro=1");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("INSERT INTO artigos_blog (titulo, data_publicacao, horario, descricao, foto_url) VALUES (@t,@d,@h,@desc,@f)", conn);
    cmd.Parameters.AddWithValue("t", titulo);
    cmd.Parameters.AddWithValue("d", data);
    cmd.Parameters.AddWithValue("h", horario);
    cmd.Parameters.AddWithValue("desc", (object?)descricao ?? DBNull.Value);
    cmd.Parameters.AddWithValue("f", (object?)fotoUrl ?? DBNull.Value);
    await cmd.ExecuteNonQueryAsync();
    return Results.Redirect("/admin");
});

// ----- APIs JSON (front em localhost:5000 — mesmo site, cookies HttpOnly)
static async Task<JsonDocument?> LerJsonDocAsync(Stream body)
{
    using var ms = new MemoryStream();
    await body.CopyToAsync(ms);
    ms.Position = 0;
    if (ms.Length == 0) return null;
    return JsonDocument.Parse(ms);
}

app.MapPost("/api/auth/login", async (HttpContext ctx, HttpRequest req) =>
{
    var doc = await LerJsonDocAsync(req.Body);
    if (doc == null) return Results.Json(new { ok = false, error = "Corpo vazio" });
    var root = doc.RootElement;
    var user = root.TryGetProperty("nome_usuario", out var u) ? u.GetString()?.Trim() : null;
    var senha = root.TryGetProperty("senha", out var s) ? s.GetString() : null;
    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(senha))
        return Results.Json(new { ok = false, error = "Usuário e senha obrigatórios" });

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT id, nome_usuario, senha_hash, eh_admin FROM usuarios WHERE nome_usuario = @u", conn);
    cmd.Parameters.AddWithValue("u", user);
    await using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync())
        return Results.Json(new { ok = false, error = "Usuário ou senha incorretos" });
    var id = r.GetInt32(0);
    var hash = r.GetString(2);
    var ehAdmin = r.GetBoolean(3);
    if (!VerificarSenha(senha, hash))
        return Results.Json(new { ok = false, error = "Usuário ou senha incorretos" });

    ctx.Response.Cookies.Append("uid", id.ToString(), CriarCookieAuth());
    ctx.Response.Cookies.Append("adm", ehAdmin ? "1" : "0", CriarCookieAuth());
    return Results.Json(new { ok = true, isAdmin = ehAdmin, redirect = "/dashboard.html" });
});

app.MapPost("/api/auth/logout", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("uid", CriarCookieAuth());
    ctx.Response.Cookies.Delete("adm", CriarCookieAuth());
    return Results.Json(new { ok = true });
});

app.MapGet("/api/auth/me", async (HttpRequest req) =>
{
    var (userId, isAdmin) = LerUsuarioCookie(req);
    if (!userId.HasValue)
        return Results.Json(new { loggedIn = false });

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("SELECT nome_usuario FROM usuarios WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", userId.Value);
    var nome = (string?)(await cmd.ExecuteScalarAsync());
    return Results.Json(new { loggedIn = true, isAdmin, nomeUsuario = nome ?? "" });
});

app.MapGet("/api/public/eventos", async () =>
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    var lista = new List<Dictionary<string, string>>();
    await using var cmd = new NpgsqlCommand(
        "SELECT id, titulo, data_evento, horario, endereco, COALESCE(descricao,''), COALESCE(foto_url,'') FROM eventos ORDER BY data_evento, horario", conn);
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        var data = r.GetDateTime(2);
        var hora = r.GetTimeSpan(3);
        lista.Add(new Dictionary<string, string>
        {
            ["id"] = r.GetInt32(0).ToString(),
            ["titulo"] = r.GetString(1),
            ["data"] = data.ToString("dd/MM/yyyy"),
            ["horario"] = hora.ToString(@"hh\:mm"),
            ["endereco"] = r.GetString(4),
            ["descricao"] = r.GetString(5),
            ["fotoUrl"] = r.GetString(6)
        });
    }
    return Results.Json(new { ok = true, eventos = lista });
});

app.MapGet("/api/user/eventos", async (HttpRequest req) =>
{
    var (userId, _) = LerUsuarioCookie(req);
    if (!userId.HasValue)
        return Results.Json(new { ok = false, error = "Não autenticado" }, statusCode: 401);

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    var inscritos = new List<Dictionary<string, string>>();
    await using (var cmd = new NpgsqlCommand(@"
        SELECT e.id, e.titulo, e.data_evento, e.horario, e.endereco, COALESCE(e.descricao,'')
        FROM inscricoes_eventos i
        INNER JOIN eventos e ON e.id = i.evento_id
        WHERE i.usuario_id = @u
        ORDER BY e.data_evento, e.horario", conn))
    {
        cmd.Parameters.AddWithValue("u", userId.Value);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var data = r.GetDateTime(2);
            var hora = r.GetTimeSpan(3);
            inscritos.Add(new Dictionary<string, string>
            {
                ["id"] = r.GetInt32(0).ToString(),
                ["titulo"] = r.GetString(1),
                ["data"] = data.ToString("dd/MM/yyyy"),
                ["horario"] = hora.ToString(@"hh\:mm"),
                ["endereco"] = r.GetString(4),
                ["descricao"] = r.GetString(5)
            });
        }
    }

    var disponiveis = new List<Dictionary<string, string>>();
    await using (var cmd = new NpgsqlCommand(@"
        SELECT e.id, e.titulo, e.data_evento, e.horario, e.endereco, COALESCE(e.descricao,''), COALESCE(e.foto_url,'')
        FROM eventos e
        WHERE NOT EXISTS (
            SELECT 1 FROM inscricoes_eventos i WHERE i.evento_id = e.id AND i.usuario_id = @u
        )
        ORDER BY e.data_evento, e.horario", conn))
    {
        cmd.Parameters.AddWithValue("u", userId.Value);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            var data = r.GetDateTime(2);
            var hora = r.GetTimeSpan(3);
            disponiveis.Add(new Dictionary<string, string>
            {
                ["id"] = r.GetInt32(0).ToString(),
                ["titulo"] = r.GetString(1),
                ["data"] = data.ToString("dd/MM/yyyy"),
                ["horario"] = hora.ToString(@"hh\:mm"),
                ["endereco"] = r.GetString(4),
                ["descricao"] = r.GetString(5),
                ["fotoUrl"] = r.GetString(6)
            });
        }
    }

    return Results.Json(new { ok = true, inscritos, disponiveis });
});

app.MapPost("/api/eventos/inscrever", async (HttpRequest req) =>
{
    var (userId, _) = LerUsuarioCookie(req);
    if (!userId.HasValue)
        return Results.Json(new { ok = false, error = "Faça login para se inscrever" }, statusCode: 401);

    var doc = await LerJsonDocAsync(req.Body);
    if (doc == null) return Results.Json(new { ok = false, error = "Corpo vazio" });
    var root = doc.RootElement;
    if (!root.TryGetProperty("evento_id", out var evEl))
        return Results.Json(new { ok = false, error = "evento_id obrigatório" });
    int eventoId;
    if (evEl.ValueKind == JsonValueKind.Number)
        eventoId = evEl.GetInt32();
    else if (evEl.ValueKind == JsonValueKind.String && int.TryParse(evEl.GetString(), out var parsed))
        eventoId = parsed;
    else
        return Results.Json(new { ok = false, error = "evento_id inválido" });

    try
    {
        await RegistrarInscricaoComEmailAsync(userId.Value, eventoId);
        return Results.Json(new { ok = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API Inscrição] {ex.Message}");
        return Results.Json(new { ok = false, error = "Não foi possível concluir a inscrição" }, statusCode: 400);
    }
});

app.MapPost("/api/auth/registro", async (HttpRequest req) =>
{
    var doc = await LerJsonDocAsync(req.Body);
    if (doc == null) return Results.Json(new { ok = false, error = "Corpo vazio" });
    var root = doc.RootElement;
    var user = root.TryGetProperty("nome_usuario", out var u) ? u.GetString()?.Trim() : null;
    var email = root.TryGetProperty("email", out var e) ? e.GetString()?.Trim() : null;
    var senha = root.TryGetProperty("senha", out var s) ? s.GetString() : null;
    if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(senha) || string.IsNullOrWhiteSpace(email))
        return Results.Json(new { ok = false, error = "Preencha todos os campos" });

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand("INSERT INTO usuarios (nome_usuario, senha_hash, email, eh_admin) VALUES (@u, @h, @e, FALSE)", conn);
    cmd.Parameters.AddWithValue("u", user);
    cmd.Parameters.AddWithValue("h", HashSenha(senha));
    cmd.Parameters.AddWithValue("e", email);
    try
    {
        await cmd.ExecuteNonQueryAsync();
        return Results.Json(new { ok = true });
    }
    catch (PostgresException px) when (px.SqlState == "23505")
    {
        return Results.Json(new { ok = false, error = "Usuário ou e-mail já cadastrado" });
    }
});

app.MapPost("/api/auth/recuperar-senha", async (HttpRequest req) =>
{
    var doc = await LerJsonDocAsync(req.Body);
    if (doc == null) return Results.Json(new { ok = false, error = "Corpo vazio" });
    var root = doc.RootElement;
    var email = root.TryGetProperty("email", out var e) ? e.GetString()?.Trim() : null;
    if (string.IsNullOrWhiteSpace(email))
        return Results.Json(new { ok = false, error = "Informe o e-mail" });

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    int? usuarioId = null;
    await using (var q = new NpgsqlCommand("SELECT id FROM usuarios WHERE LOWER(email) = LOWER(@em) LIMIT 1", conn))
    {
        q.Parameters.AddWithValue("em", email);
        var o = await q.ExecuteScalarAsync();
        if (o != null && o != DBNull.Value)
            usuarioId = Convert.ToInt32(o);
    }
    if (!usuarioId.HasValue)
        return Results.Json(new { ok = true, message = "Se o e-mail existir, você receberá um código em instantes." });

    var codigo = Random.Shared.Next(100000, 999999).ToString();
    var expira = DateTime.UtcNow.AddMinutes(20);
    await using (var del = new NpgsqlCommand("UPDATE recuperacao_senha SET usado = TRUE WHERE usuario_id = @u AND usado = FALSE", conn))
    {
        del.Parameters.AddWithValue("u", usuarioId.Value);
        await del.ExecuteNonQueryAsync();
    }
    await using (var ins = new NpgsqlCommand("INSERT INTO recuperacao_senha (usuario_id, codigo, expira_em) VALUES (@u, @c, @x)", conn))
    {
        ins.Parameters.AddWithValue("u", usuarioId.Value);
        ins.Parameters.AddWithValue("c", codigo);
        ins.Parameters.AddWithValue("x", expira);
        await ins.ExecuteNonQueryAsync();
    }

    try
    {
        await EnviarEmailAsync(email, "[AxiomCode] Código para redefinir senha",
            $"Seu código de verificação: {codigo}\n\nEle expira em 20 minutos.\n\nSe você não solicitou, ignore este e-mail.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SMTP] Falha recuperação senha: {ex.Message}");
    }
    return Results.Json(new { ok = true, message = "Se o e-mail existir, você receberá um código em instantes." });
});

app.MapPost("/api/auth/redefinir-senha", async (HttpRequest req) =>
{
    var doc = await LerJsonDocAsync(req.Body);
    if (doc == null) return Results.Json(new { ok = false, error = "Corpo vazio" });
    var root = doc.RootElement;
    var email = root.TryGetProperty("email", out var e) ? e.GetString()?.Trim() : null;
    var codigo = root.TryGetProperty("codigo", out var c) ? c.GetString()?.Trim() : null;
    var novaSenha = root.TryGetProperty("nova_senha", out var n) ? n.GetString() : null;
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(novaSenha))
        return Results.Json(new { ok = false, error = "Preencha e-mail, código e nova senha" });
    if (novaSenha.Length < 4)
        return Results.Json(new { ok = false, error = "Senha muito curta" });

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    int? usuarioId = null;
    await using (var q = new NpgsqlCommand("SELECT id FROM usuarios WHERE LOWER(email) = LOWER(@em) LIMIT 1", conn))
    {
        q.Parameters.AddWithValue("em", email);
        var o = await q.ExecuteScalarAsync();
        if (o != null && o != DBNull.Value)
            usuarioId = Convert.ToInt32(o);
    }
    if (!usuarioId.HasValue)
        return Results.Json(new { ok = false, error = "Código inválido ou expirado" });

    await using var cmd = new NpgsqlCommand(@"
        UPDATE recuperacao_senha SET usado = TRUE
        WHERE usuario_id = @u AND codigo = @c AND usado = FALSE AND expira_em > @agora
        RETURNING id", conn);
    cmd.Parameters.AddWithValue("u", usuarioId.Value);
    cmd.Parameters.AddWithValue("c", codigo);
    cmd.Parameters.AddWithValue("agora", DateTime.UtcNow);
    var okRow = await cmd.ExecuteScalarAsync();
    if (okRow == null || okRow == DBNull.Value)
        return Results.Json(new { ok = false, error = "Código inválido ou expirado" });

    await using var up = new NpgsqlCommand("UPDATE usuarios SET senha_hash = @h WHERE id = @id", conn);
    up.Parameters.AddWithValue("h", HashSenha(novaSenha));
    up.Parameters.AddWithValue("id", usuarioId.Value);
    await up.ExecuteNonQueryAsync();
    return Results.Json(new { ok = true });
});

app.MapGet("/api/admin/contatos", async (HttpRequest req) =>
{
    var (_, isAdmin) = LerUsuarioCookie(req);
    if (!isAdmin)
        return Results.Json(new { ok = false, error = "Acesso negado" }, statusCode: 403);

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    var lista = new List<Dictionary<string, string>>();
    await using var cmd = new NpgsqlCommand(
        "SELECT nome, email, telefone, assunto, mensagem, enviado_em FROM contatos ORDER BY enviado_em DESC LIMIT 100", conn);
    await using var r = await cmd.ExecuteReaderAsync();
    while (await r.ReadAsync())
    {
        lista.Add(new Dictionary<string, string>
        {
            ["nome"] = r.GetString(0),
            ["email"] = r.GetString(1),
            ["telefone"] = r.GetString(2),
            ["assunto"] = r.IsDBNull(3) ? "" : r.GetString(3),
            ["mensagem"] = r.GetString(4),
            ["enviadoEm"] = r.GetDateTime(5).ToString("dd/MM/yyyy HH:mm")
        });
    }
    return Results.Json(new { ok = true, contatos = lista });
});

Console.WriteLine("");
Console.WriteLine("  ========================================");
Console.WriteLine("  AxiomCode - Servidor iniciado!");
Console.WriteLine("  ========================================");
Console.WriteLine("  Abra o navegador e acesse:");
Console.WriteLine("  ");
Console.WriteLine("    http://localhost:5000");
Console.WriteLine("  ");
Console.WriteLine("  Para criar o admin, acesse:");
Console.WriteLine("    http://localhost:5000/setup");
Console.WriteLine("  ========================================");
Console.WriteLine("  (Deixe esta janela aberta enquanto usar o site)");
Console.WriteLine("");

app.Run();

// ============== CLASSES (POO) – refletem as tabelas do MySQL ==============
public class Usuario { public int Id { get; set; } public string NomeUsuario { get; set; } = ""; public string SenhaHash { get; set; } = ""; public string? Email { get; set; } public bool EhAdmin { get; set; } public DateTime CriadoEm { get; set; } }
public class Evento { public int Id { get; set; } public string Titulo { get; set; } = ""; public DateOnly DataEvento { get; set; } public TimeOnly Horario { get; set; } public string Endereco { get; set; } = ""; public string? Descricao { get; set; } public string? FotoUrl { get; set; } public DateTime CriadoEm { get; set; } }
public class ArtigoBlog { public int Id { get; set; } public string Titulo { get; set; } = ""; public DateOnly DataPublicacao { get; set; } public TimeOnly Horario { get; set; } public string? Descricao { get; set; } public string? FotoUrl { get; set; } public DateTime CriadoEm { get; set; } }
public class Contato { public int Id { get; set; } public string Nome { get; set; } = ""; public string Telefone { get; set; } = ""; public string Email { get; set; } = ""; public string? Assunto { get; set; } public string Mensagem { get; set; } = ""; public DateTime EnviadoEm { get; set; } }
public class InscricaoEvento { public int Id { get; set; } public int EventoId { get; set; } public int UsuarioId { get; set; } public DateTime InscritoEm { get; set; } }
