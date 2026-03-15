using MySqlConnector;
using System.Text;
using System.Security.Cryptography;

// ============== CONFIGURAÇÃO ==============
var connectionString = Environment.GetEnvironmentVariable("AXIOM_DB")
    ?? "Server=localhost;Database=axiomcode;User=root;Password=;";
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5000");
builder.Services.AddAntiforgery();
var app = builder.Build();

// Servir arquivos estáticos do site (HTML, CSS, JS, imagens) a partir da pasta SiteUni
var siteUniPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "SiteUni");
if (Directory.Exists(siteUniPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(siteUniPath),
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
static string Layout(string titulo, string body, bool isAdmin = false)
{
    var nav = @"<nav><a href=""/"">Home</a> | <a href=""/eventos"">Eventos</a> | <a href=""/blog"">Blog</a> | <a href=""/contato"">Contato</a> | ";
    if (isAdmin) nav += @"<a href=""/admin"">Admin</a> | ";
    nav += @"<a href=""/login"">Login</a></nav>";
    return $@"<!DOCTYPE html><html lang=""pt-br""><head><meta charset=""utf-8""/><title>{titulo}</title></head><body>{nav}<hr/>{body}</body></html>";
}

// ============== ROTAS ==============

// Página inicial
app.MapGet("/", () => Results.Redirect("/eventos"));

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

    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT id, nome_usuario, senha_hash, eh_admin FROM usuarios WHERE nome_usuario = @u", conn);
    cmd.Parameters.AddWithValue("u", user);
    await using var r = await cmd.ExecuteReaderAsync();
    if (!await r.ReadAsync())
        return Results.Redirect("/login?erro=1");
    var id = r.GetInt32(0);
    var hash = r.GetString(2);
    var ehAdmin = r.GetBoolean(3);
    if (!VerificarSenha(senha, hash))
        return Results.Redirect("/login?erro=1");

    ctx.Response.Cookies.Append("uid", id.ToString(), new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromHours(24) });
    ctx.Response.Cookies.Append("adm", ehAdmin ? "1" : "0", new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromHours(24) });
    return Results.Redirect(ehAdmin ? "/admin" : "/eventos");
});

app.MapGet("/logout", (HttpContext ctx) =>
{
    ctx.Response.Cookies.Delete("uid");
    ctx.Response.Cookies.Delete("adm");
    return Results.Redirect("/");
});

