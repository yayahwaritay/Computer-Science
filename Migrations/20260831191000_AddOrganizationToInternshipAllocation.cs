using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompSci.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationToInternshipAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationUserId",
                table: "InternshipAllocations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_InternshipAllocations_OrganizationUserId",
                table: "InternshipAllocations",
                column: "OrganizationUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InternshipAllocations_OrganizationUserId",
                table: "InternshipAllocations");

            migrationBuilder.DropColumn(
                name: "OrganizationUserId",
                table: "InternshipAllocations");
        }
    }
}
