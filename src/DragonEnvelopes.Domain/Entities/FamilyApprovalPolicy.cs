namespace DragonEnvelopes.Domain.Entities;

public sealed class FamilyApprovalPolicy
{
    private static readonly string[] AllowedRoles = ["Parent", "Adult", "Teen", "Child"];

    public Guid Id { get; }
    public Guid FamilyId { get; }
    public bool IsEnabled { get; private set; }
    public decimal AmountThreshold { get; private set; }
    public string RolesRequiringApprovalCsv { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyList<string> RolesRequiringApproval => ParseRoles(RolesRequiringApprovalCsv);

    public FamilyApprovalPolicy(
        Guid id,
        Guid familyId,
        bool isEnabled,
        decimal amountThreshold,
        string rolesRequiringApprovalCsv,
        DateTimeOffset updatedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Approval policy id is required.");
        }

        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        Id = id;
        FamilyId = familyId;
        IsEnabled = isEnabled;
        AmountThreshold = ValidateThreshold(amountThreshold);
        RolesRequiringApprovalCsv = NormalizeRolesCsv(rolesRequiringApprovalCsv, isEnabled);
        UpdatedAtUtc = updatedAtUtc;
    }

    public static FamilyApprovalPolicy Create(
        Guid id,
        Guid familyId,
        bool isEnabled,
        decimal amountThreshold,
        IReadOnlyCollection<string> rolesRequiringApproval,
        DateTimeOffset updatedAtUtc)
    {
        return new FamilyApprovalPolicy(
            id,
            familyId,
            isEnabled,
            amountThreshold,
            string.Join(',', NormalizeRoles(rolesRequiringApproval, isEnabled)),
            updatedAtUtc);
    }

    public void Update(
        bool isEnabled,
        decimal amountThreshold,
        IReadOnlyCollection<string> rolesRequiringApproval,
        DateTimeOffset updatedAtUtc)
    {
        IsEnabled = isEnabled;
        AmountThreshold = ValidateThreshold(amountThreshold);
        RolesRequiringApprovalCsv = NormalizeRoles(rolesRequiringApproval, isEnabled);
        UpdatedAtUtc = updatedAtUtc;
    }

    public bool RequiresApproval(string? requesterRole, decimal transactionAmount)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (Math.Abs(transactionAmount) < AmountThreshold)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(requesterRole))
        {
            return false;
        }

        return RolesRequiringApproval
            .Contains(requesterRole.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static decimal ValidateThreshold(decimal amountThreshold)
    {
        if (amountThreshold <= 0m)
        {
            throw new DomainValidationException("Approval amount threshold must be greater than zero.");
        }

        return decimal.Round(amountThreshold, 2, MidpointRounding.AwayFromZero);
    }

    private static string NormalizeRoles(
        IReadOnlyCollection<string> roles,
        bool isEnabled)
    {
        var normalized = roles
            .Select(static role => role?.Trim() ?? string.Empty)
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Select(static role => AllowedRoles.FirstOrDefault(
                allowedRole => allowedRole.Equals(role, StringComparison.OrdinalIgnoreCase))
                ?? string.Empty)
            .Where(static role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static role => Array.IndexOf(AllowedRoles, role))
            .ToArray();

        if (isEnabled && normalized.Length == 0)
        {
            throw new DomainValidationException("At least one role must require approval when policy is enabled.");
        }

        return string.Join(',', normalized);
    }

    private static string NormalizeRolesCsv(string csv, bool isEnabled)
    {
        var roles = ParseRoles(csv);
        return NormalizeRoles(roles, isEnabled);
    }

    private static IReadOnlyList<string> ParseRoles(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
