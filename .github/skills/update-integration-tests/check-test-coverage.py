#!/usr/bin/env python3
"""Check integration test coverage against the .NET releases index.

Fetches live release data, parses the test files, and reports missing or
incorrect entries. Run from the repository root:

    python3 .github/skills/update-integration-tests/check-test-coverage.py
"""

import json
import re
import sys
import urllib.request
from pathlib import Path

RELEASES_INDEX_URL = (
    "https://raw.githubusercontent.com/dotnet/core/main/release-notes/releases-index.json"
)
RELEASES_URL_TEMPLATE = (
    "https://builds.dotnet.microsoft.com/dotnet/release-metadata/{version}/releases.json"
)

TEST_DIR = Path("tests/Install-Scripts.Test")
INSTALL_TEST = TEST_DIR / "GivenThatIWantToInstallDotnetFromAScript.cs"
SDK_LINKS_TEST = TEST_DIR / "GivenThatIWantToGetTheSdkLinksFromAScript.cs"
AKAMS_TEST = TEST_DIR / "AkaMsLinksTests.cs"


def fetch_json(url: str) -> dict:
    with urllib.request.urlopen(url, timeout=30) as resp:
        return json.loads(resp.read().decode())


def get_release_details(channel_version: str) -> dict:
    url = RELEASES_URL_TEMPLATE.format(version=channel_version)
    data = fetch_json(url)
    r = data["releases"][0]
    asp = r.get("aspnetcore-runtime") or r.get("aspnetcore") or {}
    return {
        "sdk": r.get("sdk", {}).get("version"),
        "runtime": r.get("runtime", {}).get("version"),
        "aspnetcore": asp.get("version"),
        "windowsdesktop": r.get("windowsdesktop", {}).get("version"),
    }


def find_repo_root() -> Path:
    candidates = [Path.cwd()]
    script_path = Path(__file__).resolve().parent
    # walk up from script location
    for p in [script_path] + list(script_path.parents):
        if (p / TEST_DIR).exists():
            return p
    for c in candidates:
        if (c / TEST_DIR).exists():
            return c
    return Path.cwd()


def has_pattern(content: str, pattern: str) -> bool:
    return bool(re.search(pattern, content))


class Report:
    def __init__(self):
        self.issues: list[str] = []

    def ok(self, msg: str):
        print(f"    ✅ {msg}")

    def missing(self, msg: str):
        print(f"    ❌ {msg}")
        self.issues.append(msg)

    def warn(self, msg: str):
        print(f"    ⚠️  {msg}")
        self.issues.append(msg)


def check_channels(content: str, version: str, phase: str, report: Report):
    esc = re.escape(version)
    if phase == "preview":
        pat = rf'\("{esc}",\s*"{esc.replace(chr(46), r"\\\\.")}[^"]*",\s*Quality\.Preview\)'
        # simpler: just search for the literal text
        if f'("{version}"' in content and "Quality.Preview" in content:
            # more precise check
            block = content[content.find("_channels"):content.find("_runtimeBranches")]
            if f'("{version}"' in block and "Quality.Preview" in block:
                report.ok("_channels: has Preview entry")
                return
        report.missing(f"_channels: missing ({version}, Quality.Preview)")
    else:
        block = content[content.find("_channels"):content.find("_runtimeBranches")]
        has_none = f'("{version}"' in block and "Quality.None" in block
        has_ga = f'("{version}"' in block and "Quality.Ga" in block
        # More precise check
        has_none = bool(re.search(rf'\("{re.escape(version)}",\s*"[^"]+",\s*Quality\.None\)', block))
        has_ga = bool(re.search(rf'\("{re.escape(version)}",\s*"[^"]+",\s*Quality\.Ga\)', block))
        if has_none and has_ga:
            report.ok("_channels: has GA entries (None + Ga)")
        else:
            parts = []
            if not has_none:
                parts.append("Quality.None")
            if not has_ga:
                parts.append("Quality.Ga")
            report.missing(f"_channels: missing {', '.join(parts)} entries")


def check_runtime_branches(content: str, version: str, phase: str, report: Report):
    # Extract just the _runtimeBranches block
    start = content.find("_runtimeBranches")
    end = content.find("_sdkBranches")
    block = content[start:end] if start != -1 and end != -1 else content

    expected_q = "Preview" if phase == "preview" else "None"
    pat = rf'\("{re.escape(version)}",\s*"[^"]+",\s*Quality\.{expected_q}\)'
    if re.search(pat, block):
        report.ok(f"_runtimeBranches: has Quality.{expected_q} entry")
    else:
        # Check if exists with wrong quality
        any_pat = rf'\("{re.escape(version)}",\s*"[^"]+",\s*Quality\.(\w+)\)'
        m = re.search(any_pat, block)
        if m:
            report.warn(f"_runtimeBranches: has Quality.{m.group(1)}, expected Quality.{expected_q}")
        else:
            report.missing(f"_runtimeBranches: missing entry with Quality.{expected_q}")


