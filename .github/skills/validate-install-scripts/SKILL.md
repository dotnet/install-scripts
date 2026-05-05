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

```bash
REPO="dotnet/<repo_name>"
COMMIT_SHA="<full_commit_sha>"
SHORT_SHA="${COMMIT_SHA:0:8}"
BRANCH="validate-install-scripts/${SHORT_SHA}"

# Get default branch
DEFAULT_BRANCH=$(gh repo view "$REPO" --json defaultBranchRef --jq '.defaultBranchRef.name')

# Check if branch already exists
if gh api "repos/${REPO}/git/ref/heads/${BRANCH}" &>/dev/null; then
  echo "Branch ${BRANCH} already exists in ${REPO}."
  # Check if PR already exists
  EXISTING_PR=$(gh pr list --repo "$REPO" --head "$BRANCH" --state open --json url --jq '.[0].url')
  if [ -n "$EXISTING_PR" ]; then
    echo "PR already exists: $EXISTING_PR"
    # Skip this repo and move to next
    continue
  fi
fi

# Get the HEAD SHA of the default branch
BASE_SHA=$(gh api "repos/${REPO}/git/ref/heads/${DEFAULT_BRANCH}" --jq '.object.sha')

# Create the branch
gh api "repos/${REPO}/git/refs" \
  -f "ref=refs/heads/${BRANCH}" \
  -f "sha=${BASE_SHA}"

# --- Fetch and modify tools.sh ---
TOOLS_SH_RESPONSE=$(gh api "repos/${REPO}/contents/eng/common/tools.sh?ref=${BRANCH}")
TOOLS_SH_BLOB_SHA=$(echo "$TOOLS_SH_RESPONSE" | jq -r '.sha')
TOOLS_SH_CONTENT=$(echo "$TOOLS_SH_RESPONSE" | jq -r '.content' | base64 -d)

# Perform the replacement and verify exactly 1 match
MATCH_COUNT=$(echo "$TOOLS_SH_CONTENT" | grep -c 'builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.sh' || true)
if [ "$MATCH_COUNT" -ne 1 ]; then
  echo "ERROR: Expected 1 match in tools.sh, found $MATCH_COUNT. The file format may have changed."
  exit 1
fi

UPDATED_SH=$(printf '%s' "$TOOLS_SH_CONTENT" | sed 's|https://builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.sh|https://raw.githubusercontent.com/dotnet/install-scripts/'"${COMMIT_SHA}"'/src/dotnet-install.sh|')

# --- Fetch and modify tools.ps1 ---
TOOLS_PS1_RESPONSE=$(gh api "repos/${REPO}/contents/eng/common/tools.ps1?ref=${BRANCH}")
TOOLS_PS1_BLOB_SHA=$(echo "$TOOLS_PS1_RESPONSE" | jq -r '.sha')
TOOLS_PS1_CONTENT=$(echo "$TOOLS_PS1_RESPONSE" | jq -r '.content' | base64 -d)

MATCH_COUNT=$(echo "$TOOLS_PS1_CONTENT" | grep -c 'builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.ps1' || true)
if [ "$MATCH_COUNT" -ne 1 ]; then
  echo "ERROR: Expected 1 match in tools.ps1, found $MATCH_COUNT. The file format may have changed."
  exit 1
fi

UPDATED_PS1=$(printf '%s' "$TOOLS_PS1_CONTENT" | sed 's|https://builds.dotnet.microsoft.com/dotnet/scripts/\$dotnetInstallScriptVersion/dotnet-install.ps1|https://raw.githubusercontent.com/dotnet/install-scripts/'"${COMMIT_SHA}"'/src/dotnet-install.ps1|')

# --- Create blobs, tree, and commit atomically via Git Data API ---
# Create blob for tools.sh
SH_BLOB=$(printf '%s' "$UPDATED_SH" | gh api "repos/${REPO}/git/blobs" \
  --method POST \
  -f "encoding=utf-8" \
  -F "content=@-" \
  --jq '.sha')

# Create blob for tools.ps1
PS1_BLOB=$(printf '%s' "$UPDATED_PS1" | gh api "repos/${REPO}/git/blobs" \
  --method POST \
  -f "encoding=utf-8" \
  -F "content=@-" \
  --jq '.sha')

# Get the base tree
BASE_TREE=$(gh api "repos/${REPO}/git/commits/${BASE_SHA}" --jq '.tree.sha')

# Create a new tree with both file changes
NEW_TREE=$(gh api "repos/${REPO}/git/trees" \
  --method POST \
  -f "base_tree=${BASE_TREE}" \
  -f "tree[][path]=eng/common/tools.sh" \
  -f "tree[][mode]=100755" \
  -f "tree[][type]=blob" \
  -f "tree[][sha]=${SH_BLOB}" \
  -f "tree[][path]=eng/common/tools.ps1" \
  -f "tree[][mode]=100644" \
  -f "tree[][type]=blob" \
  -f "tree[][sha]=${PS1_BLOB}" \
  --jq '.sha')

# Create the commit
NEW_COMMIT=$(gh api "repos/${REPO}/git/commits" \
  --method POST \
  -f "message=Validate install scripts from dotnet/install-scripts@${SHORT_SHA}" \
  -f "tree=${NEW_TREE}" \
  -f "parents[]=${BASE_SHA}" \
  --jq '.sha')

# Update the branch ref to point to the new commit
gh api "repos/${REPO}/git/refs/heads/${BRANCH}" \
  --method PATCH \
  -f "sha=${NEW_COMMIT}"

# Open the PR
gh pr create --repo "$REPO" \
  --base "$DEFAULT_BRANCH" \
  --head "$BRANCH" \
  --title "[DO NOT MERGE] Install Scripts Update Validation PR" \
  --body "This PR validates install script changes from dotnet/install-scripts commit \`${COMMIT_SHA}\`.

**Do not merge this PR.** It will be closed once CI passes.

Install scripts commit: https://github.com/dotnet/install-scripts/commit/${COMMIT_SHA}

Changes:
- \`eng/common/tools.sh\`: Points to test install script
- \`eng/common/tools.ps1\`: Points to test install script"
```

