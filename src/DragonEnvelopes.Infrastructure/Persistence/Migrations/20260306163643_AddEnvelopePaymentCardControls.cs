using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopePaymentCardControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "envelope_payment_card_control_audits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PreviousStateJson = table.Column<string>(type: "jsonb", nullable: true),
                    NewStateJson = table.Column<string>(type: "jsonb", nullable: false),
                    ChangedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_envelope_payment_card_control_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_control_audits_envelope_payment_cards~",
                        column: x => x.CardId,
                        principalTable: "envelope_payment_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_control_audits_envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_control_audits_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "envelope_payment_card_controls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    DailyLimitAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AllowedMerchantCategoriesJson = table.Column<string>(type: "jsonb", nullable: true),
                    AllowedMerchantNamesJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_envelope_payment_card_controls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_controls_envelope_payment_cards_CardId",
                        column: x => x.CardId,
                        principalTable: "envelope_payment_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_controls_envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_card_controls_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_control_audits_CardId_ChangedAtUtc",
                table: "envelope_payment_card_control_audits",
                columns: new[] { "CardId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_control_audits_EnvelopeId",
                table: "envelope_payment_card_control_audits",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_control_audits_FamilyId",
                table: "envelope_payment_card_control_audits",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_controls_CardId",
                table: "envelope_payment_card_controls",
                column: "CardId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_controls_EnvelopeId",
                table: "envelope_payment_card_controls",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_card_controls_FamilyId_EnvelopeId",
                table: "envelope_payment_card_controls",
                columns: new[] { "FamilyId", "EnvelopeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "envelope_payment_card_control_audits");

            migrationBuilder.DropTable(
                name: "envelope_payment_card_controls");
        }
    }
}
