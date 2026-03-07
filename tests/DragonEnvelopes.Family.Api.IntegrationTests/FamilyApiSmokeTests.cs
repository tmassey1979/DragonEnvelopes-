using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Encodings.Web;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Family.Api.CrossCutting.Auth;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DragonEnvelopes.Family.Api.IntegrationTests;

public sealed class FamilyApiSmokeTests : IClassFixture<FamilyApiFactory>
{
    private readonly FamilyApiFactory _factory;

    public FamilyApiSmokeTests(FamilyApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_Live_Returns_Ok()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_Endpoint_Without_Auth_Returns_Unauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/families/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Access_Own_Family_But_Not_Other_Family()
    {
        var userId = "family-user-a";
        var ownFamilyId = Guid.Parse("d1000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d1000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var ownResponse = await client.GetAsync($"/api/v1/families/{ownFamilyId}");
        var otherResponse = await client.GetAsync($"/api/v1/families/{otherFamilyId}");

        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Create_And_Resend_Invite_For_Own_Family()
    {
        var userId = "family-invite-user-a";
        var ownFamilyId = Guid.Parse("d2000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d2000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var createResponse = await client.PostAsJsonAsync($"/api/v1/families/{ownFamilyId}/invites", new
        {
            email = "family-api-resent@test.dev",
            role = "Adult",
            expiresInHours = 24
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);

        var resendResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{ownFamilyId}/invites/{created!.Invite.Id}/resend",
            new
            {
                expiresInHours = 96
            });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
        var resent = await resendResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(resent);
        Assert.Equal(created.Invite.Id, resent!.Invite.Id);
        Assert.NotEqual(created.InviteToken, resent.InviteToken);
        Assert.True(resent.Invite.ExpiresAtUtc > created.Invite.ExpiresAtUtc);
    }

    [Fact]
    public async Task Authenticated_User_Can_List_Invite_Timeline_For_Own_Family()
    {
        var userId = "family-invite-timeline-user-a";
        var ownFamilyId = Guid.Parse("d2050000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d2050000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var inviteEmail = $"family-api-timeline-{Guid.NewGuid():N}@test.dev";
        var createResponse = await client.PostAsJsonAsync($"/api/v1/families/{ownFamilyId}/invites", new
        {
            email = inviteEmail,
            role = "Adult",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var timelineResponse = await client.GetAsync(
            $"/api/v1/families/{ownFamilyId}/invites/timeline?email={Uri.EscapeDataString(inviteEmail)}&take=20");
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);

        var timeline = await timelineResponse.Content.ReadFromJsonAsync<List<FamilyInviteTimelineEventResponse>>();
        Assert.NotNull(timeline);
        Assert.Contains(timeline!, timelineEvent => timelineEvent.EventType == "Created");

        var forbiddenResponse = await client.GetAsync($"/api/v1/families/{otherFamilyId}/invites/timeline?take=20");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Update_And_Remove_Family_Member()
    {
        var userId = "family-member-manage-user-a";
        var ownFamilyId = Guid.Parse("d2100000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d2100000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var addResponse = await client.PostAsJsonAsync($"/api/v1/families/{ownFamilyId}/members", new
        {
            keycloakUserId = "family-api-member-a",
            name = "Family API Member",
            email = "family-api-member@test.dev",
            role = "Adult"
        });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        var added = await addResponse.Content.ReadFromJsonAsync<FamilyMemberResponse>();
        Assert.NotNull(added);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/families/{ownFamilyId}/members/{added!.Id}/role",
            new UpdateFamilyMemberRoleRequest("Teen"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FamilyMemberResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Teen", updated!.Role);

        var deleteResponse = await client.DeleteAsync($"/api/v1/families/{ownFamilyId}/members/{added.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Preview_FamilyMember_Import_For_Own_Family()
    {
        var userId = "family-member-import-user-a";
        var ownFamilyId = Guid.Parse("d2150000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d2150000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var csv = "keycloakUserId,name,email,role\nimport-family-api-1,Family API Import,import-family-api-1@test.dev,Adult";
        var ownResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{ownFamilyId}/members/import/preview",
            new
            {
                csvContent = csv,
                delimiter = ",",
                headerMappings = (object?)null
            });
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);

        var preview = await ownResponse.Content.ReadFromJsonAsync<FamilyMemberImportPreviewResponse>();
        Assert.NotNull(preview);
        Assert.Equal(1, preview!.Parsed);
        Assert.Equal(1, preview.Valid);

        var otherResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{otherFamilyId}/members/import/preview",
            new
            {
                csvContent = csv,
                delimiter = ",",
                headerMappings = (object?)null
            });
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Read_AuthMe_With_FamilyMembership()
    {
        var userId = "family-auth-user-a";
        var ownFamilyId = Guid.Parse("d3000000-0000-0000-0000-000000000001");
        var otherFamilyId = Guid.Parse("d3000000-0000-0000-0000-000000000002");

        using var client = _factory.CreateClient();
        await SeedFamilyMembershipAsync(userId, ownFamilyId, otherFamilyId);
        client.DefaultRequestHeaders.Add("X-Test-User", userId);

        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        Assert.True(document.RootElement.TryGetProperty("familyIds", out var familyIdsElement));
        Assert.Equal(JsonValueKind.Array, familyIdsElement.ValueKind);
        Assert.Contains(familyIdsElement.EnumerateArray().Select(static element => element.GetGuid()), id => id == ownFamilyId);
        Assert.DoesNotContain(familyIdsElement.EnumerateArray().Select(static element => element.GetGuid()), id => id == otherFamilyId);
    }

    [Fact]
    public async Task System_Health_Endpoint_Returns_Ok()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/system/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task SeedFamilyMembershipAsync(string userId, Guid ownFamilyId, Guid otherFamilyId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.Families.RemoveRange(dbContext.Families);
        dbContext.FamilyMembers.RemoveRange(dbContext.FamilyMembers);

        var now = DateTimeOffset.UtcNow;
        dbContext.Families.AddRange(
            new DragonEnvelopes.Domain.Entities.Family(ownFamilyId, "Authorized Family", now),
            new DragonEnvelopes.Domain.Entities.Family(otherFamilyId, "Forbidden Family", now));

        dbContext.FamilyMembers.Add(new FamilyMember(
            Guid.NewGuid(),
            ownFamilyId,
            userId,
            "Parent User",
            EmailAddress.Parse("parent.user@test.local"),
            MemberRole.Parent));

        await dbContext.SaveChangesAsync();
    }
}

public sealed class FamilyApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"family-api-smoke-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://localhost/realms/dragonenvelopes",
                ["Authentication:PublicAuthority"] = "https://localhost/realms/dragonenvelopes",
                ["Authentication:Audience"] = "dragonenvelopes-api",
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=dragonenvelopes;Username=postgres;Password=postgres"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DragonEnvelopesDbContext>>();
            services.RemoveAll<DragonEnvelopesDbContext>();
            services.AddDbContext<DragonEnvelopesDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(ApiAuthorizationPolicies.AnyFamilyMember, policy => policy.RequireAuthenticatedUser());
                options.AddPolicy(ApiAuthorizationPolicies.Parent, policy => policy.RequireAuthenticatedUser());
            });
        });
    }
}

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [
                new Claim("sub", userId.ToString()),
                new Claim(ClaimTypes.Role, "Parent"),
                new Claim("role", "Parent")
            ],
            SchemeName,
            ClaimTypes.Name,
            ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
