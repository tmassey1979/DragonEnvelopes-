using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvelopeTransferTransactionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransferCounterpartyEnvelopeId",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransferDirection",
                table: "transactions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_TransferCounterpartyEnvelopeId",
                table: "transactions",
                column: "TransferCounterpartyEnvelopeId");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_TransferId",
                table: "transactions",
                column: "TransferId");

            migrationBuilder.AddForeignKey(
                name: "FK_transactions_envelopes_TransferCounterpartyEnvelopeId",
                table: "transactions",
                column: "TransferCounterpartyEnvelopeId",
                principalTable: "envelopes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_envelopes_TransferCounterpartyEnvelopeId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_TransferCounterpartyEnvelopeId",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "IX_transactions_TransferId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "TransferCounterpartyEnvelopeId",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "TransferDirection",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "transactions");
        }
    }
}
