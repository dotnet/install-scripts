# Copilot Instructions for install-scripts

## Repository Overview

This repository contains the official .NET install scripts (`dotnet-install.ps1` and `dotnet-install.sh`) used to install the .NET SDK and runtimes on Windows, Linux, and macOS. These scripts are consumed by millions of users and CI systems.

## Building and Testing

- **Build**: `./build.sh` (Linux/macOS) or `Build.cmd` (Windows)
- **Test**: `./build.sh --test` (Linux/macOS) or `Build.cmd -test` (Windows)
- **Filter tests by class**: `./build.sh --test '/p:TestRunnerAdditionalArguments=-class%20ClassName' '/p:UseMicrosoftTestingPlatformRunner=false'`
- Always use the repo build scripts rather than invoking `dotnet build`/`dotnet test` directly — the scripts set up the correct SDK version and Arcade tooling.

## Script Behavior Parity

The PowerShell script (`src/dotnet-install.ps1`) and Bash script (`src/dotnet-install.sh`) must maintain behavioral parity. When making changes to one script:

- Ensure the equivalent logic exists in the other script.
- Keep regex patterns, version detection, and extraction logic consistent between scripts.
- Both scripts follow the same logical flow: resolve version → download archive → determine which files to skip (already-installed versions) → extract → overlay non-versioned files.

## Test Infrastructure

- The test project uses **xUnit v3** with the **Microsoft Testing Platform (MTP)** runner.
- Platform-specific tests use `Assert.SkipUnless` / `Assert.SkipWhen` (not `[SkipFact]` attributes) to skip on unsupported platforms.
- Tests should **not** check for prerequisites like `tar` or `bash` before running — if a required tool is missing on CI, the test should fail loudly so the environment gets fixed.
- Prefer .NET APIs (e.g., `System.Formats.Tar`, `System.IO`) over subprocess calls for test setup (creating tarballs, symlinks, etc.).
- Integration tests download real .NET versions and can take ~19 minutes on CI. Filter to specific test classes during development.

## CI Pipeline

- PR builds run in Azure DevOps via `azure-pipelines-PR.yml` with Windows, Linux, and macOS jobs.
- Test results are published as xUnit XML from `artifacts/TestResults/Debug/*.xml`.
- The Arcade SDK handles most build infrastructure. Files under `eng/common/` come directly from Arcade and will be overwritten — never modify them. Other files under `eng/` are repo-specific and can be modified as needed.