**Important:** The `tree[]` array syntax above is illustrative. When using `gh api`, you may need to construct the JSON body directly:

```bash
NEW_TREE=$(gh api "repos/${REPO}/git/trees" \
  --method POST \
  --input - <<EOF | jq -r '.sha'
{
  "base_tree": "${BASE_TREE}",
  "tree": [
    {"path": "eng/common/tools.sh", "mode": "100755", "type": "blob", "sha": "${SH_BLOB}"},
    {"path": "eng/common/tools.ps1", "mode": "100644", "type": "blob", "sha": "${PS1_BLOB}"}
  ]
}
EOF
)
```

### Mode 2: Check CI Status and Close PRs

**When to use:** After validation PRs have been opened and you want to check if CI has passed.

**Input required:** The same commit SHA used when opening the PRs (to identify the correct branches/PRs).

**Steps:**

1. Ask the user for the commit SHA that was used to open the validation PRs.

2. For each target repo, find the PR by branch name:

```bash
SHORT_SHA="${COMMIT_SHA:0:8}"
BRANCH="validate-install-scripts/${SHORT_SHA}"

for REPO in dotnet/aspnetcore dotnet/arcade dotnet/sdk dotnet/runtime dotnet/winforms; do
  PR_INFO=$(gh pr list --repo "$REPO" --head "$BRANCH" --state open --json number,url,statusCheckRollup)
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
- **Permission denied:** If `gh` cannot create branches/PRs in a repo, report which repo failed and continue with the others.
- **Branch already exists:** If the branch exists and has an open PR, report the existing PR URL. If the branch exists but has no PR, offer to open one or delete and recreate.
- **Replacement not found:** If the expected URL pattern is not found in `tools.sh` or `tools.ps1`, stop and report that the file format may have changed. Do not open a PR with unchanged files.

## Notes

- The `eng/common/` directory in these repos is managed by Arcade. The validation PRs intentionally modify these files temporarily — the changes are never merged.
- The Git Data API approach creates a single atomic commit with both file changes, avoiding partial updates.
- CI run times vary by repo: Arcade is fastest (~30 min), Runtime can take several hours.
- The branch name `validate-install-scripts/<short-sha>` serves as the correlation key between open and check-close modes.
