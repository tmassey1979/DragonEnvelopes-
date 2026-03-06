using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopePaymentCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "envelope_payment_cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeFinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderCardId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Brand = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Last4 = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_envelope_payment_cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_envelope_payment_cards_envelope_financial_accounts_Envelope~",
                        column: x => x.EnvelopeFinancialAccountId,
                        principalTable: "envelope_financial_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_cards_envelopes_EnvelopeId",
                        column: x => x.EnvelopeId,
                        principalTable: "envelopes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_envelope_payment_cards_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_cards_EnvelopeFinancialAccountId",
                table: "envelope_payment_cards",
                column: "EnvelopeFinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_cards_EnvelopeId",
                table: "envelope_payment_cards",
                column: "EnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_cards_FamilyId_EnvelopeId",
                table: "envelope_payment_cards",
                columns: new[] { "FamilyId", "EnvelopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_envelope_payment_cards_Provider_ProviderCardId",
                table: "envelope_payment_cards",
                columns: new[] { "Provider", "ProviderCardId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "envelope_payment_cards");
        }
    }
}
