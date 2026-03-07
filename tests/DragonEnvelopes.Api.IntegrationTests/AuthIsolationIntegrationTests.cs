using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using DragonEnvelopes.Api.Services;
using DragonEnvelopes.Contracts.Families;
using DragonEnvelopes.Contracts.Financial;
using DragonEnvelopes.Contracts.Onboarding;
using DragonEnvelopes.Contracts.RecurringBills;
using DragonEnvelopes.Contracts.Runtime;
using DragonEnvelopes.Contracts.Transactions;
using DragonEnvelopes.Domain;
using DragonEnvelopes.Domain.Entities;
using DragonEnvelopes.Domain.ValueObjects;
using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
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

namespace DragonEnvelopes.Api.IntegrationTests;

public sealed class AuthIsolationIntegrationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public AuthIsolationIntegrationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Unauthorized_Request_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/accounts?familyId={TestApiFactory.FamilyAId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Read_FamilyB_Accounts()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/accounts?familyId={TestApiFactory.FamilyBId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Create_Transaction_For_FamilyB_Account()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync("/api/v1/transactions", new
        {
            accountId = TestApiFactory.AccountBId,
            amount = -10.00m,
            description = "Leak Attempt",
            merchant = "Other Store",
            occurredAt = DateTimeOffset.UtcNow,
            category = "Misc",
            envelopeId = (Guid?)null,
            splits = (object?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reports_Are_Isolated_By_Family()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var allowed = await client.GetAsync($"/api/v1/reports/envelope-balances?familyId={TestApiFactory.FamilyAId}");
        var forbidden = await client.GetAsync($"/api/v1/reports/envelope-balances?familyId={TestApiFactory.FamilyBId}");

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Update_FamilyB_Transaction()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync($"/api/v1/transactions/{TestApiFactory.TransactionBId}", new
        {
            description = "Updated",
            merchant = "Updated Merchant",
            category = "Updated Category"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Update_Own_Transaction_Metadata()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync($"/api/v1/transactions/{TestApiFactory.TransactionAId}", new
        {
            description = "Updated Groceries",
            merchant = "Updated Market",
            category = "Food"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Updated Groceries", payload!.Description);
        Assert.Equal("Updated Market", payload.Merchant);
        Assert.Equal("Food", payload.Category);
    }

    [Fact]
    public async Task UserA_Can_SoftDelete_Own_Transaction_And_Rebalance_Envelope()
    {
        var envelopeId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Envelopes.Add(new Envelope(
                envelopeId,
                TestApiFactory.FamilyAId,
                "Delete Test Envelope",
                Money.FromDecimal(200m),
                Money.FromDecimal(90m)));
            dbContext.Transactions.Add(new Transaction(
                transactionId,
                TestApiFactory.AccountAId,
                Money.FromDecimal(-10m),
                "Delete Me",
                "Delete Merchant",
                now,
                "Misc",
                envelopeId));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.DeleteAsync($"/api/v1/transactions/{transactionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var transaction = await verifyDb.Transactions.FirstOrDefaultAsync(x => x.Id == transactionId);
        var splitCount = await verifyDb.TransactionSplits.CountAsync(x => x.TransactionId == transactionId);
        var envelope = await verifyDb.Envelopes.FirstAsync(x => x.Id == envelopeId);

        Assert.NotNull(transaction);
        Assert.NotNull(transaction!.DeletedAtUtc);
        Assert.Equal(TestApiFactory.UserAId, transaction.DeletedByUserId);
        Assert.Equal(0, splitCount);
        Assert.Equal(100m, envelope.CurrentBalance.Amount);
    }

    [Fact]
    public async Task UserA_Cannot_Delete_FamilyB_Transaction()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.DeleteAsync($"/api/v1/transactions/{TestApiFactory.TransactionBId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Restore_Own_SoftDeleted_Transaction_And_Reapply_Envelope()
    {
        var envelopeId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Envelopes.Add(new Envelope(
                envelopeId,
                TestApiFactory.FamilyAId,
                "Restore Test Envelope",
                Money.FromDecimal(200m),
                Money.FromDecimal(90m)));
            dbContext.Transactions.Add(new Transaction(
                transactionId,
                TestApiFactory.AccountAId,
                Money.FromDecimal(-10m),
                "Restore Me",
                "Restore Merchant",
                now,
                "Misc",
                envelopeId));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var deleteResponse = await client.DeleteAsync($"/api/v1/transactions/{transactionId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var restoreResponse = await client.PostAsync($"/api/v1/transactions/{transactionId}/restore", null);
        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        var restoredPayload = await restoreResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(restoredPayload);
        Assert.Null(restoredPayload!.DeletedAtUtc);
        Assert.Null(restoredPayload.DeletedByUserId);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var transaction = await verifyDb.Transactions.FirstAsync(x => x.Id == transactionId);
        var envelope = await verifyDb.Envelopes.FirstAsync(x => x.Id == envelopeId);

        Assert.Null(transaction.DeletedAtUtc);
        Assert.Null(transaction.DeletedByUserId);
        Assert.Equal(90m, envelope.CurrentBalance.Amount);
    }

    [Fact]
    public async Task UserA_Cannot_Restore_FamilyB_Transaction()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync($"/api/v1/transactions/{TestApiFactory.TransactionBId}/restore", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_List_Recently_Deleted_Transactions_For_Own_Family()
    {
        var transactionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            var transaction = new Transaction(
                transactionId,
                TestApiFactory.AccountAId,
                Money.FromDecimal(-12m),
                "Deleted Listing",
                "Deleted Merchant",
                now,
                "Misc",
                envelopeId: null);
            transaction.SoftDelete(now.AddDays(-1), TestApiFactory.UserAId);
            dbContext.Transactions.Add(transaction);
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var ownResponse = await client.GetAsync($"/api/v1/transactions/deleted?familyId={TestApiFactory.FamilyAId}&days=30");
        Assert.Equal(HttpStatusCode.OK, ownResponse.StatusCode);
        var ownPayload = await ownResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        Assert.NotNull(ownPayload);
        Assert.Contains(ownPayload!, transaction => transaction.Id == transactionId && transaction.DeletedAtUtc.HasValue);

        var otherResponse = await client.GetAsync($"/api/v1/transactions/deleted?familyId={TestApiFactory.FamilyBId}&days=30");
        Assert.Equal(HttpStatusCode.Forbidden, otherResponse.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Replace_Single_Envelope_With_Splits_And_Rebalance()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync($"/api/v1/transactions/{TestApiFactory.TransactionAId}", new
        {
            description = "Groceries Split",
            merchant = "Market A",
            category = "Food",
            replaceAllocation = true,
            envelopeId = (Guid?)null,
            splits = new object[]
            {
                new
                {
                    envelopeId = TestApiFactory.EnvelopeAId,
                    amount = -4.00m,
                    category = "Food",
                    notes = "Part A"
                },
                new
                {
                    envelopeId = TestApiFactory.EnvelopeA2Id,
                    amount = -6.00m,
                    category = "Food",
                    notes = "Part B"
                }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        var envelopeA = await dbContext.Envelopes.FirstAsync(x => x.Id == TestApiFactory.EnvelopeAId);
        var envelopeA2 = await dbContext.Envelopes.FirstAsync(x => x.Id == TestApiFactory.EnvelopeA2Id);
        var transaction = await dbContext.Transactions.FirstAsync(x => x.Id == TestApiFactory.TransactionAId);
        var splitCount = await dbContext.TransactionSplits.CountAsync(x => x.TransactionId == TestApiFactory.TransactionAId);

        Assert.Equal(106.00m, envelopeA.CurrentBalance.Amount);
        Assert.Equal(94.00m, envelopeA2.CurrentBalance.Amount);
        Assert.Null(transaction.EnvelopeId);
        Assert.Equal(2, splitCount);
    }

    [Fact]
    public async Task UserA_Can_Create_List_And_Cancel_Family_Invite()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var createResponse = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = "invitee@test.dev",
            role = "Adult",
            expiresInHours = 72
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);
        Assert.False(string.IsNullOrWhiteSpace(created!.InviteToken));
        Assert.Equal("Pending", created.Invite.Status);

        var listResponse = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var list = await listResponse.Content.ReadFromJsonAsync<List<FamilyInviteResponse>>();
        Assert.NotNull(list);
        Assert.Contains(list!, invite => invite.Id == created.Invite.Id);

        var cancelResponse = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/{created.Invite.Id}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<FamilyInviteResponse>();
        Assert.NotNull(cancelled);
        Assert.Equal("Cancelled", cancelled!.Status);
    }

    [Fact]
    public async Task UserA_Cannot_Create_Invite_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/invites", new
        {
            email = "blocked@test.dev",
            role = "Adult",
            expiresInHours = 24
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Resend_Pending_Invite()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var createResponse = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = "resend@test.dev",
            role = "Adult",
            expiresInHours = 24
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);

        var resendResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/{created!.Invite.Id}/resend",
            new
            {
                expiresInHours = 96
            });

        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);
        var resent = await resendResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(resent);
        Assert.Equal(created.Invite.Id, resent!.Invite.Id);
        Assert.Equal("Pending", resent.Invite.Status);
        Assert.NotEqual(created.InviteToken, resent.InviteToken);
        Assert.True(resent.Invite.ExpiresAtUtc > created.Invite.ExpiresAtUtc);
    }

    [Fact]
    public async Task UserA_Can_List_Invite_Timeline_And_Filter_By_Email_And_EventType()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var inviteEmail = $"timeline-{Guid.NewGuid():N}@test.dev";
        var createResponse = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = inviteEmail,
            role = "Adult",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);

        var resendResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/{created!.Invite.Id}/resend",
            new
            {
                expiresInHours = 96
            });
        Assert.Equal(HttpStatusCode.OK, resendResponse.StatusCode);

        var cancelResponse = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/{created.Invite.Id}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var escapedEmail = Uri.EscapeDataString(inviteEmail);
        var timelineResponse = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/timeline?email={escapedEmail}&take=20");
        Assert.Equal(HttpStatusCode.OK, timelineResponse.StatusCode);

        var timeline = await timelineResponse.Content.ReadFromJsonAsync<List<FamilyInviteTimelineEventResponse>>();
        Assert.NotNull(timeline);
        Assert.Contains(timeline!, timelineEvent => timelineEvent.EventType == "Created" && timelineEvent.ActorUserId == TestApiFactory.UserAId);
        Assert.Contains(timeline!, timelineEvent => timelineEvent.EventType == "Resent" && timelineEvent.ActorUserId == TestApiFactory.UserAId);
        Assert.Contains(timeline!, timelineEvent => timelineEvent.EventType == "Cancelled" && timelineEvent.ActorUserId == TestApiFactory.UserAId);

        var filteredResponse = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/timeline?email={escapedEmail}&eventType=Resent&take=20");
        Assert.Equal(HttpStatusCode.OK, filteredResponse.StatusCode);

        var filteredTimeline = await filteredResponse.Content.ReadFromJsonAsync<List<FamilyInviteTimelineEventResponse>>();
        Assert.NotNull(filteredTimeline);
        Assert.NotEmpty(filteredTimeline!);
        Assert.All(
            filteredTimeline!,
            timelineEvent =>
            {
                Assert.Equal("Resent", timelineEvent.EventType);
                Assert.True(
                    string.Equals(inviteEmail, timelineEvent.Email, StringComparison.OrdinalIgnoreCase),
                    "Filtered timeline should only include invite email matches.");
            });
    }

    [Fact]
    public async Task UserA_Cannot_List_FamilyB_Invite_Timeline()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserBId);

        var createResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/invites", new
        {
            email = $"timeline-b-{Guid.NewGuid():N}@test.dev",
            role = "Adult",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/invites/timeline?take=20");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Resend_Invite_For_FamilyB()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserBId);
        var createResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/invites", new
        {
            email = "resend-blocked@test.dev",
            role = "Adult",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/invites/{created!.Invite.Id}/resend",
            new
            {
                expiresInHours = 96
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_And_Update_Own_Family_Profile()
    {
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Profile Family", now));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                TestApiFactory.UserAId,
                "Owner User",
                EmailAddress.Parse($"profile-owner-{familyId:N}@test.dev"),
                MemberRole.Parent));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var getResponse = await client.GetAsync($"/api/v1/families/{familyId}/profile");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var initial = await getResponse.Content.ReadFromJsonAsync<FamilyProfileResponse>();
        Assert.NotNull(initial);
        Assert.Equal("Profile Family", initial!.Name);
        Assert.Equal("USD", initial.CurrencyCode);
        Assert.Equal("America/Chicago", initial.TimeZoneId);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/families/{familyId}/profile", new
        {
            name = "Profile Family Updated",
            currencyCode = "EUR",
            timeZoneId = "Europe/Berlin"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FamilyProfileResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Profile Family Updated", updated!.Name);
        Assert.Equal("EUR", updated.CurrencyCode);
        Assert.Equal("Europe/Berlin", updated.TimeZoneId);
    }

    [Fact]
    public async Task UserA_Cannot_Update_FamilyB_Family_Profile()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/profile", new
        {
            name = "Blocked",
            currencyCode = "USD",
            timeZoneId = "America/Chicago"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_And_Update_Own_Family_Budget_Preferences()
    {
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Budget Pref Family", now));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                TestApiFactory.UserAId,
                "Owner User",
                EmailAddress.Parse($"budget-pref-owner-{familyId:N}@test.dev"),
                MemberRole.Parent));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var getResponse = await client.GetAsync($"/api/v1/families/{familyId}/budget-preferences");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var initial = await getResponse.Content.ReadFromJsonAsync<FamilyBudgetPreferencesResponse>();
        Assert.NotNull(initial);
        Assert.Null(initial!.PayFrequency);
        Assert.Null(initial.BudgetingStyle);
        Assert.Null(initial.HouseholdMonthlyIncome);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/families/{familyId}/budget-preferences",
            new UpdateFamilyBudgetPreferencesRequest("BiWeekly", "ZeroBased", 7200.75m));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FamilyBudgetPreferencesResponse>();
        Assert.NotNull(updated);
        Assert.Equal("BiWeekly", updated!.PayFrequency);
        Assert.Equal("ZeroBased", updated.BudgetingStyle);
        Assert.Equal(7200.75m, updated.HouseholdMonthlyIncome);
    }

    [Fact]
    public async Task UserA_Cannot_Update_FamilyB_Budget_Preferences()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/budget-preferences",
            new UpdateFamilyBudgetPreferencesRequest("Weekly", "EnvelopePriority", 5100m));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_Request_Cannot_Add_Family_Member()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members", new
        {
            keycloakUserId = "new-member-user",
            name = "New Member",
            email = "new-member@test.dev",
            role = "Adult"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Add_Family_Member_To_Own_Family()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var email = $"added-member-{Guid.NewGuid():N}@test.dev";
        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members", new
        {
            keycloakUserId = "new-member-user-a",
            name = "Added Member A",
            email,
            role = "Adult"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<FamilyMemberResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.FamilyAId, payload!.FamilyId);
        Assert.Equal("new-member-user-a", payload.KeycloakUserId);
        Assert.Equal("Adult", payload.Role);
        Assert.Equal(email, payload.Email);
    }

    [Fact]
    public async Task UserA_Cannot_Add_Family_Member_To_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/members", new
        {
            keycloakUserId = "new-member-user-b",
            name = "Added Member B",
            email = "added-member-b@test.dev",
            role = "Adult"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Update_Family_Member_Role_For_Own_Family()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var addResponse = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members", new
        {
            keycloakUserId = "member-role-change-a",
            name = "Role Change A",
            email = "member-role-change-a@test.dev",
            role = "Adult"
        });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        var added = await addResponse.Content.ReadFromJsonAsync<FamilyMemberResponse>();
        Assert.NotNull(added);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/members/{added!.Id}/role",
            new UpdateFamilyMemberRoleRequest("Teen"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FamilyMemberResponse>();
        Assert.NotNull(updated);
        Assert.Equal(added.Id, updated!.Id);
        Assert.Equal("Teen", updated.Role);
    }

    [Fact]
    public async Task UserA_Cannot_Update_FamilyB_Member_Role()
    {
        var familyBMemberId = Guid.NewGuid();
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.FamilyMembers.Add(new FamilyMember(
                familyBMemberId,
                TestApiFactory.FamilyBId,
                $"familyb-role-member-{Guid.NewGuid():N}",
                "Family B Member",
                EmailAddress.Parse($"familyb-role-member-{Guid.NewGuid():N}@test.dev"),
                MemberRole.Adult));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/members/{familyBMemberId}/role",
            new UpdateFamilyMemberRoleRequest("Teen"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Remove_Family_Member_From_Own_Family()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var addResponse = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members", new
        {
            keycloakUserId = "member-remove-a",
            name = "Remove Member A",
            email = "member-remove-a@test.dev",
            role = "Adult"
        });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        var added = await addResponse.Content.ReadFromJsonAsync<FamilyMemberResponse>();
        Assert.NotNull(added);

        var deleteResponse = await client.DeleteAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/members/{added!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var members = await listResponse.Content.ReadFromJsonAsync<List<FamilyMemberResponse>>();
        Assert.NotNull(members);
        Assert.DoesNotContain(members!, member => member.Id == added.Id);
    }

    [Fact]
    public async Task UserA_Cannot_Remove_Last_Parent_From_Family()
    {
        Guid parentMemberId;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            parentMemberId = await dbContext.FamilyMembers
                .Where(member => member.FamilyId == TestApiFactory.FamilyAId && member.KeycloakUserId == TestApiFactory.UserAId)
                .Select(member => member.Id)
                .SingleAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.DeleteAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members/{parentMemberId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Preview_And_Commit_FamilyMember_Csv_Import()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var duplicateEmail = $"family-import-{suffix}-dup@test.dev";
        var csv = string.Join('\n', [
            "keycloakUserId,name,email,role",
            $"family-import-{suffix}-1,Import One,family-import-{suffix}-1@test.dev,Adult",
            $"family-import-{suffix}-2,Import Two,{duplicateEmail},Teen",
            $"family-import-{suffix}-3,Import Three,{duplicateEmail},Child",
            $"family-import-{suffix}-4,Import Four,family-import-{suffix}-4@test.dev,UnknownRole"
        ]);

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/members/import/preview",
            new
            {
                csvContent = csv,
                delimiter = ",",
                headerMappings = (object?)null
            });

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<FamilyMemberImportPreviewResponse>();
        Assert.NotNull(preview);
        Assert.Equal(4, preview!.Parsed);
        Assert.Equal(2, preview.Valid);
        Assert.Equal(1, preview.Deduped);
        Assert.Contains(preview.Rows, row => row.RowNumber == 4 && row.Errors.Any(static error => error.Contains("Duplicate email", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(preview.Rows, row => row.RowNumber == 5 && row.Errors.Any(static error => error.Contains("Role is invalid", StringComparison.OrdinalIgnoreCase)));

        var commitResponse = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/members/import/commit",
            new
            {
                csvContent = csv,
                delimiter = ",",
                headerMappings = (object?)null,
                acceptedRowNumbers = new[] { 2, 3, 4, 5 }
            });

        Assert.Equal(HttpStatusCode.OK, commitResponse.StatusCode);
        var commit = await commitResponse.Content.ReadFromJsonAsync<FamilyMemberImportCommitResponse>();
        Assert.NotNull(commit);
        Assert.Equal(4, commit!.Parsed);
        Assert.Equal(2, commit.Valid);
        Assert.Equal(1, commit.Deduped);
        Assert.Equal(2, commit.Inserted);
        Assert.Equal(2, commit.Failed);

        var membersResponse = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members");
        Assert.Equal(HttpStatusCode.OK, membersResponse.StatusCode);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<FamilyMemberResponse>>();
        Assert.NotNull(members);
        Assert.Contains(members!, member => member.KeycloakUserId == $"family-import-{suffix}-1");
        Assert.Contains(members!, member => member.KeycloakUserId == $"family-import-{suffix}-2");
        Assert.DoesNotContain(members!, member => member.KeycloakUserId == $"family-import-{suffix}-3");
        Assert.DoesNotContain(members!, member => member.KeycloakUserId == $"family-import-{suffix}-4");
    }

    [Fact]
    public async Task UserA_Cannot_Import_FamilyMembers_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/members/import/preview",
            new
            {
                csvContent = "keycloakUserId,name,email,role\\nblocked-user,Blocked User,blocked@test.dev,Adult",
                delimiter = ",",
                headerMappings = (object?)null
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invite_Can_Be_Accepted_Anonymously_By_Token()
    {
        using var authorizedClient = _factory.CreateClient();
        authorizedClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var createResponse = await authorizedClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = "accept@test.dev",
            role = "Teen",
            expiresInHours = 24
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(created);

        using var anonymousClient = _factory.CreateClient();
        var acceptResponse = await anonymousClient.PostAsJsonAsync("/api/v1/families/invites/accept", new
        {
            inviteToken = created!.InviteToken
        });

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        var accepted = await acceptResponse.Content.ReadFromJsonAsync<FamilyInviteResponse>();
        Assert.NotNull(accepted);
        Assert.Equal("Accepted", accepted!.Status);
    }

    [Fact]
    public async Task Invite_Registration_Creates_Keycloak_User_And_Family_Member()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        const string inviteEmail = "invite-register@test.dev";
        var createInviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = inviteEmail,
            role = "Adult",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createInviteResponse.StatusCode);

        var createdInvite = await createInviteResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(createdInvite);

        using var anonymousClient = _factory.CreateClient();
        var registerResponse = await anonymousClient.PostAsJsonAsync("/api/v1/families/invites/register", new
        {
            inviteToken = createdInvite!.InviteToken,
            firstName = "Invite",
            lastName = "Register",
            email = inviteEmail,
            password = "InvitePass123!"
        });

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        var payload = await registerResponse.Content.ReadFromJsonAsync<RegisterFamilyInviteAccountResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.CreatedNewMember);
        Assert.Equal("Accepted", payload.Invite.Status);
        Assert.Equal(TestApiFactory.FamilyAId, payload.Member.FamilyId);
        Assert.Equal("Adult", payload.Member.Role);

        var membersResponse = await ownerClient.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members");
        Assert.Equal(HttpStatusCode.OK, membersResponse.StatusCode);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<FamilyMemberResponse>>();
        Assert.NotNull(members);
        Assert.Contains(members!, member => member.Email.Equals(inviteEmail, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Invite_Registration_Fails_On_Duplicate_Keycloak_Email()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        const string inviteEmail = "invite-duplicate@test.dev";
        var createInviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = inviteEmail,
            role = "Teen",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createInviteResponse.StatusCode);
        var createdInvite = await createInviteResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(createdInvite);

        using var anonymousClient = _factory.CreateClient();
        var firstRegister = await anonymousClient.PostAsJsonAsync("/api/v1/families/invites/register", new
        {
            inviteToken = createdInvite!.InviteToken,
            firstName = "Duplicate",
            lastName = "User",
            email = inviteEmail,
            password = "InvitePass123!"
        });
        Assert.Equal(HttpStatusCode.OK, firstRegister.StatusCode);

        var secondRegister = await anonymousClient.PostAsJsonAsync("/api/v1/families/invites/register", new
        {
            inviteToken = createdInvite.InviteToken,
            firstName = "Duplicate",
            lastName = "User",
            email = inviteEmail,
            password = "InvitePass123!"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, secondRegister.StatusCode);
    }

    [Fact]
    public async Task Invite_Registration_Fails_For_Cancelled_Invite()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        const string inviteEmail = "invite-cancelled@test.dev";
        var createInviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = inviteEmail,
            role = "Teen",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createInviteResponse.StatusCode);
        var createdInvite = await createInviteResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(createdInvite);

        var cancelResponse = await ownerClient.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/invites/{createdInvite!.Invite.Id}/cancel",
            content: null);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        using var anonymousClient = _factory.CreateClient();
        var registerResponse = await anonymousClient.PostAsJsonAsync("/api/v1/families/invites/register", new
        {
            inviteToken = createdInvite.InviteToken,
            firstName = "Cancelled",
            lastName = "Invite",
            email = inviteEmail,
            password = "InvitePass123!"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, registerResponse.StatusCode);
    }

    [Fact]
    public async Task Invite_Registration_Fails_For_Expired_Invite()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        const string inviteEmail = "invite-expired@test.dev";
        var createInviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = inviteEmail,
            role = "Teen",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createInviteResponse.StatusCode);
        var createdInvite = await createInviteResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(createdInvite);

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            var invite = await dbContext.FamilyInvites.SingleAsync(x => x.Id == createdInvite!.Invite.Id);
            invite.Expire(DateTimeOffset.UtcNow.AddDays(2));
            await dbContext.SaveChangesAsync();
        }

        using var anonymousClient = _factory.CreateClient();
        var registerResponse = await anonymousClient.PostAsJsonAsync("/api/v1/families/invites/register", new
        {
            inviteToken = createdInvite!.InviteToken,
            firstName = "Expired",
            lastName = "Invite",
            email = inviteEmail,
            password = "InvitePass123!"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, registerResponse.StatusCode);
    }

    [Fact]
    public async Task Invite_Redeem_Requires_Authentication()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/families/invites/redeem", new
        {
            inviteToken = "missing-auth",
            memberName = "No Auth",
            memberEmail = "noauth@test.dev"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_User_Can_Redeem_Invite_And_Become_Family_Member()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var inviteEmail = "redeemer.user@test.dev";
        var createInviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = inviteEmail,
            role = "Teen",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createInviteResponse.StatusCode);

        var createdInvite = await createInviteResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(createdInvite);

        using var redeemerClient = _factory.CreateClient();
        redeemerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, "redeemer-user");

        var redeemResponse = await redeemerClient.PostAsJsonAsync("/api/v1/families/invites/redeem", new
        {
            inviteToken = createdInvite!.InviteToken,
            memberName = "Redeemer User",
            memberEmail = inviteEmail
        });
        Assert.Equal(HttpStatusCode.OK, redeemResponse.StatusCode);

        var redeemed = await redeemResponse.Content.ReadFromJsonAsync<RedeemFamilyInviteResponse>();
        Assert.NotNull(redeemed);
        Assert.True(redeemed!.CreatedNewMember);
        Assert.Equal("Accepted", redeemed.Invite.Status);
        Assert.Equal(TestApiFactory.FamilyAId, redeemed.Member.FamilyId);
        Assert.Equal("redeemer-user", redeemed.Member.KeycloakUserId);

        var membersResponse = await ownerClient.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/members");
        Assert.Equal(HttpStatusCode.OK, membersResponse.StatusCode);
        var members = await membersResponse.Content.ReadFromJsonAsync<List<FamilyMemberResponse>>();
        Assert.NotNull(members);
        Assert.Contains(members!, member => member.KeycloakUserId == "redeemer-user");
    }

    [Fact]
    public async Task Redeem_Invite_Is_Idempotent_For_Existing_User_Membership()
    {
        using var ownerClient = _factory.CreateClient();
        ownerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var inviteEmail = $"redeemer.idempotent.{Guid.NewGuid():N}@test.dev";
        var createInviteResponse = await ownerClient.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/invites", new
        {
            email = inviteEmail,
            role = "Adult",
            expiresInHours = 24
        });
        Assert.Equal(HttpStatusCode.Created, createInviteResponse.StatusCode);
        var createdInvite = await createInviteResponse.Content.ReadFromJsonAsync<CreateFamilyInviteResponse>();
        Assert.NotNull(createdInvite);

        using var redeemerClient = _factory.CreateClient();
        redeemerClient.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, "redeemer-idempotent");

