# Colportor ID — .NET 8 + PostgreSQL + Mailhog + Docker

> Frontend em HTML/CSS/JS puro (sem React), servido via `wwwroot` da API.
> Painel Admin: `/admin/` • Carteira do Colportor: `/colportor/`

## Subir com Docker

```bash
docker compose -f deploy/docker-compose.yml up -d --build
```

Acesse:
- Swagger: http://localhost:8080/swagger
- Painel Admin: http://localhost:8080/admin/
- Carteira: http://localhost:8080/colportor/
- Mailhog (emails): http://localhost:8025

Login Admin padrão:
- **Email:** `admin@colportor.local`
- **Senha:** `admin123`

## Fluxo
1. Admin faz login no painel e cria colportor (gera usuário + senha).
2. Admin registra visitas ao PAC (data).
3. Sistema calcula status: **EM DIA**, **AVISO** (faltam 15 dias), **VENCIDO** (no dia ou depois).
4. Worker checa diariamente e envia emails (Mailhog).

## Migrações
As migrações já estão incluídas. A API executa `db.Database.Migrate()` no start.
Se quiser rodar local sem docker, configure `appsettings.json` e execute:
```bash
dotnet run --project src/Colportor.Api
```

## Segurança
- JWT simples (Admin / Colportor)
- Senhas com BCrypt
- Emails via SMTP (Mailhog em dev)
