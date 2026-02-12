# Merge Dependabot Branches

Merge all pending dependabot PRs into the main branch, validate, and deploy.

## Prerequisites

1. **You must be on the `main` branch.** Before proceeding, verify the current branch:
   ```
   git branch --show-current
   ```
   If not on `main`, abort and ask the user to switch to `main` first.

2. **Working directory must be clean.** Check with `git status`.

## Step 1: Check for Dependabot PRs

List all open dependabot PRs:
```bash
gh pr list --author app/dependabot
```

If no PRs exist, inform the user and stop.

## Step 2: Fetch and Merge Locally

1. Fetch all remote branches:
   ```bash
   git fetch --all
   ```

2. Merge each dependabot branch. For each PR branch:
   ```bash
   git merge origin/<branch-name> --no-edit
   ```

3. **If there are conflicts with lock files** (package-lock.json):
   - **Do NOT try to resolve lock file conflicts manually**
   - Accept the current version and mark resolved:
     ```bash
     git checkout --ours package-lock.json
     git add package-lock.json
     ```
   - Complete the merge commit:
     ```bash
     git commit --no-edit
     ```
   - The lock file will be regenerated in Step 4

## Step 3: Fix @types/node Version

The `@types/node` major version **must match** the Node.js major version in `engines.node` in `patchnotes-web/package.json`.

1. Check the engine version:
   ```bash
   grep '"node"' patchnotes-web/package.json
   ```

2. Check current @types/node version:
   ```bash
   grep '"@types/node"' patchnotes-web/package.json
   ```

3. If @types/node major version doesn't match the engine version (e.g., engine is `>=22` but @types/node is `^24.x.x`), fix it:
   ```bash
   cd patchnotes-web && npm install -D @types/node@^22 --legacy-peer-deps && cd ..
   ```

## Step 4: Regenerate Lock Files

After all merges, regenerate lock files to ensure consistency:
```bash
cd patchnotes-web && npm install --legacy-peer-deps && cd ..
```

## Step 5: Build and Validate

### Frontend (patchnotes-web)
```bash
cd patchnotes-web && npm run lint && npm run build && npm run test:run && cd ..
```

### Backend (.NET)
```bash
dotnet build
dotnet test
```

If any checks fail, fix the issues before proceeding.

## Step 6: Commit and Push

1. Stage all changes:
   ```bash
   git add patchnotes-web/package-lock.json
   git add patchnotes-web/package.json  # if any versions were fixed
   ```

2. Commit:
   ```bash
   git commit -m "chore(deps): merge dependabot updates and regenerate lock file"
   ```

3. Push to remote:
   ```bash
   git push origin main
   ```

## Step 7: Monitor CI/Deploy Actions

After pushing, monitor the GitHub Actions workflows:

```bash
# Wait a few seconds for workflows to start
sleep 5

# List recent workflow runs
gh run list --limit 10

# Watch for completion
gh run list --limit 5
```

Report the final status of all triggered workflows to the user.

If any workflow fails, investigate using:
```bash
gh run view <run-id> --log-failed
```
