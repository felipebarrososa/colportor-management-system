using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Colportor.Api.Migrations
{
    /// <inheritdoc />
    public partial class WhatsAppTablesCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactObservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateTable(
                name: "ContactStatusHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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

            migrationBuilder.CreateTable(
                name: "WhatsAppConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastConnection = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastDisconnection = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QrCode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    QrCodeGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QrCodeExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SessionData = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppConnections_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionContactId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Sender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MediaType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WhatsAppMessageId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SentByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_MissionContacts_MissionContactId",
                        column: x => x.MissionContactId,
                        principalTable: "MissionContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WhatsAppMessages_Users_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AvailableVariables = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactObservations_CreatedAt",
                table: "ContactObservations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContactObservations_MissionContactId",
                table: "ContactObservations",
                column: "MissionContactId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactStatusHistories_ChangedAt",
                table: "ContactStatusHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContactStatusHistories_MissionContactId",
                table: "ContactStatusHistories",
                column: "MissionContactId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConnections_CreatedAt",
                table: "WhatsAppConnections",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConnections_CreatedByUserId",
                table: "WhatsAppConnections",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConnections_PhoneNumber",
                table: "WhatsAppConnections",
                column: "PhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppConnections_Status",
                table: "WhatsAppConnections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_MissionContactId",
                table: "WhatsAppMessages",
                column: "MissionContactId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_SentByUserId",
                table: "WhatsAppMessages",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_Status",
                table: "WhatsAppMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppMessages_Timestamp",
                table: "WhatsAppMessages",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_Category",
                table: "WhatsAppTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_CreatedAt",
                table: "WhatsAppTemplates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_CreatedByUserId",
                table: "WhatsAppTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppTemplates_IsActive",
                table: "WhatsAppTemplates",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactObservations");

            migrationBuilder.DropTable(
                name: "ContactStatusHistories");

            migrationBuilder.DropTable(
                name: "WhatsAppConnections");

            migrationBuilder.DropTable(
                name: "WhatsAppMessages");

            migrationBuilder.DropTable(
                name: "WhatsAppTemplates");
        }
    }
}
