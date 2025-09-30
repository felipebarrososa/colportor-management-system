# 🚀 Deploy no Railway - Guia Completo

## Passo a Passo Detalhado

### 1. Preparação
- ✅ Repositório no GitHub
- ✅ Docker configurado
- ✅ Arquivos de produção criados

### 2. Deploy no Railway

#### 2.1 Criar Conta
1. **Acesse**: https://railway.app
2. **Clique** em "Login with GitHub"
3. **Autorize** o Railway a acessar seus repositórios

#### 2.2 Deploy da Aplicação
1. **Clique** em "New Project"
2. **Selecione** "Deploy from GitHub repo"
3. **Escolha** `felipebarrososa/colportor-management-system`
4. **Railway detectará** automaticamente o Docker
5. **Clique** em "Deploy Now"

#### 2.3 Adicionar Banco PostgreSQL
1. **No projeto**, clique em "New"
2. **Selecione** "Database" → "PostgreSQL"
3. **Aguarde** o banco ser criado
4. **Railway gerará** automaticamente as variáveis de conexão

### 3. Configurar Variáveis de Ambiente

#### 3.1 Variáveis Obrigatórias
No Railway, vá em "Variables" e adicione:

```bash
# JWT Secret (mínimo 32 caracteres)
JWT_KEY=chave_jwt_super_secreta_minima_32_caracteres_para_seguranca_total

# Environment
ASPNETCORE_ENVIRONMENT=Production
```

#### 3.2 Variáveis de Email (Opcional)
```bash
# SMTP para envio de emails
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=seu_email@gmail.com
SMTP_PASSWORD=sua_senha_de_app_do_gmail
SMTP_FROM=no-reply@seudominio.com
```

#### 3.3 Variáveis do Banco (Automáticas)
Railway criará automaticamente:
- `DATABASE_URL`
- `PGHOST`
- `PGPORT`
- `PGDATABASE`
- `PGUSER`
- `PGPASSWORD`

### 4. Configurar Connection String

#### 4.1 Converter DATABASE_URL
Railway fornece `DATABASE_URL` no formato:
```
postgresql://postgres:senha@host:port/database
```

#### 4.2 Adicionar ConnectionStrings__Default
```bash
ConnectionStrings__Default=postgresql://postgres:senha@host:port/database
```

### 5. Deploy e Teste

#### 5.1 Deploy Automático
- Railway fará build do Docker
- Executará migrações do EF Core
- Criará todas as tabelas
- Subirá a aplicação

#### 5.2 Verificar Logs
1. **Clique** na aplicação
2. **Vá** na aba "Deployments"
3. **Clique** no deployment mais recente
4. **Verifique** os logs para erros

#### 5.3 Testar Aplicação
1. **Clique** no domínio gerado (ex: `https://colportor-production.up.railway.app`)
2. **Teste** o login admin
3. **Verifique** se o banco está funcionando

### 6. Configurações Avançadas

#### 6.1 Domínio Personalizado
1. **Vá** em "Settings" → "Domains"
2. **Adicione** seu domínio
3. **Configure** DNS apontando para Railway

#### 6.2 Monitoramento
- **Métricas** em tempo real
- **Logs** centralizados
- **Alertas** configuráveis

#### 6.3 Backup
- **Backup automático** do banco
- **Point-in-time recovery**
- **Export** manual disponível

## 🔧 Troubleshooting

### Problemas Comuns

#### Build Falha
```bash
# Verificar logs
railway logs

# Verificar Dockerfile
docker build -f Dockerfile.prod -t test .
```

#### Banco Não Conecta
```bash
# Verificar variáveis
railway variables

# Testar conexão
psql $DATABASE_URL
```

#### Aplicação Não Inicia
```bash
# Verificar logs
railway logs

# Verificar variáveis obrigatórias
railway variables
```

### Comandos Úteis

#### Railway CLI
```bash
# Instalar CLI
npm install -g @railway/cli

# Login
railway login

# Deploy local
railway up

# Ver logs
railway logs

# Ver variáveis
railway variables
```

## 📊 Monitoramento

### Métricas Importantes
- **CPU/Memory** usage
- **Response time**
- **Error rate**
- **Database connections**

### Logs
- **Application logs**
- **Build logs**
- **Deployment logs**

## 🔒 Segurança

### Checklist
- ✅ **JWT_KEY** forte (32+ caracteres)
- ✅ **HTTPS** habilitado
- ✅ **CORS** configurado
- ✅ **Database** com SSL
- ✅ **Environment** variables seguras

## 💰 Custos

### Railway Pricing
- **Hobby Plan**: $5/mês
- **Pro Plan**: $20/mês
- **Database**: Incluído no plano

### Limites Gratuitos
- **$5** de crédito mensal
- **500MB** de banco
- **1GB** de RAM

## 🎉 Próximos Passos

1. **Deploy** da aplicação
2. **Configurar** variáveis
3. **Testar** funcionalidades
4. **Configurar** domínio personalizado
5. **Monitorar** performance
