namespace DragonEnvelopes.Application.Cqrs.Messaging;

public static class FamilyIntegrationEventNames
{
    public const string FamilyCreated = "FamilyCreated";
    public const string FamilyMemberAdded = "FamilyMemberAdded";
    public const string FamilyMemberRemoved = "FamilyMemberRemoved";
    public const string FamilyInviteAccepted = "FamilyInviteAccepted";
}

public sealed record FamilyCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    string FamilyName,
    string CurrencyCode,
    string TimeZoneId);

public sealed record FamilyMemberAddedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid MemberId,
    string KeycloakUserId,
    string Name,
    string Email,
    string Role);

public sealed record FamilyMemberRemovedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid MemberId,
    string KeycloakUserId,
    string Name,
    string Email,
    string Role);

public sealed record FamilyInviteAcceptedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid FamilyId,
    string CorrelationId,
    Guid InviteId,
    string Email,
    string Role,
    string? ActorUserId);
