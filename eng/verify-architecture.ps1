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

$domainRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Domain/DragonEnvelopes.Domain.csproj")
$applicationRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Application/DragonEnvelopes.Application.csproj")
$infrastructureRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Infrastructure/DragonEnvelopes.Infrastructure.csproj")
$apiRefs = Get-ProjectReferences (Join-Path $RepoRoot "src/DragonEnvelopes.Api/DragonEnvelopes.Api.csproj")
$desktopRefs = Get-ProjectReferences (Join-Path $RepoRoot "client/DragonEnvelopes.Desktop/DragonEnvelopes.Desktop.csproj")

if ($domainRefs.Count -ne 0) {
    throw "Domain layer must not reference other projects."
}

if (-not ($applicationRefs -match "DragonEnvelopes.Domain.csproj")) {
    throw "Application must reference Domain."
}

if (-not ($infrastructureRefs -match "DragonEnvelopes.Application.csproj") -or -not ($infrastructureRefs -match "DragonEnvelopes.Domain.csproj")) {
    throw "Infrastructure must reference Application and Domain."
}

if (-not ($apiRefs -match "DragonEnvelopes.Application.csproj") -or -not ($apiRefs -match "DragonEnvelopes.Infrastructure.csproj") -or -not ($apiRefs -match "DragonEnvelopes.Contracts.csproj")) {
    throw "API must reference Application, Infrastructure, and Contracts."
}

if (-not ($desktopRefs -match "DragonEnvelopes.Contracts.csproj")) {
    throw "Desktop must reference Contracts."
}

if ($desktopRefs -match "DragonEnvelopes.Domain.csproj") {
    throw "Desktop must not reference Domain."
}

Write-Host "Architecture dependency checks passed."

