using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Colportor.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixColportorLeaderRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Colportors_Regions_RegionId",
                table: "Colportors");

            migrationBuilder.DropForeignKey(
                name: "FK_Colportors_Users_LeaderId1",
                table: "Colportors");

            migrationBuilder.RenameColumn(
                name: "LeaderId1",
                table: "Colportors",
                newName: "RegionId1");

            migrationBuilder.RenameIndex(
                name: "IX_Colportors_LeaderId1",
                table: "Colportors",
                newName: "IX_Colportors_RegionId1");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropIndex(
                name: "IX_Colportors_LeaderId",
                table: "Colportors");

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
        }
    }
}
