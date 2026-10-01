---
name: prd-and-requirements
description: Write or revise the product requirements for a developer tool or library, as a short PRD plus atomic, testable requirements with IDs, and a task list of vertical slices with verification commands. Use when starting a project or milestone, when asked for a PRD, spec, requirements or task breakdown, or when a feature request is vague and needs turning into something an agent can build and verify.
---

# PRD, requirements and tasks

Three small artifacts, each with one job. The tests, not the prose, are what make the result
deterministic, so every requirement points at a check.

| Artifact | File | Job |
|---|---|---|
| PRD | `docs/PRD.md` | Intent: problem, scope, metrics, risks. 3 to 5 pages. |
| Requirements | `docs/requirements/<area>.md` | Atomic, testable statements with IDs. |
| Tasks | `docs/TASKS.md` | Ordered vertical slices an agent can pick up. |

Decisions go in `docs/adr/` (`architecture-decision-records`), not in the PRD.

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
- IDs are stable (`FMT-001`) and never reused. A removed requirement is marked withdrawn.
- A requirement with no test anchor is a draft, not a requirement.
- Prefer a golden fixture (`tests/golden/<area>/<name>.in.cs` and `.out.cs`) over a prose
  scenario. For a formatter, the fixture is the spec.
- No "and" in a statement; split it.
- Use measurable words. Replace "fast" and "readable" with a number or a fixture.

## Tasks

`docs/TASKS.md` lists vertical slices (input file in, formatted file out, exit code), not
layers. Each task has:

- **ID and title**
- **Requirements:** the IDs it satisfies
- **Files:** where the change is expected to land
- **Acceptance:** the named test or fixture that must pass
- **Verify:** the exact command, for example `dotnet test --filter "FullyQualifiedName~FMT001"`
- **Done when:** build, tests and the invariants pass, no new warnings, docs and ADR updated

Keep tasks small enough to finish in one session. If a task needs a new decision, write the ADR
first.

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
