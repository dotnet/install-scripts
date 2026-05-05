<#
.SYNOPSIS
    Opens validation PRs in key .NET repos to test install script changes.

.DESCRIPTION
    This script automates the "Validation in Key Repos" step of the install scripts
    release process. It opens draft PRs in target repos that replace the production
    install script URL with a raw GitHub URL pointing to a specific commit.

    If the user lacks push access to a target repo, the script automatically forks
    the repo and creates a cross-fork PR.

.PARAMETER CommitSha
    The full commit SHA from dotnet/install-scripts to validate.

.PARAMETER Repos
    Optional list of target repos. Defaults to: aspnetcore, arcade, sdk, runtime, winforms.

.EXAMPLE
    ./Open-ValidationPRs.ps1 -CommitSha "5147e32300a8e908f5d737c8cff63a76b4b63531"

.EXAMPLE
    ./Open-ValidationPRs.ps1 -CommitSha "5147e32300a8e908f5d737c8cff63a76b4b63531" -Repos @("arcade", "sdk")
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CommitSha,

    [Parameter(Mandatory = $false)]
    [string[]]$Repos = @("aspnetcore", "arcade", "sdk", "runtime", "winforms")
)

$ErrorActionPreference = "Stop"

$ProductionShUrl = 'https://builds.dotnet.microsoft.com/dotnet/scripts/$dotnetInstallScriptVersion/dotnet-install.sh'
$ProductionPs1Url = 'https://builds.dotnet.microsoft.com/dotnet/scripts/$dotnetInstallScriptVersion/dotnet-install.ps1'
$TestShUrl = "https://raw.githubusercontent.com/dotnet/install-scripts/$CommitSha/src/dotnet-install.sh"
$TestPs1Url = "https://raw.githubusercontent.com/dotnet/install-scripts/$CommitSha/src/dotnet-install.ps1"

$ShortSha = $CommitSha.Substring(0, 8)
$BranchName = "validate-install-scripts/$ShortSha"

function Test-GhCli {
    if (-not (Get-Command "gh" -ErrorAction SilentlyContinue)) {
        Write-Error "The 'gh' CLI is required but not found. Install from https://cli.github.com/"
        exit 1
    }
    $authStatus = gh auth status 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "gh CLI is not authenticated. Run 'gh auth login' first."
        exit 1
    }
}

function Test-CommitExists {
    param([string]$Sha)
    $result = gh api "repos/dotnet/install-scripts/commits/$Sha" --jq '.sha' 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Commit $Sha not found in dotnet/install-scripts. Verify the SHA is correct."
        exit 1
    }
    Write-Host "Verified commit exists: $Sha" -ForegroundColor Green
}

function Get-FileContent {
    param([string]$Repo, [string]$Path, [string]$Ref)
    $response = gh api "repos/$Repo/contents/${Path}?ref=$Ref" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to fetch $Path from $Repo at ref $Ref"
        return $null
    }
    $contentBase64 = $response | ConvertFrom-Json | Select-Object -ExpandProperty content
    # Decode base64 — preserve exact bytes
    [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($contentBase64))
}