        var firstRedeem = await redeemerClient.PostAsJsonAsync("/api/v1/families/invites/redeem", new
        {
            inviteToken = createdInvite!.InviteToken,
            memberName = "Redeemer Idempotent",
            memberEmail = inviteEmail
        });
        Assert.Equal(HttpStatusCode.OK, firstRedeem.StatusCode);

        var secondRedeem = await redeemerClient.PostAsJsonAsync("/api/v1/families/invites/redeem", new
        {
            inviteToken = createdInvite.InviteToken,
            memberName = "Redeemer Idempotent",
            memberEmail = inviteEmail
        });
        Assert.Equal(HttpStatusCode.OK, secondRedeem.StatusCode);

        var secondPayload = await secondRedeem.Content.ReadFromJsonAsync<RedeemFamilyInviteResponse>();
        Assert.NotNull(secondPayload);
        Assert.False(secondPayload!.CreatedNewMember);
        Assert.Equal("Accepted", secondPayload.Invite.Status);
    }

    [Fact]
    public async Task RecurringExecutionRepository_HasExecution_ChecksIdempotencyKey()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRecurringBillExecutionRepository>();

        var dueDate = new DateOnly(2026, 3, 7);
        var initialHasExecution = await repo.HasExecutionAsync(TestApiFactory.RecurringBillAId, dueDate);
        Assert.False(initialHasExecution);

