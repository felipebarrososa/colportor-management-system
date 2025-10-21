using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Colportor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddContactObservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactObservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionContactId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactObservations_MissionContacts_MissionContactId",
                        column: x => x.MissionContactId,
                        principalTable: "MissionContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactObservations_MissionContactId",
                table: "ContactObservations",
                column: "MissionContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactObservations_CreatedAt",
                table: "ContactObservations",
                column: "CreatedAt");

            migrationBuilder.CreateTable(
                name: "ContactStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionContactId = table.Column<int>(type: "integer", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactStatusHistories_MissionContacts_MissionContactId",
                        column: x => x.MissionContactId,
                        principalTable: "MissionContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactStatusHistories_MissionContactId",
                table: "ContactStatusHistories",
                column: "MissionContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactStatusHistories_ChangedAt",
                table: "ContactStatusHistories",
                column: "ChangedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactStatusHistories");

            migrationBuilder.DropTable(
                name: "ContactObservations");
        }
    }
}
