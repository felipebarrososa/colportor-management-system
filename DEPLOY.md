# 🚀 Guia de Deploy para Produção

## Opções de Deploy

### 1. Railway (Recomendado) - Mais Fácil

#### Passo a Passo:
1. **Acesse**: https://railway.app
2. **Conecte** sua conta GitHub
3. **Clique** em "New Project" → "Deploy from GitHub repo"
4. **Selecione** o repositório `colportor-management-system`
5. **Railway detectará** automaticamente o Docker

#### Configurar Variáveis de Ambiente:
```bash
POSTGRES_PASSWORD=senha_super_segura_123
JWT_KEY=chave_jwt_minima_32_caracteres_para_seguranca
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=seu_email@gmail.com
SMTP_PASSWORD=sua_senha_de_app
SMTP_FROM=no-reply@seudominio.com
```

#### Adicionar Banco PostgreSQL:
1. **Clique** em "New" → "Database" → "PostgreSQL"
2. **Railway criará** automaticamente as variáveis de conexão
3. **Atualize** a `ConnectionStrings__Default` se necessário

### 2. Render - Alternativa Gratuita

#### Passo a Passo:
1. **Acesse**: https://render.com
2. **Conecte** GitHub e selecione o repositório
3. **Escolha** "Web Service"
4. **Configure**:
   - **Build Command**: `docker build -f Dockerfile.prod -t colportor .`
   - **Start Command**: `docker run -p 10000:8080 colportor`
   - **Port**: 8080

#### Adicionar Banco:
1. **Crie** um "PostgreSQL" service
2. **Copie** a connection string
3. **Adicione** como variável de ambiente

### 3. Vercel + Backend Separado

#### Frontend no Vercel:
1. **Acesse**: https://vercel.com
2. **Importe** o repositório
3. **Configure**:
   - **Framework Preset**: Other
   - **Build Command**: `echo "Static files only"`
   - **Output Directory**: `src/Colportor.Api/wwwroot`

#### Backend em Railway/Render:
- Use as opções 1 ou 2 acima
- **Atualize** as URLs no frontend

## 🔧 Configurações Importantes

### Variáveis de Ambiente Obrigatórias:
```bash
# Banco de dados
POSTGRES_PASSWORD=senha_forte
ConnectionStrings__Default=Host=db;Port=5432;Database=colportor;Username=colp;Password=senha_forte

# JWT (mínimo 32 caracteres)
JWT_KEY=chave_super_secreta_minima_32_caracteres

# Email (opcional)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=seu_email@gmail.com
SMTP_PASSWORD=sua_senha_de_app
SMTP_FROM=no-reply@seudominio.com
```

### Domínio Personalizado:
1. **Compre** um domínio (ex: GoDaddy, Namecheap)
2. **Configure** DNS apontando para o serviço
3. **Adicione** certificado SSL (automático na maioria dos serviços)

## 🐳 Comandos Docker Locais

### Testar localmente:
```bash
# Usar docker-compose de produção
docker-compose -f docker-compose.prod.yml up --build

# Ou build manual
docker build -f Dockerfile.prod -t colportor-prod .
docker run -p 8080:8080 colportor-prod
```

### Logs e Debug:
```bash
# Ver logs
docker-compose -f docker-compose.prod.yml logs -f

# Entrar no container
docker exec -it colportor_api_1 /bin/bash
```

## 📊 Monitoramento

### Métricas Importantes:
- **CPU/Memory** usage
- **Response time** da API
- **Database** connections
- **Error rate**

### Logs:
- **Application logs** no console
- **Database logs** no serviço de banco
- **Error tracking** (opcional: Sentry)

## 🔒 Segurança

### Checklist:
- ✅ **Senhas fortes** para banco e JWT
- ✅ **HTTPS** habilitado
- ✅ **CORS** configurado corretamente
- ✅ **Rate limiting** (opcional)
- ✅ **Backup** do banco de dados
- ✅ **Monitoramento** de erros

## 🚨 Troubleshooting

### Problemas Comuns:
1. **Build falha**: Verificar Dockerfile e dependências
2. **Banco não conecta**: Verificar variáveis de ambiente
3. **Email não envia**: Verificar credenciais SMTP
4. **Frontend não carrega**: Verificar CORS e URLs

### Logs Úteis:
```bash
# Railway
railway logs

# Render
# Ver na dashboard

# Docker local
docker logs container_name
```
