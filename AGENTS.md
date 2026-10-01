# AGENTS.md

`dotnet-fast-format`: a fast, `.editorconfig`-aware C# formatter CLI. It parses with Roslyn
syntax trees (no MSBuild), prints through its own doc-printer, and aims to replace
`dotnet format`'s whitespace/style pass.

**Status: pre-code.** The repo has docs and skills only. Commands marked *(planned)* do not
exist yet. Replace each with the real command when the script lands, and delete this note.

## Commands

| Task | Command |
|---|---|
| Restore + build | `dotnet build -warnaserror` *(planned)* |
| All tests | `dotnet test` *(planned)* |
| One test | `dotnet test --filter "FullyQualifiedName~<name>"` *(planned)* |
| Full gate (what CI runs) | `dotnet test` *(planned)* |

`dotnet test` is the whole gate. Golden snapshots (Verify), the invariants and the corpus run
are all tests, and `TreatWarningsAsErrors` in `Directory.Build.props` makes warnings fail the
build. There is no separate verify script. Benchmarks live in their own project and are not part
of the gate.

Run the full gate before saying a task is done. If a test was filtered out or a count looks
wrong, say so; do not report green on tests that did not run.

**Merging:** once CI exists, `main` is protected with the `dotnet test` check required, so
nothing reaches `main` with a failing rule. Until then, run the gate locally before every PR.

## Non-negotiable invariants

Formatting must never change program meaning. Every change is checked against these (see the
`formatter-verification` skill):

1. **Idempotent:** `format(format(x)) == format(x)`.
2. **Tree-preserving:** output parses to the same syntax tree, ignoring trivia.
3. **No loss:** no comment or preprocessor directive is dropped or moved across code.
4. **Fail safe:** on any error, leave the file untouched and exit non-zero.

## Do not

- Hand-edit `*.verified.*` snapshots. Review the `*.received.*` diff, then accept it
  deliberately.
- Change `global.json`, package versions, or analyzer severities to make a build pass.
- Disable, skip, or loosen a test to get green.
- Add a formatting option without an ADR (see `architecture-decision-records`). The option set
  is intentionally small.
- Change stable output style without a style-changelog entry (see `docs/` once it exists).
- Mix concerns in one PR: one rule or fix per change.

## Workflow

- Write the failing test or golden fixture first, then the code.
- Docs ship in the same change as the code (`dotnet-docs-sync`).
- Commits follow [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)
  (`feat:`, `fix:`, `docs:`, `test:`, `perf:`, `ci:`). Terms used in these docs are defined in
  [`docs/glossary.md`](docs/glossary.md).

## Skills in `.claude/skills/`

`add-formatting-rule`, `formatter-verification`, `prd-and-requirements`, `dotnet-docs-sync`,
`dotnet-xml-docs`, `mermaid-diagrams`, `architecture-decision-records`, `dotnet-repo-docs`.
