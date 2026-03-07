using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonEnvelopes.Contracts.IntegrationEvents;

public static class IntegrationEventEnvelopeJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static byte[] SerializeToUtf8Bytes<TPayload>(IntegrationEventEnvelope<TPayload> envelope)
    {
        return JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions);
    }

    public static IntegrationEventEnvelope<TPayload>? Deserialize<TPayload>(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<IntegrationEventEnvelope<TPayload>>(utf8Json, SerializerOptions);
    }

    public static IntegrationEventEnvelope<TPayload>? Deserialize<TPayload>(byte[] utf8Json)
    {
        return JsonSerializer.Deserialize<IntegrationEventEnvelope<TPayload>>(utf8Json, SerializerOptions);
    }
}