        await repo.AddAsync(new RecurringBillExecution(
            Guid.NewGuid(),
            TestApiFactory.RecurringBillAId,
            TestApiFactory.FamilyAId,
            dueDate,
            DateTimeOffset.UtcNow,
            transactionId: null,
            result: "Posted",
            notes: "Integration test"));

        var finalHasExecution = await repo.HasExecutionAsync(TestApiFactory.RecurringBillAId, dueDate);
        Assert.True(finalHasExecution);
    }

    [Fact]
    public async Task UserA_Can_List_Own_RecurringBill_Executions()
    {
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.RecurringBillExecutions.Add(new RecurringBillExecution(
                Guid.NewGuid(),
                TestApiFactory.RecurringBillAId,
                TestApiFactory.FamilyAId,
                new DateOnly(2026, 3, 1),
                DateTimeOffset.UtcNow,
                transactionId: TestApiFactory.TransactionAId,
                result: "Posted",
                notes: null));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/recurring-bills/{TestApiFactory.RecurringBillAId}/executions?take=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<RecurringBillExecutionResponse>>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!);
        Assert.Equal(TestApiFactory.RecurringBillAId, payload![0].RecurringBillId);
        Assert.StartsWith($"{TestApiFactory.RecurringBillAId:N}:", payload[0].IdempotencyKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UserA_Can_Filter_Own_RecurringBill_Executions_By_Result_And_DateRange()
    {
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.RecurringBillExecutions.AddRange(
                new RecurringBillExecution(
                    Guid.NewGuid(),
                    TestApiFactory.RecurringBillAId,
                    TestApiFactory.FamilyAId,
                    new DateOnly(2026, 3, 1),
                    new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero),
                    transactionId: null,
                    result: "FilterFailed",
                    notes: "failed march"),
                new RecurringBillExecution(
                    Guid.NewGuid(),
                    TestApiFactory.RecurringBillAId,
                    TestApiFactory.FamilyAId,
                    new DateOnly(2026, 2, 1),
                    new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero),
                    transactionId: null,
                    result: "FilterFailed",
                    notes: "failed february"),
                new RecurringBillExecution(
                    Guid.NewGuid(),
                    TestApiFactory.RecurringBillAId,
                    TestApiFactory.FamilyAId,
                    new DateOnly(2026, 3, 1),
                    new DateTimeOffset(2026, 3, 1, 13, 0, 0, TimeSpan.Zero),
                    transactionId: null,
                    result: "Posted",
                    notes: "posted march"));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/recurring-bills/{TestApiFactory.RecurringBillAId}/executions?take=20&result=FilterFailed&fromDate=2026-03-01&toDate=2026-03-31");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<RecurringBillExecutionResponse>>();
        Assert.NotNull(payload);
        Assert.Single(payload!);
        Assert.Equal("FilterFailed", payload[0].Result);
        Assert.Equal(new DateOnly(2026, 3, 1), payload[0].DueDate);
        Assert.Equal("failed march", payload[0].Notes);
    }

    [Fact]
    public async Task UserA_Cannot_List_FamilyB_RecurringBill_Executions()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/recurring-bills/{TestApiFactory.RecurringBillBId}/executions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Run_Manual_Recurring_AutoPost_For_Own_Family()
    {
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var recurringBillId = Guid.NewGuid();

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Manual AutoPost Family", DateTimeOffset.UtcNow));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                TestApiFactory.UserAId,
                "Owner User",
                EmailAddress.Parse($"manual-autopost-owner-{familyId:N}@test.dev"),
                MemberRole.Parent));
            dbContext.Accounts.Add(new Account(
                accountId,
                familyId,
                "Manual AutoPost Checking",
                AccountType.Checking,
                Money.FromDecimal(1000m)));
            dbContext.RecurringBills.Add(new RecurringBill(
                recurringBillId,
                familyId,
                "Manual AutoPost Bill",
                "Manual Merchant",
                Money.FromDecimal(75m),
                RecurringBillFrequency.Monthly,
                dayOfMonth: 1,
                startDate: new DateOnly(2026, 1, 1),
                endDate: null,
                isActive: true));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync($"/api/v1/families/{familyId}/recurring-bills/auto-post/run?dueDate=2026-03-01", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RecurringAutoPostRunResponse>();
        Assert.NotNull(payload);
        Assert.Equal(familyId, payload!.FamilyId);
        Assert.Equal(1, payload.PostedCount);
        Assert.Equal(1, payload.DueBillCount);
        Assert.Contains(payload.Executions, execution =>
            execution.RecurringBillId == recurringBillId
            && execution.Result == "Posted");
    }

    [Fact]
    public async Task UserA_Cannot_Run_Manual_Recurring_AutoPost_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/recurring-bills/auto-post/run?dueDate=2026-03-01",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teen_With_Family_Access_Cannot_Run_Manual_Recurring_AutoPost()
    {
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var recurringBillId = Guid.NewGuid();
        const string teenUserId = "teen-user-autopost";

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Teen AutoPost Family", DateTimeOffset.UtcNow));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                teenUserId,
                "Teen User",
                EmailAddress.Parse($"teen-autopost-{familyId:N}@test.dev"),
                MemberRole.Teen));
            dbContext.Accounts.Add(new Account(
                accountId,
                familyId,
                "Teen AutoPost Checking",
                AccountType.Checking,
                Money.FromDecimal(800m)));
            dbContext.RecurringBills.Add(new RecurringBill(
                recurringBillId,
                familyId,
                "Teen Restricted Bill",
                "Teen Restricted Merchant",
                Money.FromDecimal(25m),
                RecurringBillFrequency.Monthly,
                dayOfMonth: 1,
                startDate: new DateOnly(2026, 1, 1),
                endDate: null,
                isActive: true));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, teenUserId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Teen");

        var response = await client.PostAsync(
            $"/api/v1/families/{familyId}/recurring-bills/auto-post/run?dueDate=2026-03-01",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_And_Update_Own_Onboarding_Profile()
    {
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Onboarding Profile Family", now));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                TestApiFactory.UserAId,
                "Owner User",
                EmailAddress.Parse($"onboarding-owner-{familyId:N}@test.dev"),
                MemberRole.Parent));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var getResponse = await client.GetAsync($"/api/v1/families/{familyId}/onboarding");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var initial = await getResponse.Content.ReadFromJsonAsync<OnboardingProfileResponse>();
        Assert.NotNull(initial);
        Assert.False(initial!.MembersCompleted);
        Assert.False(initial!.AccountsCompleted);
        Assert.False(initial.EnvelopesCompleted);
        Assert.False(initial.BudgetCompleted);
        Assert.False(initial.PlaidCompleted);
        Assert.False(initial.StripeAccountsCompleted);
        Assert.False(initial.CardsCompleted);
        Assert.False(initial.AutomationCompleted);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/families/{familyId}/onboarding", new
        {
            membersCompleted = true,
            accountsCompleted = true,
            envelopesCompleted = true,
            budgetCompleted = true,
            plaidCompleted = true,
            stripeAccountsCompleted = true,
            cardsCompleted = true,
            automationCompleted = true
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<OnboardingProfileResponse>();
        Assert.NotNull(updated);
        Assert.True(updated!.IsCompleted);
        Assert.NotNull(updated.CompletedAtUtc);
    }

    [Fact]
    public async Task UserA_Cannot_Update_FamilyB_Onboarding_Profile()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/onboarding", new
        {
            membersCompleted = true,
            accountsCompleted = true,
            envelopesCompleted = true,
            budgetCompleted = true,
            plaidCompleted = true,
            stripeAccountsCompleted = true,
            cardsCompleted = true,
            automationCompleted = true
        });

        Assert.Equal(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Get_FamilyB_Onboarding_Profile()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/onboarding");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Reconcile_Own_Onboarding_Profile_From_Family_Data()
    {
        var familyId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var envelopeId = Guid.NewGuid();
        var envelopeFinancialAccountId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Onboarding Reconcile Family", now));
            dbContext.FamilyMembers.AddRange(
                new FamilyMember(
                    Guid.NewGuid(),
                    familyId,
                    TestApiFactory.UserAId,
                    "Owner User",
                    EmailAddress.Parse($"reconcile-owner-{familyId:N}@test.dev"),
                    MemberRole.Parent),
                new FamilyMember(
                    Guid.NewGuid(),
                    familyId,
                    "member-b",
                    "Second Member",
                    EmailAddress.Parse($"reconcile-member-{familyId:N}@test.dev"),
                    MemberRole.Adult));
            dbContext.Accounts.Add(new Account(
                accountId,
                familyId,
                "Reconcile Checking",
                AccountType.Checking,
                Money.FromDecimal(900m)));
            dbContext.Envelopes.Add(new Envelope(
                envelopeId,
                familyId,
                "Reconcile Envelope",
                Money.FromDecimal(250m),
                Money.FromDecimal(125m)));
            dbContext.Budgets.Add(new Budget(
                Guid.NewGuid(),
                familyId,
                BudgetMonth.Parse("2027-03"),
                Money.FromDecimal(4500m)));
            dbContext.PlaidAccountLinks.Add(new PlaidAccountLink(
                Guid.NewGuid(),
                familyId,
                accountId,
                "plaid_reconcile_account",
                now,
                now));
            dbContext.EnvelopeFinancialAccounts.Add(new EnvelopeFinancialAccount(
                envelopeFinancialAccountId,
                familyId,
                envelopeId,
                "Stripe",
                "fa_reconcile_001",
                now,
                now));
            dbContext.EnvelopePaymentCards.Add(new EnvelopePaymentCard(
                Guid.NewGuid(),
                familyId,
                envelopeId,
                envelopeFinancialAccountId,
                "Stripe",
                "card_reconcile_001",
                "Virtual",
                "Active",
                "Visa",
                "4242",
                now,
                now));
            dbContext.AutomationRules.Add(new AutomationRule(
                Guid.NewGuid(),
                familyId,
                "Auto Rule Reconcile",
                AutomationRuleType.Categorization,
                priority: 1,
                isEnabled: true,
                conditionsJson: "{\"merchantContains\":\"Store\"}",
                actionJson: "{\"setCategory\":\"Groceries\"}",
                createdAt: now,
                updatedAt: now));

            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{familyId}/onboarding/reconcile",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OnboardingProfileResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.MembersCompleted);
        Assert.True(payload.AccountsCompleted);
        Assert.True(payload.EnvelopesCompleted);
        Assert.True(payload.BudgetCompleted);
        Assert.True(payload.PlaidCompleted);
        Assert.True(payload.StripeAccountsCompleted);
        Assert.True(payload.CardsCompleted);
        Assert.True(payload.AutomationCompleted);
        Assert.True(payload.IsCompleted);
        Assert.NotNull(payload.CompletedAtUtc);
    }

    [Fact]
    public async Task UserA_Cannot_Reconcile_FamilyB_Onboarding_Profile()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/onboarding/reconcile",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Run_Onboarding_Bootstrap_For_Own_Family()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/onboarding/bootstrap", new
        {
            accounts = new[]
            {
                new
                {
                    name = "Starter Savings",
                    type = "Savings",
                    openingBalance = 250.00m
                }
            },
            envelopes = new[]
            {
                new
                {
                    name = "Utilities",
                    monthlyBudget = 180.00m
                }
            },
            budget = new
            {
                month = "2027-01",
                totalIncome = 5000.00m
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<OnboardingBootstrapResponse>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.AccountsCreated);
        Assert.Equal(1, payload.EnvelopesCreated);
        Assert.True(payload.BudgetCreated);
    }

    [Fact]
    public async Task UserA_Cannot_Run_Onboarding_Bootstrap_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/onboarding/bootstrap", new
        {
            accounts = new object[] { },
            envelopes = new object[] { },
            budget = new
            {
                month = "2027-02",
                totalIncome = 5000.00m
            }
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_Own_Financial_Status()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/financial/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<FamilyFinancialStatusResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.FamilyAId, payload!.FamilyId);
        Assert.False(payload.PlaidConnected);
        Assert.False(payload.StripeConnected);
        Assert.True(response.Headers.TryGetValues("X-Trace-Id", out var traceHeaderValues));
        Assert.False(string.IsNullOrWhiteSpace(traceHeaderValues!.FirstOrDefault()));
    }

    [Fact]
    public async Task UserA_Cannot_Get_FamilyB_Financial_Status()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/financial/status");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Rewrap_Own_Family_Provider_Secrets()
    {
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Secret Rewrap Family", now));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                TestApiFactory.UserAId,
                "User A Rewrap",
                EmailAddress.Parse($"rewrap-{familyId:N}@test.dev"),
                MemberRole.Parent));
            dbContext.FamilyFinancialProfiles.Add(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                plaidItemId: "item_legacy",
                plaidAccessToken: "legacy_plaid_token",
                stripeCustomerId: "legacy_cus",
                stripeDefaultPaymentMethodId: "legacy_pm",
                createdAtUtc: now,
                updatedAtUtc: now));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{familyId}/financial/security/rewrap-provider-secrets",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RewrapProviderSecretsResponse>();
        Assert.NotNull(payload);
        Assert.Equal(familyId, payload!.FamilyId);
        Assert.True(payload.ProfileFound);
        Assert.Equal(3, payload.FieldsTouched);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var stored = await verifyDbContext.FamilyFinancialProfiles
            .AsNoTracking()
            .SingleAsync(x => x.FamilyId == familyId);
        Assert.StartsWith("enc:v1:", stored.PlaidAccessToken, StringComparison.Ordinal);
        Assert.StartsWith("enc:v1:", stored.StripeCustomerId, StringComparison.Ordinal);
        Assert.StartsWith("enc:v1:", stored.StripeDefaultPaymentMethodId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adult_With_Family_Access_Cannot_Rewrap_Provider_Secrets()
    {
        var familyId = Guid.NewGuid();
        const string adultUserId = "adult-user-rewrap";

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Adult Rewrap Family", DateTimeOffset.UtcNow));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                adultUserId,
                "Adult User",
                EmailAddress.Parse($"adult-rewrap-{familyId:N}@test.dev"),
                MemberRole.Adult));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, adultUserId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Adult");

        var response = await client.PostAsync(
            $"/api/v1/families/{familyId}/financial/security/rewrap-provider-secrets",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Rewrap_FamilyB_Provider_Secrets()
    {
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Forbidden Rewrap Family", now));
            dbContext.FamilyMembers.Add(new FamilyMember(
                Guid.NewGuid(),
                familyId,
                TestApiFactory.UserBId,
                "User B Rewrap",
                EmailAddress.Parse($"rewrap-b-{familyId:N}@test.dev"),
                MemberRole.Parent));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{familyId}/financial/security/rewrap-provider-secrets",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_Own_Provider_Activity_Health()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProviderActivityHealthResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.FamilyAId, payload!.FamilyId);
        Assert.Equal("Degraded", payload.NotificationDispatch.Status);
        Assert.True(payload.NotificationDispatch.FailedCount >= 1);
        Assert.NotNull(payload.LastStripeWebhook);
        Assert.Equal("webhook.family_a", payload.LastStripeWebhook!.EventType);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
        Assert.True(response.Headers.TryGetValues("X-Trace-Id", out var traceHeaderValues));
        Assert.False(string.IsNullOrWhiteSpace(traceHeaderValues!.FirstOrDefault()));
    }

    [Fact]
    public async Task UserA_Cannot_Get_FamilyB_Provider_Activity_Health()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/financial/provider-activity");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_Own_Provider_Activity_Timeline()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity/timeline?take=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProviderActivityTimelineResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.FamilyAId, payload!.FamilyId);
        Assert.Equal(20, payload.RequestedTake);
        Assert.Contains(
            payload.Events,
            evt => evt.Source == "StripeWebhook" && evt.EventType == "webhook.family_a");
        Assert.Contains(
            payload.Events,
            evt => evt.Source == "StripeWebhook"
                   && evt.StripeWebhookEventId == TestApiFactory.StripeWebhookRecordAId);
        Assert.Contains(
            payload.Events,
            evt => evt.Source == "PlaidWebhook" && evt.EventType == "TRANSACTIONS.SYNC_UPDATES_AVAILABLE");
        Assert.Contains(
            payload.Events,
            evt => evt.Source == "NotificationDispatch"
                   && evt.Status == "Failed"
                   && evt.NotificationDispatchEventId.HasValue);
        Assert.DoesNotContain(
            payload.Events,
            evt => evt.EventType == "webhook.family_b_marker");
        Assert.DoesNotContain(
            payload.Events,
            evt => evt.EventType == "BALANCE.DEFAULT_UPDATE");
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
        Assert.True(response.Headers.TryGetValues("X-Trace-Id", out var traceHeaderValues));
        Assert.False(string.IsNullOrWhiteSpace(traceHeaderValues!.FirstOrDefault()));
    }

    [Fact]
    public async Task UserA_Can_Filter_Own_Provider_Activity_Timeline_By_Plaid_Source()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity/timeline?take=20&source=PlaidWebhook");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProviderActivityTimelineResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.FamilyAId, payload!.FamilyId);
        Assert.NotEmpty(payload.Events);
        Assert.All(payload.Events, timelineEvent => Assert.Equal("PlaidWebhook", timelineEvent.Source));
        Assert.Contains(payload.Events, timelineEvent => timelineEvent.EventType == "TRANSACTIONS.SYNC_UPDATES_AVAILABLE");
        Assert.DoesNotContain(payload.Events, timelineEvent => timelineEvent.EventType == "BALANCE.DEFAULT_UPDATE");
    }

    [Fact]
    public async Task UserA_Can_Filter_Own_Provider_Activity_Timeline_By_Source_And_Status()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity/timeline?take=20&source=NotificationDispatch&status=failed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProviderActivityTimelineResponse>();
        Assert.NotNull(payload);
        Assert.All(payload!.Events, timelineEvent =>
        {
            Assert.Equal("NotificationDispatch", timelineEvent.Source);
            Assert.Equal("Failed", timelineEvent.Status, ignoreCase: true);
        });
        Assert.NotEmpty(payload.Events);
        Assert.Equal(20, payload.RequestedTake);
    }

    [Fact]
    public async Task Provider_Activity_Timeline_With_Invalid_Source_Filter_Returns400()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity/timeline?source=UnknownSource");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Get_FamilyB_Provider_Activity_Timeline()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/financial/provider-activity/timeline");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Get_Own_Notification_Preferences()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/notifications/preferences");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<NotificationPreferenceResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.FamilyAId, payload!.FamilyId);
        Assert.Equal(TestApiFactory.UserAId, payload.UserId);
    }

    [Fact]
    public async Task UserA_Cannot_Get_FamilyB_Notification_Preferences()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/notifications/preferences");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_List_Own_Failed_Notification_Dispatch_Events()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/notifications/dispatch-events/failed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<FailedNotificationDispatchEventResponse>>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!);
        Assert.Contains(payload!, evt => evt.Id == TestApiFactory.NotificationEventA2Id);
    }

    [Fact]
    public async Task UserA_Can_Retry_Own_Failed_Notification_Dispatch_Event()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/notifications/dispatch-events/{TestApiFactory.NotificationEventAId}/retry",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<RetryNotificationDispatchEventResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.NotificationEventAId, payload!.Id);
        Assert.Equal("Sent", payload.Status);
        Assert.True(payload.AttemptCount >= 4);
        Assert.NotNull(payload.SentAtUtc);
    }

    [Fact]
    public async Task UserA_Cannot_Retry_FamilyB_Failed_Notification_Dispatch_Event()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/notifications/dispatch-events/{TestApiFactory.NotificationEventBId}/retry",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Replay_Own_Failed_Notification_Dispatch_Event_From_Timeline_Idempotently()
    {
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            var notification = new SpendNotificationEvent(
                eventId,
                TestApiFactory.FamilyAId,
                TestApiFactory.UserAId,
                TestApiFactory.EnvelopeAId,
                Guid.NewGuid(),
                "evt_timeline_replay_a",
                "Email",
                12.34m,
                "Replay Merchant",
                87.66m,
                now.AddMinutes(-5));
            notification.MarkRetry("attempt 1", now.AddMinutes(-4), 3);
            notification.MarkRetry("attempt 2", now.AddMinutes(-3), 3);
            notification.MarkRetry("attempt 3", now.AddMinutes(-2), 3);
            dbContext.SpendNotificationEvents.Add(notification);
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var firstReplay = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity/timeline/notifications/{eventId}/replay",
            content: null);
        Assert.Equal(HttpStatusCode.OK, firstReplay.StatusCode);
        var firstPayload = await firstReplay.Content.ReadFromJsonAsync<RetryNotificationDispatchEventResponse>();
        Assert.NotNull(firstPayload);
        Assert.Equal("Sent", firstPayload!.Status);
        Assert.True(firstPayload.AttemptCount >= 4);
        Assert.NotNull(firstPayload.SentAtUtc);

        var secondReplay = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity/timeline/notifications/{eventId}/replay",
            content: null);
        Assert.Equal(HttpStatusCode.OK, secondReplay.StatusCode);
        var secondPayload = await secondReplay.Content.ReadFromJsonAsync<RetryNotificationDispatchEventResponse>();
        Assert.NotNull(secondPayload);
        Assert.Equal("Sent", secondPayload!.Status);
        Assert.Equal(firstPayload.AttemptCount, secondPayload.AttemptCount);
        Assert.Equal(firstPayload.SentAtUtc, secondPayload.SentAtUtc);
    }

    [Fact]
    public async Task UserA_Cannot_Replay_FamilyB_Notification_Dispatch_Event_From_Timeline()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/financial/provider-activity/timeline/notifications/{TestApiFactory.NotificationEventBId}/replay",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Replay_Own_Failed_Stripe_Webhook_Event_From_Timeline()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/provider-activity/timeline/stripe-webhooks/{TestApiFactory.StripeWebhookRecordAFailedId}/replay",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ReplayStripeWebhookEventResponse>();
        Assert.NotNull(payload);
        Assert.Equal(TestApiFactory.StripeWebhookRecordAFailedId, payload!.Id);
        Assert.Equal("Replayed", payload.Status);
        Assert.Equal("Ignored", payload.Outcome);
        Assert.Null(payload.ErrorMessage);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var persisted = await dbContext.StripeWebhookEvents
            .AsNoTracking()
            .SingleAsync(x => x.Id == TestApiFactory.StripeWebhookRecordAFailedId);
        Assert.Equal("Replayed", persisted.ProcessingStatus);
        Assert.Null(persisted.ErrorMessage);
    }

    [Fact]
    public async Task UserA_Cannot_Replay_FamilyB_Stripe_Webhook_Event_From_Timeline()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/financial/provider-activity/timeline/stripe-webhooks/{TestApiFactory.StripeWebhookRecordBId}/replay",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_List_Own_Family_Financial_Accounts()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/financial-accounts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<EnvelopeFinancialAccountResponse>>();
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task UserA_Cannot_List_FamilyB_Financial_Accounts()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/financial-accounts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Link_Stripe_Envelope_Financial_Account_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/envelopes/{TestApiFactory.EnvelopeBId}/financial-accounts/stripe",
            new
            {
                displayName = "Family B Envelope"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Create_Plaid_Account_Link_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/financial/plaid/account-links",
            new
            {
                accountId = TestApiFactory.AccountBId,
                plaidAccountId = "plaid_account_blocked"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_Delete_Own_Plaid_Account_Link()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.DeleteAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/plaid/account-links/{TestApiFactory.PlaidLinkAId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var listResponse = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyAId}/financial/plaid/account-links");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var payload = await listResponse.Content.ReadFromJsonAsync<List<PlaidAccountLinkResponse>>();
        Assert.NotNull(payload);
        Assert.DoesNotContain(payload!, link => link.Id == TestApiFactory.PlaidLinkAId);
    }

    [Fact]
    public async Task UserA_Cannot_Delete_FamilyB_Plaid_Account_Link()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.DeleteAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/financial/plaid/account-links/{TestApiFactory.PlaidLinkBId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Sync_Plaid_Transactions_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/financial/plaid/sync-transactions",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Refresh_Plaid_Balances_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/financial/plaid/refresh-balances",
            content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Get_Plaid_Reconciliation_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/financial/plaid/reconciliation");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Can_List_Own_Envelope_Cards()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/envelopes/{TestApiFactory.EnvelopeAId}/cards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<EnvelopePaymentCardResponse>>();
        Assert.NotNull(payload);
    }

    [Fact]
    public async Task UserA_Cannot_List_FamilyB_Envelope_Cards()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync($"/api/v1/families/{TestApiFactory.FamilyBId}/envelopes/{TestApiFactory.EnvelopeBId}/cards");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Issue_Virtual_Card_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/envelopes/{TestApiFactory.EnvelopeBId}/cards/virtual",
            new
            {
                cardholderName = "Blocked User"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Issue_Physical_Card_For_FamilyB()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/envelopes/{TestApiFactory.EnvelopeBId}/cards/physical",
            new
            {
                cardholderName = "Blocked User",
                recipientName = "Blocked User",
                addressLine1 = "123 Main St",
                addressLine2 = (string?)null,
                city = "Austin",
                stateOrProvince = "TX",
                postalCode = "78701",
                countryCode = "US"
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Get_FamilyB_Physical_Card_Issuance()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/envelopes/{TestApiFactory.EnvelopeBId}/cards/{Guid.NewGuid()}/issuance");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_Upsert_FamilyB_Card_Controls()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/envelopes/{TestApiFactory.EnvelopeBId}/cards/{Guid.NewGuid()}/controls",
            new
            {
                dailyLimitAmount = 25m,
                allowedMerchantCategories = new[] { "grocery_stores" },
                allowedMerchantNames = new[] { "Target" }
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserA_Cannot_List_FamilyB_Card_Control_Audit()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.GetAsync(
            $"/api/v1/families/{TestApiFactory.FamilyBId}/envelopes/{TestApiFactory.EnvelopeBId}/cards/{Guid.NewGuid()}/controls/audit");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Onboarding_Bootstrap_Rejects_Duplicate_Account_Names_In_Request()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, TestApiFactory.UserAId);

        var response = await client.PostAsJsonAsync($"/api/v1/families/{TestApiFactory.FamilyAId}/onboarding/bootstrap", new
        {
            accounts = new[]
            {
                new { name = "Duplicate Name", type = "Checking", openingBalance = 50m },
                new { name = "Duplicate Name", type = "Savings", openingBalance = 10m }
            },
            envelopes = Array.Empty<object>(),
            budget = (object?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task System_Health_Is_Available_Anonymously()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/system/health");
        var payload = await response.Content.ReadFromJsonAsync<ApiHealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Healthy", payload!.Status);
    }

    [Fact]
    public async Task System_Version_Is_Available_Anonymously()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/system/version");
        var payload = await response.Content.ReadFromJsonAsync<ApiVersionResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Version));
        Assert.False(string.IsNullOrWhiteSpace(payload.Environment));
    }

    [Fact]
    public async Task Stripe_Webhook_With_Invalid_Signature_Returns401()
    {
        using var client = _factory.CreateClient();
        var payload = "{\"id\":\"evt_invalid_sig\",\"type\":\"card_transaction\",\"data\":{\"object\":{\"card\":\"card_test\",\"amount\":100}}}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "t=1700000000,v1=invalidsignature");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Plaid_Webhook_With_InvalidJson_Returns400()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent("{\"webhook_type\":\"TRANSACTIONS\"", Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Plaid_Webhook_Without_Signature_When_Verification_Enabled_Returns401_And_Persists_Failure()
    {
        const string signingSecret = "plaid_webhook_test_secret";
        const string payload = "{\"webhook_type\":\"TRANSACTIONS\",\"webhook_code\":\"SYNC_UPDATES_AVAILABLE\",\"item_id\":\"item_unsigned\"}";

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOptions<PlaidWebhookVerificationOptions>>();
                services.AddSingleton<IOptions<PlaidWebhookVerificationOptions>>(
                    Options.Create(new PlaidWebhookVerificationOptions
                    {
                        Enabled = true,
                        SigningSecret = signingSecret,
                        SignatureToleranceSeconds = 300,
                        AllowUnsignedInDevelopment = false
                    }));
            }));

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var persisted = await dbContext.PlaidWebhookEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PayloadJson == payload);
        Assert.NotNull(persisted);
        Assert.Equal("Failed", persisted!.ProcessingStatus);
        Assert.Equal("Unknown", persisted.WebhookType);
        Assert.Contains("signature verification failed", persisted.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Plaid_Webhook_With_Valid_Signature_When_Verification_Enabled_ReturnsIgnored()
    {
        const string signingSecret = "plaid_webhook_test_secret";
        const string payload = "{\"webhook_type\":\"TRANSACTIONS\",\"webhook_code\":\"SYNC_UPDATES_AVAILABLE\",\"item_id\":\"item_unknown_signed\"}";

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOptions<PlaidWebhookVerificationOptions>>();
                services.AddSingleton<IOptions<PlaidWebhookVerificationOptions>>(
                    Options.Create(new PlaidWebhookVerificationOptions
                    {
                        Enabled = true,
                        SigningSecret = signingSecret,
                        SignatureToleranceSeconds = 300,
                        AllowUnsignedInDevelopment = false
                    }));
            }));

        using var client = factory.CreateClient();
        using var request = CreatePlaidWebhookRequest(payload, signingSecret);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        Assert.NotNull(result);
        Assert.Equal("Ignored", result!.Outcome);
        Assert.Equal("item_unknown_signed", result.ItemId);
    }

    [Fact]
    public async Task Plaid_Webhook_With_Unknown_Item_ReturnsIgnored()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(
                "{\"webhook_type\":\"TRANSACTIONS\",\"webhook_code\":\"SYNC_UPDATES_AVAILABLE\",\"item_id\":\"item_unknown\"}",
                Encoding.UTF8,
                "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Ignored", payload!.Outcome);
        Assert.Null(payload.FamilyId);
        Assert.Equal("item_unknown", payload.ItemId);
    }

    [Fact]
    public async Task Plaid_Webhook_With_UnsupportedType_ReturnsIgnored_WithFamilyContext()
    {
        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        const string itemId = "item_webhook_test_supported";

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Families.Add(new Family(familyId, "Plaid Webhook Family", now));
            dbContext.FamilyFinancialProfiles.Add(new FamilyFinancialProfile(
                Guid.NewGuid(),
                familyId,
                plaidItemId: itemId,
                plaidAccessToken: "token_webhook_test",
                stripeCustomerId: null,
                stripeDefaultPaymentMethodId: null,
                createdAtUtc: now,
                updatedAtUtc: now));
            await dbContext.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(
                $"{{\"webhook_type\":\"ITEM\",\"webhook_code\":\"WEBHOOK_UPDATE_ACKNOWLEDGED\",\"item_id\":\"{itemId}\"}}",
                Encoding.UTF8,
                "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Ignored", payload!.Outcome);
        Assert.Equal(familyId, payload.FamilyId);
        Assert.Equal("ITEM", payload.WebhookType);
    }

    [Fact]
    public async Task Plaid_Webhook_Transactions_With_Matched_Item_ReturnsProcessed()
    {
        var syncService = new TestPlaidTransactionSyncService();
        var balanceService = new TestPlaidBalanceReconciliationService();

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPlaidTransactionSyncService>();
                services.AddSingleton<IPlaidTransactionSyncService>(syncService);
                services.RemoveAll<IPlaidBalanceReconciliationService>();
                services.AddSingleton<IPlaidBalanceReconciliationService>(balanceService);
            }));

        var familyId = Guid.NewGuid();
        const string itemId = "item_webhook_processed";
        await SeedPlaidWebhookProfileAsync(factory.Services, familyId, itemId);

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(
                $"{{\"webhook_type\":\"TRANSACTIONS\",\"webhook_code\":\"SYNC_UPDATES_AVAILABLE\",\"item_id\":\"{itemId}\"}}",
                Encoding.UTF8,
                "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Processed", payload!.Outcome);
        Assert.Equal("TRANSACTIONS", payload.WebhookType);
        Assert.Equal(familyId, payload.FamilyId);
        Assert.Equal(1, syncService.SyncFamilyCallCount);
        Assert.Equal(familyId, syncService.LastFamilyId);
        Assert.Equal(0, balanceService.RefreshFamilyCallCount);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var persistedEvent = await dbContext.PlaidWebhookEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ItemId == itemId && x.FamilyId == familyId && x.ProcessingStatus == "Processed");
        Assert.NotNull(persistedEvent);
        Assert.Equal("TRANSACTIONS", persistedEvent!.WebhookType);
        Assert.Equal("SYNC_UPDATES_AVAILABLE", persistedEvent.WebhookCode);
    }

    [Fact]
    public async Task Plaid_Webhook_Transactions_Duplicate_Delivery_Is_Suppressed()
    {
        var syncService = new TestPlaidTransactionSyncService();
        var balanceService = new TestPlaidBalanceReconciliationService();

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPlaidTransactionSyncService>();
                services.AddSingleton<IPlaidTransactionSyncService>(syncService);
                services.RemoveAll<IPlaidBalanceReconciliationService>();
                services.AddSingleton<IPlaidBalanceReconciliationService>(balanceService);
            }));

        var familyId = Guid.NewGuid();
        const string itemId = "item_webhook_duplicate_transactions";
        const string payload = "{\"webhook_type\":\"TRANSACTIONS\",\"webhook_code\":\"SYNC_UPDATES_AVAILABLE\",\"item_id\":\"item_webhook_duplicate_transactions\"}";
        await SeedPlaidWebhookProfileAsync(factory.Services, familyId, itemId);

        using var client = factory.CreateClient();
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        Assert.NotNull(firstPayload);
        Assert.NotNull(secondPayload);
        Assert.Equal("Processed", firstPayload!.Outcome);
        Assert.Equal("Duplicate", secondPayload!.Outcome);
        Assert.Equal(1, syncService.SyncFamilyCallCount);
        Assert.Equal(0, balanceService.RefreshFamilyCallCount);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var persistedStatuses = await dbContext.PlaidWebhookEvents
            .AsNoTracking()
            .Where(x => x.ItemId == itemId)
            .OrderBy(x => x.ReceivedAtUtc)
            .Select(x => x.ProcessingStatus)
            .ToListAsync();
        Assert.Contains("Processed", persistedStatuses);
        Assert.Contains("Duplicate", persistedStatuses);
    }

    [Fact]
    public async Task Plaid_Webhook_Balance_Duplicate_Delivery_Is_Suppressed()
    {
        var syncService = new TestPlaidTransactionSyncService();
        var balanceService = new TestPlaidBalanceReconciliationService();

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPlaidTransactionSyncService>();
                services.AddSingleton<IPlaidTransactionSyncService>(syncService);
                services.RemoveAll<IPlaidBalanceReconciliationService>();
                services.AddSingleton<IPlaidBalanceReconciliationService>(balanceService);
            }));

        var familyId = Guid.NewGuid();
        const string itemId = "item_webhook_duplicate_balance";
        const string payload = "{\"webhook_type\":\"BALANCE\",\"webhook_code\":\"DEFAULT_UPDATE\",\"item_id\":\"item_webhook_duplicate_balance\"}";
        await SeedPlaidWebhookProfileAsync(factory.Services, familyId, itemId);

        using var client = factory.CreateClient();
        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        Assert.NotNull(firstPayload);
        Assert.NotNull(secondPayload);
        Assert.Equal("Processed", firstPayload!.Outcome);
        Assert.Equal("Duplicate", secondPayload!.Outcome);
        Assert.Equal(0, syncService.SyncFamilyCallCount);
        Assert.Equal(1, balanceService.RefreshFamilyCallCount);
    }

    [Fact]
    public async Task Plaid_Webhook_Transactions_With_Matched_Item_And_SyncFailure_ReturnsFailed()
    {
        var syncService = new TestPlaidTransactionSyncService { ThrowOnSync = true };
        var balanceService = new TestPlaidBalanceReconciliationService();

        using var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPlaidTransactionSyncService>();
                services.AddSingleton<IPlaidTransactionSyncService>(syncService);
                services.RemoveAll<IPlaidBalanceReconciliationService>();
                services.AddSingleton<IPlaidBalanceReconciliationService>(balanceService);
            }));

        var familyId = Guid.NewGuid();
        const string itemId = "item_webhook_failed";
        await SeedPlaidWebhookProfileAsync(factory.Services, familyId, itemId);

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(
                $"{{\"webhook_type\":\"TRANSACTIONS\",\"webhook_code\":\"SYNC_UPDATES_AVAILABLE\",\"item_id\":\"{itemId}\"}}",
                Encoding.UTF8,
                "application/json")
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<PlaidWebhookProcessResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Failed", payload!.Outcome);
        Assert.Equal("TRANSACTIONS", payload.WebhookType);
        Assert.Equal(familyId, payload.FamilyId);
        Assert.Equal(1, syncService.SyncFamilyCallCount);
        Assert.Equal(familyId, syncService.LastFamilyId);
        Assert.Equal(0, balanceService.RefreshFamilyCallCount);

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var dbContext = verificationScope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
        var persistedEvent = await dbContext.PlaidWebhookEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ItemId == itemId && x.FamilyId == familyId && x.ProcessingStatus == "Failed");
        Assert.NotNull(persistedEvent);
        Assert.Equal("TRANSACTIONS", persistedEvent!.WebhookType);
        Assert.Equal("SYNC_UPDATES_AVAILABLE", persistedEvent.WebhookCode);
    }

    private static HttpRequestMessage CreatePlaidWebhookRequest(string payload, string signingSecret)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = CreatePlaidSignature(payload, signingSecret, timestamp);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/plaid")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Plaid-Signature", $"t={timestamp.ToString(CultureInfo.InvariantCulture)},v1={signature}");
        return request;
    }

    private static string CreatePlaidSignature(string payload, string signingSecret, long timestamp)
    {
        var signedPayload = $"{timestamp.ToString(CultureInfo.InvariantCulture)}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
    }

    private static async Task SeedPlaidWebhookProfileAsync(IServiceProvider services, Guid familyId, string itemId)
    {
        var now = DateTimeOffset.UtcNow;
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();

        dbContext.Families.Add(new Family(familyId, $"Plaid Webhook Family {familyId:N}", now));
        dbContext.FamilyFinancialProfiles.Add(new FamilyFinancialProfile(
            Guid.NewGuid(),
            familyId,
            plaidItemId: itemId,
            plaidAccessToken: "token_webhook_test",
            stripeCustomerId: null,
            stripeDefaultPaymentMethodId: null,
            createdAtUtc: now,
            updatedAtUtc: now));

        await dbContext.SaveChangesAsync();
    }
}

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid FamilyAId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid FamilyBId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid AccountAId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    public static readonly Guid AccountBId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
    public static readonly Guid EnvelopeAId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");
    public static readonly Guid EnvelopeA2Id = Guid.Parse("aaaaaaaa-2222-2222-2222-222222222222");
    public static readonly Guid EnvelopeBId = Guid.Parse("bbbbbbbb-1111-1111-1111-111111111111");
    public static readonly Guid RecurringBillAId = Guid.Parse("12345678-90ab-cdef-1234-567890abcdef");
    public static readonly Guid RecurringBillBId = Guid.Parse("22345678-90ab-cdef-1234-567890abcdef");
    public static readonly Guid NotificationEventAId = Guid.Parse("31000000-0000-0000-0000-000000000001");
    public static readonly Guid NotificationEventA2Id = Guid.Parse("31000000-0000-0000-0000-000000000003");
    public static readonly Guid NotificationEventBId = Guid.Parse("31000000-0000-0000-0000-000000000002");
    public static readonly Guid StripeWebhookRecordAId = Guid.Parse("41000000-0000-0000-0000-000000000001");
    public static readonly Guid StripeWebhookRecordBId = Guid.Parse("41000000-0000-0000-0000-000000000002");
    public static readonly Guid StripeWebhookRecordAFailedId = Guid.Parse("41000000-0000-0000-0000-000000000003");
    public static readonly Guid PlaidWebhookRecordAId = Guid.Parse("42000000-0000-0000-0000-000000000001");
    public static readonly Guid PlaidWebhookRecordBId = Guid.Parse("42000000-0000-0000-0000-000000000002");
    public static readonly Guid PlaidLinkAId = Guid.Parse("21000000-0000-0000-0000-000000000001");
    public static readonly Guid PlaidLinkBId = Guid.Parse("21000000-0000-0000-0000-000000000002");
    public static readonly Guid TransactionAId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    public static readonly Guid TransactionBId = Guid.Parse("66666666-7777-8888-9999-aaaaaaaaaaaa");
    public const string UserAId = "user-a";
    public const string UserBId = "user-b";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "http://test-authority",
                ["Authentication:Audience"] = "dragonenvelopes-api",
                ["Stripe:Webhooks:Enabled"] = "true",
                ["Stripe:Webhooks:SigningSecret"] = "whsec_test",
                ["Stripe:Webhooks:SignatureToleranceSeconds"] = "300",
                ["Plaid:Webhooks:Enabled"] = "false",
                ["Plaid:Webhooks:AllowUnsignedInDevelopment"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IKeycloakProvisioningService>();
            services.AddSingleton<IKeycloakProvisioningService, TestKeycloakProvisioningService>();

            var dbDescriptor = services.SingleOrDefault(
                descriptor => descriptor.ServiceType == typeof(DbContextOptions<DragonEnvelopesDbContext>));
            if (dbDescriptor is not null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<DragonEnvelopesDbContext>(options =>
                options.UseInMemoryDatabase("dragonenvelopes-auth-isolation-tests"));

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.RemoveAll<IOptions<PlaidWebhookVerificationOptions>>();
            services.AddSingleton<IOptions<PlaidWebhookVerificationOptions>>(
                Options.Create(new PlaidWebhookVerificationOptions
                {
                    Enabled = false,
                    AllowUnsignedInDevelopment = false,
                    SigningSecret = string.Empty,
                    SignatureToleranceSeconds = 300
                }));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DragonEnvelopesDbContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            Seed(dbContext);
        });
    }

    private static void Seed(DragonEnvelopesDbContext dbContext)
    {
        var familyA = new Family(FamilyAId, "Family A", DateTimeOffset.UtcNow);
        var familyB = new Family(FamilyBId, "Family B", DateTimeOffset.UtcNow);
        dbContext.Families.AddRange(familyA, familyB);

        dbContext.FamilyMembers.AddRange(
            new FamilyMember(Guid.NewGuid(), FamilyAId, UserAId, "User A", EmailAddress.Parse("a@test.dev"), MemberRole.Parent),
            new FamilyMember(Guid.NewGuid(), FamilyBId, UserBId, "User B", EmailAddress.Parse("b@test.dev"), MemberRole.Parent));

        dbContext.Accounts.AddRange(
            new Account(AccountAId, FamilyAId, "Checking A", AccountType.Checking, Money.FromDecimal(1000m)),
            new Account(AccountBId, FamilyBId, "Checking B", AccountType.Checking, Money.FromDecimal(1000m)));

        dbContext.Envelopes.AddRange(
            new Envelope(EnvelopeAId, FamilyAId, "Groceries A", Money.FromDecimal(200m), Money.FromDecimal(100m)),
            new Envelope(EnvelopeA2Id, FamilyAId, "Groceries A2", Money.FromDecimal(200m), Money.FromDecimal(100m)),
            new Envelope(EnvelopeBId, FamilyBId, "Groceries B", Money.FromDecimal(200m), Money.FromDecimal(100m)));

        dbContext.Transactions.AddRange(
            new Transaction(
                TransactionAId,
                AccountAId,
                Money.FromDecimal(-10.00m),
                "Groceries",
                "Market A",
                DateTimeOffset.UtcNow,
                "Food",
                EnvelopeAId),
            new Transaction(
                TransactionBId,
                AccountBId,
                Money.FromDecimal(-11.00m),
                "Coffee",
                "Cafe B",
                DateTimeOffset.UtcNow,
                "Dining"));

        dbContext.RecurringBills.AddRange(
            new RecurringBill(
                RecurringBillAId,
                FamilyAId,
                "Rent",
                "Landlord",
                Money.FromDecimal(1000m),
                RecurringBillFrequency.Monthly,
                dayOfMonth: 1,
                startDate: new DateOnly(2026, 1, 1),
                endDate: null,
                isActive: true),
            new RecurringBill(
                RecurringBillBId,
                FamilyBId,
                "Utilities",
                "Utility Co",
                Money.FromDecimal(120m),
                RecurringBillFrequency.Monthly,
                dayOfMonth: 5,
                startDate: new DateOnly(2026, 1, 1),
                endDate: null,
                isActive: true));

        var now = DateTimeOffset.UtcNow;
        dbContext.StripeWebhookEvents.AddRange(
            new StripeWebhookEvent(
                StripeWebhookRecordAId,
                "evt_stripe_family_a",
                "webhook.family_a",
                FamilyAId,
                EnvelopeAId,
                cardId: null,
                "Processed",
                errorMessage: null,
                "{\"family\":\"A\"}",
                now.AddMinutes(-18),
                now.AddMinutes(-17)),
            new StripeWebhookEvent(
                StripeWebhookRecordBId,
                "evt_stripe_family_b",
                "webhook.family_b_marker",
                FamilyBId,
                EnvelopeBId,
                cardId: null,
                "Processed",
                errorMessage: "family_b_marker_error",
                "{\"family\":\"B\"}",
                now.AddMinutes(-16),
                now.AddMinutes(-15)),
            new StripeWebhookEvent(
                StripeWebhookRecordAFailedId,
                "evt_stripe_family_a_failed",
                "issuing_authorization.request",
                FamilyAId,
                EnvelopeAId,
                cardId: null,
                "Failed",
                errorMessage: "Simulated Stripe processing failure",
                "{\"id\":\"evt_stripe_family_a_failed\",\"type\":\"issuing_authorization.request\",\"data\":{\"object\":{\"amount\":100}}}",
                now.AddMinutes(-20),
                now.AddMinutes(-19)));

        dbContext.PlaidWebhookEvents.AddRange(
            new PlaidWebhookEvent(
                PlaidWebhookRecordAId,
                "TRANSACTIONS",
                "SYNC_UPDATES_AVAILABLE",
                "item_seed_family_a",
                FamilyAId,
                "Processed",
                errorMessage: null,
                "{\"family\":\"A\",\"source\":\"plaid\"}",
                now.AddMinutes(-14),
                now.AddMinutes(-13)),
            new PlaidWebhookEvent(
                PlaidWebhookRecordBId,
                "BALANCE",
                "DEFAULT_UPDATE",
                "item_seed_family_b",
                FamilyBId,
                "Failed",
                errorMessage: "family_b_plaid_failure",
                "{\"family\":\"B\",\"source\":\"plaid\"}",
                now.AddMinutes(-12),
                now.AddMinutes(-11)));

        dbContext.PlaidAccountLinks.AddRange(
            new PlaidAccountLink(
                PlaidLinkAId,
                FamilyAId,
                AccountAId,
                "plaid_account_a",
                now,
                now),
            new PlaidAccountLink(
                PlaidLinkBId,
                FamilyBId,
                AccountBId,
                "plaid_account_b",
                now,
                now));

        var notificationA = new SpendNotificationEvent(
            NotificationEventAId,
            FamilyAId,
            UserAId,
            EnvelopeAId,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "evt_notification_a",
            "Email",
            23.75m,
            "Seed Market A",
            76.25m,
            now.AddMinutes(-30));
        notificationA.MarkRetry("seed retry 1", now.AddMinutes(-29), 3);
        notificationA.MarkRetry("seed retry 2", now.AddMinutes(-28), 3);
        notificationA.MarkRetry("seed retry 3", now.AddMinutes(-27), 3);

        var notificationA2 = new SpendNotificationEvent(
            NotificationEventA2Id,
            FamilyAId,
            UserAId,
            EnvelopeA2Id,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "evt_notification_a2",
            "Email",
            9.99m,
            "Seed Utility",
            90.01m,
            now.AddMinutes(-26));
        notificationA2.MarkRetry("seed retry 1", now.AddMinutes(-25), 3);
        notificationA2.MarkRetry("seed retry 2", now.AddMinutes(-24), 3);
        notificationA2.MarkRetry("seed retry 3", now.AddMinutes(-23), 3);

        var notificationB = new SpendNotificationEvent(
            NotificationEventBId,
            FamilyBId,
            UserBId,
            EnvelopeBId,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "evt_notification_b",
            "Email",
            14.50m,
            "Seed Market B",
            85.50m,
            now.AddMinutes(-22));
        notificationB.MarkRetry("seed retry 1", now.AddMinutes(-21), 3);
        notificationB.MarkRetry("seed retry 2", now.AddMinutes(-20), 3);
        notificationB.MarkRetry("seed retry 3", now.AddMinutes(-19), 3);

        dbContext.SpendNotificationEvents.AddRange(notificationA, notificationA2, notificationB);

        dbContext.SaveChanges();
    }
}

