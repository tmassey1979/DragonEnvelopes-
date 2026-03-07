using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class EnvelopeService(IEnvelopeRepository envelopeRepository) : IEnvelopeService
{
    public async Task<EnvelopeDetails> CreateAsync(
        Guid familyId,
        string name,
        decimal monthlyBudget,
        string? rolloverMode = null,
        decimal? rolloverCap = null,
        CancellationToken cancellationToken = default)
    {
        if (!await envelopeRepository.FamilyExistsAsync(familyId, cancellationToken))
        {
            throw new DomainValidationException("Family was not found.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        if (await envelopeRepository.EnvelopeNameExistsAsync(familyId, normalizedName, cancellationToken: cancellationToken))
        {
            throw new DomainValidationException("An envelope with the same name already exists.");
        }

        var envelope = new Envelope(
            Guid.NewGuid(),
            familyId,
            normalizedName,
            Money.FromDecimal(monthlyBudget).EnsureNonNegative("MonthlyBudget"),
            Money.Zero,
            ResolveRolloverMode(rolloverMode, EnvelopeRolloverMode.Full),
            ParseRolloverCap(rolloverCap));

        await envelopeRepository.AddEnvelopeAsync(envelope, cancellationToken);
        return Map(envelope);
    }

    public async Task<EnvelopeDetails?> GetByIdAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
        return envelope is null ? null : Map(envelope);
    }

    public async Task<IReadOnlyList<EnvelopeDetails>> ListByFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        var items = await envelopeRepository.ListByFamilyAsync(familyId, cancellationToken);
        return items.Select(Map).ToArray();
    }

    public async Task<EnvelopeDetails> UpdateAsync(
        Guid envelopeId,
        string name,
        decimal monthlyBudget,
        bool isArchived,
        string? rolloverMode = null,
        decimal? rolloverCap = null,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId, cancellationToken);
        if (envelope is null)
        {
            throw new DomainValidationException("Envelope was not found.");
        }

        var normalizedName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        if (!string.Equals(envelope.Name, normalizedName, StringComparison.OrdinalIgnoreCase)
            && await envelopeRepository.EnvelopeNameExistsAsync(
                envelope.FamilyId,
                normalizedName,
                envelope.Id,
                cancellationToken))
        {
            throw new DomainValidationException("An envelope with the same name already exists.");
        }

        if (!string.Equals(envelope.Name, normalizedName, StringComparison.Ordinal))
        {
            envelope.Rename(normalizedName);
        }

        var budget = Money.FromDecimal(monthlyBudget).EnsureNonNegative("MonthlyBudget");
        if (envelope.MonthlyBudget != budget)
        {
            envelope.SetMonthlyBudget(budget);
        }

        if (isArchived && !envelope.IsArchived)
        {
            envelope.Archive();
        }
        else if (!isArchived)
        {
            var parsedRolloverMode = ResolveRolloverMode(rolloverMode, envelope.RolloverMode);
            var parsedRolloverCap = ResolveRolloverCap(parsedRolloverMode, rolloverCap, envelope.RolloverCap);
            if (envelope.RolloverMode != parsedRolloverMode
                || envelope.RolloverCap?.Amount != parsedRolloverCap?.Amount)
            {
                envelope.UpdateRolloverPolicy(parsedRolloverMode, parsedRolloverCap);
            }
        }

        await envelopeRepository.SaveChangesAsync(cancellationToken);
        return Map(envelope);
    }

    public async Task<EnvelopeDetails> ArchiveAsync(Guid envelopeId, CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId, cancellationToken);
        if (envelope is null)
        {
            throw new DomainValidationException("Envelope was not found.");
        }

        if (!envelope.IsArchived)
        {
            envelope.Archive();
            await envelopeRepository.SaveChangesAsync(cancellationToken);
        }

        return Map(envelope);
    }

    public async Task<EnvelopeDetails> UpdateRolloverPolicyAsync(
        Guid envelopeId,
        string rolloverMode,
        decimal? rolloverCap,
        CancellationToken cancellationToken = default)
    {
        var envelope = await envelopeRepository.GetByIdForUpdateAsync(envelopeId, cancellationToken);
        if (envelope is null)
        {
            throw new DomainValidationException("Envelope was not found.");
        }

        envelope.UpdateRolloverPolicy(
            ParseRolloverMode(rolloverMode),
            ParseRolloverCap(rolloverCap));
        await envelopeRepository.SaveChangesAsync(cancellationToken);
        return Map(envelope);
    }

    private static EnvelopeDetails Map(Envelope envelope)
    {
        return new EnvelopeDetails(
            envelope.Id,
            envelope.FamilyId,
            envelope.Name,
            envelope.MonthlyBudget.Amount,
            envelope.CurrentBalance.Amount,
            envelope.RolloverMode.ToString(),
            envelope.RolloverCap?.Amount,
            envelope.LastActivityAt,
            envelope.IsArchived);
    }

    private static EnvelopeRolloverMode ParseRolloverMode(string mode)
    {
        if (!Enum.TryParse<EnvelopeRolloverMode>(mode, ignoreCase: true, out var parsedMode)
            || !Enum.IsDefined(parsedMode))
        {
            throw new DomainValidationException("Rollover mode is invalid.");
        }

        return parsedMode;
    }

    private static EnvelopeRolloverMode ResolveRolloverMode(string? mode, EnvelopeRolloverMode fallback)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return fallback;
        }

        return ParseRolloverMode(mode);
    }

    private static Money? ParseRolloverCap(decimal? rolloverCap)
    {
        return rolloverCap.HasValue
            ? Money.FromDecimal(rolloverCap.Value).EnsureNonNegative("RolloverCap")
            : null;
    }

    private static Money? ResolveRolloverCap(
        EnvelopeRolloverMode mode,
        decimal? requestedCap,
        Money? fallbackCap)
    {
        if (mode != EnvelopeRolloverMode.Cap)
        {
            return null;
        }

        return requestedCap.HasValue
            ? ParseRolloverCap(requestedCap)
            : fallbackCap;
    }
}
