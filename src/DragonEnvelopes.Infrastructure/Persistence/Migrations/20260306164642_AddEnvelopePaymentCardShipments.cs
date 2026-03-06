using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopePaymentCardShipments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "envelope_payment_card_shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AddressLine2 = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    City = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StateOrProvince = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Carrier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_envelope_payment_card_shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_shipments_envelope_payment_cards_Card~",
                        column: x => x.CardId,
                        principalTable: "envelope_payment_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_shipments_envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_shipments_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_shipments_CardId",
                table: "envelope_payment_card_shipments",
                column: "CardId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_shipments_EnvelopeId",
                table: "envelope_payment_card_shipments",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_shipments_FamilyId_EnvelopeId",
                table: "envelope_payment_card_shipments",
                columns: new[] { "FamilyId", "EnvelopeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "envelope_payment_card_shipments");
        }
    }
}
