param(
    [string]$RepoRoot = ".",
    [string]$ReportRelativePath = "artifacts/contract-drift/contract-drift-report.md"
)

$ErrorActionPreference = "Stop"

$resolvedRepoRoot = (Resolve-Path $RepoRoot).Path
$reportPath = Join-Path $resolvedRepoRoot $ReportRelativePath
$reportDirectory = Split-Path -Path $reportPath -Parent
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null

$endpointDirectories = @(
    "src/DragonEnvelopes.Api/Endpoints",
    "src/DragonEnvelopes.Family.Api/Endpoints",
    "src/DragonEnvelopes.Ledger.Api/Endpoints"
)

$desktopServiceDirectory = "client/DragonEnvelopes.Desktop/Services"

function Normalize-RoutePath {
    param([string]$RoutePath)

    if ([string]::IsNullOrWhiteSpace($RoutePath))
    {
        return ""
    }

    $normalized = $RoutePath.Trim()

    if ($normalized.Contains("?"))
    {
        $normalized = $normalized.Split("?", 2)[0]
    }

    if ($normalized.StartsWith("/api/v1/", [StringComparison]::OrdinalIgnoreCase))
    {
        $normalized = $normalized.Substring(8)
    }
    elseif ($normalized.StartsWith("api/v1/", [StringComparison]::OrdinalIgnoreCase))
    {
        $normalized = $normalized.Substring(7)
    }

    $normalized = $normalized.Replace('\', '/')
    $normalized = $normalized.TrimStart('/')
    $normalized = [Regex]::Replace($normalized, "\{[^}]+\}", "{}")
    $normalized = [Regex]::Replace($normalized, "/+", "/")

    return $normalized.ToLowerInvariant()
}

function Is-CandidateRouteLiteral {
    param([string]$Literal)

    if ([string]::IsNullOrWhiteSpace($Literal))
    {
        return $false
    }

    if ($Literal.Contains("://"))
    {
        return $false
    }

    if ($Literal.Contains(" "))
    {
        return $false
    }

    return $Literal -match '^[A-Za-z0-9_\-{}:/.?=&]+$'
}

function Get-LineNumber {
    param(
        [string]$Content,
        [int]$Index
    )

    if ($Index -le 0)
    {
        return 1
    }

    return ($Content.Substring(0, $Index).Split("`n").Length)
}

function Resolve-PathExpression {
    param(
        [string]$Expression,
        [hashtable]$VariableRoutes
    )

    if ([string]::IsNullOrWhiteSpace($Expression))
    {
        return @()
    }

    $trimmedExpression = $Expression.Trim()
    if ($trimmedExpression.StartsWith('$"') -or $trimmedExpression.StartsWith('"'))
    {
        $resolvedLiteral = $trimmedExpression.TrimStart('$').Trim('"')
        return @($resolvedLiteral)
    }

    if ($VariableRoutes.ContainsKey($trimmedExpression))
    {
        return @($VariableRoutes[$trimmedExpression])
    }

    return @()
}

function Get-RouteSegments {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path))
    {
        return @()
    }

    return @($Path.Split('/', [StringSplitOptions]::RemoveEmptyEntries))
}

function Test-RouteMatch {
    param(
        [string]$Method,
        [string]$ClientPath,
        [System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[string]]]$ServerRoutesByMethod
    )

    if (-not $ServerRoutesByMethod.ContainsKey($Method))
    {
        return $false
    }

    $clientSegments = Get-RouteSegments $ClientPath
    foreach ($serverPath in $ServerRoutesByMethod[$Method])
    {
        $serverSegments = Get-RouteSegments $serverPath
        if ($clientSegments.Count -ne $serverSegments.Count)
        {
            continue
        }

        $matches = $true
        for ($index = 0; $index -lt $clientSegments.Count; $index++)
        {
            $clientSegment = $clientSegments[$index]
            $serverSegment = $serverSegments[$index]
            if ($clientSegment -ne "{}" -and $serverSegment -ne "{}" -and $clientSegment -ne $serverSegment)
            {
                $matches = $false
                break
            }
        }

        if ($matches)
        {
            return $true
        }
    }

    return $false
}

