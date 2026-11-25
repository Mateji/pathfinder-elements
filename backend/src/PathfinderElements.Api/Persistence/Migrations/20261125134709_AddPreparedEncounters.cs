using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderElements.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreparedEncounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "prepared_encounters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prepared_encounters", x => x.id);
                    table.ForeignKey(
                        name: "FK_prepared_encounters_AspNetUsers_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "encounter_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    encounter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    monster_template_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_encounter_participants", x => x.id);
                    table.CheckConstraint("ck_encounter_participants_one_template", "(character_template_id IS NULL) <> (monster_template_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_encounter_participants_character_templates_character_templa~",
                        column: x => x.character_template_id,
                        principalTable: "character_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_encounter_participants_monster_templates_monster_template_id",
                        column: x => x.monster_template_id,
                        principalTable: "monster_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_encounter_participants_prepared_encounters_encounter_id",
                        column: x => x.encounter_id,
                        principalTable: "prepared_encounters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_encounter_participants_character_template_id",
                table: "encounter_participants",
                column: "character_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_encounter_participants_encounter_id",
                table: "encounter_participants",
                column: "encounter_id");

            migrationBuilder.CreateIndex(
                name: "IX_encounter_participants_monster_template_id",
                table: "encounter_participants",
                column: "monster_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_prepared_encounters_owner_user_id",
                table: "prepared_encounters",
                column: "owner_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "encounter_participants");

            migrationBuilder.DropTable(
                name: "prepared_encounters");
        }
    }
}
