using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Colportor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderBirthDateSimple : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adicionar coluna Gender se não existir
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Colportors' AND column_name = 'Gender') THEN
                        ALTER TABLE ""Colportors"" ADD COLUMN ""Gender"" text;
                    END IF;
                END $$;
            ");

            // Adicionar coluna BirthDate se não existir
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                   WHERE table_name = 'Colportors' AND column_name = 'BirthDate') THEN
                        ALTER TABLE ""Colportors"" ADD COLUMN ""BirthDate"" timestamp with time zone;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover coluna Gender se existir
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'Colportors' AND column_name = 'Gender') THEN
                        ALTER TABLE ""Colportors"" DROP COLUMN ""Gender"";
                    END IF;
                END $$;
            ");

            // Remover coluna BirthDate se existir
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name = 'Colportors' AND column_name = 'BirthDate') THEN
                        ALTER TABLE ""Colportors"" DROP COLUMN ""BirthDate"";
                    END IF;
                END $$;
            ");
        }
    }
}
