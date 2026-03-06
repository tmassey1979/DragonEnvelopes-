using DragonEnvelopes.Application.DTOs;

namespace DragonEnvelopes.Application.Services;

public interface IEnvelopePaymentCardControlService
{
    Task<EnvelopePaymentCardControlDetails> UpsertControlsAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        decimal? dailyLimitAmount,
        IReadOnlyCollection<string>? allowedMerchantCategories,
        IReadOnlyCollection<string>? allowedMerchantNames,
        string? changedBy,
        CancellationToken cancellationToken = default);

    Task<EnvelopePaymentCardControlDetails?> GetByCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnvelopePaymentCardControlAuditDetails>> ListAuditByCardAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        CancellationToken cancellationToken = default);

    Task<CardSpendEvaluationDetails> EvaluateSpendAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        string merchantName,
        string? merchantCategory,
        decimal amount,
        decimal spentTodayAmount,
        CancellationToken cancellationToken = default);
}
