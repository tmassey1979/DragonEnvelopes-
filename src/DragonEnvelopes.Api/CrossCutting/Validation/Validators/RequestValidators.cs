using DragonEnvelopes.Contracts.Accounts;
using DragonEnvelopes.Contracts.Automation;
using DragonEnvelopes.Contracts.Budgets;
using DragonEnvelopes.Contracts.Envelopes;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Contracts.Imports;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Contracts.Transactions;
using FluentValidation;
using System.Text.Json;

namespace DragonEnvelopes.Api.CrossCutting.Validation.Validators;

public sealed class CreateFamilyRequestValidator : AbstractValidator<CreateFamilyRequest>
{
    public CreateFamilyRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public sealed class CompleteFamilyOnboardingRequestValidator : AbstractValidator<CompleteFamilyOnboardingRequest>
{
    public CompleteFamilyOnboardingRequestValidator()
    {
        RuleFor(static request => request.FamilyName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.PrimaryGuardianFirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.PrimaryGuardianLastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(static request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);
    }
}

public sealed class AddFamilyMemberRequestValidator : AbstractValidator<AddFamilyMemberRequest>
{
    private static readonly string[] AllowedRoles = ["Parent", "Adult", "Teen", "Child"];

    public AddFamilyMemberRequestValidator()
    {
        RuleFor(static request => request.KeycloakUserId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(static request => request.Role)
            .NotEmpty()
            .Must(static role => AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");
    }
}

public sealed class CreateFamilyInviteRequestValidator : AbstractValidator<CreateFamilyInviteRequest>
{
    private static readonly string[] AllowedRoles = ["Parent", "Adult", "Teen", "Child"];

    public CreateFamilyInviteRequestValidator()
    {
        RuleFor(static request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(static request => request.Role)
            .NotEmpty()
            .Must(static role => AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Role must be one of: {string.Join(", ", AllowedRoles)}.");

        RuleFor(static request => request.ExpiresInHours)
            .InclusiveBetween(1, 24 * 30);
    }
}

public sealed class AcceptFamilyInviteRequestValidator : AbstractValidator<AcceptFamilyInviteRequest>
{
    public AcceptFamilyInviteRequestValidator()
    {
        RuleFor(static request => request.InviteToken)
            .NotEmpty()
            .MaximumLength(256);
    }
}

public sealed class CreatePlaidLinkTokenRequestValidator : AbstractValidator<CreatePlaidLinkTokenRequest>
{
    public CreatePlaidLinkTokenRequestValidator()
    {
        RuleFor(static request => request.ClientUserId)
            .MaximumLength(128);

        RuleFor(static request => request.ClientName)
            .MaximumLength(128);
    }
}

public sealed class ExchangePlaidPublicTokenRequestValidator : AbstractValidator<ExchangePlaidPublicTokenRequest>
{
    public ExchangePlaidPublicTokenRequestValidator()
    {
        RuleFor(static request => request.PublicToken)
            .NotEmpty()
            .MaximumLength(512);
    }
}

public sealed class CreateStripeSetupIntentRequestValidator : AbstractValidator<CreateStripeSetupIntentRequest>
{
    public CreateStripeSetupIntentRequestValidator()
    {
        RuleFor(static request => request.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(static request => request.Name)
            .MaximumLength(128);
    }
}

public sealed class CreateStripeEnvelopeFinancialAccountRequestValidator : AbstractValidator<CreateStripeEnvelopeFinancialAccountRequest>
{
    public CreateStripeEnvelopeFinancialAccountRequestValidator()
    {
        RuleFor(static request => request.DisplayName)
            .MaximumLength(128);
    }
}

public sealed class CreateVirtualEnvelopeCardRequestValidator : AbstractValidator<CreateVirtualEnvelopeCardRequest>
{
    public CreateVirtualEnvelopeCardRequestValidator()
    {
        RuleFor(static request => request.CardholderName)
            .MaximumLength(128);
    }
}

public sealed class UpsertEnvelopePaymentCardControlRequestValidator : AbstractValidator<UpsertEnvelopePaymentCardControlRequest>
{
    public UpsertEnvelopePaymentCardControlRequestValidator()
    {
        RuleFor(static request => request.DailyLimitAmount)
            .GreaterThanOrEqualTo(0m)
            .When(static request => request.DailyLimitAmount.HasValue);

        RuleForEach(static request => request.AllowedMerchantCategories)
            .NotEmpty()
            .MaximumLength(64);

        RuleForEach(static request => request.AllowedMerchantNames)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(static request => request)
            .Must(static request => HasAnyControl(request))
            .WithMessage("At least one spending control must be provided.");

        RuleFor(static request => request.AllowedMerchantCategories)
            .Must(static categories => HasNoDuplicates(categories))
            .WithMessage("AllowedMerchantCategories contains duplicates.");

        RuleFor(static request => request.AllowedMerchantNames)
            .Must(static merchants => HasNoDuplicates(merchants))
            .WithMessage("AllowedMerchantNames contains duplicates.");

        RuleFor(static request => request.AllowedMerchantCategories)
            .Must(static categories => !HasWildcardConflict(categories))
            .WithMessage("Wildcard category cannot be combined with specific categories.");

        RuleFor(static request => request.AllowedMerchantNames)
            .Must(static merchants => !HasWildcardConflict(merchants))
            .WithMessage("Wildcard merchant cannot be combined with specific merchants.");
    }

    private static bool HasAnyControl(UpsertEnvelopePaymentCardControlRequest request)
    {
        return request.DailyLimitAmount.HasValue
               || request.AllowedMerchantCategories is { Count: > 0 }
               || request.AllowedMerchantNames is { Count: > 0 };
    }

    private static bool HasNoDuplicates(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count <= 1)
        {
            return true;
        }

        return values.Distinct(StringComparer.OrdinalIgnoreCase).Count() == values.Count;
    }

    private static bool HasWildcardConflict(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count <= 1)
        {
            return false;
        }

        return values.Any(static value => value.Equals("*", StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class EvaluateEnvelopeCardSpendRequestValidator : AbstractValidator<EvaluateEnvelopeCardSpendRequest>
{
    public EvaluateEnvelopeCardSpendRequestValidator()
    {
        RuleFor(static request => request.MerchantName)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(static request => request.MerchantCategory)
            .MaximumLength(64);

        RuleFor(static request => request.Amount)
            .GreaterThan(0m);

        RuleFor(static request => request.SpentTodayAmount)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class OnboardingBootstrapRequestValidator : AbstractValidator<OnboardingBootstrapRequest>
{
    public OnboardingBootstrapRequestValidator()
    {
        RuleFor(static request => request)
            .Must(static request => request.Accounts.Count > 0 || request.Envelopes.Count > 0 || request.Budget is not null)
            .WithMessage("At least one onboarding bootstrap item is required.");

        RuleForEach(static request => request.Accounts)
            .SetValidator(new OnboardingBootstrapAccountRequestValidator());

        RuleForEach(static request => request.Envelopes)
            .SetValidator(new OnboardingBootstrapEnvelopeRequestValidator());

        RuleFor(static request => request.Budget)
            .SetValidator(new OnboardingBootstrapBudgetRequestValidator()!)
            .When(static request => request.Budget is not null);
    }
}

public sealed class OnboardingBootstrapAccountRequestValidator : AbstractValidator<OnboardingBootstrapAccountRequest>
{
    private static readonly string[] AllowedTypes = ["Checking", "Savings", "Cash", "Credit"];

    public OnboardingBootstrapAccountRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.Type)
            .NotEmpty()
            .Must(static value => AllowedTypes.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}.");

        RuleFor(static request => request.OpeningBalance)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class OnboardingBootstrapEnvelopeRequestValidator : AbstractValidator<OnboardingBootstrapEnvelopeRequest>
{
    public OnboardingBootstrapEnvelopeRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.MonthlyBudget)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class OnboardingBootstrapBudgetRequestValidator : AbstractValidator<OnboardingBootstrapBudgetRequest>
{
    public OnboardingBootstrapBudgetRequestValidator()
    {
        RuleFor(static request => request.Month)
            .NotEmpty()
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$");

        RuleFor(static request => request.TotalIncome)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    private static readonly string[] AllowedAccountTypes = ["Checking", "Savings", "Cash", "Credit"];

    public CreateAccountRequestValidator()
    {
        RuleFor(static request => request.FamilyId)
            .NotEmpty();

        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.Type)
            .NotEmpty()
            .Must(static accountType => AllowedAccountTypes.Contains(accountType, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedAccountTypes)}.");

        RuleFor(static request => request.OpeningBalance)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateEnvelopeRequestValidator : AbstractValidator<CreateEnvelopeRequest>
{
    public CreateEnvelopeRequestValidator()
    {
        RuleFor(static request => request.FamilyId)
            .NotEmpty();

        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.MonthlyBudget)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class UpdateEnvelopeRequestValidator : AbstractValidator<UpdateEnvelopeRequest>
{
    public UpdateEnvelopeRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.MonthlyBudget)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(static request => request.AccountId)
            .NotEmpty();

        RuleFor(static request => request.Amount)
            .NotEqual(0m);

        RuleFor(static request => request.Description)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(static request => request.Merchant)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(static request => request.OccurredAt)
            .NotEmpty();

        RuleFor(static request => request.Category)
            .MaximumLength(100);

        RuleFor(static request => request)
            .Must(static request => request.EnvelopeId is null || request.Splits is not { Count: > 0 })
            .WithMessage("EnvelopeId cannot be set when splits are provided.");

        RuleFor(static request => request)
            .Must(static request => request.Splits is not { Count: > 0 } || HasMatchingSplitTotal(request))
            .WithMessage("Split totals must equal transaction amount.");

        RuleForEach(static request => request.Splits)
            .SetValidator(new TransactionSplitRequestValidator());
    }

    private static bool HasMatchingSplitTotal(CreateTransactionRequest request)
    {
        if (request.Splits is null || request.Splits.Count == 0)
        {
            return true;
        }

        var splitTotal = request.Splits.Sum(static split => split.Amount);
        return splitTotal == request.Amount;
    }
}

public sealed class TransactionSplitRequestValidator : AbstractValidator<TransactionSplitRequest>
{
    public TransactionSplitRequestValidator()
    {
        RuleFor(static request => request.EnvelopeId)
            .NotEmpty();

        RuleFor(static request => request.Amount)
            .NotEqual(0m);

        RuleFor(static request => request.Category)
            .MaximumLength(100);

        RuleFor(static request => request.Notes)
            .MaximumLength(500);
    }
}

public sealed class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(static request => request.Description)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(static request => request.Merchant)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(static request => request.Category)
            .MaximumLength(100);

        RuleFor(static request => request)
            .Must(static request => !request.ReplaceAllocation || request.EnvelopeId is null || request.Splits is not { Count: > 0 })
            .WithMessage("EnvelopeId cannot be set when splits are provided.");

        RuleFor(static request => request)
            .Must(static request => !request.ReplaceAllocation || request.Splits is not { Count: > 0 } || request.Splits.Sum(static split => split.Amount) != 0m)
            .WithMessage("Split totals cannot be zero when replacing allocation.");

        RuleForEach(static request => request.Splits)
            .SetValidator(new TransactionSplitRequestValidator());
    }
}

public sealed class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(static request => request.FamilyId)
            .NotEmpty();

        RuleFor(static request => request.Month)
            .NotEmpty()
            .Matches(@"^\d{4}-(0[1-9]|1[0-2])$");

        RuleFor(static request => request.TotalIncome)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        RuleFor(static request => request.TotalIncome)
            .GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateAutomationRuleRequestValidator : AbstractValidator<CreateAutomationRuleRequest>
{
    private static readonly string[] AllowedTypes = ["Categorization", "Allocation"];

    public CreateAutomationRuleRequestValidator()
    {
        RuleFor(static request => request.FamilyId)
            .NotEmpty();

        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.RuleType)
            .NotEmpty()
            .Must(static type => AllowedTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"RuleType must be one of: {string.Join(", ", AllowedTypes)}.");

        RuleFor(static request => request.Priority)
            .GreaterThanOrEqualTo(1);

        RuleFor(static request => request.ConditionsJson)
            .NotEmpty()
            .Must(static json => IsJsonObject(json))
            .WithMessage("ConditionsJson must be a valid JSON object.");

        RuleFor(static request => request.ActionJson)
            .NotEmpty()
            .Must(static json => IsJsonObject(json))
            .WithMessage("ActionJson must be a valid JSON object.");
    }

    private static bool IsJsonObject(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class UpdateAutomationRuleRequestValidator : AbstractValidator<UpdateAutomationRuleRequest>
{
    public UpdateAutomationRuleRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(static request => request.Priority)
            .GreaterThanOrEqualTo(1);

        RuleFor(static request => request.ConditionsJson)
            .NotEmpty()
            .Must(static json => CreateAutomationRuleRequestValidator_IsJsonObject(json))
            .WithMessage("ConditionsJson must be a valid JSON object.");

        RuleFor(static request => request.ActionJson)
            .NotEmpty()
            .Must(static json => CreateAutomationRuleRequestValidator_IsJsonObject(json))
            .WithMessage("ActionJson must be a valid JSON object.");
    }

    private static bool CreateAutomationRuleRequestValidator_IsJsonObject(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class CreateRecurringBillRequestValidator : AbstractValidator<CreateRecurringBillRequest>
{
    private static readonly string[] AllowedFrequencies = ["Monthly", "Weekly", "BiWeekly"];

    public CreateRecurringBillRequestValidator()
    {
        RuleFor(static request => request.FamilyId)
            .NotEmpty();

        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(static request => request.Merchant)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(static request => request.Amount)
            .GreaterThan(0m);

        RuleFor(static request => request.Frequency)
            .NotEmpty()
            .Must(static value => AllowedFrequencies.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Frequency must be one of: {string.Join(", ", AllowedFrequencies)}.");

        RuleFor(static request => request.DayOfMonth)
            .InclusiveBetween(1, 31);

        RuleFor(static request => request.EndDate)
            .Must((request, endDate) => !endDate.HasValue || endDate.Value >= request.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public sealed class UpdateRecurringBillRequestValidator : AbstractValidator<UpdateRecurringBillRequest>
{
    private static readonly string[] AllowedFrequencies = ["Monthly", "Weekly", "BiWeekly"];

    public UpdateRecurringBillRequestValidator()
    {
        RuleFor(static request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(static request => request.Merchant)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(static request => request.Amount)
            .GreaterThan(0m);

        RuleFor(static request => request.Frequency)
            .NotEmpty()
            .Must(static value => AllowedFrequencies.Contains(value, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Frequency must be one of: {string.Join(", ", AllowedFrequencies)}.");

        RuleFor(static request => request.DayOfMonth)
            .InclusiveBetween(1, 31);

        RuleFor(static request => request.EndDate)
            .Must((request, endDate) => !endDate.HasValue || endDate.Value >= request.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");
    }
}

public sealed class ImportPreviewRequestValidator : AbstractValidator<ImportPreviewRequest>
{
    public ImportPreviewRequestValidator()
    {
        RuleFor(static request => request.FamilyId)
            .NotEmpty();

        RuleFor(static request => request.AccountId)
            .NotEmpty();

        RuleFor(static request => request.CsvContent)
            .NotEmpty();

        RuleFor(static request => request.Delimiter)
            .Must(static delimiter => string.IsNullOrEmpty(delimiter) || delimiter.Length == 1)
            .WithMessage("Delimiter must be exactly one character when provided.");
    }
}

public sealed class ImportCommitRequestValidator : AbstractValidator<ImportCommitRequest>
{
    public ImportCommitRequestValidator()
    {
        RuleFor(static request => request.FamilyId)
            .NotEmpty();

        RuleFor(static request => request.AccountId)
            .NotEmpty();

        RuleFor(static request => request.CsvContent)
            .NotEmpty();

        RuleFor(static request => request.Delimiter)
            .Must(static delimiter => string.IsNullOrEmpty(delimiter) || delimiter.Length == 1)
            .WithMessage("Delimiter must be exactly one character when provided.");
    }
}
