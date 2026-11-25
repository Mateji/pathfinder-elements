using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderElements.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryImportReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "library_import_receipts",
                columns: table => new
                {
                    owner_user_id = table.Column<string>(type: "text", nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_library_import_receipts", x => new { x.owner_user_id, x.fingerprint });
                    table.ForeignKey(
                        name: "FK_library_import_receipts_AspNetUsers_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "library_import_receipts");
        }
    }
}
