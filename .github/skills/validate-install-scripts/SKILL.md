---
name: validate-install-scripts
description: >
  Open validation PRs in key .NET repos to test install script changes against their CI.
  Use this skill when asked to validate install scripts, open validation PRs, or check/close
  validation PRs. Supports two modes: 'open' (create PRs) and 'check-close' (verify CI and close).
---

# Validate Install Scripts in Key Repos

This skill automates the "Validation in Key Repos" step of the install scripts release process.
It opens PRs titled `[DO NOT MERGE] Install Scripts Update Validation PR` in key .NET repos
that replace the production install script URL with a raw GitHub URL pointing to a specific commit.

## Prerequisites

- The `gh` CLI must be authenticated with permissions to create branches and PRs in the target repos.
- The commit SHA must exist in the `dotnet/install-scripts` repository on GitHub.

## Quick Start (Preferred)

If PowerShell (Core or Windows PowerShell) is available, use the scripts directly:

```powershell
# Open validation PRs
./.github/skills/validate-install-scripts/Open-ValidationPRs.ps1 -CommitSha "<sha>"

# Later, check CI and close passed PRs
./.github/skills/validate-install-scripts/Close-ValidationPRs.ps1 -CommitSha "<sha>"

# To auto-close without prompting:
./.github/skills/validate-install-scripts/Close-ValidationPRs.ps1 -CommitSha "<sha>" -AutoClose

# To target specific repos only:
./.github/skills/validate-install-scripts/Open-ValidationPRs.ps1 -CommitSha "<sha>" -Repos @("arcade", "sdk")
```

If PowerShell is not available, follow the manual steps below using the `gh` CLI directly.

## Modes

### Mode 1: Open Validation PRs

**When to use:** After pushing install script changes that need validation across repos.

**Input required:** A commit SHA from `dotnet/install-scripts` to validate.

**Steps:**

1. Ask the user for the commit SHA to validate (or accept it as input).

2. Verify the commit exists on GitHub:
```bash
gh api "repos/dotnet/install-scripts/commits/<COMMIT_SHA>" --jq '.sha'
```

3. For each target repository, use `gh` CLI to:
   a. Check if a branch/PR already exists for this SHA (handle idempotency)
   b. Create a new branch named `validate-install-scripts/<short-sha>`
   c. Modify both `eng/common/tools.sh` and `eng/common/tools.ps1` in a single commit
   d. Open a PR with title `[DO NOT MERGE] Install Scripts Update Validation PR`

4. Report all opened PR URLs to the user.

**Target repositories:**
- `dotnet/aspnetcore`
- `dotnet/arcade`
- `dotnet/sdk`
- `dotnet/runtime`
- `dotnet/winforms`

**URL replacements:**

In `eng/common/tools.sh`, find the line containing the install script URL assignment:
```bash
# Find (the URL assignment line — contains this literal text):
local install_script_url="https://builds.dotnet.microsoft.com/dotnet/scripts/$dotnetInstallScriptVersion/dotnet-install.sh"

# Replace the entire line with:
local install_script_url="https://raw.githubusercontent.com/dotnet/install-scripts/<COMMIT_SHA>/src/dotnet-install.sh"
```

In `eng/common/tools.ps1`, find the URI assignment line:
```powershell
# Find (the URI assignment line — contains this literal text):
$uri = "https://builds.dotnet.microsoft.com/dotnet/scripts/$dotnetInstallScriptVersion/dotnet-install.ps1"

# Replace the entire line with:
$uri = "https://raw.githubusercontent.com/dotnet/install-scripts/<COMMIT_SHA>/src/dotnet-install.ps1"
```

**Implementation using `gh` CLI and Git Data API:**

For each repo, execute the following logic. **Stop and report an error** if any step fails.

**Important: Preserving file content exactly.** Shell command substitution `$(...)` strips trailing newlines, and `printf '%s'` omits them. To avoid spurious whitespace diffs, use **base64 encoding** to round-trip file content through the Git Data API. Decode from the API response, perform the sed replacement on the decoded bytes written to a temp file, then re-encode to base64 for the blob creation. This preserves trailing newlines and avoids any shell-induced whitespace changes.

**Important: Fork fallback for repos without push access.** Before creating a branch, check if the user has push access with `gh api "repos/${REPO}" --jq '.permissions.push'`. If `false`, fork the repo first with `gh repo fork "${REPO}" --clone=false`, then create the branch and commits in the user's fork. When opening the PR, use `--head "<username>:${BRANCH}"` to create a cross-fork PR.

