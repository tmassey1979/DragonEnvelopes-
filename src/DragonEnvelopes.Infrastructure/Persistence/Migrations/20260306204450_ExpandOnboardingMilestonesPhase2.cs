using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandOnboardingMilestonesPhase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutomationCompleted",
                table: "onboarding_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CardsCompleted",
                table: "onboarding_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MembersCompleted",
                table: "onboarding_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PlaidCompleted",
                table: "onboarding_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StripeAccountsCompleted",
                table: "onboarding_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutomationCompleted",
                table: "onboarding_profiles");

            migrationBuilder.DropColumn(
                name: "CardsCompleted",
                table: "onboarding_profiles");

            migrationBuilder.DropColumn(
                name: "MembersCompleted",
                table: "onboarding_profiles");

            migrationBuilder.DropColumn(
                name: "PlaidCompleted",
                table: "onboarding_profiles");

            migrationBuilder.DropColumn(
                name: "StripeAccountsCompleted",
                table: "onboarding_profiles");
        }
    }
}