public sealed class TestPlaidTransactionSyncService : IPlaidTransactionSyncService
{
    public int SyncFamilyCallCount { get; private set; }

    public Guid? LastFamilyId { get; private set; }

    public bool ThrowOnSync { get; init; }

    public Task<PlaidAccountLinkDetails> UpsertAccountLinkAsync(
        Guid familyId,
        Guid accountId,
        string plaidAccountId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not used in this test service.");
    }

    public Task<IReadOnlyList<PlaidAccountLinkDetails>> ListAccountLinksAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PlaidAccountLinkDetails>>([]);
    }

    public Task DeleteAccountLinkAsync(
        Guid familyId,
        Guid linkId,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Not used in this test service.");
    }

    public Task<PlaidTransactionSyncDetails> SyncFamilyAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        SyncFamilyCallCount += 1;
        LastFamilyId = familyId;

        if (ThrowOnSync)
        {
            throw new InvalidOperationException("Simulated Plaid sync failure.");
        }

        return Task.FromResult(new PlaidTransactionSyncDetails(
            familyId,
            PulledCount: 3,
            InsertedCount: 2,
            DedupedCount: 1,
            UnmappedCount: 0,
            NextCursor: "cursor_webhook",
            ProcessedAtUtc: DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<PlaidTransactionSyncDetails>> SyncConnectedFamiliesAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PlaidTransactionSyncDetails>>([]);
    }
}

