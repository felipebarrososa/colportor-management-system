# 🗄️ Configuração do Banco de Dados - Supabase

## Passo a Passo para Supabase (Gratuito)

### 1. Criar conta no Supabase
1. **Acesse**: https://supabase.com
2. **Clique** em "Start your project"
3. **Conecte** com GitHub
4. **Crie** um novo projeto

### 2. Configurar o banco
1. **Vá** para "Settings" → "Database"
2. **Copie** a connection string
3. **Exemplo**: `postgresql://postgres:[PASSWORD]@db.[PROJECT].supabase.co:5432/postgres`

### 3. Executar migrações
```bash
# Instalar CLI do Supabase
npm install -g supabase

# Ou usar Docker
docker run --rm -it supabase/cli:latest --help

# Conectar e executar migrações
supabase db push
```

### 4. Configurar variáveis de ambiente
```bash
# No Railway/Render, adicione:
ConnectionStrings__Default=postgresql://postgres:[PASSWORD]@db.[PROJECT].supabase.co:5432/postgres
```

## Alternativa: Railway PostgreSQL

### 1. No Railway
1. **Deploy** da aplicação
2. **Clique** em "New" → "Database" → "PostgreSQL"
3. **Railway criará** automaticamente as variáveis de ambiente

### 2. Executar migrações
```bash
# Railway executa automaticamente as migrações do EF Core
# Ou execute manualmente:
dotnet ef database update
```

## Alternativa: Render PostgreSQL

### 1. No Render
1. **Crie** um "PostgreSQL" service
2. **Copie** a connection string
3. **Adicione** como variável de ambiente

## Testando a Conexão

### Verificar se o banco está funcionando:
```bash
# Teste local
docker-compose -f docker-compose.prod.yml up db

# Ou conectar diretamente
psql "postgresql://postgres:[PASSWORD]@[HOST]:5432/postgres"
```

## Estrutura do Banco

O sistema criará automaticamente estas tabelas:
- `Users` - Usuários (Admin, Leader, Colportor)
- `Colportors` - Dados dos colportores
- `Regions` - Regiões
- `Countries` - Países
- `Visits` - Visitas dos colportores
- `PacEnrollments` - Solicitações PAC
- `NotificationLogs` - Logs de notificações

## Backup e Segurança

### Supabase:
- ✅ Backup automático
- ✅ Point-in-time recovery
- ✅ SSL obrigatório
- ✅ Firewall configurável

### Railway:
- ✅ Backup automático
- ✅ SSL obrigatório
- ✅ Monitoramento

## Troubleshooting

### Problemas comuns:
1. **Conexão recusada**: Verificar URL e credenciais
2. **Migrações falham**: Verificar permissões do usuário
3. **Timeout**: Verificar firewall e região

### Logs úteis:
```bash
# Railway
railway logs

# Supabase
# Ver na dashboard do Supabase
```
