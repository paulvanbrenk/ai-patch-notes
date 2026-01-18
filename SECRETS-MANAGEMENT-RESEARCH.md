# Secrets Management for Multi-Worktree Development

Research report for pa-ad1: Evaluating secrets management solutions for multi-worktree git development environments.

## Executive Summary

**Recommendation: direnv + dotenvx (hybrid approach)**

For multi-worktree development, the combination of **direnv** for per-worktree environment isolation and **dotenvx** for encrypted secrets provides the best balance of developer experience, security, and worktree compatibility.

## Solutions Evaluated

### 1. dotenv / dotenvx

**What it is:** Environment variable loader for Node.js projects. dotenvx is the modern successor with encryption support.

**Pros:**
- Simple, widely adopted pattern (`.env` files)
- dotenvx adds encryption via ECIES (Elliptic Curve Integrated Encryption)
- Supports multiple environments (`.env.development`, `.env.production`)
- Encrypted `.env.vault` files can be committed to git
- Cross-platform support

**Cons:**
- Node.js/npm-centric (less natural for .NET projects)
- Each worktree needs its own `.env` file copy
- No automatic environment switching when changing directories

**Multi-worktree compatibility:** Medium - requires manual `.env` setup per worktree

### 2. direnv

**What it is:** Shell extension that loads/unloads environment variables automatically based on directory.

**Pros:**
- Automatic environment switching when `cd`-ing between worktrees
- Language-agnostic (works with .NET, Node, Python, etc.)
- Supports nested environments via `source_env`
- Can integrate with keyring/secrets managers (gnome-keyring, AWS Secrets Manager)
- Each worktree naturally gets isolated environment

**Cons:**
- Requires shell integration (bash/zsh/fish)
- `.envrc` files should not be committed (contain secrets or paths)
- Slightly more setup complexity

**Multi-worktree compatibility:** Excellent - designed for directory-based environment isolation

### 3. git-secret

**What it is:** GPG-based tool to encrypt files in git repositories.

**Pros:**
- Files stored encrypted in git, decrypted locally
- Access managed via GPG keys (add/remove team members)
- Works with any file type
- Transparent workflow once set up

**Cons:**
- GPG key management complexity
- All worktrees share same encrypted files (git content)
- Decrypted files must be in `.gitignore`
- Re-encryption needed when team membership changes

**Multi-worktree compatibility:** Low - encrypted files are in git (shared), decrypted files need per-worktree management

### 4. SOPS (Secrets OPerationS)

**What it is:** Mozilla/CNCF tool for encrypting structured files (YAML, JSON, ENV).

**Pros:**
- Encrypts values only (keys visible for easier review)
- Supports AWS KMS, GCP KMS, Azure Key Vault, age, PGP
- Git diff integration (shows decrypted diffs)
- Well-suited for GitOps and Kubernetes secrets
- Cloud key services simplify access management

**Cons:**
- Overkill for local development secrets
- Requires KMS setup or PGP keys
- Better suited for infrastructure/deployment secrets than dev environments

**Multi-worktree compatibility:** Medium - encrypted files in git, but decryption context is per-worktree

## Multi-Worktree Considerations

Git worktrees share the same `.git` directory but have separate working directories. This means:

| Aspect | Shared | Per-worktree |
|--------|--------|--------------|
| `.gitignore` patterns | Yes | No |
| Committed files | Yes | No |
| Untracked/ignored files | No | Yes |
| Environment variables | No | Yes |

**Key insight:** Secrets must either be:
1. **Encrypted in git** (shared, decrypted per-worktree), or
2. **Local to each worktree** (not in git, managed separately)

## Recommended Approach

### Option A: direnv + dotenvx (Recommended)

```
worktree-main/
  .envrc          # loads local .env, gitignored
  .env            # dotenvx encrypted, gitignored
  .env.keys       # decryption key, gitignored

worktree-feature/
  .envrc          # loads local .env, gitignored
  .env            # dotenvx encrypted, gitignored
  .env.keys       # decryption key, gitignored
```

**Setup:**
1. Install direnv and configure shell integration
2. Install dotenvx: `npm install -g @dotenvx/dotenvx`
3. Create `.envrc` template (committed): sources `.env` if exists
4. Each developer runs `dotenvx encrypt` in their worktree
5. Add to `.gitignore`: `.env`, `.env.keys`, `.envrc`

**Benefits:**
- Automatic env loading when switching worktrees
- Encrypted secrets never accidentally committed
- Works with .NET, Node, any stack via environment variables

### Option B: SOPS for shared secrets + direnv for local overrides

Better for teams needing shared secret access (staging/prod credentials).

```
.sops.yaml                    # SOPS config (committed)
secrets/config.enc.yaml       # encrypted shared secrets (committed)
.envrc                        # gitignored, loads decrypted secrets
```

### Option C: git-secret for file encryption

Better for projects with config files that must be encrypted rather than env vars.

## Implementation for This Project

Given this is a .NET project with a web frontend:

1. **Add to `.gitignore`:**
   ```
   # Secrets
   .env
   .env.*
   !.env.example
   .envrc
   ```

2. **Create `.env.example`** (committed) documenting required variables

3. **Install direnv** on dev machines

4. **Create `.envrc.template`** (committed):
   ```bash
   # Copy to .envrc and customize
   dotenv_if_exists
   ```

5. **Document in README** the secrets setup process

## Sources

- [dotenvx](https://dotenvx.com/) - Modern dotenv with encryption
- [dotenv-vault](https://github.com/dotenv-org/dotenv-vault) - Team sync for .env files
- [direnv](https://github.com/direnv/direnv) - Directory-based environment management
- [Secrets Management with Direnv](https://www.papermtn.co.uk/secrets-management-managing-environment-variables-with-direnv/)
- [git-secret](https://sobolevn.me/git-secret/) - GPG-based git encryption
- [SOPS](https://github.com/getsops/sops) - CNCF secrets management
- [SOPS Guide](https://blog.gitguardian.com/a-comprehensive-guide-to-sops/)