```bash
REPO="dotnet/<repo_name>"
COMMIT_SHA="<full_commit_sha>"
SHORT_SHA="${COMMIT_SHA:0:8}"
BRANCH="validate-install-scripts/${SHORT_SHA}"

# Get default branch
DEFAULT_BRANCH=$(gh repo view "$REPO" --json defaultBranchRef --jq '.defaultBranchRef.name')

# --- Determine where to push: upstream or fork ---
HAS_PUSH=$(gh api "repos/${REPO}" --jq '.permissions.push')
if [ "$HAS_PUSH" = "true" ]; then
  TARGET_REPO="$REPO"
  PR_HEAD="$BRANCH"
else
  # Fork the repo (idempotent — returns existing fork if already forked)
  gh repo fork "$REPO" --clone=false
  GH_USER=$(gh api user --jq '.login')
  TARGET_REPO="${GH_USER}/$(basename $REPO)"
  PR_HEAD="${GH_USER}:${BRANCH}"
  # Sync fork's default branch with upstream
  gh repo sync "$TARGET_REPO" --branch "$DEFAULT_BRANCH"
fi

# Check if branch already exists
if gh api "repos/${TARGET_REPO}/git/ref/heads/${BRANCH}" &>/dev/null; then
  echo "Branch ${BRANCH} already exists in ${TARGET_REPO}."
  EXISTING_PR=$(gh pr list --repo "$REPO" --head "$PR_HEAD" --state open --json url --jq '.[0].url')
  if [ -n "$EXISTING_PR" ]; then
    echo "PR already exists: $EXISTING_PR"
    # Skip this repo and move to next
    continue
  fi
fi

# Get the HEAD SHA of the default branch
BASE_SHA=$(gh api "repos/${TARGET_REPO}/git/ref/heads/${DEFAULT_BRANCH}" --jq '.object.sha')

# Create the branch
gh api "repos/${TARGET_REPO}/git/refs" \
  -f "ref=refs/heads/${BRANCH}" \
  -f "sha=${BASE_SHA}"

# --- Fetch file content using base64 to preserve exact bytes ---
# Write raw bytes to temp files to avoid shell newline stripping
TMPDIR=$(mktemp -d)
gh api "repos/${REPO}/contents/eng/common/tools.sh?ref=${DEFAULT_BRANCH}" --jq '.content' | base64 -d > "$TMPDIR/tools.sh"
gh api "repos/${REPO}/contents/eng/common/tools.ps1?ref=${DEFAULT_BRANCH}" --jq '.content' | base64 -d > "$TMPDIR/tools.ps1"

# --- Verify and replace in tools.sh ---
MATCH_COUNT=$(grep -c 'builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.sh' "$TMPDIR/tools.sh" || true)
if [ "$MATCH_COUNT" -ne 1 ]; then
  echo "ERROR: Expected 1 match in tools.sh, found $MATCH_COUNT. The file format may have changed."
  rm -rf "$TMPDIR"
  exit 1
fi

sed -i 's|https://builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.sh|https://raw.githubusercontent.com/dotnet/install-scripts/'"${COMMIT_SHA}"'/src/dotnet-install.sh|' "$TMPDIR/tools.sh"

# --- Verify and replace in tools.ps1 ---
MATCH_COUNT=$(grep -c 'builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.ps1' "$TMPDIR/tools.ps1" || true)
if [ "$MATCH_COUNT" -ne 1 ]; then
  echo "ERROR: Expected 1 match in tools.ps1, found $MATCH_COUNT. The file format may have changed."
  rm -rf "$TMPDIR"
  exit 1
fi

sed -i 's|https://builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.ps1|https://raw.githubusercontent.com/dotnet/install-scripts/'"${COMMIT_SHA}"'/src/dotnet-install.ps1|' "$TMPDIR/tools.ps1"

# --- Create blobs using base64 encoding to preserve exact bytes ---
SH_BLOB=$(base64 -w 0 "$TMPDIR/tools.sh" | gh api "repos/${TARGET_REPO}/git/blobs" \
  --method POST \
  -f "encoding=base64" \
  -F "content=@-" \
  --jq '.sha')

PS1_BLOB=$(base64 -w 0 "$TMPDIR/tools.ps1" | gh api "repos/${TARGET_REPO}/git/blobs" \
  --method POST \
  -f "encoding=base64" \
  -F "content=@-" \
  --jq '.sha')

rm -rf "$TMPDIR"

# --- Create tree and commit atomically ---
BASE_TREE=$(gh api "repos/${TARGET_REPO}/git/commits/${BASE_SHA}" --jq '.tree.sha')

NEW_TREE=$(cat <<EOF | gh api "repos/${TARGET_REPO}/git/trees" --method POST --input - --jq '.sha'
{
  "base_tree": "${BASE_TREE}",
  "tree": [
    {"path": "eng/common/tools.sh", "mode": "100755", "type": "blob", "sha": "${SH_BLOB}"},
    {"path": "eng/common/tools.ps1", "mode": "100644", "type": "blob", "sha": "${PS1_BLOB}"}
  ]
}
EOF
)

NEW_COMMIT=$(cat <<EOF | gh api "repos/${TARGET_REPO}/git/commits" --method POST --input - --jq '.sha'
{
  "message": "Validate install scripts from dotnet/install-scripts@${SHORT_SHA}",
  "tree": "${NEW_TREE}",
  "parents": ["${BASE_SHA}"]
}
EOF
)

# Update the branch ref
gh api "repos/${TARGET_REPO}/git/refs/heads/${BRANCH}" \
  --method PATCH \
  -f "sha=${NEW_COMMIT}"

# Open a draft PR
gh pr create --repo "$REPO" \
  --base "$DEFAULT_BRANCH" \
  --head "$PR_HEAD" \
  --title "[DO NOT MERGE] Install Scripts Update Validation PR" \
  --draft \
  --body "This PR validates install script changes from dotnet/install-scripts commit \`${COMMIT_SHA}\`.

**Do not merge this PR.** It will be closed once CI passes.

Install scripts commit: https://github.com/dotnet/install-scripts/commit/${COMMIT_SHA}

Changes:
- \`eng/common/tools.sh\`: Points to test install script
- \`eng/common/tools.ps1\`: Points to test install script"
```

