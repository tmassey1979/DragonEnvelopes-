using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;

namespace DragonEnvelopes.Application.Services;

public sealed class ParentSpendNotificationService(
    IFamilyRepository familyRepository,
    INotificationPreferenceRepository notificationPreferenceRepository,
    ISpendNotificationEventRepository spendNotificationEventRepository,
    IClock clock) : IParentSpendNotificationService
{
    public async Task<NotificationPreferenceDetails> GetPreferenceAsync(
        Guid familyId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFamilyUserMembershipAsync(familyId, userId, cancellationToken);

        var existing = await notificationPreferenceRepository.GetByFamilyAndUserAsync(
            familyId,
            userId,
            cancellationToken);
        if (existing is null)
        {
            return new NotificationPreferenceDetails(
                familyId,
                userId,
                EmailEnabled: true,
                InAppEnabled: true,
                SmsEnabled: false,
                clock.UtcNow);
        }

        return Map(existing);
    }

    public async Task<NotificationPreferenceDetails> UpsertPreferenceAsync(
        Guid familyId,
        string userId,
        bool emailEnabled,
        bool inAppEnabled,
        bool smsEnabled,
        CancellationToken cancellationToken = default)
    {
        await EnsureFamilyUserMembershipAsync(familyId, userId, cancellationToken);

        var existing = await notificationPreferenceRepository.GetByFamilyAndUserForUpdateAsync(
            familyId,
            userId,
            cancellationToken);
        if (existing is null)
        {
            var created = new NotificationPreference(
                Guid.NewGuid(),
                familyId,
                userId,
                emailEnabled,
                inAppEnabled,
                smsEnabled,
                clock.UtcNow);
            await notificationPreferenceRepository.AddAsync(created, cancellationToken);
            await notificationPreferenceRepository.SaveChangesAsync(cancellationToken);
            return Map(created);
        }

        existing.Update(emailEnabled, inAppEnabled, smsEnabled, clock.UtcNow);
        await notificationPreferenceRepository.SaveChangesAsync(cancellationToken);
        return Map(existing);
    }

    public async Task<SpendNotificationQueueResult> QueueSpendNotificationsAsync(
        Guid familyId,
        Guid envelopeId,
        Guid cardId,
        string webhookEventId,
        decimal amount,
        string merchant,
        decimal remainingBalance,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0m)
        {
            return new SpendNotificationQueueResult(0);
        }

        var members = await familyRepository.ListMembersAsync(familyId, cancellationToken);
        var parents = members.Where(static member => member.Role == MemberRole.Parent).ToArray();
        if (parents.Length == 0)
        {
            return new SpendNotificationQueueResult(0);
        }

        var now = clock.UtcNow;
        var normalizedMerchant = string.IsNullOrWhiteSpace(merchant) ? "Unknown Merchant" : merchant.Trim();
        var notifications = new List<SpendNotificationEvent>();

        foreach (var parent in parents)
        {
            var preference = await notificationPreferenceRepository.GetByFamilyAndUserAsync(
                familyId,
                parent.KeycloakUserId,
                cancellationToken);
            var emailEnabled = preference?.EmailEnabled ?? true;
            var inAppEnabled = preference?.InAppEnabled ?? true;
            var smsEnabled = preference?.SmsEnabled ?? false;

            if (inAppEnabled)
            {
                notifications.Add(CreateEvent("InApp"));
            }

            if (emailEnabled)
            {
                notifications.Add(CreateEvent("Email"));
            }

            if (smsEnabled)
            {
                notifications.Add(CreateEvent("Sms"));
            }

            SpendNotificationEvent CreateEvent(string channel)
            {
                return new SpendNotificationEvent(
                    Guid.NewGuid(),
                    familyId,
                    parent.KeycloakUserId,
                    envelopeId,
                    cardId,
                    webhookEventId,
                    channel,
                    amount,
                    normalizedMerchant,
                    remainingBalance,
                    now);
            }
        }

        if (notifications.Count == 0)
        {
            return new SpendNotificationQueueResult(0);
        }

        await spendNotificationEventRepository.AddRangeAsync(notifications, cancellationToken);
        await spendNotificationEventRepository.SaveChangesAsync(cancellationToken);
        return new SpendNotificationQueueResult(notifications.Count);
    }

    private async Task EnsureFamilyUserMembershipAsync(
        Guid familyId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (familyId == Guid.Empty)
        {
            throw new DomainValidationException("Family id is required.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainValidationException("User id is required.");
        }

        var members = await familyRepository.ListMembersAsync(familyId, cancellationToken);
        if (!members.Any(member => member.KeycloakUserId.Equals(userId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainValidationException("User is not a member of the requested family.");
        }
    }

    private static NotificationPreferenceDetails Map(NotificationPreference preference)
    {
        return new NotificationPreferenceDetails(
            preference.FamilyId,
            preference.UserId,
            preference.EmailEnabled,
            preference.InAppEnabled,
            preference.SmsEnabled,
            preference.UpdatedAtUtc);
    }
}
