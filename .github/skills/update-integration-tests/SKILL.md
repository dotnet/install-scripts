---
name: update-integration-tests
description: >
  Update the integration tests in this repository after a .NET preview1 or GA release.
  Use this skill when asked to update tests for a new .NET version release, when an issue
  mentions updating integration tests for a .NET release, or when test failures indicate
  a new .NET version has shipped and tests are out of date.
---

# Updating Integration Tests After a .NET Release

This skill guides the process of updating integration tests when a new major .NET version reaches **preview1** or **GA (General Availability)**.

## Step 0: Run the Coverage Check Script

Before making any changes, run the helper script to identify exactly what needs updating:

```bash
python3 .github/skills/update-integration-tests/check-test-coverage.py
```

This script fetches the .NET releases index, parses the test files, and reports:
- Missing version entries in `_channels`, `_runtimeBranches`, and `_sdkBranches`
- Incorrect Quality values (e.g., a GA version still using `Quality.Preview`)
- Missing `[InlineData]` entries for specific version tests
- Missing aka.ms test entries
- LTS/STS alias correctness

Use the script output to guide the changes below.

## Step 1: Fetch Release Data

Fetch the .NET releases index to determine the current state of all .NET versions:

```bash
curl -s https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json
```

This JSON file contains an array of release entries. Each entry has these key fields:

| Field | Description | Example Values |
|-------|-------------|----------------|
| `channel-version` | The .NET major.minor version | `"11.0"`, `"10.0"` |
| `support-phase` | Current lifecycle phase | `"preview"`, `"active"`, `"eol"` |
| `release-type` | Support model | `"lts"`, `"sts"` |
| `latest-sdk` | Latest SDK version string | `"11.0.100-preview.1.26104.118"`, `"10.0.103"` |
| `latest-runtime` | Latest runtime version string | `"11.0.0-preview.1.26104.118"`, `"10.0.3"` |

For detailed per-release version information (including ASP.NET Core and Windows Desktop runtime versions), fetch the channel's `releases.json`:

```bash
curl -s https://builds.dotnet.microsoft.com/dotnet/release-metadata/X.0/releases.json
```

The per-release JSON contains component versions under the first release entry:
- `releases[0].sdk.version` — SDK version (e.g., `"11.0.100-preview.1.26104.118"`)
- `releases[0].runtime.version` — .NET runtime version (e.g., `"11.0.0-preview.1.26104.118"`)
- `releases[0].aspnetcore-runtime.version` — ASP.NET Core runtime version
- `releases[0].windowsdesktop.version` — Windows Desktop runtime version

## Step 2: Determine What Updates Are Needed

Compare the releases index data against the test files to identify gaps. **Important: Check ALL active and preview versions**, not just the newest one. Previous updates may have left gaps (e.g., a version going from preview to GA without updating `_sdkBranches`).

### How to identify the release type

| `support-phase` value | What it means | Test update type |
|---|---|---|
| `"preview"` | Version is in preview (preview1 or later) | **Preview update** — add new version entries with `Quality.Preview` |
| `"active"` | Version has reached GA and is actively supported | **GA update** — change preview entries to `Quality.None` + `Quality.Ga`, update LTS/STS aliases |

### How to identify LTS vs STS

Use the `release-type` field from the releases index:
- `"lts"` → Long Term Support — this version should be the target of the `"LTS"` test channel alias
- `"sts"` → Standard Term Support — this version should be the target of the `"STS"` test channel alias

Only the **latest active** LTS and STS versions should be referenced by the `"LTS"` and `"STS"` aliases.

## Step 3: Make the Test Updates

### Key Files

All test files are under `tests/Install-Scripts.Test/`:

