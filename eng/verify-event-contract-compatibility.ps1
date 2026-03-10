param(
    [string]$RepoRoot = ".",
    [string]$CatalogRelativePath = "docs/architecture/event-contract-catalog-v1.json",
    [string]$PolicyRelativePath = "eng/event-contract-breaking-change-policy.json",
    [string]$ReportRelativePath = "artifacts/contract-compatibility/event-contract-compatibility-report.md"
)

$ErrorActionPreference = "Stop"

$resolvedRepoRoot = (Resolve-Path $RepoRoot).Path
$catalogPath = Join-Path $resolvedRepoRoot $CatalogRelativePath
$policyPath = Join-Path $resolvedRepoRoot $PolicyRelativePath
$reportPath = Join-Path $resolvedRepoRoot $ReportRelativePath
$reportDirectory = Split-Path -Path $reportPath -Parent
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

if (-not (Test-Path $catalogPath))
{
    throw "Event contract catalog was not found at '$catalogPath'."
}

if (-not (Test-Path $policyPath))
{
    throw "Breaking change policy file was not found at '$policyPath'."
}

$catalogGitPath = $CatalogRelativePath.Replace('\', '/')

function Get-ContractKey {
    param([object]$Contract)

    return "$($Contract.routingKey)|$($Contract.schemaVersion)"
}

function To-Set {
    param([object[]]$Values)

    $set = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::Ordinal)
    foreach ($value in @($Values))
    {
        if ($null -eq $value)
        {
            continue
        }

        $null = $set.Add($value.ToString())
    }

    return $set
}

function Get-BaselineCatalogContent {
    param([string]$GitPath)

    $candidates = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_BASE_REF))
    {
        $candidates.Add("origin/$($env:GITHUB_BASE_REF)")
    }

    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_EVENT_BEFORE))
    {
        $candidates.Add($env:GITHUB_EVENT_BEFORE)
    }

    $candidates.Add("HEAD~1")

    foreach ($candidateRef in $candidates | Select-Object -Unique)
    {
        try
        {
            $content = git show "$candidateRef`:$GitPath" 2>$null
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($content))
            {
                return [PSCustomObject]@{
                    Ref = $candidateRef
                    Content = $content
                }
            }
        }
        catch
        {
            # Ignore missing refs and continue trying fallback refs.
        }
    }

    return $null
}

function Is-ApprovalActive {
    param([object]$Approval)

    $approvalId = Get-PropertyValue -Object $Approval -PropertyName "id"
    if ([string]::IsNullOrWhiteSpace($approvalId))
    {
        return $false
    }

    $expiresOn = Get-PropertyValue -Object $Approval -PropertyName "expiresOn"
    if ([string]::IsNullOrWhiteSpace($expiresOn))
    {
        return $true
    }

    $expiryDate = [DateTime]::MinValue
    if (-not [DateTime]::TryParse($expiresOn, [ref]$expiryDate))
    {
        return $false
    }

    return $expiryDate.Date -ge [DateTime]::UtcNow.Date
}
function Get-PropertyValue {
    param(
        [object]$Object,
        [string]$PropertyName
    )

    if ($null -eq $Object)
    {
        return $null
    }

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property)
    {
        return $null
    }

    return $property.Value
}

$currentCatalog = Get-Content -Path $catalogPath -Raw | ConvertFrom-Json
$policy = Get-Content -Path $policyPath -Raw | ConvertFrom-Json

$baselineSource = Get-BaselineCatalogContent -GitPath $catalogGitPath
$baselineRef = "none"
$baselineCatalog = $null

if ($null -ne $baselineSource)
{
    $baselineRef = $baselineSource.Ref
    $baselineCatalog = $baselineSource.Content | ConvertFrom-Json
}
else
{
    $baselineCatalog = $currentCatalog
}

$currentContracts = @($currentCatalog.contracts)
$baselineContracts = @($baselineCatalog.contracts)

$currentByKey = @{}
foreach ($contract in $currentContracts)
{
    $currentByKey[(Get-ContractKey $contract)] = $contract
}

$baselineByKey = @{}
foreach ($contract in $baselineContracts)
{
    $baselineByKey[(Get-ContractKey $contract)] = $contract
}

$currentKeys = To-Set $currentByKey.Keys
$baselineKeys = To-Set $baselineByKey.Keys

$addedKeys = @($currentKeys | Where-Object { -not $baselineKeys.Contains($_) } | Sort-Object)
$removedKeys = @($baselineKeys | Where-Object { -not $currentKeys.Contains($_) } | Sort-Object)

$modifications = New-Object System.Collections.Generic.List[hashtable]

$sharedKeys = @($currentKeys | Where-Object { $baselineKeys.Contains($_) } | Sort-Object)
foreach ($key in $sharedKeys)
{
    $before = $baselineByKey[$key]
    $after = $currentByKey[$key]

    $beforeFields = To-Set @($before.requiredPayloadFields)
    $afterFields = To-Set @($after.requiredPayloadFields)
    $removedFields = @($beforeFields | Where-Object { -not $afterFields.Contains($_) } | Sort-Object)
    $addedFields = @($afterFields | Where-Object { -not $beforeFields.Contains($_) } | Sort-Object)

    $eventNameChanged = $before.eventName -ne $after.eventName
    $sourceServiceChanged = $before.sourceService -ne $after.sourceService
    $payloadTypeChanged = $before.payloadType -ne $after.payloadType
    $hasFieldDrift = $removedFields.Count -gt 0 -or $addedFields.Count -gt 0

    if ($eventNameChanged -or $sourceServiceChanged -or $payloadTypeChanged -or $hasFieldDrift)
    {
        $modifications.Add([PSCustomObject]@{
                key = $key
                routingKey = $after.routingKey
                schemaVersion = $after.schemaVersion
                eventNameBefore = $before.eventName
                eventNameAfter = $after.eventName
                sourceServiceBefore = $before.sourceService
                sourceServiceAfter = $after.sourceService
                payloadTypeBefore = $before.payloadType
                payloadTypeAfter = $after.payloadType
                removedFields = $removedFields
                addedFields = $addedFields
            })
    }
}

