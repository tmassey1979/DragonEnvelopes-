using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Automation;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Imports;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Contracts.Transactions;

namespace DragonEnvelopes.Family.Api.Endpoints;

internal static class EndpointMappers
{
    public static FamilyResponse MapFamilyResponse(FamilyDetails family)
    {
        return new FamilyResponse(
            family.Id,
            family.Name,
            family.CreatedAt,
            family.Members
                .Select(static member => new FamilyMemberResponse(
                    member.Id,
                    member.FamilyId,
                    member.KeycloakUserId,
                    member.Name,
                    member.Email,
                    member.Role))
                .ToArray());
    }

    public static FamilyProfileResponse MapFamilyProfileResponse(FamilyProfileDetails family)
    {
        return new FamilyProfileResponse(
            family.Id,
            family.Name,
            family.CurrencyCode,
            family.TimeZoneId,
            family.CreatedAt,
            family.UpdatedAt);
    }

    public static FamilyBudgetPreferencesResponse MapFamilyBudgetPreferencesResponse(FamilyBudgetPreferencesDetails details)
    {
        return new FamilyBudgetPreferencesResponse(
            details.FamilyId,
            details.PayFrequency,
            details.BudgetingStyle,
            details.HouseholdMonthlyIncome,
            details.UpdatedAt);
    }

    public static FamilyMemberResponse MapFamilyMemberResponse(FamilyMemberDetails member)
    {
        return new FamilyMemberResponse(
            member.Id,
            member.FamilyId,
            member.KeycloakUserId,
            member.Name,
            member.Email,
            member.Role);
    }

    public static FamilyInviteResponse MapFamilyInviteResponse(FamilyInviteDetails invite)
    {
        return new FamilyInviteResponse(
            invite.Id,
            invite.FamilyId,
            invite.Email,
            invite.Role,
            invite.Status,
            invite.CreatedAtUtc,
            invite.ExpiresAtUtc,
            invite.AcceptedAtUtc,
            invite.CancelledAtUtc);
    }

    public static FamilyInviteTimelineEventResponse MapFamilyInviteTimelineEventResponse(FamilyInviteTimelineEventDetails timelineEvent)
    {
        return new FamilyInviteTimelineEventResponse(
            timelineEvent.Id,
            timelineEvent.FamilyId,
            timelineEvent.InviteId,
            timelineEvent.Email,
            timelineEvent.EventType,
            timelineEvent.ActorUserId,
            timelineEvent.OccurredAtUtc);
    }

    public static RedeemFamilyInviteResponse MapRedeemFamilyInviteResponse(FamilyInviteRedemptionDetails details)
    {
        return new RedeemFamilyInviteResponse(
            MapFamilyInviteResponse(details.Invite),
            MapFamilyMemberResponse(details.Member),
            details.CreatedNewMember);
    }

    public static OnboardingProfileResponse MapOnboardingProfileResponse(OnboardingProfileDetails profile)
    {
        return new OnboardingProfileResponse(
            profile.Id,
            profile.FamilyId,
            profile.MembersCompleted,
            profile.AccountsCompleted,
            profile.EnvelopesCompleted,
            profile.BudgetCompleted,
            profile.PlaidCompleted,
            profile.StripeAccountsCompleted,
            profile.CardsCompleted,
            profile.AutomationCompleted,
            profile.IsCompleted,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.CompletedAtUtc);
    }

    public static AccountResponse MapAccountResponse(AccountDetails account)
    {
        return new AccountResponse(
            account.Id,
            account.FamilyId,
            account.Name,
            account.Type,
            account.Balance);
    }

    public static TransactionResponse MapTransactionResponse(TransactionDetails transaction)
    {
        return new TransactionResponse(
            transaction.Id,
            transaction.AccountId,
            transaction.Amount,
            transaction.Description,
            transaction.Merchant,
            transaction.OccurredAt,
            transaction.Category,
            transaction.EnvelopeId,
            transaction.Splits.Select(static split => new TransactionSplitResponse(
                    split.Id,
                    split.TransactionId,
                    split.EnvelopeId,
                    split.Amount,
                    split.Category,
                    split.Notes))
                .ToArray());
    }

