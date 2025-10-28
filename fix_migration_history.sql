-- Script para marcar migrations existentes como aplicadas no banco de produção
-- Execute este script no PostgreSQL de produção ANTES do próximo deploy

-- Inserir migrations que já existem no banco mas não estão no histórico
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

-- Verificar migrations aplicadas
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId";
