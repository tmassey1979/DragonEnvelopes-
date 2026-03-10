using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportProjectionReplayRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_projection_replay_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProjectionSet = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromOccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ToOccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDryRun = table.Column<bool>(type: "boolean", nullable: false),
                    ResetState = table.Column<bool>(type: "boolean", nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    MaxEvents = table.Column<int>(type: "integer", nullable: false),
                    ThrottleMilliseconds = table.Column<int>(type: "integer", nullable: false),
                    TargetedEventCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessedEventCount = table.Column<int>(type: "integer", nullable: false),
                    AppliedCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    BatchesProcessed = table.Column<int>(type: "integer", nullable: false),
                    WasCappedByMaxEvents = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_projection_replay_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_projection_replay_runs_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_projection_replay_runs_FamilyId_StartedAtUtc",
                table: "report_projection_replay_runs",
                columns: new[] { "FamilyId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_report_projection_replay_runs_ProjectionSet_StartedAtUtc",
                table: "report_projection_replay_runs",
                columns: new[] { "ProjectionSet", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_report_projection_replay_runs_Status_StartedAtUtc",
                table: "report_projection_replay_runs",
                columns: new[] { "Status", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_projection_replay_runs");
        }
    }
}
