param(
    [string]$RepoRoot = ".",
    [string]$OutputRelativePath = "docs/architecture/event-contract-catalog-v1.json"
)

$ErrorActionPreference = "Stop"

$resolvedRepoRoot = (Resolve-Path $RepoRoot).Path
$outputPath = Join-Path $resolvedRepoRoot $OutputRelativePath
$outputDirectory = Split-Path -Path $outputPath -Parent
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$messagingDirectory = Join-Path $resolvedRepoRoot "src/DragonEnvelopes.Application/Cqrs/Messaging"
if (-not (Test-Path $messagingDirectory))
{
    throw "Could not locate messaging contracts at '$messagingDirectory'."
}

$recordPattern = [Regex]::new(
    'public\s+sealed\s+record\s+(?<name>[A-Za-z_]\w*)\s*\((?<args>.*?)\)\s*;',
    [System.Text.RegularExpressions.RegexOptions]::Singleline)

$payloadFieldMap = @{}
$messagingFiles = Get-ChildItem -Path $messagingDirectory -Filter *.cs -File -Recurse
foreach ($file in $messagingFiles)
{
    $content = Get-Content -Path $file.FullName -Raw
    foreach ($match in $recordPattern.Matches($content))
    {
        $recordName = $match.Groups["name"].Value.Trim()
        $args = $match.Groups["args"].Value
        $fieldNames = @()
        foreach ($segment in $args.Split(","))
        {
            $trimmedSegment = $segment.Trim()
            if ([string]::IsNullOrWhiteSpace($trimmedSegment))
            {
                continue
            }

            $tokens = $trimmedSegment.Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)
            if ($tokens.Length -eq 0)
            {
                continue
            }

            $fieldNames += $tokens[-1].Trim()
        }

        $payloadFieldMap[$recordName] = $fieldNames | Sort-Object -Unique
    }
}

