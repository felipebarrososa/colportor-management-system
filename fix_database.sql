-- Script para adicionar as colunas Gender e BirthDate na tabela Colportors
-- Conecte ao banco usando: psql "postgresql://postgres:RNPzVecUtMNYkYRidaREmveXIZZHKsGD@caboose.proxy.rlwy.net:36144/railway"

-- Verificar se as colunas existem
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Colportors' 
AND column_name IN ('Gender', 'BirthDate');

-- Adicionar coluna Gender se não existir
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'Colportors' AND column_name = 'Gender') THEN
        ALTER TABLE "Colportors" ADD COLUMN "Gender" text;
        RAISE NOTICE 'Coluna Gender adicionada com sucesso';
    ELSE
        RAISE NOTICE 'Coluna Gender já existe';
    END IF;
END $$;

-- Adicionar coluna BirthDate se não existir
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_name = 'Colportors' AND column_name = 'BirthDate') THEN
        ALTER TABLE "Colportors" ADD COLUMN "BirthDate" timestamp with time zone;
        RAISE NOTICE 'Coluna BirthDate adicionada com sucesso';
    ELSE
        RAISE NOTICE 'Coluna BirthDate já existe';
    END IF;
END $$;

-- Verificar se as colunas foram criadas
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'Colportors' 
AND column_name IN ('Gender', 'BirthDate')
ORDER BY column_name;
