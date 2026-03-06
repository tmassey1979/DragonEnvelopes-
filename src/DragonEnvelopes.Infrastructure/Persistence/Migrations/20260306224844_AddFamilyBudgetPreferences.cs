using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyBudgetPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BudgetingStyle",
                table: "families",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HouseholdMonthlyIncome",
                table: "families",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayFrequency",
                table: "families",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BudgetingStyle",
                table: "families");

            migrationBuilder.DropColumn(
                name: "HouseholdMonthlyIncome",
                table: "families");

            migrationBuilder.DropColumn(
                name: "PayFrequency",
                table: "families");
        }
    }
}
