using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaidBalanceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plaid_balance_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaidAccountId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InternalBalanceBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProviderBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InternalBalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DriftAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefreshedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plaid_balance_snapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_plaid_balance_snapshots_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_plaid_balance_snapshots_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plaid_balance_snapshots_AccountId",
                table: "plaid_balance_snapshots",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_plaid_balance_snapshots_FamilyId_AccountId_RefreshedAtUtc",
                table: "plaid_balance_snapshots",
                columns: new[] { "FamilyId", "AccountId", "RefreshedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_plaid_balance_snapshots_FamilyId_RefreshedAtUtc",
                table: "plaid_balance_snapshots",
                columns: new[] { "FamilyId", "RefreshedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plaid_balance_snapshots");
        }
    }
}
