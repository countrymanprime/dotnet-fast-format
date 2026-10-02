# Tasks

Draft v0.1, 2026-10-01. Tasks are vertical slices. While there is one contributor this list is
enough; move each task to a GitHub issue when there are several (see `prd-and-requirements`).
Commands marked *(planned)* do not exist yet.

Every milestone is gated by `dotnet test`: idempotency, tree-equivalence, no-loss, and the pinned
corpus run all pass, with no new warnings.

## M0: Foundations

Nothing formats yet. The goal is a harness where every later change is checked automatically.

| ID | Task | Requirements | Acceptance | Verify |
|---|---|---|---|---|
| T-001 | Create the solution, `global.json`, `Directory.Build.props` (nullable, warnings as errors) and `Directory.Packages.props` | none | Solution builds clean (an empty solution cannot build, so it holds the empty `Core` project) | `dotnet build -warnaserror` (done) |
| T-002 | CLI project skeleton: `dotnet fast-format --help` and exit codes | CLI-003 | Help prints; bad args exit 2 | `dotnet test --filter "FullyQualifiedName~Cli"` *(planned)* |
| T-003 | Test project with [Verify](https://github.com/VerifyTests/Verify) and the shared invariant helper (idempotent, tree-preserving, no-loss, valid output) | FMT-001, FMT-002, FMT-003, FMT-005 | Helper fails on a deliberately broken formatter | `dotnet test --filter "FullyQualifiedName~Invariants"` (done) |
| T-004 | CI workflow running `dotnet test` on Windows and Linux; then require it on `main` | all | Required check on the Pull Request ruleset | PR shows the check *(planned)* |
| T-005 | Pin the corpus (repository URLs plus commit SHAs) and a fetch step | FMT-001, FMT-002 | Fetch is reproducible | corpus test *(planned)* |
| T-006 | Baseline: time `dotnet format` and CSharpier on the corpus; record in an ADR; set PERF target | PERF-003 | Numbers recorded | benchmark run *(planned)* |

## Later milestones

| Milestone | Scope |
|---|---|
| M1 | Roslyn parse, doc IR and printer for a minimal subset (namespaces, classes, members); parse-error handling |
| M2 | Statements, expressions, string literals |
| M3 | Comments and trivia; preprocessor strategy decided (ADR) |
| M4 | `.editorconfig` resolution and the first supported keys; BOM and line endings |
| M5 | Parallelism, cache, benchmarks and the performance gate |
| M6 | `--check`, `--stdin`, packaging as a .NET tool |
| After | Stretch: the parts of `dotnet format`'s style pass that need no semantic model |

Each milestone gets its tasks written when the previous one finishes, so the plan stays
accurate.