def check_sdk_branches(content: str, version: str, phase: str, report: Report):
    major = version.split(".")[0]
    sdk_branch = f"{major}.0.1xx"

    # Extract just the _sdkBranches block
    start = content.find("_sdkBranches")
    end = content.find("InstallSdkFromChannelTestCases")
    block = content[start:end] if start != -1 and end != -1 else content

    expected_q = "Preview" if phase == "preview" else "Daily"
    pat = rf'\("{re.escape(sdk_branch)}",\s*"[^"]+",\s*Quality\.{expected_q}\)'
    if re.search(pat, block):
        report.ok(f"_sdkBranches: has ({sdk_branch}, Quality.{expected_q})")
    else:
        any_pat = rf'\("{re.escape(sdk_branch)}",\s*"[^"]+",\s*Quality\.(\w+)\)'
        m = re.search(any_pat, block)
        if m:
            report.warn(
                f"_sdkBranches: ({sdk_branch}) has Quality.{m.group(1)}, "
                f"expected Quality.{expected_q}"
            )
        else:
            report.missing(f"_sdkBranches: missing ({sdk_branch}, Quality.{expected_q})")


def check_specific_version_tests(content: str, details: dict, phase: str, report: Report):
    """Check specific version InlineData entries.

    These tests use a fixed version from when the entry was first added — they do NOT
    track the latest version. We just verify that SOME version matching the major.minor
    exists near each test method.
    """
    tests = {
        "WhenInstallingASpecificVersionOfTheSdk": details.get("sdk"),
        "WhenInstallingASpecificVersionOfDotnetRuntime": details.get("runtime"),
        "WhenInstallingASpecificVersionOfAspNetCoreRuntime": details.get("aspnetcore"),
        "WhenInstallingASpecificVersionOfWindowsdesktopRuntime": details.get("windowsdesktop"),
    }
    for method, ver in tests.items():
        if not ver:
            continue
        # Extract major.minor prefix (e.g., "11.0" from "11.0.100-preview.1.26104.118")
        m = re.match(r"(\d+\.\d+)", ver)
        if not m:
            continue
        major_minor = m.group(1)

        # Find the method and look at InlineData entries before it
        method_idx = content.find(f"public void {method}")
        if method_idx == -1:
            method_idx = content.find(f"public async Task {method}")
        if method_idx == -1:
            continue

        # Check InlineData entries in the ~2000 chars before the method
        section = content[max(0, method_idx - 2000):method_idx]
        found = bool(re.search(rf'\[InlineData\("{re.escape(major_minor)}\.\d', section))

        short_name = method.replace("WhenInstallingASpecificVersionOf", "")
        if found:
            report.ok(f"Specific version ({short_name}): has {major_minor}.x entry")
        else:
            report.missing(f"Specific version ({short_name}): missing entry for {major_minor}.x (e.g., {ver})")


def check_sdk_links_runtime(content: str, version: str, report: Report):
    for runtime in ("dotnet", "aspnetcore", "windowsdesktop"):
        pat = rf'\[InlineData\("{re.escape(version)}",\s*"{runtime}"'
        if re.search(pat, content):
            report.ok(f"WhenChannelResolvesToASpecificRuntimeVersion ({runtime}): present")
        else:
            report.missing(
                f"WhenChannelResolvesToASpecificRuntimeVersion ({runtime}): "
                f'missing [InlineData("{version}", "{runtime}"...)]'
            )


def check_sdk_links_sdk_channel(content: str, version: str, report: Report):
    # The method is WhenChannelResolvesToASpecificSDKVersion
    pat = rf'\[InlineData\("{re.escape(version)}"\)'
    if re.search(pat, content):
        report.ok("WhenChannelResolvesToASpecificSDKVersion: present")
    else:
        report.missing(
            f'WhenChannelResolvesToASpecificSDKVersion: missing [InlineData("{version}")]'
        )


def check_akams(content: str, version: str, phase: str, report: Report):
    if phase == "preview":
        report.ok("AkaMsLinksTests: skipped (preview — aka.ms links not live)")
        return
    pat = rf'\[InlineData\("{re.escape(version)}",\s*"ga"'
    if re.search(pat, content):
        report.ok("AkaMsLinksTests SDK_IntegrationTest: has GA entry")
    else:
        report.missing(f'AkaMsLinksTests SDK_IntegrationTest: missing GA entry for {version}')


def check_aliases(content: str, latest_lts: str | None, latest_sts: str | None, report: Report):
    # Extract just the _channels block
    start = content.find("_channels")
    end = content.find("_runtimeBranches")
    block = content[start:end] if start != -1 and end != -1 else content

    if latest_lts:
        # In the C# source, dots are escaped as \\. so "10.0" becomes "10\\.0"
        version_escaped = latest_lts.replace(".", "\\\\.")
        target = f'("LTS", "{version_escaped}'
        if target in block:
            report.ok(f"LTS alias targets {latest_lts}")
        else:
            # Find what it currently targets
            m = re.search(r'\("LTS",\s*"(\d+)\\\\.(\d+)', block)
            current = f"{m.group(1)}.{m.group(2)}" if m else "unknown"
            if current == latest_lts:
                report.ok(f"LTS alias targets {latest_lts}")
            else:
                report.warn(f"LTS alias targets {current}, should target {latest_lts}")

    if latest_sts:
        version_escaped = latest_sts.replace(".", "\\\\.")
        target = f'("STS", "{version_escaped}'
        if target in block:
            report.ok(f"STS alias targets {latest_sts}")
        else:
            m = re.search(r'\("STS",\s*"(\d+)\\\\.(\d+)', block)
            current = f"{m.group(1)}.{m.group(2)}" if m else "unknown"
            if current == latest_sts:
                report.ok(f"STS alias targets {latest_sts}")
            else:
                report.warn(f"STS alias targets {current}, should target {latest_sts}")


