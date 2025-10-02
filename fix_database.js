// Script Node.js para conectar ao banco e executar o SQL
// Execute: node fix_database.js

const { Client } = require('pg');

const connectionString = 'postgresql://postgres:RNPzVecUtMNYkYRidaREmveXIZZHKsGD@caboose.proxy.rlwy.net:36144/railway';

async function fixDatabase() {
    const client = new Client({
        connectionString: connectionString
    });

    try {
        console.log('🔌 Conectando ao banco de dados...');
        await client.connect();
        console.log('✅ Conectado com sucesso!');

        // Verificar colunas existentes
        console.log('🔍 Verificando colunas existentes...');
        const checkColumns = await client.query(`
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'Colportors' 
            AND column_name IN ('Gender', 'BirthDate')
        `);
        
        console.log('Colunas encontradas:', checkColumns.rows);

        // Adicionar coluna Gender
        console.log('➕ Adicionando coluna Gender...');
        await client.query(`
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
        `);

        // Adicionar coluna BirthDate
        console.log('➕ Adicionando coluna BirthDate...');
        await client.query(`
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
        `);

        // Verificar colunas finais
        console.log('✅ Verificando colunas finais...');
        const finalCheck = await client.query(`
            SELECT column_name, data_type, is_nullable
            FROM information_schema.columns 
            WHERE table_name = 'Colportors' 
            AND column_name IN ('Gender', 'BirthDate')
            ORDER BY column_name
        `);
        
        console.log('Colunas finais:', finalCheck.rows);
        console.log('🎉 Script executado com sucesso!');
        console.log('📝 Agora você pode descomentar as colunas no código e fazer o deploy.');

    } catch (error) {
        console.error('❌ Erro:', error.message);
    } finally {
        await client.end();
    }
}

fixDatabase();