$stringLiteralPattern = [Regex]::new('\$?"(?<value>[^"]+)"', [System.Text.RegularExpressions.RegexOptions]::Singleline)
$serverRoutePattern = [Regex]::new('Map(?<method>Get|Post|Put|Delete|Patch)\(\s*"(?<path>[^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$serverRoutes = New-Object System.Collections.Generic.HashSet[string]
$serverRoutesByMethod = New-Object 'System.Collections.Generic.Dictionary[string, System.Collections.Generic.List[string]]'

foreach ($relativeDirectory in $endpointDirectories)
{
    $directoryPath = Join-Path $resolvedRepoRoot $relativeDirectory
    if (-not (Test-Path $directoryPath))
    {
        continue
    }

    $endpointFiles = Get-ChildItem -Path $directoryPath -Filter *.cs -File -Recurse
    foreach ($file in $endpointFiles)
    {
        $content = Get-Content -Path $file.FullName -Raw
        foreach ($match in $serverRoutePattern.Matches($content))
        {
            $method = $match.Groups['method'].Value.ToUpperInvariant()
            $path = Normalize-RoutePath $match.Groups['path'].Value
            if ([string]::IsNullOrWhiteSpace($path) -or $path.Contains("://"))
            {
                continue
            }

            $null = $serverRoutes.Add("$method $path")
            if (-not $serverRoutesByMethod.ContainsKey($method))
            {
                $serverRoutesByMethod[$method] = New-Object 'System.Collections.Generic.List[string]'
            }

            if (-not $serverRoutesByMethod[$method].Contains($path))
            {
                $serverRoutesByMethod[$method].Add($path)
            }
        }
    }
}

$clientPatterns = @(
    [PSCustomObject]@{ Method = "GET"; Regex = [Regex]::new('_apiClient\.GetAsync\(\s*(?<pathExpr>\$?"[^"]+"|[A-Za-z_]\w*)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline) },
    [PSCustomObject]@{ Method = "GET"; Regex = [Regex]::new('GetAsync<[^>]+>\(\s*(?<pathExpr>\$?"[^"]+"|[A-Za-z_]\w*)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline) },
    [PSCustomObject]@{ Method = "GET"; Regex = [Regex]::new('GetListAsync<[^>]+>\(\s*(?<pathExpr>\$?"[^"]+"|[A-Za-z_]\w*)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline) },
    [PSCustomObject]@{ Method = "POST"; Regex = [Regex]::new('PostAsync\(\s*(?<pathExpr>\$?"[^"]+"|[A-Za-z_]\w*)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline) },
    [PSCustomObject]@{ Method = ""; Regex = [Regex]::new('HttpRequestMessage\(\s*HttpMethod\.(?<method>Get|Post|Put|Delete|Patch)\s*,\s*(?<pathExpr>\$?"[^"]+"|[A-Za-z_]\w*)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline) },
    [PSCustomObject]@{ Method = ""; Regex = [Regex]::new('SendAsync<[^>]+>\(\s*HttpMethod\.(?<method>Get|Post|Put|Delete|Patch)\s*,\s*(?<pathExpr>\$?"[^"]+"|[A-Za-z_]\w*)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline) },
    [PSCustomObject]@{ Method = ""; Regex = [Regex]::new('SendAsync\(\s*HttpMethod\.(?<method>Get|Post|Put|Delete|Patch)\s*,\s*(?<pathExpr>\$?"[^"]+"|[A-Za-z_]\w*)', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline) }
)

$variableDeclarationPattern = [Regex]::new('(?:\bvar\b|\bstring\b|\bconst\s+string\b)\s+(?<name>[A-Za-z_]\w*)\s*=\s*(?<expression>[^;]+);', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [System.Text.RegularExpressions.RegexOptions]::Singleline)

$serviceDirectoryPath = Join-Path $resolvedRepoRoot $desktopServiceDirectory
$serviceFiles = Get-ChildItem -Path $serviceDirectoryPath -Filter *.cs -File -Recurse
$clientUsages = @()
$usageKeys = New-Object System.Collections.Generic.HashSet[string]

foreach ($file in $serviceFiles)
{
    $content = Get-Content -Path $file.FullName -Raw
    $variableRouteMap = @{}

    foreach ($variableMatch in $variableDeclarationPattern.Matches($content))
    {
        $variableName = $variableMatch.Groups['name'].Value
        $assignmentExpression = $variableMatch.Groups['expression'].Value

        foreach ($literalMatch in $stringLiteralPattern.Matches($assignmentExpression))
        {
            $literalValue = $literalMatch.Groups['value'].Value
            if (-not (Is-CandidateRouteLiteral $literalValue))
            {
                continue
            }

            if (-not $variableRouteMap.ContainsKey($variableName))
            {
                $variableRouteMap[$variableName] = New-Object System.Collections.Generic.HashSet[string]
            }

            $normalizedLiteral = Normalize-RoutePath $literalValue
            if (-not [string]::IsNullOrWhiteSpace($normalizedLiteral))
            {
                $null = $variableRouteMap[$variableName].Add($literalValue)
            }
        }
    }

    foreach ($pattern in $clientPatterns)
    {
        foreach ($match in $pattern.Regex.Matches($content))
        {
            $method = if ([string]::IsNullOrWhiteSpace($pattern.Method))
            {
                $match.Groups['method'].Value.ToUpperInvariant()
            }
            else
            {
                $pattern.Method
            }

            $lineNumber = Get-LineNumber -Content $content -Index $match.Index
            $pathExpressions = Resolve-PathExpression -Expression $match.Groups['pathExpr'].Value -VariableRoutes $variableRouteMap

            foreach ($rawPath in $pathExpressions)
            {
                if (-not (Is-CandidateRouteLiteral $rawPath))
                {
                    continue
                }

                $path = Normalize-RoutePath $rawPath
                if ([string]::IsNullOrWhiteSpace($path))
                {
                    continue
                }

                $usageKey = "$($file.FullName)|$lineNumber|$method|$path"
                if ($usageKeys.Contains($usageKey))
                {
                    continue
                }

                $null = $usageKeys.Add($usageKey)
                $clientUsages += [PSCustomObject]@{
                    Method = $method
                    Path = $path
                    RouteKey = "$method $path"
                    RawPath = $rawPath
                    File = $file.FullName
                    Line = $lineNumber
                }
            }
        }
    }
}

$mismatches = $clientUsages | Where-Object {
    if ($serverRoutes.Contains($_.RouteKey))
    {
        return $false
    }

    return -not (Test-RouteMatch -Method $_.Method -ClientPath $_.Path -ServerRoutesByMethod $serverRoutesByMethod)
} | Sort-Object RouteKey, File, Line

$lines = @(
    "# Contract Drift Report",
    "",
    "- Generated on: $(Get-Date -Format o)",
    "- Server routes discovered: $($serverRoutes.Count)",
    "- Desktop route usages discovered: $($clientUsages.Count)",
    "- Mismatches: $($mismatches.Count)",
    ""
)

if ($mismatches.Count -eq 0)
{
    $lines += "No contract drift detected between desktop service calls and API endpoint mappings."
}
else
{
    $lines += "## Missing Routes"
    $lines += ""
    $lines += "| Method | Normalized Path | File | Line | Raw Path |"
    $lines += "| --- | --- | --- | --- | --- |"

    foreach ($mismatch in $mismatches)
    {
        $relativeFile = [System.IO.Path]::GetRelativePath($resolvedRepoRoot, $mismatch.File)
        $lines += "| $($mismatch.Method) | $($mismatch.Path) | $relativeFile | $($mismatch.Line) | $($mismatch.RawPath.Replace('|', '\\|')) |"
    }
}

$lines | Set-Content -Path $reportPath
Write-Host "Report written to $reportPath"

if ($mismatches.Count -gt 0)
{
    throw "Contract drift detected. Review $reportPath for route mismatches."
}