| File | Purpose |
|------|---------|
| `GivenThatIWantToInstallDotnetFromAScript.cs` | Tests that install .NET SDK and runtimes using channels, branches, quality flags, and specific versions |
| `GivenThatIWantToGetTheSdkLinksFromAScript.cs` | Dry-run tests that verify SDK download links resolve correctly for channels, runtimes, and exact versions |
| `AkaMsLinksTests.cs` | Tests that verify aka.ms redirect links work for SDK and runtime downloads |
| `Assets/*.verified.txt` | Verified snapshot files for exact-version dry-run tests (used by the Verify library). **Only created for GA versions, not preview.** |
| `Utils/Quality.cs` | Defines the `Quality` flags enum: `None`, `Daily`, `Preview`, `Ga`, `All` |

---

### Preview Release Updates

When the releases index shows a version with `"support-phase": "preview"` that is not yet in the tests, add entries for it.

Use the `latest-sdk` and `latest-runtime` values from the releases index (or the per-release `releases.json`) as the specific version numbers.

#### `GivenThatIWantToInstallDotnetFromAScript.cs`

**`_channels` list** — Add a preview entry at the end (before the closing `};`):
```csharp
("X.0", "X\\.0\\..*", Quality.Preview),
```

**`_runtimeBranches` list** — Add at the end:
```csharp
("X.0", "X\\.0\\..*", Quality.Preview),
```

**`_sdkBranches` list** — Add at the end:
```csharp
("X.0.1xx", "X\\.0\\..*", Quality.Preview),
```

**`WhenInstallingASpecificVersionOfTheSdk`** — Add `[InlineData]` using the `latest-sdk` value from the releases index:
```csharp
[InlineData("X.0.100-preview.N.NNNNN.NNN")]
```

**`WhenInstallingASpecificVersionOfDotnetRuntime`** — Add `[InlineData]` using `runtime.version` from `releases.json`:
```csharp
[InlineData("X.0.0-preview.N.NNNNN.NNN")]
```

**`WhenInstallingASpecificVersionOfAspNetCoreRuntime`** — Use `aspnetcore-runtime.version`:
```csharp
[InlineData("X.0.0-preview.N.NNNNN.NNN")]
```

**`WhenInstallingASpecificVersionOfWindowsdesktopRuntime`** — Use `windowsdesktop.version`:
```csharp
[InlineData("X.0.0-preview.N.NNNNN.NNN")]
```

**Do NOT add entries to these tests for preview versions** (they are GA-only):
- `WhenAnExactVersionIsPassedToBash`
- `WhenAnExactVersionIsPassedToPowershell`
- `WhenInstallingAnAlreadyInstalledVersion`

#### `GivenThatIWantToGetTheSdkLinksFromAScript.cs`

**`WhenChannelResolvesToASpecificRuntimeVersion`** — Add `[InlineData]` entries for the new channel across all three runtime type blocks. Insert after the last numeric version for each block, before `"STS"`, `"LTS"`, or `"master"` entries:

```csharp
// In the "dotnet" block:
[InlineData("X.0", "dotnet")]
[InlineData("X.0", "dotnet", true)]

// In the "aspnetcore" block:
[InlineData("X.0", "aspnetcore")]
[InlineData("X.0", "aspnetcore", true)]

// In the "windowsdesktop" block:
[InlineData("X.0", "windowsdesktop")]
[InlineData("X.0", "windowsdesktop", true)]
```

**`WhenChannelResolvesToASpecificSDKVersion`** — Add:
```csharp
[InlineData("X.0")]
```
Insert after the last numeric version `[InlineData]`, before `"STS"`.

**Do NOT add entries to `WhenAnExactVersionIsPassedToBash` or `WhenAnExactVersionIsPassedToPowershell`** for preview versions. These tests use the Verify library and require `.verified.txt` snapshot files that can only be generated from the actual script output. They are only added during GA updates.

#### `AkaMsLinksTests.cs`

Aka.ms link tests should **not** be added at preview1 stage. The aka.ms redirect links are not live until GA.

---

### GA Release Updates

When the releases index shows a version with `"support-phase": "active"` whose tests still use `Quality.Preview`, update them.

