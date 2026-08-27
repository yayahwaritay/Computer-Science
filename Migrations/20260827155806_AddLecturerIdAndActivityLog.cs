using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompSci.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturerIdAndActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LecturerId",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Backfill existing Lecturer accounts (Role = 1) with a sequential LecturerId, ordered
            // by CreatedAt, so "every lecturer" applies retroactively and not just going forward.
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT ""Id"", ROW_NUMBER() OVER (ORDER BY ""CreatedAt"") AS rn
                    FROM ""Users""
                    WHERE ""Role"" = 1
                )
                UPDATE ""Users"" u
                SET ""LecturerId"" = 'LEC-' || LPAD(numbered.rn::text, 4, '0')
                FROM numbered
                WHERE u.""Id"" = numbered.""Id"";
            ");

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LecturerId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_LecturerId",
                table: "Users",
                column: "LecturerId",
                unique: true,
                filter: "\"LecturerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_EntityType",
                table: "ActivityLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_Timestamp",
                table: "ActivityLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropIndex(
                name: "IX_Users_LecturerId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LecturerId",
                table: "Users");
        }
    }
}