public sealed class TestPlaidBalanceReconciliationService : IPlaidBalanceReconciliationService
{
    public int RefreshFamilyCallCount { get; private set; }

    public Task<PlaidBalanceRefreshDetails> RefreshFamilyBalancesAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        RefreshFamilyCallCount += 1;
        return Task.FromResult(new PlaidBalanceRefreshDetails(
            familyId,
            RefreshedCount: 1,
            DriftedCount: 0,
            TotalAbsoluteDrift: 0m,
            RefreshedAtUtc: DateTimeOffset.UtcNow));
    }

    public Task<PlaidReconciliationReportDetails> GetReconciliationReportAsync(
        Guid familyId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PlaidReconciliationReportDetails(
            familyId,
            DateTimeOffset.UtcNow,
            []));
    }

    public Task<IReadOnlyList<PlaidBalanceRefreshDetails>> RefreshConnectedFamiliesAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PlaidBalanceRefreshDetails>>([]);
    }
}

public sealed class TestKeycloakProvisioningService : IKeycloakProvisioningService
{
    private readonly Dictionary<string, string> _emailToUserId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _userIdToEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task<string> CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (_emailToUserId.ContainsKey(normalizedEmail))
        {
            throw new DomainValidationException("A Keycloak user with this email already exists.");
        }

        var userId = $"test-kc-{Guid.NewGuid():N}";
        _emailToUserId[normalizedEmail] = userId;
        _userIdToEmail[userId] = normalizedEmail;
        return Task.FromResult(userId);
    }

    public Task AssignRealmRoleAsync(
        string userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_userIdToEmail.Remove(userId, out var email))
        {
            _emailToUserId.Remove(email);
        }

        return Task.CompletedTask;
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";
    public const string UserHeader = "X-Test-User";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var userId) || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var roles = Request.Headers.TryGetValue(RoleHeader, out var roleHeaderValue)
            ? roleHeaderValue.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(static role => !string.IsNullOrWhiteSpace(role))
                .ToArray()
            : [];
        if (roles.Length == 0)
        {
            roles = ["Parent"];
        }

        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim("preferred_username", userId.ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