// ----- Setup: criar primeiro usuário admin (só funciona se ainda não existir nenhum usuário)
app.MapGet("/setup", async () =>
{
    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM usuarios", conn);
    var count = (long)(await cmd.ExecuteScalarAsync() ?? 0);
    if (count > 0)
        return Results.Content(Layout("Setup", "<p>Já existem usuários. Use o login normal.</p><a href=\"/login\">Login</a>"), "text/html; charset=utf-8");
    var html = Layout("Criar primeiro admin", @"
        <h1>Criar primeiro administrador</h1>
        <form method=""post"" action=""/setup"">
            <label>Nome de usuário: <input name=""nome_usuario"" required /></label><br/>
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
            <label>Senha: <input name=""senha"" type=""password"" required /></label><br/>
            <button type=""submit"">Criar conta</button>
        </form>");
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapPost("/registro", async (HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var user = form["nome_usuario"].ToString()?.Trim() ?? "";
    var senha = form["senha"].ToString() ?? "";
    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(senha))
        return Results.Redirect("/registro?erro=1");
    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("INSERT IGNORE INTO usuarios (nome_usuario, senha_hash, eh_admin) VALUES (@u, @h, FALSE)", conn);
    cmd.Parameters.AddWithValue("u", user);
    cmd.Parameters.AddWithValue("h", HashSenha(senha));
    var n = await cmd.ExecuteNonQueryAsync();
    return Results.Redirect(n > 0 ? "/login" : "/registro?erro=2");
});

app.MapPost("/setup", async (HttpContext ctx, HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var user = form["nome_usuario"].ToString()?.Trim() ?? "";
    var senha = form["senha"].ToString() ?? "";
    if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(senha))
        return Results.Redirect("/setup?erro=1");

    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var c = new MySqlCommand("SELECT COUNT(*) FROM usuarios", conn);
    var count = (long)(await c.ExecuteScalarAsync() ?? 0);
    if (count > 0)
        return Results.Redirect("/login");

    await using var cmd = new MySqlCommand("INSERT INTO usuarios (nome_usuario, senha_hash, eh_admin) VALUES (@u, @h, TRUE)", conn);
    cmd.Parameters.AddWithValue("u", user);
    cmd.Parameters.AddWithValue("h", HashSenha(senha));
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

    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("INSERT INTO contatos (nome, telefone, email, assunto, mensagem) VALUES (@n,@t,@e,@a,@m)", conn);
    cmd.Parameters.AddWithValue("n", nome);
    cmd.Parameters.AddWithValue("t", telefone);
    cmd.Parameters.AddWithValue("e", email);
    cmd.Parameters.AddWithValue("a", (object?)assunto ?? DBNull.Value);
    cmd.Parameters.AddWithValue("m", mensagem);
    await cmd.ExecuteNonQueryAsync();
    return Results.Redirect("/contato.html?ok=1");
});

// ----- Eventos (lista pública; usuário pode se inscrever)
app.MapGet("/eventos", async (HttpRequest req) =>
{
    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT id, titulo, data_evento, horario, endereco, descricao, foto_url FROM eventos ORDER BY data_evento, horario", conn);
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
        return Results.Redirect("/login?voltar=/eventos");
    var form = await req.ReadFormAsync();
    if (!int.TryParse(form["evento_id"].ToString(), out var eventoId))
        return Results.Redirect("/eventos");
    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("INSERT IGNORE INTO inscricoes_eventos (evento_id, usuario_id) VALUES (@e,@u)", conn);
    cmd.Parameters.AddWithValue("e", eventoId);
    cmd.Parameters.AddWithValue("u", userId.Value);
    await cmd.ExecuteNonQueryAsync();
    return Results.Redirect("/eventos?inscrito=1");
});

// ----- Blog (só leitura)
app.MapGet("/blog", async () =>
{
    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT id, titulo, data_publicacao, horario, descricao, foto_url FROM artigos_blog ORDER BY data_publicacao DESC, horario DESC", conn);
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
    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("SELECT titulo, data_publicacao, horario, descricao, foto_url FROM artigos_blog WHERE id = @id", conn);
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

    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    var sbEventos = new StringBuilder();
    await using (var cmd = new MySqlCommand("SELECT id, titulo, data_evento, horario, endereco FROM eventos ORDER BY data_evento DESC", conn))
    await using (var r = await cmd.ExecuteReaderAsync())
        while (await r.ReadAsync())
            sbEventos.AppendLine($"<li>{r.GetString(1)} - {r.GetDateTime(2):dd/MM/yyyy} - <a href=\"/admin/eventos\">Ver</a></li>");

    var sbBlog = new StringBuilder();
    await using (var cmd = new MySqlCommand("SELECT id, titulo, data_publicacao FROM artigos_blog ORDER BY data_publicacao DESC", conn))
    await using (var r = await cmd.ExecuteReaderAsync())
        while (await r.ReadAsync())
            sbBlog.AppendLine($"<li>{r.GetString(1)} - <a href=\"/blog/{r.GetInt32(0)}\">Ver</a></li>");

    var body = $@"
        <h1>Área do administrador</h1>
        <p><a href=""/admin/eventos"">Lançar evento</a> | <a href=""/admin/blog"">Lançar artigo (blog)</a></p>
        <h2>Eventos publicados</h2><ul>{sbEventos}</ul>
        <h2>Artigos do blog</h2><ul>{sbBlog}</ul>";
    return Results.Content(Layout("Admin", body, true), "text/html; charset=utf-8");
});

// Admin: criar evento (título, data, horário, endereço obrigatórios; descrição e foto opcionais)
app.MapGet("/admin/eventos", (HttpRequest req) =>
{
    var (_, isAdmin) = LerUsuarioCookie(req);
    if (!isAdmin) return Results.Redirect("/login");
    var html = Layout("Novo evento", @"
        <h1>Lançar evento</h1>
        <form method=""post"" action=""/admin/eventos"">
            <label>Título *: <input name=""titulo"" required /></label><br/>
            <label>Data *: <input name=""data_evento"" type=""date"" required /></label><br/>
            <label>Horário *: <input name=""horario"" type=""time"" required /></label><br/>
            <label>Endereço *: <input name=""endereco"" required /></label><br/>
            <label>Descrição: <textarea name=""descricao""></textarea></label><br/>
            <label>URL da foto: <input name=""foto_url"" /></label><br/>
            <button type=""submit"">Criar evento</button>
        </form>", true);
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapPost("/admin/eventos", async (HttpContext ctx, HttpRequest req) =>
{
    var (_, isAdmin) = LerUsuarioCookie(req);
    if (!isAdmin) return Results.Redirect("/login");
    var form = await req.ReadFormAsync();
    var titulo = form["titulo"].ToString()?.Trim() ?? "";
    var dataStr = form["data_evento"].ToString();
    var horarioStr = form["horario"].ToString();
    var endereco = form["endereco"].ToString()?.Trim() ?? "";
    var descricao = form["descricao"].ToString()?.Trim();
    var fotoUrl = form["foto_url"].ToString()?.Trim();
    if (string.IsNullOrEmpty(titulo) || string.IsNullOrEmpty(dataStr) || string.IsNullOrEmpty(horarioStr) || string.IsNullOrEmpty(endereco))
        return Results.Redirect("/admin/eventos?erro=1");
    if (!DateOnly.TryParse(dataStr, out var data) || !TimeOnly.TryParse(horarioStr, out var horario))
        return Results.Redirect("/admin/eventos?erro=1");

    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("INSERT INTO eventos (titulo, data_evento, horario, endereco, descricao, foto_url) VALUES (@t,@d,@h,@e,@desc,@f)", conn);
    cmd.Parameters.AddWithValue("t", titulo);
    cmd.Parameters.AddWithValue("d", data);
    cmd.Parameters.AddWithValue("h", horario);
    cmd.Parameters.AddWithValue("e", endereco);
    cmd.Parameters.AddWithValue("desc", (object?)descricao ?? DBNull.Value);
    cmd.Parameters.AddWithValue("f", (object?)fotoUrl ?? DBNull.Value);
    await cmd.ExecuteNonQueryAsync();
    return Results.Redirect("/admin");
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

    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = new MySqlCommand("INSERT INTO artigos_blog (titulo, data_publicacao, horario, descricao, foto_url) VALUES (@t,@d,@h,@desc,@f)", conn);
    cmd.Parameters.AddWithValue("t", titulo);
    cmd.Parameters.AddWithValue("d", data);
    cmd.Parameters.AddWithValue("h", horario);
    cmd.Parameters.AddWithValue("desc", (object?)descricao ?? DBNull.Value);
    cmd.Parameters.AddWithValue("f", (object?)fotoUrl ?? DBNull.Value);
    await cmd.ExecuteNonQueryAsync();
    return Results.Redirect("/admin");
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
