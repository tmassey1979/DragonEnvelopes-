using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "family_approval_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AmountThreshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RolesRequiringApprovalCsv = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_approval_policies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_family_approval_policies_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_approval_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedByRole = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Merchant = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RequestNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ResolvedByRole = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ApprovedTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_approval_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_approval_requests_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_approval_requests_envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_purchase_approval_requests_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_approval_requests_transactions_ApprovedTransaction~",
                        column: x => x.ApprovedTransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "purchase_approval_timeline_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_approval_timeline_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_approval_timeline_events_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_approval_timeline_events_purchase_approval_request~",
                        column: x => x.ApprovalRequestId,
                        principalTable: "purchase_approval_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_family_approval_policies_FamilyId",
                table: "family_approval_policies",
                column: "FamilyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_requests_AccountId",
                table: "purchase_approval_requests",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_requests_ApprovedTransactionId",
                table: "purchase_approval_requests",
                column: "ApprovedTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_requests_EnvelopeId",
                table: "purchase_approval_requests",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_requests_FamilyId_Status_CreatedAtUtc",
                table: "purchase_approval_requests",
                columns: new[] { "FamilyId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_timeline_events_ApprovalRequestId_Occurre~",
                table: "purchase_approval_timeline_events",
                columns: new[] { "ApprovalRequestId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_approval_timeline_events_FamilyId_OccurredAtUtc",
                table: "purchase_approval_timeline_events",
                columns: new[] { "FamilyId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "family_approval_policies");

            migrationBuilder.DropTable(
                name: "purchase_approval_timeline_events");

            migrationBuilder.DropTable(
                name: "purchase_approval_requests");
        }
    }
}
