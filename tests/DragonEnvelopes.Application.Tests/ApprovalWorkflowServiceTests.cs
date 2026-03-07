using DragonEnvelopes.Application.DTOs;
using DragonEnvelopes.Application.Interfaces;
using DragonEnvelopes.Application.Services;
using DragonEnvelopes.Domain.Entities;
using Moq;

namespace DragonEnvelopes.Application.Tests;

public sealed class ApprovalWorkflowServiceTests
{
    [Fact]
    public async Task TryCreateBlockedRequestAsync_WhenPolicyRequiresApproval_CreatesBlockedRequest_AndTimelineEvent()
    {
        var familyId = Guid.Parse("c1000000-0000-0000-0000-000000000001");
        var accountId = Guid.Parse("c1000000-0000-0000-0000-000000000002");
        var now = new DateTimeOffset(2026, 3, 7, 18, 0, 0, TimeSpan.Zero);

        var policyRepository = new Mock<IApprovalPolicyRepository>(MockBehavior.Strict);
        policyRepository
            .Setup(x => x.GetByFamilyIdAsync(familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FamilyApprovalPolicy.Create(
                Guid.NewGuid(),
                familyId,
                isEnabled: true,
                amountThreshold: 100m,
                rolesRequiringApproval: ["Child"],
                updatedAtUtc: now));

        var requestRepository = new Mock<IApprovalRequestRepository>(MockBehavior.Strict);
        PurchaseApprovalRequest? persistedRequest = null;
        requestRepository
            .Setup(x => x.AddAsync(It.IsAny<PurchaseApprovalRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseApprovalRequest, CancellationToken>((request, _) => persistedRequest = request)
            .Returns(Task.CompletedTask);

        PurchaseApprovalTimelineEvent? persistedTimeline = null;
        requestRepository
            .Setup(x => x.AddTimelineEventAsync(It.IsAny<PurchaseApprovalTimelineEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseApprovalTimelineEvent, CancellationToken>((timeline, _) => persistedTimeline = timeline)
            .Returns(Task.CompletedTask);
        requestRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var transactionRepository = new Mock<ITransactionRepository>(MockBehavior.Strict);
        transactionRepository
            .Setup(x => x.AccountBelongsToFamilyAsync(accountId, familyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ApprovalWorkflowService(
            policyRepository.Object,
            requestRepository.Object,
            transactionRepository.Object,
            Mock.Of<ITransactionService>(),
            Mock.Of<IClock>(clock => clock.UtcNow == now));

        var blocked = await service.TryCreateBlockedRequestAsync(
            familyId,
            accountId,
            "child-user",
            "Child",
            -150m,
            "Gaming console",
            "Dragon Electronics",
            now,
            "Shopping",
            null,
            CancellationToken.None);

        Assert.NotNull(blocked);
        Assert.Equal(PurchaseApprovalRequestStatus.Blocked, blocked!.Status);
        Assert.NotNull(persistedRequest);
        Assert.Equal(PurchaseApprovalRequestStatus.Blocked, persistedRequest!.Status);
        Assert.NotNull(persistedTimeline);
        Assert.Equal(PurchaseApprovalTimelineEventType.Blocked, persistedTimeline!.EventType);
        Assert.Equal("Blocked", persistedTimeline.Status);

        policyRepository.VerifyAll();
        requestRepository.VerifyAll();
        transactionRepository.VerifyAll();
    }

    [Fact]
    public async Task ApproveAsync_PostsTransaction_AndMarksRequestApproved()
    {
        var familyId = Guid.Parse("c2000000-0000-0000-0000-000000000001");
        var requestId = Guid.Parse("c2000000-0000-0000-0000-000000000002");
        var accountId = Guid.Parse("c2000000-0000-0000-0000-000000000003");
        var now = new DateTimeOffset(2026, 3, 7, 18, 30, 0, TimeSpan.Zero);
        var occurredAt = now.AddMinutes(-10);
        var approvedTransactionId = Guid.Parse("c2000000-0000-0000-0000-000000000004");

        var approvalRequest = new PurchaseApprovalRequest(
            requestId,
            familyId,
            accountId,
            "child-user",
            "Child",
            -125m,
            "Sneakers",
            "Dragon Shoes",
            occurredAt,
            "Shopping",
            null,
            PurchaseApprovalRequestStatus.Pending,
            null,
            now.AddMinutes(-5));

        var requestRepository = new Mock<IApprovalRequestRepository>(MockBehavior.Strict);
        requestRepository
            .Setup(x => x.GetByIdForUpdateAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(approvalRequest);
        requestRepository
            .Setup(x => x.AddTimelineEventAsync(It.IsAny<PurchaseApprovalTimelineEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        requestRepository
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var transactionService = new Mock<ITransactionService>(MockBehavior.Strict);
        transactionService
            .Setup(x => x.CreateAsync(
                accountId,
                -125m,
                "Sneakers",
                "Dragon Shoes",
                occurredAt,
                "Shopping",
                null,
                false,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionDetails(
                approvedTransactionId,
                accountId,
                -125m,
                "Sneakers",
                "Dragon Shoes",
                occurredAt,
                "Shopping",
                null,
                null,
                null,
                null,
                []));

        var service = new ApprovalWorkflowService(
            Mock.Of<IApprovalPolicyRepository>(),
            requestRepository.Object,
            Mock.Of<ITransactionRepository>(),
            transactionService.Object,
            Mock.Of<IClock>(clock => clock.UtcNow == now));

        var approved = await service.ApproveAsync(
            requestId,
            "parent-user",
            "Parent",
            "approved",
            CancellationToken.None);

        Assert.Equal(PurchaseApprovalRequestStatus.Approved, approved.Status);
        Assert.Equal(approvedTransactionId, approved.ApprovedTransactionId);
        Assert.Equal("parent-user", approved.ResolvedByUserId);
        Assert.Equal("Parent", approved.ResolvedByRole);

        requestRepository.VerifyAll();
        transactionService.VerifyAll();
    }
}
