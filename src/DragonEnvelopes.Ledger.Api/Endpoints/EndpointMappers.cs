using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Automation;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Contracts.EnvelopeGoals;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Imports;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Contracts.Reports;
using DragonEnvelopes.Contracts.Scenarios;
using DragonEnvelopes.Contracts.Transactions;

namespace DragonEnvelopes.Ledger.Api.Endpoints;

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
                .ToArray(),
            transaction.TransferId,
            transaction.TransferCounterpartyEnvelopeId,
            transaction.TransferDirection,
            transaction.DeletedAtUtc,
            transaction.DeletedByUserId);
    }

    public static EnvelopeTransferResponse MapEnvelopeTransferResponse(EnvelopeTransferDetails transfer)
    {
        return new EnvelopeTransferResponse(
            transfer.TransferId,
            transfer.FamilyId,
            transfer.AccountId,
            transfer.FromEnvelopeId,
            transfer.ToEnvelopeId,
            transfer.Amount,
            transfer.OccurredAt,
            transfer.Notes,
            transfer.DebitTransactionId,
            transfer.CreditTransactionId);
    }

    public static EnvelopeResponse MapEnvelopeResponse(EnvelopeDetails envelope)
    {
        return new EnvelopeResponse(
            envelope.Id,
            envelope.FamilyId,
            envelope.Name,
            envelope.MonthlyBudget,
            envelope.CurrentBalance,
            envelope.RolloverMode,
            envelope.RolloverCap,
            envelope.LastActivityAt,
            envelope.IsArchived);
    }

    public static EnvelopeGoalResponse MapEnvelopeGoalResponse(EnvelopeGoalDetails goal)
    {
        return new EnvelopeGoalResponse(
            goal.Id,
            goal.FamilyId,
            goal.EnvelopeId,
            goal.EnvelopeName,
            goal.CurrentBalance,
            goal.TargetAmount,
            goal.DueDate,
            goal.Status,
            goal.CreatedAtUtc,
            goal.UpdatedAtUtc);
    }

    public static EnvelopeGoalProjectionResponse MapEnvelopeGoalProjectionResponse(EnvelopeGoalProjectionDetails projection)
    {
        return new EnvelopeGoalProjectionResponse(
            projection.GoalId,
            projection.FamilyId,
            projection.EnvelopeId,
            projection.EnvelopeName,
            projection.CurrentBalance,
            projection.TargetAmount,
            projection.DueDate,
            projection.GoalStatus,
            projection.ProgressPercent,
            projection.ExpectedProgressPercent,
            projection.ExpectedBalance,
            projection.VarianceAmount,
            projection.ProjectionStatus);
    }

    public static EnvelopeRolloverPreviewResponse MapEnvelopeRolloverPreviewResponse(EnvelopeRolloverPreviewDetails details)
    {
        return new EnvelopeRolloverPreviewResponse(
            details.FamilyId,
            details.Month,
            details.GeneratedAtUtc,
            details.TotalSourceBalance,
            details.TotalRolloverBalance,
            details.Items.Select(MapEnvelopeRolloverItemResponse).ToArray());
    }

    public static EnvelopeRolloverApplyResponse MapEnvelopeRolloverApplyResponse(EnvelopeRolloverApplyDetails details)
    {
        return new EnvelopeRolloverApplyResponse(
            details.RunId,
            details.FamilyId,
            details.Month,
            details.AlreadyApplied,
            details.AppliedAtUtc,
            details.AppliedByUserId,
            details.EnvelopeCount,
            details.TotalRolloverBalance,
            details.Items.Select(MapEnvelopeRolloverItemResponse).ToArray());
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

    public static RecurringBillExecutionResponse MapRecurringBillExecutionResponse(
        RecurringBillExecutionDetails execution)
    {
        return new RecurringBillExecutionResponse(
            execution.Id,
            execution.RecurringBillId,
            execution.FamilyId,
            execution.DueDate,
            execution.ExecutedAtUtc,
            execution.TransactionId,
            execution.Result,
            execution.Notes,
            execution.IdempotencyKey);
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

    public static ScenarioSimulationResponse MapScenarioSimulationResponse(ScenarioSimulationDetails details)
    {
        return new ScenarioSimulationResponse(
            details.FamilyId,
            details.StartingBalance,
            details.MonthlyIncome,
            details.FixedExpenses,
            details.EffectiveExpenses,
            details.NetMonthlyChange,
            details.MonthHorizon,
            details.DepletionMonth,
            details.EndingBalance,
            details.Months.Select(MapScenarioSimulationMonthResponse).ToArray());
    }

    private static EnvelopeRolloverItemResponse MapEnvelopeRolloverItemResponse(EnvelopeRolloverItemDetails details)
    {
        return new EnvelopeRolloverItemResponse(
            details.EnvelopeId,
            details.EnvelopeName,
            details.CurrentBalance,
            details.RolloverMode,
            details.RolloverCap,
            details.RolloverBalance,
            details.AdjustmentAmount);
    }

    private static ScenarioSimulationMonthResponse MapScenarioSimulationMonthResponse(ScenarioSimulationMonthDetails details)
    {
        return new ScenarioSimulationMonthResponse(
            details.MonthIndex,
            details.Month,
            details.ProjectedBalance,
            details.Income,
            details.Expenses);
    }
}
