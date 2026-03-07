using DragonEnvelopes.Contracts.IntegrationEvents;

namespace DragonEnvelopes.Application.Tests;

public sealed class IntegrationEventEnvelopeTests
{
    [Fact]
    public void EnvelopeJson_RoundTrip_PreservesMetadataAndPayload()
    {
        var payload = new TestPayload(Guid.NewGuid(), Guid.NewGuid(), 42.15m);
        var envelope = IntegrationEventEnvelopeFactory.Create(
            eventName: "ledger.transaction.created.v1",
            schemaVersion: "1.0",
            sourceService: "ledger-api",
            correlationId: Guid.NewGuid().ToString("D"),
            causationId: null,
            familyId: payload.FamilyId,
            payload: payload,
            occurredAtUtc: DateTimeOffset.UtcNow,
            eventId: Guid.NewGuid().ToString("D"));

        var json = IntegrationEventEnvelopeJson.SerializeToUtf8Bytes(envelope);
        var restored = IntegrationEventEnvelopeJson.Deserialize<TestPayload>(json);

        Assert.NotNull(restored);
        Assert.Equal(envelope.EventId, restored!.EventId);
        Assert.Equal(envelope.EventName, restored.EventName);
        Assert.Equal(envelope.SchemaVersion, restored.SchemaVersion);
        Assert.Equal(envelope.SourceService, restored.SourceService);
        Assert.Equal(envelope.CorrelationId, restored.CorrelationId);
        Assert.Equal(envelope.FamilyId, restored.FamilyId);
        Assert.Equal(payload.TransactionId, restored.Payload.TransactionId);
        Assert.Equal(payload.FamilyId, restored.Payload.FamilyId);
        Assert.Equal(payload.Amount, restored.Payload.Amount);
    }

    [Fact]
    public void EnvelopeValidator_ReturnsErrors_ForMissingRequiredMetadata()
    {
        var envelope = new IntegrationEventEnvelope<TestPayload>(
            EventId: "not-a-guid",
            EventName: "",
            SchemaVersion: "bad",
            OccurredAtUtc: default,
            PublishedAtUtc: default,
            SourceService: "",
            CorrelationId: "",
            CausationId: null,
            FamilyId: null,
            Payload: null!);

        var isValid = IntegrationEventEnvelopeValidator.TryValidate(envelope, out var errors);

        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void EnvelopeValidator_IsSupportedMajorVersion_ValidatesSchemaMajor()
    {
        Assert.True(IntegrationEventEnvelopeValidator.IsSupportedMajorVersion("1.0", 1));
        Assert.True(IntegrationEventEnvelopeValidator.IsSupportedMajorVersion("1.4", 1));
        Assert.False(IntegrationEventEnvelopeValidator.IsSupportedMajorVersion("2.0", 1));
        Assert.False(IntegrationEventEnvelopeValidator.IsSupportedMajorVersion("invalid", 1));
    }

    private sealed record TestPayload(Guid TransactionId, Guid FamilyId, decimal Amount);
}
