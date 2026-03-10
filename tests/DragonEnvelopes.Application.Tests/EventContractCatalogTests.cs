using System.Reflection;
using System.Text.Json;
using DragonEnvelopes.Application.Cqrs.Messaging;
using DragonEnvelopes.Contracts.IntegrationEvents;

namespace DragonEnvelopes.Application.Tests;

public sealed class EventContractCatalogTests
{
    [Fact]
    public void Catalog_Contracts_Match_RoutingKeys_And_CanonicalEventNames()
    {
        var catalog = LoadCatalog();

        var catalogRoutingKeys = catalog.Contracts
            .Select(static contract => contract.RoutingKey)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var currentRoutingKeys = typeof(IntegrationEventRoutingKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.FieldType == typeof(string))
            .Select(static field => (string)field.GetValue(null)!)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(currentRoutingKeys, catalogRoutingKeys);

        var catalogEventNames = catalog.Contracts
            .Select(static contract => contract.EventName)
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        var canonicalEventNames = new[]
            {
                typeof(FamilyIntegrationEventNames),
                typeof(LedgerIntegrationEventNames),
                typeof(PlanningIntegrationEventNames),
                typeof(AutomationIntegrationEventNames),
                typeof(FinancialIntegrationEventNames)
            }
            .SelectMany(static type => type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(static field => field.FieldType == typeof(string))
                .Select(static field => (string)field.GetValue(null)!))
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(canonicalEventNames, catalogEventNames);
    }

    [Fact]
    public void Catalog_Contracts_Map_To_PayloadTypes_And_RequiredFields()
    {
        var catalog = LoadCatalog();
        var messagingAssembly = typeof(IntegrationEventRoutingKeys).Assembly;

        foreach (var contract in catalog.Contracts)
        {
            var payloadType = messagingAssembly.GetType(
                $"DragonEnvelopes.Application.Cqrs.Messaging.{contract.PayloadType}",
                throwOnError: false,
                ignoreCase: false);
            Assert.NotNull(payloadType);

            var payloadProperties = payloadType!
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(static property => property.Name)
                .ToHashSet(StringComparer.Ordinal);
            Assert.NotEmpty(contract.RequiredPayloadFields);
            foreach (var requiredField in contract.RequiredPayloadFields)
            {
                Assert.Contains(requiredField, payloadProperties);
            }
        }
    }

    [Fact]
    public void Catalog_EnvelopeRequiredFields_Are_Validated_By_EnvelopeValidator()
    {
        var catalog = LoadCatalog();
        var envelope = new IntegrationEventEnvelope<object>(
            EventId: "",
            EventName: "",
            SchemaVersion: "",
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

        var expectedErrorsByField = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["eventId"] = "eventId is required.",
            ["eventName"] = "eventName is required.",
            ["schemaVersion"] = "schemaVersion is required.",
            ["occurredAtUtc"] = "occurredAtUtc is required.",
            ["publishedAtUtc"] = "publishedAtUtc is required.",
            ["sourceService"] = "sourceService is required.",
            ["correlationId"] = "correlationId is required.",
            ["payload"] = "payload is required."
        };

        foreach (var requiredField in catalog.EnvelopeRequiredFields)
        {
            if (expectedErrorsByField.TryGetValue(requiredField, out var expectedError))
            {
                Assert.Contains(expectedError, errors);
            }
        }
    }

    private static EventContractCatalogDocument LoadCatalog()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var catalogPath = Path.Combine(
            repositoryRoot,
            "docs",
            "architecture",
            "event-contract-catalog-v1.json");
        Assert.True(File.Exists(catalogPath), $"Event contract catalog not found: {catalogPath}");

        var json = File.ReadAllText(catalogPath);
        var catalog = JsonSerializer.Deserialize<EventContractCatalogDocument>(
            json,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(catalog);
        Assert.NotEmpty(catalog!.Contracts);
        return catalog;
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DragonEnvelopes.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to resolve repository root from test execution directory.");
    }

    private sealed record EventContractCatalogDocument(
        string CatalogVersion,
        IReadOnlyList<string> EnvelopeRequiredFields,
        IReadOnlyList<EventContractCatalogContract> Contracts);

    private sealed record EventContractCatalogContract(
        string Id,
        string RoutingKey,
        string EventName,
        string SchemaVersion,
        string SourceService,
        string PayloadType,
        IReadOnlyList<string> RequiredPayloadFields);
}
