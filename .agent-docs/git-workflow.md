# Git Workflow

## Quick Reference

```bash
# Start new work
git checkout main && git pull origin main
git checkout -b feature/<name>-bd-<id>

# Keep branch updated
git fetch origin && git rebase main

# Session completion (MANDATORY)
git pull --rebase && bd sync && git push && git status
```

## Branch Naming

| Prefix | Purpose | Example |
|--------|---------|---------|
| `feature/` | New functionality | `feature/chest-sync-bd-0df` |
| `bugfix/` | Bug fixes | `bugfix/player-desync-bd-123` |
| `chore/` | Maintenance, tooling | `chore/build-script` |
| `hotfix/` | Urgent production fixes | `hotfix/crash-on-load` |

**Convention**: `<prefix>/<descriptive-name>-bd-<issue-id>`

## Branch Workflow

### 1. Create Branch
```bash
git checkout main
git pull origin main
git checkout -b feature/my-feature-bd-XYZ
bd update XYZ --status in_progress  # Claim the issue
```

### 2. Develop
```bash
git add <files>
git commit -m "feat: description (bd-XYZ)"
git push origin feature/my-feature-bd-XYZ
```

### 3. Sync with Main
```bash
git fetch origin
git rebase main                    # Preferred: linear history
git push --force-with-lease origin feature/my-feature-bd-XYZ
```

### 4. Merge
- Use **Squash and Merge** for feature branches
- Delete branch after merge: `git branch -d <name> && git push origin --delete <name>`
- Close issue: `bd close XYZ`

## bd Integration

**CRITICAL**: `bd sync` must run with git operations to maintain work context.

### When to Run bd sync
- End of every session (MANDATORY)
- After creating/closing issues
- Before `git pull` or `git push`

### Session Complete Sequence
```bash
# 1. Quality gates (if code changed)
npm test && npm run lint

# 2. Commit and push
git add <files>
git commit -m "..."
bd sync                  # Commit beads changes
git pull --rebase        # Get latest
git push                 # Push to remote
git status               # Verify: "up to date with origin"

# 3. Update issues
bd close <id>            # Close completed work
bd sync                  # Final sync
```

### Commit Message Format
```
<type>: <description> (bd-<issue-id>)

Types: feat, fix, chore, docs, refactor, test
```

## Upstream Sync

**Upstream**: `Fcornaire/megabonk-together`

### Setup (once)
```bash
git remote add upstream https://github.com/Fcornaire/megabonk-together.git
```

### Sync Main with Upstream
```bash
git checkout main
git pull origin main
git fetch upstream
git rebase upstream/main              # Preferred: clean history
git push --force-with-lease origin main
```

### Sync Feature Branch with Upstream
```bash
git checkout feature/my-feature
git fetch upstream
git rebase upstream/main
git push --force-with-lease origin feature/my-feature
```

## PR Checklist

- [ ] Branch created from latest `main`
- [ ] All commits reference `bd-<id>`
- [ ] Branch name follows convention
- [ ] Rebased on latest `main` (no merge conflicts)
- [ ] Description links to `bd` issue(s)
- [ ] Ready for review

## Related Docs

- [AGENTS.md](../AGENTS.md) - Full bd workflow and session completion protocol
- [project-structure.md](./project-structure.md) - Codebase organization
