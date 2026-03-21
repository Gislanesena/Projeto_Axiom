$env:AXIOM_SMTP_HOST = "smtp.office365.com"
$env:AXIOM_SMTP_PORT = "587"
$env:AXIOM_SMTP_USER = "SEU_EMAIL@dominio.com"
$env:AXIOM_SMTP_PASS = "SUA_SENHA_OU_APP_PASSWORD"
$env:AXIOM_SMTP_FROM = "SEU_EMAIL@dominio.com"
$env:AXIOM_SMTP_SSL  = "true"

# Conexao com PostgreSQL (ajuste se necessario)
$env:AXIOM_DB = "Host=localhost;Port=5432;Database=axiomcode;Username=postgres;Password=123456"

dotnet run