    public static EnvelopeResponse MapEnvelopeResponse(EnvelopeDetails envelope)
    {
        return new EnvelopeResponse(
            envelope.Id,
            envelope.FamilyId,
            envelope.Name,
            envelope.MonthlyBudget,
            envelope.CurrentBalance,
            envelope.LastActivityAt,
            envelope.IsArchived);
    }

    public static BudgetResponse MapBudgetResponse(BudgetDetails budget)
    {
        return new BudgetResponse(
            budget.Id,
            budget.FamilyId,
            budget.Month,
            budget.TotalIncome,
            budget.AllocatedAmount,
            budget.RemainingAmount);
    }

    public static RecurringBillResponse MapRecurringBillResponse(RecurringBillDetails recurringBill)
    {
        return new RecurringBillResponse(
            recurringBill.Id,
            recurringBill.FamilyId,
            recurringBill.Name,
            recurringBill.Merchant,
            recurringBill.Amount,
            recurringBill.Frequency,
            recurringBill.DayOfMonth,
            recurringBill.StartDate,
            recurringBill.EndDate,
            recurringBill.IsActive);
    }

    public static RecurringBillProjectionItemResponse MapRecurringBillProjectionItemResponse(
        RecurringBillProjectionItemDetails item)
    {
        return new RecurringBillProjectionItemResponse(
            item.RecurringBillId,
            item.Name,
            item.Merchant,
            item.Amount,
            item.DueDate);
    }

    public static ImportPreviewResponse MapImportPreviewResponse(ImportPreviewDetails preview)
    {
        return new ImportPreviewResponse(
            preview.Parsed,
            preview.Valid,
            preview.Deduped,
            preview.Rows.Select(static row => new ImportPreviewRowResponse(
                    row.RowNumber,
                    row.OccurredOn,
                    row.Amount,
                    row.Merchant,
                    row.Description,
                    row.Category,
                    row.IsDuplicate,
                    row.Errors))
                .ToArray());
    }

    public static ImportCommitResponse MapImportCommitResponse(ImportCommitDetails commit)
    {
        return new ImportCommitResponse(
            commit.Parsed,
            commit.Valid,
            commit.Deduped,
            commit.Inserted,
            commit.Failed);
    }

    public static EnvelopeBalanceReportResponse MapEnvelopeBalanceReportResponse(EnvelopeBalanceReportDetails details)
    {
        return new EnvelopeBalanceReportResponse(
            details.EnvelopeId,
            details.EnvelopeName,
            details.MonthlyBudget,
            details.CurrentBalance,
            details.IsArchived);
    }

    public static MonthlySpendReportPointResponse MapMonthlySpendReportPointResponse(MonthlySpendReportPointDetails details)
    {
        return new MonthlySpendReportPointResponse(details.Month, details.TotalSpend);
    }

    public static CategoryBreakdownReportItemResponse MapCategoryBreakdownReportItemResponse(CategoryBreakdownReportItemDetails details)
    {
        return new CategoryBreakdownReportItemResponse(details.Category, details.TotalSpend);
    }

    public static RemainingBudgetReportResponse MapRemainingBudgetReportResponse(BudgetDetails budget)
    {
        return new RemainingBudgetReportResponse(
            budget.Id,
            budget.FamilyId,
            budget.Month,
            budget.TotalIncome,
            budget.AllocatedAmount,
            budget.RemainingAmount);
    }

    public static AutomationRuleResponse MapAutomationRuleResponse(AutomationRuleDetails rule)
    {
        return new AutomationRuleResponse(
            rule.Id,
            rule.FamilyId,
            rule.Name,
            rule.RuleType,
            rule.Priority,
            rule.IsEnabled,
            rule.ConditionsJson,
            rule.ActionJson,
            rule.CreatedAt,
            rule.UpdatedAt);
    }
}
