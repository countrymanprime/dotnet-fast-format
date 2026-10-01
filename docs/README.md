# Documentation

| Document | What it answers |
|---|---|
| [PRD.md](PRD.md) | What are we building, for whom, and what is out of scope? |
| [requirements/core.md](requirements/core.md) | Which behaviors must hold, and which test proves each? |
| [TASKS.md](TASKS.md) | What gets built, in what order? |
| [architecture.md](architecture.md) | How will it be structured? (planned design) |
| [decisions/](decisions/README.md) | Why was each major choice made? |

## How the documents relate

- The **PRD** is the single living product document. It changes rarely.
- New capabilities get a numbered spec in `docs/specs/` (created when the first one is needed)
  instead of growing the PRD. See the `prd-and-requirements` skill.
- **Requirements** carry stable IDs (`FMT-`, `CFG-`, `CLI-`, `PERF-`). Every requirement points at a
  test. A requirement with no test is a draft.
- **Decisions** are append-only ADRs. A changed decision gets a new ADR that supersedes the old one.
- Docs change in the same pull request as the code that makes them untrue.

All documents are currently drafts written before any code exists. Where a fact is not yet known,
the document says so under *Open questions*.