$breakingChanges = New-Object System.Collections.Generic.List[hashtable]

foreach ($removedKey in $removedKeys)
{
    $removedContract = $baselineByKey[$removedKey]
    $breakingChanges.Add([PSCustomObject]@{
            key = $removedKey
            routingKey = $removedContract.routingKey
            schemaVersion = $removedContract.schemaVersion
            reason = "Contract removed from catalog."
        })
}

foreach ($modification in $modifications)
{
    $reasonParts = New-Object System.Collections.Generic.List[string]
    if ($modification.eventNameBefore -ne $modification.eventNameAfter)
    {
        $reasonParts.Add("eventName changed")
    }

    if ($modification.sourceServiceBefore -ne $modification.sourceServiceAfter)
    {
        $reasonParts.Add("sourceService changed")
    }

    if ($modification.payloadTypeBefore -ne $modification.payloadTypeAfter)
    {
        $reasonParts.Add("payloadType changed")
    }

    if ($modification.removedFields.Count -gt 0)
    {
        $reasonParts.Add("required payload fields removed")
    }

    if ($reasonParts.Count -gt 0)
    {
        $breakingChanges.Add([PSCustomObject]@{
                key = $modification.key
                routingKey = $modification.routingKey
                schemaVersion = $modification.schemaVersion
                reason = ($reasonParts -join ", ")
            })
    }
}

$approvedBreakingIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
if ($null -ne (Get-PropertyValue -Object $policy -PropertyName "approvedBreakingChanges"))
{
    foreach ($approval in @($policy.approvedBreakingChanges))
    {
        if (Is-ApprovalActive -Approval $approval)
        {
            $null = $approvedBreakingIds.Add((Get-PropertyValue -Object $approval -PropertyName "id"))
        }
    }
}

$unapprovedBreaking = @($breakingChanges | Where-Object { -not $approvedBreakingIds.Contains($_.key) })

$lines = @(
    "# Event Contract Compatibility Report",
    "",
    "- Generated on: $(Get-Date -Format o)",
    "- Baseline reference: $baselineRef",
    "- Current contracts: $($currentContracts.Count)",
    "- Baseline contracts: $($baselineContracts.Count)",
    "- Added contracts: $($addedKeys.Count)",
    "- Removed contracts: $($removedKeys.Count)",
    "- Modified contracts: $($modifications.Count)",
    "- Breaking changes: $($breakingChanges.Count)",
    "- Unapproved breaking changes: $($unapprovedBreaking.Count)",
    ""
)

if ($addedKeys.Count -gt 0)
{
    $lines += "## Added Contracts"
    $lines += ""
    $lines += "| Key | Event Name | Payload Type |"
    $lines += "| --- | --- | --- |"
    foreach ($key in $addedKeys)
    {
        $contract = $currentByKey[$key]
        $lines += "| $key | $($contract.eventName) | $($contract.payloadType) |"
    }
    $lines += ""
}

if ($modifications.Count -gt 0)
{
    $lines += "## Modified Contracts"
    $lines += ""
    $lines += "| Key | Event Name | Source Service | Payload Type | Removed Fields | Added Fields |"
    $lines += "| --- | --- | --- | --- | --- | --- |"
    foreach ($modification in $modifications | Sort-Object key)
    {
        $eventNameDiff = "$($modification.eventNameBefore) -> $($modification.eventNameAfter)"
        $sourceDiff = "$($modification.sourceServiceBefore) -> $($modification.sourceServiceAfter)"
        $payloadTypeDiff = "$($modification.payloadTypeBefore) -> $($modification.payloadTypeAfter)"
        $removed = if ($modification.removedFields.Count -eq 0) { "-" } else { ($modification.removedFields -join ", ") }
        $added = if ($modification.addedFields.Count -eq 0) { "-" } else { ($modification.addedFields -join ", ") }
        $lines += "| $($modification.key) | $eventNameDiff | $sourceDiff | $payloadTypeDiff | $removed | $added |"
    }
    $lines += ""
}

if ($breakingChanges.Count -gt 0)
{
    $lines += "## Breaking Changes"
    $lines += ""
    $lines += "| Key | Reason | Approved |"
    $lines += "| --- | --- | --- |"
    foreach ($breaking in $breakingChanges | Sort-Object key)
    {
        $approved = if ($approvedBreakingIds.Contains($breaking.key)) { "yes" } else { "no" }
        $lines += "| $($breaking.key) | $($breaking.reason) | $approved |"
    }
    $lines += ""
}
else
{
    $lines += "No breaking contract changes detected."
    $lines += ""
}

$lines | Set-Content -Path $reportPath
Write-Host "Event contract compatibility report written to $reportPath"

if ($unapprovedBreaking.Count -gt 0)
{
    throw "Unapproved breaking event contract changes detected. Review $reportPath and either add a non-breaking change or register an approved policy exception with version bump plan."
}
