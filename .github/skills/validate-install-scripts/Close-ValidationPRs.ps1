<#
.SYNOPSIS
    Checks CI status on validation PRs and closes them when CI passes.

.DESCRIPTION
    This script checks the CI status of previously opened validation PRs
    (identified by commit SHA) and closes PRs where all required checks have passed.

.PARAMETER CommitSha
    The full commit SHA that was used when opening the validation PRs.

.PARAMETER Repos
    Optional list of target repos. Defaults to: aspnetcore, arcade, sdk, runtime, winforms.

.PARAMETER AutoClose
    If set, automatically close PRs where checks have passed. Otherwise, prompts for confirmation.

.EXAMPLE
    ./Close-ValidationPRs.ps1 -CommitSha "5147e32300a8e908f5d737c8cff63a76b4b63531"

.EXAMPLE
    ./Close-ValidationPRs.ps1 -CommitSha "5147e32300a8e908f5d737c8cff63a76b4b63531" -AutoClose
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CommitSha,

    [Parameter(Mandatory = $false)]
    [string[]]$Repos = @("aspnetcore", "arcade", "sdk", "runtime", "winforms"),

    [Parameter(Mandatory = $false)]
    [switch]$AutoClose
)

$ErrorActionPreference = "Stop"

$ShortSha = $CommitSha.Substring(0, 8)
$BranchName = "validate-install-scripts/$ShortSha"

function Find-ValidationPR {
    param([string]$RepoName)

    $repo = "dotnet/$RepoName"
    $ghUser = gh api user --jq '.login'

    # Check direct branch first
    $prJson = gh pr list --repo $repo --head $BranchName --state open --json number,url,headRefName 2>$null
    $prs = $prJson | ConvertFrom-Json

    # If not found, check fork-based branch
    if (-not $prs -or $prs.Count -eq 0) {
        $prJson = gh pr list --repo $repo --head "${ghUser}:${BranchName}" --state open --json number,url,headRefName 2>$null
        $prs = $prJson | ConvertFrom-Json
    }

    if ($prs -and $prs.Count -gt 0) {
        return @{
            Repo = $repo
            Number = $prs[0].number
            Url = $prs[0].url
        }
    }
    return $null
}

function Get-PRCheckStatus {
    param([string]$Repo, [int]$Number)

    $checksJson = gh pr checks --repo $Repo $Number --json name,state,conclusion 2>&1
    if ($LASTEXITCODE -ne 0) {
        return "unknown"
    }

    $checks = $checksJson | ConvertFrom-Json
    if (-not $checks -or $checks.Count -eq 0) {
        return "no-checks"
    }

    $failed = $checks | Where-Object { $_.conclusion -eq "FAILURE" -or $_.conclusion -eq "ERROR" }
    $pending = $checks | Where-Object { $_.state -eq "PENDING" -or $_.state -eq "QUEUED" -or $_.state -eq "IN_PROGRESS" }

    if ($failed.Count -gt 0) {
        return "failed"
    }
    elseif ($pending.Count -gt 0) {
        return "pending"
    }
    else {
        return "passed"
    }
}

# --- Main ---
Write-Host "Validate Install Scripts - Check & Close PRs" -ForegroundColor Cyan
Write-Host "Commit: $CommitSha" -ForegroundColor Cyan
Write-Host "Branch: $BranchName" -ForegroundColor Cyan
Write-Host ""

$results = @()

foreach ($repoName in $Repos) {
    $pr = Find-ValidationPR -RepoName $repoName

    if (-not $pr) {
        $results += [PSCustomObject]@{
            Repo = "dotnet/$repoName"
            Status = "not-found"
            Url = ""
            Number = 0
        }
        continue
    }

    $status = Get-PRCheckStatus -Repo $pr.Repo -Number $pr.Number
    $results += [PSCustomObject]@{
        Repo = $pr.Repo
        Status = $status
        Url = $pr.Url
        Number = $pr.Number
    }
}

# Display status table
Write-Host "`n=== CI Status ===" -ForegroundColor Cyan
foreach ($r in $results) {
    $icon = switch ($r.Status) {
        "passed" { "[PASS]" }
        "failed" { "[FAIL]" }
        "pending" { "[WAIT]" }
        "not-found" { "[----]" }
        default { "[????]" }
    }
    $color = switch ($r.Status) {
        "passed" { "Green" }
        "failed" { "Red" }
        "pending" { "Yellow" }
        default { "Gray" }
    }
    $urlDisplay = if ($r.Url) { $r.Url } else { "No PR found" }
    Write-Host "  $icon $($r.Repo): $urlDisplay" -ForegroundColor $color
}

# Close passed PRs
$passedPRs = $results | Where-Object { $_.Status -eq "passed" }
if ($passedPRs.Count -gt 0) {
    Write-Host "`n=== Closing Passed PRs ===" -ForegroundColor Cyan

    foreach ($pr in $passedPRs) {
        if (-not $AutoClose) {
            $confirm = Read-Host "  Close $($pr.Repo) PR #$($pr.Number)? [Y/n]"
            if ($confirm -eq "n" -or $confirm -eq "N") {
                Write-Host "  Skipped." -ForegroundColor Yellow
                continue
            }
        }

        gh pr close --repo $pr.Repo $pr.Number --delete-branch --comment "CI passed. Closing validation PR."
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Closed: $($pr.Url)" -ForegroundColor Green
        }
        else {
            Write-Warning "  Failed to close PR #$($pr.Number) in $($pr.Repo)"
        }
    }
}

# Report pending
$pendingPRs = $results | Where-Object { $_.Status -eq "pending" }
if ($pendingPRs.Count -gt 0) {
    Write-Host "`nSome PRs still have pending checks. Re-run this script later to check again." -ForegroundColor Yellow
}

# Report failures
$failedPRs = $results | Where-Object { $_.Status -eq "failed" }
if ($failedPRs.Count -gt 0) {
    Write-Host "`nSome PRs have failed checks. Investigate the failures:" -ForegroundColor Red
    foreach ($pr in $failedPRs) {
        Write-Host "  $($pr.Url)" -ForegroundColor Red
    }
}
