using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaidWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plaid_webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WebhookType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    WebhookCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ItemId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plaid_webhook_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plaid_webhook_events_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plaid_webhook_events_FamilyId_ProcessedAtUtc",
                table: "plaid_webhook_events",
                columns: new[] { "FamilyId", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_plaid_webhook_events_ItemId_ProcessedAtUtc",
                table: "plaid_webhook_events",
                columns: new[] { "ItemId", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_plaid_webhook_events_WebhookType_ProcessedAtUtc",
                table: "plaid_webhook_events",
                columns: new[] { "WebhookType", "ProcessedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plaid_webhook_events");
        }
    }
}