def check_exact_version_tests(
    sdk_links_content: str, version: str, phase: str, report: Report
):
    """Check WhenAnExactVersionIsPassedToBash/Powershell — GA only."""
    if phase == "preview":
        return  # These are GA-only tests

    major = version.split(".")[0]

    for method in ("WhenAnExactVersionIsPassedToBash", "WhenAnExactVersionIsPassedToPowershell"):
        # Check for any SDK version starting with major.0 (e.g., "8.0.303", "9.0.100")
        pat = rf'\[InlineData\("{re.escape(major)}\.0\.\d+'
        if re.search(pat, sdk_links_content):
            report.ok(f"{method}: has {major}.0.x entry")
        else:
            report.missing(f"{method}: missing entry for {major}.0.x SDK version")


def main():
    root = find_repo_root()
    install_content = (root / INSTALL_TEST).read_text() if (root / INSTALL_TEST).exists() else ""
    sdk_links_content = (
        (root / SDK_LINKS_TEST).read_text() if (root / SDK_LINKS_TEST).exists() else ""
    )
    akams_content = (root / AKAMS_TEST).read_text() if (root / AKAMS_TEST).exists() else ""

    if not install_content:
        print(f"ERROR: Could not find {INSTALL_TEST} (looked in {root})")
        sys.exit(1)

    print("Fetching .NET releases index...")
    index = fetch_json(RELEASES_INDEX_URL)
    releases = [
        r
        for r in index["releases-index"]
        if r.get("support-phase") in ("preview", "active")
    ]

    if not releases:
        print("No active or preview releases found.")
        return

    releases.sort(key=lambda r: float(r["channel-version"]))

    # Determine LTS/STS alias targets
    active_lts = [
        r["channel-version"]
        for r in releases
        if r["support-phase"] == "active" and r["release-type"] == "lts"
    ]
    active_sts = [
        r["channel-version"]
        for r in releases
        if r["support-phase"] == "active" and r["release-type"] == "sts"
    ]
    latest_lts = max(active_lts, key=float) if active_lts else None
    latest_sts = max(active_sts, key=float) if active_sts else None

    print()
    print("=" * 64)
    print("  RELEASE STATUS")
    print("=" * 64)
    for r in sorted(releases, key=lambda x: float(x["channel-version"]), reverse=True):
        v = r["channel-version"]
        phase = r["support-phase"]
        rtype = r["release-type"].upper()
        sdk = r["latest-sdk"]
        runtime = r["latest-runtime"]
        print(f"  {v:>5}  {phase:<8} {rtype:<4}  SDK {sdk}, Runtime {runtime}")
    if latest_lts:
        print(f"\n  LTS alias → {latest_lts}")
    if latest_sts:
        print(f"  STS alias → {latest_sts}")

    report = Report()

    print()
    print("=" * 64)
    print("  TEST COVERAGE")
    print("=" * 64)

    for r in releases:
        v = r["channel-version"]
        phase = r["support-phase"]
        rtype = r["release-type"]

        print(f"\n  [{v}] ({phase}, {rtype})")

        # Fetch component versions
        try:
            details = get_release_details(v)
        except Exception as e:
            print(f"    ⚠️  Could not fetch release details: {e}")
            details = {
                "sdk": r["latest-sdk"],
                "runtime": r["latest-runtime"],
                "aspnetcore": None,
                "windowsdesktop": None,
            }

        # GivenThatIWantToInstallDotnetFromAScript.cs checks
        check_channels(install_content, v, phase, report)
        check_runtime_branches(install_content, v, phase, report)
        check_sdk_branches(install_content, v, phase, report)
        check_specific_version_tests(install_content, details, phase, report)

        # GivenThatIWantToGetTheSdkLinksFromAScript.cs checks
        check_sdk_links_runtime(sdk_links_content, v, report)
        check_sdk_links_sdk_channel(sdk_links_content, v, report)
        check_exact_version_tests(sdk_links_content, v, phase, report)

        # AkaMsLinksTests.cs checks
        check_akams(akams_content, v, phase, report)

    # LTS/STS aliases
    print(f"\n  [LTS/STS Aliases]")
    check_aliases(install_content, latest_lts, latest_sts, report)

    # Summary
    print()
    print("=" * 64)
    if report.issues:
        print(f"  ⚠️  {len(report.issues)} issue(s) found")
        sys.exit(1)
    else:
        print("  ✅ All test entries appear up to date")


if __name__ == "__main__":
    main()
