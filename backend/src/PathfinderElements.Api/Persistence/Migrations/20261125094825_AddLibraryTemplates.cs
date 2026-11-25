using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderElements.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    armor_class = table.Column<int>(type: "integer", nullable: false),
                    flat_footed_armor_class = table.Column<int>(type: "integer", nullable: false),
                    fortitude_save = table.Column<int>(type: "integer", nullable: false),
                    initiative_modifier = table.Column<int>(type: "integer", nullable: false),
                    max_hit_points = table.Column<int>(type: "integer", nullable: false),
                    reflex_save = table.Column<int>(type: "integer", nullable: false),
                    touch_armor_class = table.Column<int>(type: "integer", nullable: false),
                    will_save = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_templates", x => x.id);
                    table.CheckConstraint("ck_character_templates_max_hit_points", "max_hit_points > 0");
                    table.ForeignKey(
                        name: "FK_character_templates_AspNetUsers_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "monster_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    armor_class = table.Column<int>(type: "integer", nullable: false),
                    flat_footed_armor_class = table.Column<int>(type: "integer", nullable: false),
                    fortitude_save = table.Column<int>(type: "integer", nullable: false),
                    initiative_modifier = table.Column<int>(type: "integer", nullable: false),
                    max_hit_points = table.Column<int>(type: "integer", nullable: false),
                    reflex_save = table.Column<int>(type: "integer", nullable: false),
                    touch_armor_class = table.Column<int>(type: "integer", nullable: false),
                    will_save = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_monster_templates", x => x.id);
                    table.CheckConstraint("ck_monster_templates_max_hit_points", "max_hit_points > 0");
                    table.ForeignKey(
                        name: "FK_monster_templates_AspNetUsers_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_templates_owner_user_id",
                table: "character_templates",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_monster_templates_owner_user_id",
                table: "monster_templates",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_templates");

            migrationBuilder.DropTable(
                name: "monster_templates");
        }
    }
}
