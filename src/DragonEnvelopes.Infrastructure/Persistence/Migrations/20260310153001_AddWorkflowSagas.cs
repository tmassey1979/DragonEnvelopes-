using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowSagas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_sagas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrentStep = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CompensationAction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_sagas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_sagas_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_saga_timeline_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SagaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Step = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_saga_timeline_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workflow_saga_timeline_events_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workflow_saga_timeline_events_workflow_sagas_SagaId",
                        column: x => x.SagaId,
                        principalTable: "workflow_sagas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_saga_timeline_events_FamilyId_OccurredAtUtc",
                table: "workflow_saga_timeline_events",
                columns: new[] { "FamilyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_saga_timeline_events_SagaId_OccurredAtUtc",
                table: "workflow_saga_timeline_events",
                columns: new[] { "SagaId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_saga_timeline_events_WorkflowType_OccurredAtUtc",
                table: "workflow_saga_timeline_events",
                columns: new[] { "WorkflowType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_sagas_FamilyId_WorkflowType_UpdatedAtUtc",
                table: "workflow_sagas",
                columns: new[] { "FamilyId", "WorkflowType", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_sagas_Status_UpdatedAtUtc",
                table: "workflow_sagas",
                columns: new[] { "Status", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_workflow_sagas_WorkflowType_CorrelationId",
                table: "workflow_sagas",
                columns: new[] { "WorkflowType", "CorrelationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_saga_timeline_events");

            migrationBuilder.DropTable(
                name: "workflow_sagas");
        }
    }
}
