using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathfinderElements.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMonsterAttackSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "attack_sequences",
                table: "monster_templates",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "attack_sequences",
                table: "monster_templates");
        }
    }
}
