using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class ApiClientOptionsTests
{
    [Fact]
    public void ResolveServiceBaseUrls_Fallback_To_BaseUrl_When_SplitUrls_NotProvided()
    {
        var options = new ApiClientOptions
        {
            BaseUrl = "http://localhost:18088/api/v1",
            FamilyBaseUrl = null,
            LedgerBaseUrl = null,
            FinancialBaseUrl = null
        };

        var familyBaseUrl = options.ResolveFamilyBaseUrl();
        var ledgerBaseUrl = options.ResolveLedgerBaseUrl();
        var financialBaseUrl = options.ResolveFinancialBaseUrl();

        Assert.Equal("http://localhost:18088/api/v1/", familyBaseUrl);
        Assert.Equal("http://localhost:18088/api/v1/", ledgerBaseUrl);
        Assert.Equal("http://localhost:18088/api/v1/", financialBaseUrl);
    }

    [Fact]
    public void ResolveServiceBaseUrls_Use_SplitUrls_When_Provided()
    {
        var options = new ApiClientOptions
        {
            BaseUrl = "http://localhost:18088/api/v1/",
            FamilyBaseUrl = "http://localhost:18089/api/v1",
            LedgerBaseUrl = "http://localhost:18090/api/v1",
            FinancialBaseUrl = "http://localhost:18091/api/v1"
        };

        var familyBaseUrl = options.ResolveFamilyBaseUrl();
        var ledgerBaseUrl = options.ResolveLedgerBaseUrl();
        var financialBaseUrl = options.ResolveFinancialBaseUrl();

        Assert.Equal("http://localhost:18089/api/v1/", familyBaseUrl);
        Assert.Equal("http://localhost:18090/api/v1/", ledgerBaseUrl);
        Assert.Equal("http://localhost:18091/api/v1/", financialBaseUrl);
    }
}