$contractSeeds = @(
    @{ RoutingKey = "family.family.created.v1"; EventName = "FamilyCreated"; SourceService = "family-api"; PayloadType = "FamilyCreatedIntegrationEvent" },
    @{ RoutingKey = "family.member.added.v1"; EventName = "FamilyMemberAdded"; SourceService = "family-api"; PayloadType = "FamilyMemberAddedIntegrationEvent" },
    @{ RoutingKey = "family.member.removed.v1"; EventName = "FamilyMemberRemoved"; SourceService = "family-api"; PayloadType = "FamilyMemberRemovedIntegrationEvent" },
    @{ RoutingKey = "family.invite.accepted.v1"; EventName = "FamilyInviteAccepted"; SourceService = "family-api"; PayloadType = "FamilyInviteAcceptedIntegrationEvent" },

    @{ RoutingKey = "ledger.transaction.created.v1"; EventName = "TransactionCreated"; SourceService = "ledger-api"; PayloadType = "LedgerTransactionCreatedIntegrationEvent" },
    @{ RoutingKey = "ledger.transaction.updated.v1"; EventName = "TransactionUpdated"; SourceService = "ledger-api"; PayloadType = "TransactionUpdatedIntegrationEvent" },
    @{ RoutingKey = "ledger.transaction.deleted.v1"; EventName = "TransactionDeleted"; SourceService = "ledger-api"; PayloadType = "TransactionDeletedIntegrationEvent" },
    @{ RoutingKey = "ledger.transaction.restored.v1"; EventName = "TransactionRestored"; SourceService = "ledger-api"; PayloadType = "TransactionRestoredIntegrationEvent" },
    @{ RoutingKey = "ledger.approval-request.created.v1"; EventName = "ApprovalRequestCreated"; SourceService = "ledger-api"; PayloadType = "ApprovalRequestCreatedIntegrationEvent" },
    @{ RoutingKey = "ledger.approval-request.approved.v1"; EventName = "ApprovalRequestApproved"; SourceService = "ledger-api"; PayloadType = "ApprovalRequestApprovedIntegrationEvent" },
    @{ RoutingKey = "ledger.approval-request.denied.v1"; EventName = "ApprovalRequestDenied"; SourceService = "ledger-api"; PayloadType = "ApprovalRequestDeniedIntegrationEvent" },

    @{ RoutingKey = "planning.envelope.created.v1"; EventName = "EnvelopeCreated"; SourceService = "planning-api"; PayloadType = "EnvelopeCreatedIntegrationEvent" },
    @{ RoutingKey = "planning.envelope.updated.v1"; EventName = "EnvelopeUpdated"; SourceService = "planning-api"; PayloadType = "EnvelopeUpdatedIntegrationEvent" },
    @{ RoutingKey = "planning.envelope.archived.v1"; EventName = "EnvelopeArchived"; SourceService = "planning-api"; PayloadType = "EnvelopeArchivedIntegrationEvent" },
    @{ RoutingKey = "planning.envelope.rollover-policy-updated.v1"; EventName = "EnvelopeRolloverPolicyUpdated"; SourceService = "planning-api"; PayloadType = "EnvelopeRolloverPolicyUpdatedIntegrationEvent" },
    @{ RoutingKey = "planning.budget.created.v1"; EventName = "BudgetCreated"; SourceService = "planning-api"; PayloadType = "BudgetCreatedIntegrationEvent" },
    @{ RoutingKey = "planning.budget.updated.v1"; EventName = "BudgetUpdated"; SourceService = "planning-api"; PayloadType = "BudgetUpdatedIntegrationEvent" },
    @{ RoutingKey = "planning.recurring-bill.created.v1"; EventName = "RecurringBillCreated"; SourceService = "planning-api"; PayloadType = "RecurringBillCreatedIntegrationEvent" },
    @{ RoutingKey = "planning.recurring-bill.updated.v1"; EventName = "RecurringBillUpdated"; SourceService = "planning-api"; PayloadType = "RecurringBillUpdatedIntegrationEvent" },
    @{ RoutingKey = "planning.recurring-bill.deleted.v1"; EventName = "RecurringBillDeleted"; SourceService = "planning-api"; PayloadType = "RecurringBillDeletedIntegrationEvent" },
    @{ RoutingKey = "planning.envelope-goal.created.v1"; EventName = "EnvelopeGoalCreated"; SourceService = "planning-api"; PayloadType = "EnvelopeGoalCreatedIntegrationEvent" },
    @{ RoutingKey = "planning.envelope-goal.updated.v1"; EventName = "EnvelopeGoalUpdated"; SourceService = "planning-api"; PayloadType = "EnvelopeGoalUpdatedIntegrationEvent" },
    @{ RoutingKey = "planning.envelope-goal.deleted.v1"; EventName = "EnvelopeGoalDeleted"; SourceService = "planning-api"; PayloadType = "EnvelopeGoalDeletedIntegrationEvent" },

    @{ RoutingKey = "automation.rule.created.v1"; EventName = "AutomationRuleCreated"; SourceService = "automation-api"; PayloadType = "AutomationRuleCreatedIntegrationEvent" },
    @{ RoutingKey = "automation.rule.updated.v1"; EventName = "AutomationRuleUpdated"; SourceService = "automation-api"; PayloadType = "AutomationRuleUpdatedIntegrationEvent" },
    @{ RoutingKey = "automation.rule.enabled.v1"; EventName = "AutomationRuleEnabled"; SourceService = "automation-api"; PayloadType = "AutomationRuleEnabledIntegrationEvent" },
    @{ RoutingKey = "automation.rule.disabled.v1"; EventName = "AutomationRuleDisabled"; SourceService = "automation-api"; PayloadType = "AutomationRuleDisabledIntegrationEvent" },
    @{ RoutingKey = "automation.rule.deleted.v1"; EventName = "AutomationRuleDeleted"; SourceService = "automation-api"; PayloadType = "AutomationRuleDeletedIntegrationEvent" },
    @{ RoutingKey = "automation.rule.executed.v1"; EventName = "AutomationRuleExecuted"; SourceService = "automation-api"; PayloadType = "AutomationRuleExecutedIntegrationEvent" },

    @{ RoutingKey = "financial.stripe.financial-account.provisioned.v1"; EventName = "StripeFinancialAccountProvisioned"; SourceService = "financial-api"; PayloadType = "StripeFinancialAccountProvisionedIntegrationEvent" },
    @{ RoutingKey = "financial.card.virtual-issued.v1"; EventName = "CardVirtualIssued"; SourceService = "financial-api"; PayloadType = "CardVirtualIssuedIntegrationEvent" },
    @{ RoutingKey = "financial.card.physical-issued.v1"; EventName = "CardPhysicalIssued"; SourceService = "financial-api"; PayloadType = "CardPhysicalIssuedIntegrationEvent" },
    @{ RoutingKey = "financial.card.frozen.v1"; EventName = "CardFrozen"; SourceService = "financial-api"; PayloadType = "CardFrozenIntegrationEvent" },
    @{ RoutingKey = "financial.card.unfrozen.v1"; EventName = "CardUnfrozen"; SourceService = "financial-api"; PayloadType = "CardUnfrozenIntegrationEvent" },
    @{ RoutingKey = "financial.card.cancelled.v1"; EventName = "CardCancelled"; SourceService = "financial-api"; PayloadType = "CardCancelledIntegrationEvent" },
    @{ RoutingKey = "financial.provider-notification.dispatch-failed.v1"; EventName = "ProviderNotificationDispatchFailed"; SourceService = "financial-api"; PayloadType = "ProviderNotificationDispatchFailedIntegrationEvent" },
    @{ RoutingKey = "financial.provider-notification.dispatch-retried.v1"; EventName = "ProviderNotificationDispatchRetried"; SourceService = "financial-api"; PayloadType = "ProviderNotificationDispatchRetriedIntegrationEvent" }
)

$contracts = foreach ($seed in $contractSeeds)
{
    if (-not $payloadFieldMap.ContainsKey($seed.PayloadType))
    {
        throw "Could not resolve payload type '$($seed.PayloadType)' from messaging contracts."
    }

    $requiredFields = $payloadFieldMap[$seed.PayloadType]

    [PSCustomObject]@{
        id = "$($seed.RoutingKey)|1.0"
        routingKey = $seed.RoutingKey
        eventName = $seed.EventName
        schemaVersion = "1.0"
        sourceService = $seed.SourceService
        payloadType = $seed.PayloadType
        requiredPayloadFields = $requiredFields
    }
}

$catalog = [PSCustomObject]@{
    catalogVersion = "1.0"
    generatedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    envelopeRequiredFields = @(
        "eventId",
        "eventName",
        "schemaVersion",
        "occurredAtUtc",
        "publishedAtUtc",
        "sourceService",
        "correlationId",
        "payload"
    )
    contracts = $contracts
}

$catalog | ConvertTo-Json -Depth 10 | Set-Content -Path $outputPath
Write-Host "Event contract catalog generated at $outputPath"
