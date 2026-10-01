---
name: prd-and-requirements
description: Write or revise the product requirements for a developer tool or library, as one short product PRD plus numbered per-feature specs, atomic testable requirements with IDs, and tasks (GitHub issues) as vertical slices with verification commands. Use when starting a project or milestone, when asked for a PRD, spec, requirements or task breakdown, when adding a new capability that needs its own spec, or when a feature request is vague and needs turning into something an agent can build and verify.
---

# PRD, requirements and tasks

Three small artifacts, each with one job. The tests, not the prose, are what make the result
deterministic, so every requirement points at a check.

| Artifact | File | Job |
|---|---|---|
| Product PRD (one) | `docs/PRD.md` | Why the product exists: problem, scope, metrics, risks. 3 to 5 pages. Changes rarely. |
| Feature specs (many) | `docs/specs/NNNN-<feature>/spec.md` | One capability each, with its own requirements. Added over time as deltas. |
| Requirements | inside the PRD or a spec, or `docs/requirements/<area>.md` | Atomic, testable statements with IDs. |
| Tasks | GitHub issues (or `docs/TASKS.md` while the project is small) | Ordered vertical slices an agent can pick up. |

Decisions go in `docs/adr/` (`architecture-decision-records`), not in the PRD or specs.

## One PRD, many specs

- **Edit `docs/PRD.md`** when product scope, non-goals, success metrics or the CLI contract
  change.
- **Add a spec** for any new capability (for example `.editorconfig` support or preprocessor
  handling). Number it (`0001`, `0002`, ...), never renumber, and mark a replaced one
  `Superseded by NNNN`.
- A spec uses the PRD outline below, cut down: problem, non-goals, requirements, risks, test plan,
  open questions. It links to the PRD instead of restating it.
- Keep the PRD and specs in the repo, not in issue bodies. They are reviewed in PRs and versioned
  with the code; issue bodies can be edited with no review and drift.
- Each spec links its tracking issue, and the issue links back to the spec.

## PRD outline

Omit a section with no real content. Cut market sizing, personas, RACI and long roadmaps.

1. **Summary.** One paragraph: what it is, who it is for, why it beats the alternatives.
2. **Problem and gap.** The concrete user pain, and what existing tools (`dotnet format`,
   CSharpier) do not do.
3. **Appetite and non-goals.** A time box, and an explicit list of what is out (for example: no
   linting, no import sorting, no semantic rewrites).
4. **Success metrics.** Numbers with a named corpus: wall-clock time, 100% idempotency, 100%
   tree-equivalence, crash count.
5. **Scenarios.** CLI, CI `--check`, editor or pre-commit use.
6. **Formatting principles.** What the formatter cares about and what it deliberately does not
   (correctness first, print rather than transform, preserve author line breaks where chosen).
7. **Configuration surface.** Every option justified; fewer is better.
8. **CLI contract.** Commands, flags, exit codes, stdout vs stderr.
9. **Risks and rabbit holes.** Comment attachment, preprocessor directives, trivia, line endings.
10. **Prior art.** What was studied and what is copied or rejected.
11. **Open questions.** Mark each `UNRESOLVED` until it becomes an ADR.
12. **Milestones and definition of done.**

## Requirements

One requirement per row, written in EARS form:

| Pattern | Template |
|---|---|
| Always true | The formatter shall `<behavior>`. |
| Event | When `<trigger>`, the formatter shall `<behavior>`. |
| State | While `<state>`, the formatter shall `<behavior>`. |
| Optional | Where `<feature/option>` is set, the formatter shall `<behavior>`. |
| Unwanted | If `<condition>`, then the formatter shall `<behavior>`. |

Row format: `ID | EARS statement | Priority (Must/Should/Could/Won't) | Scenario or golden file | Test ID or command`.

Rules:
- IDs are stable and never reused. Prefix them by area: `FMT-` formatting output, `CFG-`
  configuration and `.editorconfig`, `CLI-` commands and exit codes, `PERF-` speed. A removed
  requirement is marked withdrawn, not deleted. IDs stay valid across the PRD and all specs.
- A requirement with no test anchor is a draft, not a requirement.
- Prefer a golden fixture (`tests/golden/<area>/<name>.in.cs` and `.out.cs`) over a prose
  scenario. For a formatter, the fixture is the spec.
- No "and" in a statement; split it.
- Use measurable words. Replace "fast" and "readable" with a number or a fixture.

## Tasks

Tasks are vertical slices (input file in, formatted file out, exit code), not layers. While the
project has one contributor, a `docs/TASKS.md` list is enough. Once there are several
contributors or parallel work, move each task to a GitHub issue so it gets status, assignment
and PR linking. Do not keep both; if you migrate, replace `docs/TASKS.md` with a link to the
issue list. Each task has:

- **ID and title**
- **Requirements:** the IDs it satisfies
- **Files:** where the change is expected to land
- **Acceptance:** the named test or fixture that must pass
- **Verify:** the exact command, for example `dotnet test --filter "FullyQualifiedName~FMT001"`
- **Done when:** build, tests and the invariants pass, no new warnings, docs and ADR updated

Keep tasks small enough to finish in one session. If a task needs a new decision, write the ADR
first.

### Task as a GitHub issue

Use this body so an agent can pick the issue up with `gh issue view <n>`:

```markdown
**Spec:** docs/specs/0002-editorconfig/spec.md
**Requirements:** CFG-001, CFG-002
**Files:** src/.../EditorConfigReader.cs, tests/golden/config/
**Acceptance:** golden fixtures `config/indent-size.*` pass
**Verify:** `dotnet test --filter "FullyQualifiedName~CFG001"`
**Done when:** build, tests and invariants pass; no new warnings; docs and ADR updated
```

Create one with `gh issue create --title "<task>" --body-file <file>` only after the user
approves publishing it; issues in a public repo are visible to everyone.

## Milestone gates

Gate every milestone on the formatter invariants (see `formatter-verification`): idempotent,
tree-preserving, and no crash on the pinned corpus. A suggested order: scaffold and test harness,
minimal syntax subset, statements and expressions, comments and trivia, `.editorconfig`,
parallelism and benchmarks, `--check`/`--stdin` and CI.

## Review checklist

- [ ] Every requirement has an ID, a priority and a test anchor
- [ ] Non-goals are written down
- [ ] Success metrics are numbers on a named corpus
- [ ] Open questions are marked, with an owner or a plan to resolve them
- [ ] The PRD does not restate the design; link to it
- [ ] New capabilities get a numbered spec instead of growing the PRD
- [ ] Each spec and its tracking issue link to each other
