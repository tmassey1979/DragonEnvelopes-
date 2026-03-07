using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyReconciliationDriftThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReconciliationDriftThreshold",
                table: "family_financial_profiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 25m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReconciliationDriftThreshold",
                table: "family_financial_profiles");
        }
    }
}
