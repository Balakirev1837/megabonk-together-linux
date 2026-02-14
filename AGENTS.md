# Agent Instructions

This project uses **bd** (beads) for issue tracking. Run `bd onboard` to get started.

## Why bd Matters

**bd is critical for maintaining work continuity across sessions.** Every session should:

1. **Start with `bd ready`** - Find what's available to work on
2. **Track work in bd** - Create issues for discovered work, update status as you progress
3. **End with `bd sync`** - Sync beads state with git so the next session has context

**Without bd sync, your work context is lost.** The next agent won't know what you discovered, what's blocked, or what's in progress.

## Issue Management
- **Agent Ownership:** Agents are responsible for creating, updating, and closing issues using `bd`. 
- **Human Role:** The human user does not manage the beads database or run `bd` commands; these are strictly tools for AI agents to maintain continuity and plan work across sessions.
- **Planning:** Always use `bd create` to break down large tasks into specific, actionable steps as part of the initial planning phase.

## Quick Reference

```bash
bd ready              # Find available work
bd show <id>          # View issue details
bd update <id> --status in_progress  # Claim work
bd close <id>         # Complete work
bd sync               # Sync with git
bd blocked            # Show blocked issues
bd dep add <from> <to>  # Add dependency
```

## When to Use bd sync

- **End of every session** - Ensures beads state is committed with git
- **After creating issues** - Pushes new issues to remote
- **After closing issues** - Records completed work
- **Before context loss** - If you think context might be compacted/cleared

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds
3. **Update issue status** - Close finished work, update in-progress items
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

## Testing

**Test documentation:** `.agent-docs/testing.md`

**Quick commands:**
```bash
dotnet test src/tests                                    # Run all 145+ tests
dotnet test src/tests --filter "MessageParsing"          # Serialization tests
dotnet test src/tests --filter "ProtocolCoverage"        # Union coverage tests
```

**When to run tests:**
- Before every commit with code changes
- After adding/modifying message types or models
- After changing quantization logic

