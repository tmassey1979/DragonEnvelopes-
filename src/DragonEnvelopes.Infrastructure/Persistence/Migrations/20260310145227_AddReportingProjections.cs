using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_envelope_balance_projections",
                columns: table => new
                {
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MonthlyBudget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    LastEventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastEventOccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_envelope_balance_projections", x => x.EnvelopeId);
                    table.ForeignKey(
                        name: "FK_report_envelope_balance_projections_envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_envelope_balance_projections_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_projection_applied_events",
                columns: table => new
                {
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoutingKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceService = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventOccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_projection_applied_events", x => x.OutboxMessageId);
                    table.ForeignKey(
                        name: "FK_report_projection_applied_events_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_projection_applied_events_integration_outbox_message~",
                        column: x => x.OutboxMessageId,
                        principalTable: "integration_outbox_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_transaction_projections",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TransferId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LastEventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LastEventOccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_transaction_projections", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_report_transaction_projections_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_transaction_projections_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_report_transaction_projections_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_envelope_balance_projections_FamilyId_EnvelopeName",
                table: "report_envelope_balance_projections",
                columns: new[] { "FamilyId", "EnvelopeName" });

            migrationBuilder.CreateIndex(
                name: "IX_report_envelope_balance_projections_FamilyId_IsArchived",
                table: "report_envelope_balance_projections",
                columns: new[] { "FamilyId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_report_envelope_balance_projections_LastEventOccurredAtUtc",
                table: "report_envelope_balance_projections",
                column: "LastEventOccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_report_projection_applied_events_EventId",
                table: "report_projection_applied_events",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_report_projection_applied_events_FamilyId_AppliedAtUtc",
                table: "report_projection_applied_events",
                columns: new[] { "FamilyId", "AppliedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_report_projection_applied_events_ProcessingStatus_AppliedAt~",
                table: "report_projection_applied_events",
                columns: new[] { "ProcessingStatus", "AppliedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_report_projection_applied_events_SourceService_RoutingKey_A~",
                table: "report_projection_applied_events",
                columns: new[] { "SourceService", "RoutingKey", "AppliedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_report_transaction_projections_AccountId",
                table: "report_transaction_projections",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_report_transaction_projections_FamilyId_Category_OccurredAt",
                table: "report_transaction_projections",
                columns: new[] { "FamilyId", "Category", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_report_transaction_projections_FamilyId_IsDeleted_OccurredAt",
                table: "report_transaction_projections",
                columns: new[] { "FamilyId", "IsDeleted", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_report_transaction_projections_FamilyId_OccurredAt",
                table: "report_transaction_projections",
                columns: new[] { "FamilyId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_report_transaction_projections_LastEventOccurredAtUtc",
                table: "report_transaction_projections",
                column: "LastEventOccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_envelope_balance_projections");

            migrationBuilder.DropTable(
                name: "report_projection_applied_events");

            migrationBuilder.DropTable(
                name: "report_transaction_projections");
        }
    }
}
