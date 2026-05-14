using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PH.DbAppSettings.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                schema: "dbo",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, defaultValue: "Production"),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => new { x.Key, x.Environment });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings",
                schema: "dbo");
        }
    }
}
