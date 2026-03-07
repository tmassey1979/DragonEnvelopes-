using DragonEnvelopes.Desktop.Api;

namespace DragonEnvelopes.Desktop.Tests;

public sealed class ApiClientOptionsTests
{
    [Fact]
    public void ResolveFamilyAndLedgerBaseUrls_Fallback_To_BaseUrl_When_SplitUrls_NotProvided()
    {
        var options = new ApiClientOptions
        {
            BaseUrl = "http://localhost:18088/api/v1",
            FamilyBaseUrl = null,
            LedgerBaseUrl = null
        };

        var familyBaseUrl = options.ResolveFamilyBaseUrl();
        var ledgerBaseUrl = options.ResolveLedgerBaseUrl();

        Assert.Equal("http://localhost:18088/api/v1/", familyBaseUrl);
        Assert.Equal("http://localhost:18088/api/v1/", ledgerBaseUrl);
    }

    [Fact]
    public void ResolveFamilyAndLedgerBaseUrls_Use_SplitUrls_When_Provided()
    {
        var options = new ApiClientOptions
        {
            BaseUrl = "http://localhost:18088/api/v1/",
            FamilyBaseUrl = "http://localhost:18089/api/v1",
            LedgerBaseUrl = "http://localhost:18090/api/v1"
        };

        var familyBaseUrl = options.ResolveFamilyBaseUrl();
        var ledgerBaseUrl = options.ResolveLedgerBaseUrl();

        Assert.Equal("http://localhost:18089/api/v1/", familyBaseUrl);
        Assert.Equal("http://localhost:18090/api/v1/", ledgerBaseUrl);
    }
}
