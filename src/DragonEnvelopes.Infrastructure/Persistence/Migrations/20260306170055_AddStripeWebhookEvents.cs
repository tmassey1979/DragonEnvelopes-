using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeWebhookEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stripe_webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CardId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_webhook_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stripe_webhook_events_envelope_payment_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "envelope_payment_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stripe_webhook_events_envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_stripe_webhook_events_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stripe_webhook_events_CardId",
                table: "stripe_webhook_events",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_webhook_events_EnvelopeId",
                table: "stripe_webhook_events",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_stripe_webhook_events_EventId",
                table: "stripe_webhook_events",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stripe_webhook_events_EventType_ProcessedAtUtc",
                table: "stripe_webhook_events",
                columns: new[] { "EventType", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_stripe_webhook_events_FamilyId",
                table: "stripe_webhook_events",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stripe_webhook_events");
        }
    }
}