### Mode 2: Check CI Status and Close PRs

**When to use:** After validation PRs have been opened and you want to check if CI has passed.

**Input required:** The same commit SHA used when opening the PRs (to identify the correct branches/PRs).

**Steps:**

1. Ask the user for the commit SHA that was used to open the validation PRs.

2. For each target repo, find the PR by branch name (check both direct and fork-based PRs):

```bash
SHORT_SHA="${COMMIT_SHA:0:8}"
BRANCH="validate-install-scripts/${SHORT_SHA}"
GH_USER=$(gh api user --jq '.login')

for REPO in dotnet/aspnetcore dotnet/arcade dotnet/sdk dotnet/runtime dotnet/winforms; do
  # Search for PR from direct branch or from user's fork
  PR_INFO=$(gh pr list --repo "$REPO" --head "$BRANCH" --state open --json number,url,statusCheckRollup)
  if [ -z "$PR_INFO" ] || [ "$PR_INFO" = "[]" ]; then
    PR_INFO=$(gh pr list --repo "$REPO" --head "${GH_USER}:${BRANCH}" --state open --json number,url,statusCheckRollup)
  fi
  # Process results...
done
```

3. For each PR found, check the CI status:

```bash
gh pr checks --repo "$REPO" <PR_NUMBER>
```

4. Report a summary table to the user showing each repo's status:
   - ✅ All checks passed → offer to close
   - ⏳ Checks still running → report pending
   - ❌ Checks failed → report failures and let user decide

5. For PRs where all **required** checks have passed, close them and delete the branch:

```bash
gh pr close --repo "$REPO" <PR_NUMBER> --delete-branch --comment "CI passed. Closing validation PR."
```

6. If some PRs are still pending, tell the user they can re-invoke this skill later.

## Error Handling

- **Commit not found:** If the commit SHA doesn't exist in `dotnet/install-scripts`, stop immediately and tell the user.
- **No push access:** If the user lacks push access to a repo, fork it and create the branch in the fork. Open a cross-fork PR using `--head "<username>:<branch>"`. This is automatic — do not ask the user.
- **Branch already exists:** If the branch exists and has an open PR, report the existing PR URL. If the branch exists but has no PR, offer to open one or delete and recreate.
- **Replacement not found:** If the expected URL pattern is not found in `tools.sh` or `tools.ps1`, stop and report that the file format may have changed. Do not open a PR with unchanged files.
- **Fork already exists:** `gh repo fork` is idempotent — if the fork already exists it returns successfully. No special handling needed.

## Notes

- The `eng/common/` directory in these repos is managed by Arcade. The validation PRs intentionally modify these files temporarily — the changes are never merged.
- PRs are opened as **drafts** to prevent accidental merge. Combined with the `[DO NOT MERGE]` title prefix, this provides two layers of protection.
- The Git Data API approach creates a single atomic commit with both file changes, avoiding partial updates.
- File content is round-tripped through **base64 encoding** (decode from API → write to temp file → sed in-place → re-encode for blob creation) to preserve exact bytes including trailing newlines and line endings.
- If the user lacks push access to a target repo, the skill automatically forks the repo and creates a cross-fork PR. This makes the skill usable by anyone with a GitHub account.
- CI run times vary by repo: Arcade is fastest (~30 min), Runtime can take several hours.
- The branch name `validate-install-scripts/<short-sha>` serves as the correlation key between open and check-close modes.
