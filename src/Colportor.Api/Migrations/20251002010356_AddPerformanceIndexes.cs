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
            migrationBuilder.DropForeignKey(
                name: "FK_Colportors_Regions_RegionId",
                table: "Colportors");

            migrationBuilder.DropForeignKey(
                name: "FK_Colportors_Regions_RegionId1",
                table: "Colportors");

            migrationBuilder.DropForeignKey(
                name: "FK_Colportors_Users_LeaderId",
                table: "Colportors");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Regions_RegionId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Regions_RegionId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RegionId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_LeaderId",
                table: "Colportors");

            migrationBuilder.DropColumn(
                name: "RegionId1",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "RegionId1",
                table: "Colportors",
                newName: "LeaderId1");

            migrationBuilder.RenameIndex(
                name: "IX_Colportors_RegionId1",
                table: "Colportors",
                newName: "IX_Colportors_LeaderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Colportors_Regions_RegionId",
                table: "Colportors",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Colportors_Users_LeaderId1",
                table: "Colportors",
                column: "LeaderId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Regions_RegionId",
                table: "Users",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id");

            // Adicionar índices para performance
            migrationBuilder.CreateIndex(
                name: "IX_Colportors_LeaderId",
                table: "Colportors",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_City",
                table: "Colportors",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_CPF",
                table: "Colportors",
                column: "CPF");

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_LastVisitDate",
                table: "Colportors",
                column: "LastVisitDate");

            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_ColportorId",
                table: "PacEnrollments",
                column: "ColportorId");

            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_LeaderId",
                table: "PacEnrollments",
                column: "LeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PacEnrollments_Status",
                table: "PacEnrollments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover índices de performance
            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_Status",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_LeaderId",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_PacEnrollments_ColportorId",
                table: "PacEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_LastVisitDate",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_CPF",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_City",
                table: "Colportors");

            migrationBuilder.DropIndex(
                name: "IX_Colportors_LeaderId",
                table: "Colportors");

            migrationBuilder.DropForeignKey(
                name: "FK_Colportors_Regions_RegionId",
                table: "Colportors");

            migrationBuilder.DropForeignKey(
                name: "FK_Colportors_Users_LeaderId1",
                table: "Colportors");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Regions_RegionId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "LeaderId1",
                table: "Colportors",
                newName: "RegionId1");

            migrationBuilder.RenameIndex(
                name: "IX_Colportors_LeaderId1",
                table: "Colportors",
                newName: "IX_Colportors_RegionId1");

            migrationBuilder.AddColumn<int>(
                name: "RegionId1",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RegionId1",
                table: "Users",
                column: "RegionId1");

            migrationBuilder.CreateIndex(
                name: "IX_Colportors_LeaderId",
                table: "Colportors",
                column: "LeaderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Colportors_Regions_RegionId",
                table: "Colportors",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Colportors_Regions_RegionId1",
                table: "Colportors",
                column: "RegionId1",
                principalTable: "Regions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Colportors_Users_LeaderId",
                table: "Colportors",
                column: "LeaderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Regions_RegionId",
                table: "Users",
                column: "RegionId",
                principalTable: "Regions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Regions_RegionId1",
                table: "Users",
                column: "RegionId1",
                principalTable: "Regions",
                principalColumn: "Id");
        }
    }
}
