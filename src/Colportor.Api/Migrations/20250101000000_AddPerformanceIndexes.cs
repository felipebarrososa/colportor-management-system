using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Colportor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Índices para Users
            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RegionId",
                table: "Users",
                column: "RegionId");

            // Índices para Colportors
            migrationBuilder.CreateIndex(
                name: "IX_Colportors_CPF",
                table: "Colportors",
                column: "CPF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_RegionId",
                table: "Colportors",
                column: "RegionId");

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_LeaderId",
                table: "Colportors",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_Gender",
                table: "Colportors",
                column: "Gender");

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_FullName",
                table: "Colportors",
                column: "FullName");

            // Índices para PacEnrollments
            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_ColportorId",
                table: "PacEnrollments",
                column: "ColportorId");

            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_LeaderId",
                table: "PacEnrollments",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_StartDate",
                table: "PacEnrollments",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_EndDate",
                table: "PacEnrollments",
                column: "EndDate");

            // Índice composto para consultas de calendário
            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_DateRange",
                table: "PacEnrollments",
                columns: new[] { "StartDate", "EndDate" });

            // Índices para Visits
            migrationBuilder.CreateIndex(
                name: "IX_Visits_ColportorId",
                table: "Visits",
                column: "ColportorId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_Date",
                table: "Visits",
                column: "Date");

            // Índices para Regions
            migrationBuilder.CreateIndex(
                name: "IX_Regions_CountryId",
                table: "Regions",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Regions_Name",
                table: "Regions",
                column: "Name");

            // Índice composto para consultas de região por país
            migrationBuilder.CreateIndex(
                name: "IX_Regions_CountryId_Name",
                table: "Regions",
                columns: new[] { "CountryId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover índices na ordem inversa
            migrationBuilder.DropIndex(
                name: "IX_Regions_CountryId_Name",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Regions_Name",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Regions_CountryId",
                table: "Regions");

            migrationBuilder.DropIndex(
                name: "IX_Visits_Date",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Visits_ColportorId",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_DateRange",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_EndDate",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_StartDate",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_LeaderId",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_ColportorId",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_FullName",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_Gender",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_LeaderId",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_RegionId",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_CPF",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Users_RegionId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");
        }
    }
}
