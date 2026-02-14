# Documentation Consolidation Notice

Several root-level documentation files have been consolidated into `.agent-docs/` for better AI agent consumption.

## Consolidation Summary

| Original File | Status | Content Now In |
|---------------|--------|----------------|
| `TECHNICAL_NOTES.md` | Consolidated | `.agent-docs/architecture.md`, `.agent-docs/implementation-notes.md` |
| `SYNC_ANALYSIS.md` | Consolidated | `.agent-docs/implementation-notes.md` |
| `OPTIMIZATION_PLAN.md` | Consolidated | `.agent-docs/implementation-notes.md` |
| `PICKUP_CHEST_SYNC_PLAN.md` | Implemented | `.agent-docs/game-sync-systems.md` |
| `LINUX_PORTING_NOTES.md` | Historical | `.agent-docs/build-run-instructions.md` |
| `PROTON_SETUP.md` | Consolidated | `.agent-docs/build-run-instructions.md` |
| `DEVELOPMENT_WORKFLOW.md` | Consolidated | `.agent-docs/build-run-instructions.md` |
| `THUNDERSTORE_BUILD.md` | Consolidated | `.agent-docs/build-run-instructions.md` |

## What's Different in .agent-docs/

The `.agent-docs/` directory is optimized for AI agents:

- **Token-efficient**: Bullet points, tables over prose
- **Single-topic files**: One concept per file
- **Searchable**: Clear section headers
- **Machine-parseable**: Consistent format
- **Code references**: File paths and line numbers

## Files Kept in Root

| File | Reason |
|------|--------|
| `README.md` | User-facing documentation (GitHub landing page) |
| `NETPLAY_CHANGES.md` | User-facing gameplay info |
| `IMPLEMENTATION_ROADMAP.md` | Active task tracking |
| `AGENTS.md` | AI agent workflow (beads) |

## New .agent-docs/ Files

| File | Purpose |
|------|---------|
| `message-types.md` | Network message reference |
| `manager-services.md` | Service layer reference |
| `testing.md` | Testing approach and checklists |

## Quick Start for Agents

```
.agent-docs/
├── README.md              # Start here - index to all docs
├── architecture.md        # System overview
├── build-run-instructions.md  # Build and run
├── networking.md          # Network protocol
├── codebase-reference.md  # File reference
├── game-sync-systems.md   # Sync systems
├── bepinex-harmony-modding.md  # Patching reference
├── implementation-notes.md # Issues and optimizations
├── server-deployment.md   # Server setup
├── message-types.md       # Message reference
├── manager-services.md    # Service reference
└── testing.md             # Testing guide
```

Start with `.agent-docs/README.md` for navigation.
