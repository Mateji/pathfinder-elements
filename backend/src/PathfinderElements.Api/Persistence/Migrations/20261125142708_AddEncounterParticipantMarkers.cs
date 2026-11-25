using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderElements.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEncounterParticipantMarkers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_encounter_participants_encounter_id",
                table: "encounter_participants");

            migrationBuilder.AddColumn<int>(
                name: "next_participant_number",
                table: "prepared_encounters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "color",
                table: "encounter_participants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "number",
                table: "encounter_participants",
                type: "integer",
                nullable: true);

            // Existing rows have no insertion order. Approximate their previous visible list order and break ties by ID.
            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT participant.id, row_number() OVER (
                        PARTITION BY participant.encounter_id
                        ORDER BY participant.character_template_id IS NULL,
                            COALESCE(character_template.name, monster_template.name), participant.id
                    )::integer AS number
                    FROM encounter_participants AS participant
                    LEFT JOIN character_templates AS character_template ON character_template.id = participant.character_template_id
                    LEFT JOIN monster_templates AS monster_template ON monster_template.id = participant.monster_template_id
                )
                UPDATE encounter_participants AS participant
                SET number = numbered.number,
                    color = (ARRAY['#C8192F', '#0057D9', '#FFC400', '#007A3D', '#7231B8', '#E86000', '#008EA1', '#C20876'])
                        [1 + (numbered.number - 1) % 8]
                FROM numbered WHERE numbered.id = participant.id;

                UPDATE prepared_encounters SET next_participant_number = 1;
                UPDATE prepared_encounters AS encounter
                SET next_participant_number = numbered.next_number
                FROM (
                    SELECT encounter_id, MAX(number) + 1 AS next_number
                    FROM encounter_participants GROUP BY encounter_id
                ) AS numbered
                WHERE numbered.encounter_id = encounter.id;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "next_participant_number",
                table: "prepared_encounters",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "color",
                table: "encounter_participants",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "number",
                table: "encounter_participants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_prepared_encounters_next_participant_number",
                table: "prepared_encounters",
                sql: "next_participant_number > 0");

            migrationBuilder.CreateIndex(
                name: "IX_encounter_participants_encounter_id_number",
                table: "encounter_participants",
                columns: new[] { "encounter_id", "number" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_encounter_participants_color",
                table: "encounter_participants",
                sql: "color ~ '^#[0-9A-F]{6}$'");

            migrationBuilder.AddCheckConstraint(
                name: "ck_encounter_participants_number",
                table: "encounter_participants",
                sql: "number > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_prepared_encounters_next_participant_number",
                table: "prepared_encounters");

            migrationBuilder.DropIndex(
                name: "IX_encounter_participants_encounter_id_number",
                table: "encounter_participants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_encounter_participants_color",
                table: "encounter_participants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_encounter_participants_number",
                table: "encounter_participants");

            migrationBuilder.DropColumn(
                name: "next_participant_number",
                table: "prepared_encounters");

            migrationBuilder.DropColumn(
                name: "color",
                table: "encounter_participants");

            migrationBuilder.DropColumn(
                name: "number",
                table: "encounter_participants");

            migrationBuilder.CreateIndex(
                name: "IX_encounter_participants_encounter_id",
                table: "encounter_participants",
                column: "encounter_id");
        }
    }
}
