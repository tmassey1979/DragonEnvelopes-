namespace DragonEnvelopes.Contracts.IntegrationEvents;

public static class IntegrationEventEnvelopeValidator
{
    public static bool TryValidate<TPayload>(
        IntegrationEventEnvelope<TPayload>? envelope,
        out IReadOnlyList<string> errors)
    {
        var validationErrors = new List<string>();
        if (envelope is null)
        {
            validationErrors.Add("Envelope is required.");
            errors = validationErrors;
            return false;
        }

        if (string.IsNullOrWhiteSpace(envelope.EventId))
        {
            validationErrors.Add("eventId is required.");
        }
        else if (!Guid.TryParse(envelope.EventId, out _))
        {
            validationErrors.Add("eventId must be a GUID.");
        }

        if (string.IsNullOrWhiteSpace(envelope.EventName))
        {
            validationErrors.Add("eventName is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.SchemaVersion))
        {
            validationErrors.Add("schemaVersion is required.");
        }
        else if (!TryParseVersion(envelope.SchemaVersion, out _))
        {
            validationErrors.Add("schemaVersion must be formatted as 'major.minor'.");
        }

        if (envelope.OccurredAtUtc == default)
        {
            validationErrors.Add("occurredAtUtc is required.");
        }

        if (envelope.PublishedAtUtc == default)
        {
            validationErrors.Add("publishedAtUtc is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.SourceService))
        {
            validationErrors.Add("sourceService is required.");
        }

        if (string.IsNullOrWhiteSpace(envelope.CorrelationId))
        {
            validationErrors.Add("correlationId is required.");
        }

        if (envelope.Payload is null)
        {
            validationErrors.Add("payload is required.");
        }

        errors = validationErrors;
        return validationErrors.Count == 0;
    }

    public static bool IsSupportedMajorVersion(string schemaVersion, int supportedMajorVersion)
    {
        if (!TryParseVersion(schemaVersion, out var version))
        {
            return false;
        }

        return version.Major == supportedMajorVersion;
    }

    private static bool TryParseVersion(string schemaVersion, out Version version)
    {
        version = new Version(0, 0);
        if (string.IsNullOrWhiteSpace(schemaVersion))
        {
            return false;
        }

        if (!Version.TryParse(schemaVersion.Trim(), out var parsedVersion) || parsedVersion is null)
        {
            return false;
        }

        version = parsedVersion;
        return true;
    }
}
