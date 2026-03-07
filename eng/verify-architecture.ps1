param(
    [string]$RepoRoot = "."
)

$ErrorActionPreference = "Stop"

function Get-ProjectReferences {
    param(
        [string]$ProjectPath
    )

    $output = dotnet list $ProjectPath reference
    return $output |
        Where-Object { $_ -match "^\s*(\.\.[\\/]|[A-Za-z]:).+\.csproj\s*$" } |
        ForEach-Object { $_.Trim() }
}

function Assert-ContainsReference {
    param(
        [string[]]$References,
        [string]$RequiredReference,
        [string]$ErrorMessage
    )

    if (-not ($References -match [regex]::Escape($RequiredReference))) {
        throw $ErrorMessage
    }
}

function Assert-DoesNotContainReference {
    param(
        [string[]]$References,
        [string]$ForbiddenReference,
        [string]$ErrorMessage
    )

    if ($References -match [regex]::Escape($ForbiddenReference)) {
        throw $ErrorMessage
    }
}

$domainRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Domain/DragonEnvelopes.Domain.csproj")
$contractsRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Contracts/DragonEnvelopes.Contracts.csproj")
$applicationRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Application/DragonEnvelopes.Application.csproj")
$infrastructureRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj")
$providerClientsRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.ProviderClients/DragonEnvelopes.ProviderClients.csproj")
$apiRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj")
$familyApiRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Family.Api/DragonEnvelopes.Family.Api.csproj")
$ledgerApiRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Ledger.Api/DragonEnvelopes.Ledger.Api.csproj")
$desktopRefs = Get-ProjectReferences (Join-Path $RepoRoot "client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj")

if ($domainRefs.Count -ne 0) {
    throw "Domain layer must not reference other projects."
}

if ($contractsRefs.Count -ne 0) {
    throw "Contracts layer must not reference other projects."
}

Assert-ContainsReference $applicationRefs "DragonEnvelopes.Domain.csproj" "Application must reference Domain."
Assert-ContainsReference $providerClientsRefs "DragonEnvelopes.Application.csproj" "ProviderClients must reference Application."
Assert-ContainsReference $providerClientsRefs "DragonEnvelopes.Domain.csproj" "ProviderClients must reference Domain."
Assert-ContainsReference $infrastructureRefs "DragonEnvelopes.Application.csproj" "Infrastructure must reference Application."
Assert-ContainsReference $infrastructureRefs "DragonEnvelopes.Domain.csproj" "Infrastructure must reference Domain."

foreach ($api in @(
    @{ Name = "API"; References = $apiRefs },
    @{ Name = "Family API"; References = $familyApiRefs },
    @{ Name = "Ledger API"; References = $ledgerApiRefs }
)) {
    Assert-ContainsReference $api.References "DragonEnvelopes.Application.csproj" "$($api.Name) must reference Application."
    Assert-ContainsReference $api.References "DragonEnvelopes.Infrastructure.csproj" "$($api.Name) must reference Infrastructure."
    Assert-ContainsReference $api.References "DragonEnvelopes.Contracts.csproj" "$($api.Name) must reference Contracts."
    Assert-ContainsReference $api.References "DragonEnvelopes.ProviderClients.csproj" "$($api.Name) must reference ProviderClients."
}

Assert-ContainsReference $desktopRefs "DragonEnvelopes.Contracts.csproj" "Desktop must reference Contracts."

Assert-DoesNotContainReference $desktopRefs "DragonEnvelopes.Domain.csproj" "Desktop must not reference Domain."
Assert-DoesNotContainReference $desktopRefs "DragonEnvelopes.Application.csproj" "Desktop must not reference Application."
Assert-DoesNotContainReference $desktopRefs "DragonEnvelopes.Infrastructure.csproj" "Desktop must not reference Infrastructure."

Write-Host "Architecture dependency checks passed."

