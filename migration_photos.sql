-- Migração para criar tabela Photos
CREATE TABLE IF NOT EXISTS "Photos" (
    "Id" SERIAL PRIMARY KEY,
    "FileName" TEXT NOT NULL,
    "ContentType" TEXT NOT NULL,
    "Data" BYTEA NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ColportorId" INTEGER NULL
);

-- Criar índice para ColportorId
CREATE INDEX IF NOT EXISTS "IX_Photos_ColportorId" ON "Photos" ("ColportorId");

-- Adicionar foreign key para Colportors
ALTER TABLE "Photos" 
ADD CONSTRAINT "FK_Photos_Colportors_ColportorId" 
FOREIGN KEY ("ColportorId") 
REFERENCES "Colportors" ("Id") 
ON DELETE SET NULL;
