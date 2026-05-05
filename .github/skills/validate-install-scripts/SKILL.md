---
name: validate-install-scripts
description: >
  Open validation PRs in key .NET repos to test install script changes against their CI.
  Use this skill when asked to validate install scripts, open validation PRs, or check/close
  validation PRs. Supports two modes: 'open' (create PRs) and 'check-close' (verify CI and close).
---

# Validate Install Scripts in Key Repos

This skill automates the "Validation in Key Repos" step of the install scripts release process.
It opens draft PRs titled `[DO NOT MERGE] Install Scripts Update Validation PR` in key .NET repos
that replace the production install script URL with a raw GitHub URL pointing to a specific commit.

## Prerequisites

- The `gh` CLI must be authenticated with permissions to create branches and PRs in the target repos.
- The commit SHA must exist in the `dotnet/install-scripts` repository on GitHub.
- If the user lacks push access to a target repo, the scripts automatically fork and create cross-fork PRs.

## Usage

### Mode 1: Open Validation PRs

Ask the user for the commit SHA, then run:

```powershell
pwsh .github/skills/validate-install-scripts/Open-ValidationPRs.ps1 -CommitSha "<sha>"
```

To target specific repos only:

```powershell
pwsh .github/skills/validate-install-scripts/Open-ValidationPRs.ps1 -CommitSha "<sha>" -Repos @("arcade", "sdk")
```

### Mode 2: Check CI Status and Close PRs

Ask the user for the same commit SHA used to open the PRs, then run:

```powershell
pwsh .github/skills/validate-install-scripts/Close-ValidationPRs.ps1 -CommitSha "<sha>"
```

To auto-close without prompting:

```powershell
pwsh .github/skills/validate-install-scripts/Close-ValidationPRs.ps1 -CommitSha "<sha>" -AutoClose
```

## Target Repositories

- `dotnet/aspnetcore`
- `dotnet/arcade`
- `dotnet/sdk`
- `dotnet/runtime`
- `dotnet/winforms`

## What the Scripts Do

Both scripts use the `gh` CLI and GitHub's Git Data API. The open script:

1. Verifies the commit exists in `dotnet/install-scripts`
2. For each target repo:
   - Checks push access; if denied, forks the repo automatically
   - Creates a branch named `validate-install-scripts/<short-sha>`
   - Fetches `eng/common/tools.sh` and `eng/common/tools.ps1`
   - Replaces the production URL with the raw GitHub URL for the given commit
   - Creates an atomic commit (both files in one commit) via the Git Data API
   - Opens a draft PR

The close script:

1. Finds open validation PRs by branch name (handles both direct and fork-based PRs)
2. Checks CI status on each
3. Closes PRs where all checks have passed, reports pending/failed ones

## URL Replacements

In `eng/common/tools.sh`:
```
https://builds.dotnet.microsoft.com/dotnet/scripts/$dotnetInstallScriptVersion/dotnet-install.sh
→ https://raw.githubusercontent.com/dotnet/install-scripts/<COMMIT_SHA>/src/dotnet-install.sh
```

In `eng/common/tools.ps1`:
```
https://builds.dotnet.microsoft.com/dotnet/scripts/$dotnetInstallScriptVersion/dotnet-install.ps1
→ https://raw.githubusercontent.com/dotnet/install-scripts/<COMMIT_SHA>/src/dotnet-install.ps1
```

## Notes

- PRs are opened as **drafts** with `[DO NOT MERGE]` title to prevent accidental merge.
- File content is round-tripped through base64 to preserve exact bytes (no trailing whitespace changes).
- The branch name `validate-install-scripts/<short-sha>` correlates open and check-close modes.
- CI run times vary: Arcade ~30 min, Runtime can take several hours.

