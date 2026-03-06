using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaidTransactionSyncPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plaid_account_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaidAccountId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plaid_account_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plaid_account_links_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plaid_account_links_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plaid_sync_cursors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cursor = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plaid_sync_cursors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plaid_sync_cursors_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plaid_synced_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaidTransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plaid_synced_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plaid_synced_transactions_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plaid_synced_transactions_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plaid_synced_transactions_transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plaid_account_links_AccountId",
                table: "plaid_account_links",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_plaid_account_links_FamilyId_AccountId",
                table: "plaid_account_links",
                columns: new[] { "FamilyId", "AccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plaid_account_links_FamilyId_PlaidAccountId",
                table: "plaid_account_links",
                columns: new[] { "FamilyId", "PlaidAccountId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plaid_sync_cursors_FamilyId",
                table: "plaid_sync_cursors",
                column: "FamilyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plaid_synced_transactions_AccountId",
                table: "plaid_synced_transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_plaid_synced_transactions_FamilyId_PlaidTransactionId",
                table: "plaid_synced_transactions",
                columns: new[] { "FamilyId", "PlaidTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_plaid_synced_transactions_FamilyId_TransactionId",
                table: "plaid_synced_transactions",
                columns: new[] { "FamilyId", "TransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_plaid_synced_transactions_TransactionId",
                table: "plaid_synced_transactions",
                column: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plaid_account_links");

            migrationBuilder.DropTable(
                name: "plaid_sync_cursors");

            migrationBuilder.DropTable(
                name: "plaid_synced_transactions");
        }
    }
}