function Open-ValidationPR {
    param([string]$RepoName)

    $Repo = "dotnet/$RepoName"
    Write-Host "`n=== Processing $Repo ===" -ForegroundColor Cyan

    # Get default branch
    $defaultBranch = gh repo view $Repo --json defaultBranchRef --jq '.defaultBranchRef.name'
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Failed to get default branch for $Repo. Skipping."
        return $null
    }

    # Check push access
    $hasPush = gh api "repos/$Repo" --jq '.permissions.push' 2>$null
    if ($hasPush -eq "true") {
        $targetRepo = $Repo
        $prHead = $BranchName
        Write-Host "  Push access confirmed." -ForegroundColor Green
    }
    else {
        Write-Host "  No push access. Forking..." -ForegroundColor Yellow
        gh repo fork $Repo --clone=false 2>$null
        $ghUser = gh api user --jq '.login'
        $targetRepo = "$ghUser/$RepoName"
        $prHead = "${ghUser}:${BranchName}"
        # Sync fork
        gh repo sync $targetRepo --branch $defaultBranch 2>$null
        Write-Host "  Using fork: $targetRepo" -ForegroundColor Yellow
    }

    # Check if branch/PR already exists
    $branchExists = gh api "repos/$targetRepo/git/ref/heads/$BranchName" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Branch already exists." -ForegroundColor Yellow
        $existingPr = gh pr list --repo $Repo --head $prHead --state open --json url --jq '.[0].url'
        if ($existingPr) {
            Write-Host "  PR already exists: $existingPr" -ForegroundColor Green
            return $existingPr
        }
    }
    else {
        # Create branch
        $baseSha = gh api "repos/$targetRepo/git/ref/heads/$defaultBranch" --jq '.object.sha'
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "  Failed to get base SHA. Skipping $Repo."
            return $null
        }

        $null = gh api "repos/$targetRepo/git/refs" -f "ref=refs/heads/$BranchName" -f "sha=$baseSha"
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "  Failed to create branch. Skipping $Repo."
            return $null
        }
        Write-Host "  Branch created from $defaultBranch"
    }

    # Fetch files from upstream (always read from dotnet/ org, not fork)
    $toolsShContent = Get-FileContent -Repo $Repo -Path "eng/common/tools.sh" -Ref $defaultBranch
    $toolsPs1Content = Get-FileContent -Repo $Repo -Path "eng/common/tools.ps1" -Ref $defaultBranch

    if (-not $toolsShContent -or -not $toolsPs1Content) {
        Write-Warning "  Failed to fetch file contents. Skipping $Repo."
        return $null
    }

    # Verify and replace in tools.sh
    $shMatches = ([regex]::Matches($toolsShContent, [regex]::Escape($ProductionShUrl))).Count
    if ($shMatches -ne 1) {
        Write-Warning "  Expected 1 match in tools.sh, found $shMatches. File format may have changed. Skipping."
        return $null
    }
    $updatedSh = $toolsShContent.Replace($ProductionShUrl, $TestShUrl)

    # Verify and replace in tools.ps1
    $ps1Matches = ([regex]::Matches($toolsPs1Content, [regex]::Escape($ProductionPs1Url))).Count
    if ($ps1Matches -ne 1) {
        Write-Warning "  Expected 1 match in tools.ps1, found $ps1Matches. File format may have changed. Skipping."
        return $null
    }
    $updatedPs1 = $toolsPs1Content.Replace($ProductionPs1Url, $TestPs1Url)

    # Create blobs using base64 to preserve exact bytes
    $shBase64 = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($updatedSh))
    $ps1Base64 = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($updatedPs1))

    $shBlob = gh api "repos/$targetRepo/git/blobs" --method POST -f "encoding=base64" -f "content=$shBase64" --jq '.sha'
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  Failed to create tools.sh blob. Skipping $Repo."
        return $null
    }

    $ps1Blob = gh api "repos/$targetRepo/git/blobs" --method POST -f "encoding=base64" -f "content=$ps1Base64" --jq '.sha'
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  Failed to create tools.ps1 blob. Skipping $Repo."
        return $null
    }

    # Get base tree and create atomic commit
    $baseSha = gh api "repos/$targetRepo/git/ref/heads/$BranchName" --jq '.object.sha'
    $baseTree = gh api "repos/$targetRepo/git/commits/$baseSha" --jq '.tree.sha'

    $treeJson = @{
        base_tree = $baseTree
        tree = @(
            @{ path = "eng/common/tools.sh"; mode = "100755"; type = "blob"; sha = $shBlob }
            @{ path = "eng/common/tools.ps1"; mode = "100644"; type = "blob"; sha = $ps1Blob }
        )
    } | ConvertTo-Json -Depth 3 -Compress

    $newTree = $treeJson | gh api "repos/$targetRepo/git/trees" --method POST --input - --jq '.sha'
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  Failed to create tree. Skipping $Repo."
        return $null
    }

    $commitJson = @{
        message = "Validate install scripts from dotnet/install-scripts@$ShortSha"
        tree = $newTree
        parents = @($baseSha)
    } | ConvertTo-Json -Depth 3 -Compress

    $newCommit = $commitJson | gh api "repos/$targetRepo/git/commits" --method POST --input - --jq '.sha'
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  Failed to create commit. Skipping $Repo."
        return $null
    }

    # Update branch ref
    $null = gh api "repos/$targetRepo/git/refs/heads/$BranchName" --method PATCH -f "sha=$newCommit"

    # Open draft PR
    $prBody = @"
This PR validates install script changes from dotnet/install-scripts commit ``$CommitSha``.

**Do not merge this PR.** It will be closed once CI passes.

Install scripts commit: https://github.com/dotnet/install-scripts/commit/$CommitSha

Changes:
- ``eng/common/tools.sh``: Points to test install script
- ``eng/common/tools.ps1``: Points to test install script
"@

    $prUrl = gh pr create --repo $Repo `
        --base $defaultBranch `
        --head $prHead `
        --title "[DO NOT MERGE] Install Scripts Update Validation PR" `
        --draft `
        --body $prBody

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  PR opened: $prUrl" -ForegroundColor Green
        return $prUrl
    }
    else {
        Write-Warning "  Failed to create PR for $Repo."
        return $null
    }
}

# --- Main ---
Write-Host "Validate Install Scripts - Open PRs" -ForegroundColor Cyan
Write-Host "Commit: $CommitSha" -ForegroundColor Cyan
Write-Host "Branch: $BranchName" -ForegroundColor Cyan
Write-Host ""

Test-GhCli
Test-CommitExists -Sha $CommitSha

$results = @{}
foreach ($repo in $Repos) {
    $prUrl = Open-ValidationPR -RepoName $repo
    $results[$repo] = $prUrl
}

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
foreach ($repo in $Repos) {
    $url = $results[$repo]
    if ($url) {
        Write-Host "  dotnet/${repo}: $url" -ForegroundColor Green
    }
    else {
        Write-Host "  dotnet/${repo}: FAILED" -ForegroundColor Red
    }
}
