-- Script para resolver conflito de migrations no banco de produção
-- Execute este script no PostgreSQL de produção

-- 1. Marcar todas as migrations existentes como aplicadas
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES 
    ('202409290001_Initial', '8.0.8'),
    ('20251001124027_AddLeaderPersonalData', '8.0.8'),
    ('20251001193944_AddPacEnrollmentsToColportor', '8.0.8'),
    ('20251001212504_FixColportorLeaderRelationship', '8.0.8'),
    ('20251001213603_RemoveColportorCountryProperties', '8.0.8'),
    ('20251002124656_AddGenderAndBirthDateToColportors', '8.0.8'),
    ('20251002130658_AddGenderBirthDateSimple', '8.0.8'),
    ('20251013220000_AddPhotosTable', '8.0.8')
ON CONFLICT ("MigrationId") DO NOTHING;

-- 2. Verificar quais migrations estão marcadas
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId";

-- 3. Verificar se as tabelas do WhatsApp existem
SELECT 
    CASE WHEN EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WhatsAppConnections') 
         THEN 'WhatsAppConnections: EXISTE' 
         ELSE 'WhatsAppConnections: NÃO EXISTE' END as status_whatsapp_connections,
    CASE WHEN EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WhatsAppMessages') 
         THEN 'WhatsAppMessages: EXISTE' 
         ELSE 'WhatsAppMessages: NÃO EXISTE' END as status_whatsapp_messages,
    CASE WHEN EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WhatsAppTemplates') 
         THEN 'WhatsAppTemplates: EXISTE' 
         ELSE 'WhatsAppTemplates: NÃO EXISTE' END as status_whatsapp_templates;
