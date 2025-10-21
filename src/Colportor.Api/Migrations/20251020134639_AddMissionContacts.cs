using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Colportor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Regions_Countries_CountryId1",
                table: "Regions");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Regions_CountryId1",
                table: "Regions");

            migrationBuilder.DropColumn(
                name: "CountryId1",
                table: "Regions");

            migrationBuilder.CreateTable(
                name: "MissionContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegionId = table.Column<int>(type: "integer", nullable: false),
                    LeaderId = table.Column<int>(type: "integer", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedByColportorId = table.Column<int>(type: "integer", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaritalStatus = table.Column<string>(type: "text", nullable: true),
                    Nationality = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Profession = table.Column<string>(type: "text", nullable: true),
                    SpeaksOtherLanguages = table.Column<bool>(type: "boolean", nullable: false),
                    OtherLanguages = table.Column<string>(type: "text", nullable: true),
                    FluencyLevel = table.Column<string>(type: "text", nullable: true),
                    Church = table.Column<string>(type: "text", nullable: true),
                    ConversionTime = table.Column<string>(type: "text", nullable: true),
                    MissionsDedicationPlan = table.Column<string>(type: "text", nullable: true),
                    HasPassport = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LastContactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextFollowUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionContacts_Colportors_CreatedByColportorId",
                        column: x => x.CreatedByColportorId,
                        principalTable: "Colportors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MissionContacts_Regions_RegionId",
                        column: x => x.RegionId,
                        principalTable: "Regions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionContacts_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MissionContacts_Users_LeaderId",
                        column: x => x.LeaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionContacts_CreatedAt",
                table: "MissionContacts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MissionContacts_CreatedByColportorId",
                table: "MissionContacts",
                column: "CreatedByColportorId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionContacts_CreatedByUserId",
                table: "MissionContacts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionContacts_LeaderId",
                table: "MissionContacts",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionContacts_RegionId",
                table: "MissionContacts",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionContacts_Status",
                table: "MissionContacts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionContacts");

            migrationBuilder.AddColumn<int>(
                name: "CountryId1",
                table: "Regions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ColportorId = table.Column<int>(type: "integer", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_Colportors_ColportorId",
                        column: x => x.ColportorId,
                        principalTable: "Colportors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Regions_CountryId1",
                table: "Regions",
                column: "CountryId1");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ColportorId",
                table: "Photos",
                column: "ColportorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Regions_Countries_CountryId1",
                table: "Regions",
                column: "CountryId1",
                principalTable: "Countries",
                principalColumn: "Id");
        }
    }
}
