# Script PowerShell para conectar ao banco e executar o SQL
# Execute: .\fix_database.ps1

$connectionString = "postgresql://postgres:RNPzVecUtMNYkYRidaREmveXIZZHKsGD@caboose.proxy.rlwy.net:36144/railway"

Write-Host "Conectando ao banco de dados..." -ForegroundColor Green

# Verificar se psql está instalado
try {
    $psqlVersion = psql --version
    Write-Host "PostgreSQL client encontrado: $psqlVersion" -ForegroundColor Green
} catch {
    Write-Host "PostgreSQL client não encontrado. Instale o PostgreSQL ou use o pgAdmin." -ForegroundColor Red
    Write-Host "Download: https://www.postgresql.org/download/" -ForegroundColor Yellow
    exit 1
}

# Executar o script SQL
Write-Host "Executando script SQL..." -ForegroundColor Green
psql $connectionString -f fix_database.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "Script executado com sucesso!" -ForegroundColor Green
    Write-Host "Agora você pode descomentar as colunas no código e fazer o deploy." -ForegroundColor Yellow
} else {
    Write-Host "Erro ao executar o script. Verifique a conexão." -ForegroundColor Red
}
