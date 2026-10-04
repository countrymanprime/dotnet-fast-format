# AGENTS.md

`dotnet-fast-format`: a fast, `.editorconfig`-aware C# formatter CLI. It parses with Roslyn
syntax trees (no MSBuild), prints through its own doc-printer, and aims to replace
`dotnet format`'s whitespace/style pass.

**Status: foundations (M0).** The solution, test harness and invariant helper exist; there is no
formatter yet (the tests use an identity formatter) and the CLI is a skeleton: `--help` and
`--version` work, and any other run exits 2 with "not implemented".

## Commands

| Task | Command |
|---|---|
| Restore + build | `dotnet build -warnaserror` |
| All tests (fast tier) | `dotnet test` |
| One test | `dotnet test -- --filter-method "*<name>*"` (or `--filter-class`) |
| Slow tier (network, corpus) | `dotnet test -p:TestTier=slow` |
| Every test | `dotnet test -p:TestTier=all` |
| Full gate (what CI runs on a PR) | `dotnet test` |
| Benchmarks (not in the gate) | `dotnet run --project benchmarks/DotnetFastFormat.Benchmarks -c Release -- --output .bench/results` |
| Style check (second CI job) | `dotnet format DotnetFastFormat.slnx --verify-no-changes` |
| Fix style | `dotnet format DotnetFastFormat.slnx` |

`dotnet test` is the whole gate. Golden snapshots (Verify), the invariants and the corpus run
are all tests, and `TreatWarningsAsErrors` in `Directory.Build.props` makes warnings fail the
build, including the StyleCop and Meziantou analyzers ([ADR 0004](docs/decisions/0004-enforce-style-with-analyzers-and-dotnet-format.md)). There is no separate verify script. Benchmarks live in their own project and are not part
of the gate.

Tests marked `Slow` run in the scheduled `Slow tests` workflow, not in the PR gate
([ADR 0005](docs/decisions/0005-tier-tests-with-a-slow-trait.md)). VSTest-style `--filter "..."` is
not accepted by this runner together with the tier; use the native filters above. Run
`-p:TestTier=all` when you touch the corpus or fetch step.

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