Use `latest-sdk` and `latest-runtime` from the releases index to get the GA version numbers. For the initial GA release, versions are typically `X.0.100` (SDK), `X.0.0` (runtimes).

#### `GivenThatIWantToInstallDotnetFromAScript.cs`

**`_channels` list** — Replace the preview entry with GA entries:
```csharp
// Before:
("X.0", "X\\.0\\..*", Quality.Preview),

// After:
("X.0", "X\\.0\\..*", Quality.None),
("X.0", "X\\.0\\..*", Quality.Ga),
```

**Update `"LTS"` or `"STS"` alias** — Use the `release-type` field from the releases index to determine which alias to update. Find the **latest active** version with the matching `release-type` and update the alias to match its version regex:
```csharp
// If release-type is "lts" and X.0 is the newest active LTS:
("LTS", "X\\.0\\..*", Quality.None),

// If release-type is "sts" and X.0 is the newest active STS:
("STS", "X\\.0\\..*", Quality.None),
```

To determine the correct alias targets, filter the releases index for entries where `support-phase` is `"active"` and group by `release-type`. The highest `channel-version` in each group gets the alias.

**`_runtimeBranches` list** — Update quality from Preview to None:
```csharp
// Before:
("X.0", "X\\.0\\..*", Quality.Preview),
// After:
("X.0", "X\\.0\\..*", Quality.None),
```

**`_sdkBranches` list** — Change `Quality.Preview` to `Quality.Daily`. This follows the established pattern — all GA versions (7.0, 8.0, 9.0, etc.) use `Quality.Daily` for their SDK branch entries:
```csharp
// Before:
("X.0.1xx", "X\\.0\\..*", Quality.Preview),
// After:
("X.0.1xx", "X\\.0\\..*", Quality.Daily),
```

**Specific version tests** — Replace preview version strings with GA versions from the releases index. For the initial GA release, use the `.0` versions from the first GA release in `releases.json`:
- `WhenInstallingASpecificVersionOfTheSdk`: `"X.0.100"`
- `WhenInstallingASpecificVersionOfDotnetRuntime`: `"X.0.0"`
- `WhenInstallingASpecificVersionOfAspNetCoreRuntime`: `"X.0.0"`
- `WhenInstallingASpecificVersionOfWindowsdesktopRuntime`: `"X.0.0"`

#### `GivenThatIWantToGetTheSdkLinksFromAScript.cs`

**`WhenAnExactVersionIsPassedToBash` and `WhenAnExactVersionIsPassedToPowershell`** — Add the GA SDK version:
```csharp
[InlineData("X.0.100", null)]
```

**Verified asset files** — Create `.verified.txt` snapshot files under `tests/Install-Scripts.Test/Assets/`. Copy the format from existing files (e.g., the `10.0.100` files) and replace version numbers. File naming convention:
```
GivenThatIWantToGetTheSdkLinksFromAScript.WhenAnExactVersionIsPassedToBash_version=X.0.100_runtime=null.verified.txt
GivenThatIWantToGetTheSdkLinksFromAScript.WhenAnExactVersionIsPassedToPowershell_version=X.0.100_runtime=null.verified.txt
```

Bash verified file content template:
```
dotnet_install: Warning: Use of --runtime-id is obsolete and should be limited to the versions below 2.1. To override architecture, use --architecture option instead. To override OS, use --os option instead.
dotnet-install: Payload URLs:
dotnet-install: URL #0 - primary: https://builds.dotnet.microsoft.com/dotnet/Sdk/X.0.100/dotnet-sdk-X.0.100-osx-x64.tar.gz
dotnet-install: URL #1 - legacy: https://builds.dotnet.microsoft.com/dotnet/Sdk/X.0.100/dotnet-dev-osx-x64.X.0.100.tar.gz
dotnet-install: URL #2 - primary: https://ci.dot.net/public/Sdk/X.0.100/dotnet-sdk-X.0.100-osx-x64.tar.gz
dotnet-install: URL #3 - legacy: https://ci.dot.net/public/Sdk/X.0.100/dotnet-dev-osx-x64.X.0.100.tar.gz
dotnet-install: Repeatable invocation: ./dotnet-install.sh --version "X.0.100" --install-dir "dotnet-sdk" --architecture "x64" --os "osx" -runtimeid "osx"
```

