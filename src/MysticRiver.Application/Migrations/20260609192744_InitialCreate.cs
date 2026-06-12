using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MysticRiver.Application.Migrations;
/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BattleSessions",
            columns: table => new
            {
                BattleId = table.Column<string>(type: "text", nullable: false),
                HostPlayerId = table.Column<string>(type: "text", nullable: true),
                GuestPlayerId = table.Column<string>(type: "text", nullable: true),
                MatchStatus = table.Column<int>(type: "integer", nullable: false),
                RoundNumber = table.Column<int>(type: "integer", nullable: false),
                StateVersion = table.Column<int>(type: "integer", nullable: false),
                EnemyAttackPower = table.Column<int>(type: "integer", nullable: false),
                CurrentTurnCreatureId = table.Column<string>(type: "text", nullable: true),
                ForcedWinnerCreatureId = table.Column<string>(type: "text", nullable: true),
                ForcedEndReason = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BattleSessions", x => x.BattleId);
            });

        migrationBuilder.CreateTable(
            name: "CreatureSnapshots",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                BattleId = table.Column<string>(type: "text", nullable: false),
                CreatureId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Hp = table.Column<int>(type: "integer", nullable: false),
                MaxHp = table.Column<int>(type: "integer", nullable: false),
                Mana = table.Column<int>(type: "integer", nullable: false),
                MaxMana = table.Column<int>(type: "integer", nullable: false),
                Shield = table.Column<int>(type: "integer", nullable: false),
                StatusEffectsJson = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CreatureSnapshots", x => x.Id);
                table.ForeignKey(
                    name: "FK_CreatureSnapshots_BattleSessions_BattleId",
                    column: x => x.BattleId,
                    principalTable: "BattleSessions",
                    principalColumn: "BattleId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CreatureSnapshots_BattleId_CreatureId",
            table: "CreatureSnapshots",
            columns: new[] { "BattleId", "CreatureId" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CreatureSnapshots");

        migrationBuilder.DropTable(
            name: "BattleSessions");
    }
}
