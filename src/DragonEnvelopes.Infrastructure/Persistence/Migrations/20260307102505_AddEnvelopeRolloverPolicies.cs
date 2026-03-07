using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopeRolloverPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RolloverCap",
                table: "envelopes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolloverMode",
                table: "envelopes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.CreateTable(
                name: "envelope_rollover_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Month = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EnvelopeCount = table.Column<int>(type: "integer", nullable: false),
                    TotalRolloverBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_envelope_rollover_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_envelope_rollover_runs_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_envelope_rollover_runs_FamilyId_Month",
                table: "envelope_rollover_runs",
                columns: new[] { "FamilyId", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "envelope_rollover_runs");

            migrationBuilder.DropColumn(
                name: "RolloverCap",
                table: "envelopes");

            migrationBuilder.DropColumn(
                name: "RolloverMode",
                table: "envelopes");
        }
    }
}