PowerShell verified file content template:
```
dotnet-install: Payload URLs:
dotnet-install: URL #0 - primary: https://builds.dotnet.microsoft.com/dotnet/Sdk/X.0.100/dotnet-sdk-X.0.100-win-x64.zip
dotnet-install: URL #1 - legacy: https://builds.dotnet.microsoft.com/dotnet/Sdk/X.0.100/dotnet-dev-win-x64.X.0.100.zip
dotnet-install: URL #2 - primary: https://ci.dot.net/public/Sdk/X.0.100/dotnet-sdk-X.0.100-win-x64.zip
dotnet-install: URL #3 - legacy: https://ci.dot.net/public/Sdk/X.0.100/dotnet-dev-win-x64.X.0.100.zip
dotnet-install: Repeatable invocation: .\dotnet-install.ps1 -Version "X.0.100" -InstallDir "dotnet-sdk" -Architecture "x64"
```

#### `AkaMsLinksTests.cs`

**`SDK_IntegrationTest`** — Add a GA quality entry:
```csharp
[InlineData("X.0", "ga", @"https://aka.ms/dotnet/X.0/dotnet-sdk-")]
```

**`Runtime_IntegrationTest`** — The runtime aka.ms tests are currently very sparse (only 6.0 and 7.0 windowsdesktop daily entries are active). Adding new runtime entries is optional. Check if existing patterns suggest adding them.

## Step 4: Validate

After making changes, verify that the solution builds:
```bash
dotnet build tests/Install-Scripts.Test/Install-Scripts.Test.csproj
```

Run the coverage check script again to confirm all gaps are filled:
```bash
python3 .github/skills/update-integration-tests/check-test-coverage.py
```

## Checklist Summary

### Preview1
- [ ] Ran `check-test-coverage.py` to identify gaps
- [ ] Fetched releases index and per-release details for exact version numbers
- [ ] `_channels`: Added preview entry
- [ ] `_runtimeBranches`: Added preview entry
- [ ] `_sdkBranches`: Added preview entry with `Quality.Preview`
- [ ] `WhenChannelResolvesToASpecificRuntimeVersion`: Added InlineData for dotnet, aspnetcore, windowsdesktop
- [ ] `WhenChannelResolvesToASpecificSDKVersion`: Added InlineData for the channel
- [ ] Specific version install tests: Added InlineData with real version numbers (SDK, runtime, aspnetcore, windowsdesktop)
- [ ] Verified no entries added to GA-only tests (`WhenAnExactVersionIsPassedToBash/Powershell`, `WhenInstallingAnAlreadyInstalledVersion`)
- [ ] **Checked all active versions** — fixed any stale entries from prior updates (e.g., `_sdkBranches` still using `Quality.Preview` for a version that went GA)
- [ ] Build succeeds

### GA
- [ ] Ran `check-test-coverage.py` to identify gaps
- [ ] Fetched releases index and identified the GA version numbers and LTS/STS classification
- [ ] `_channels`: Changed from Preview to None + Ga; updated LTS/STS alias
- [ ] `_runtimeBranches`: Changed from Preview to None
- [ ] `_sdkBranches`: Changed from `Quality.Preview` to `Quality.Daily`
- [ ] Specific version install tests: Updated from preview to GA version numbers
- [ ] `WhenAnExactVersionIsPassedToBash/Powershell`: Added GA SDK version InlineData
- [ ] Verified asset files: Created `.verified.txt` files for new exact versions
- [ ] `AkaMsLinksTests.SDK_IntegrationTest`: Added GA quality entry
- [ ] Build succeeds
