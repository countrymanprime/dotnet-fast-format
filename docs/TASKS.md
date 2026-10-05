# Tasks

Draft v0.2, 2026-10-04. Tasks are vertical slices. While there is one contributor this list is
enough; move each task to a GitHub issue when there are several (see `prd-and-requirements`).
Commands marked *(planned)* do not exist yet. M0 is done and merged.

Every milestone is gated by `dotnet test`: idempotency, tree-equivalence, no-loss, and the pinned
corpus run all pass, with no new warnings.

## M0: Foundations

Nothing formats yet. The goal is a harness where every later change is checked automatically.

| ID | Task | Requirements | Acceptance | Verify |
|---|---|---|---|---|
| T-001 | Create the solution, `global.json`, `Directory.Build.props` (nullable, warnings as errors) and `Directory.Packages.props` | none | Solution builds clean (an empty solution cannot build, so it holds the empty `Core` project) | `dotnet build -warnaserror` (done) |
| T-002 | CLI project skeleton: `dotnet fast-format --help` and exit codes | CLI-003 | Help prints; bad args exit 2 | `dotnet test -- --filter-class "*CliTests"` (done) |
| T-003 | Test project with [Verify](https://github.com/VerifyTests/Verify) and the shared invariant helper (idempotent, tree-preserving, no-loss, valid output) | FMT-001, FMT-002, FMT-003, FMT-005 | Helper fails on a deliberately broken formatter | `dotnet test -- --filter-class "*InvariantsTests"` (done) |
| T-004 | CI workflow running `dotnet test` on Windows and Linux; then require it on `main` | all | Required check on the Pull Request ruleset | PR shows the checks `checks / dotnet test (ubuntu-latest)`, `checks / dotnet test (windows-latest)` and `checks / dotnet format` (workflows done: `pull-request.yml` before merge, `main.yml` after; requiring them on `main` is a repo setting, still to do) |
| T-005 | Pin the corpus (repository URLs plus commit SHAs) and a fetch step | FMT-001, FMT-002 | Fetch is reproducible: each repo is checked out at its pinned commit, and a clean checkout is reused | `dotnet test -- --filter-class "*Corpus*"` (fast, offline) and `dotnet test -p:TestTier=slow` (network) (done) |
| T-006 | Baseline: time `dotnet format` and CSharpier on the corpus; record in an ADR; set PERF target | PERF-003 | Numbers recorded | `dotnet run --project benchmarks/DotnetFastFormat.Benchmarks -c Release` (done; [ADR 0006](decisions/0006-formatting-speed-baseline.md) accepted: the target is a ratio to CSharpier) |

## M1: first real formatting

Goal: `dotnet fast-format <folder>` formats loose `.cs` files in place for namespaces, usings, type
declarations and member signatures. Anything not yet supported, and anything carrying a comment or
directive, is copied through unchanged ([ADR 0008](decisions/0008-print-unsupported-syntax-verbatim.md)).
A file with a syntax error is left untouched and the run exits 2. Every fixture and every corpus file
passes the four invariants.

Each task is done when the build, tests and invariants pass with no new warnings, and its docs and ADRs
are updated. Fixtures are written by hand from `docs/style.md`, before the code. Suggested pull requests:
**A** = T-100 to T-102 and T-107, **B** = T-103 to T-105, **C** = T-106, T-108 and T-109. T-101 does not depend on
T-100; T-102 does.

| ID | Task | Requirements | Files | Acceptance | Verify |
|---|---|---|---|---|---|
| T-100 | Decide the open questions: accept or change ADRs [0007](decisions/0007-default-style-is-microsoft-conventions-at-100-columns.md) (default style), [0008](decisions/0008-print-unsupported-syntax-verbatim.md) (verbatim fallback) and [0009](decisions/0009-self-check-output-before-writing.md) (self-check) | none | `docs/decisions/` | The three ADRs are Accepted, or superseded by a chosen alternative | done provisionally under the author's delegation after an independent review; the author's review is pending |
| T-101 | Doc IR and printer primitives (text, concat, group, indent, line, softline, hardline, fill) and the layout to a width | none (internal) | `src/DotnetFastFormat.Core/Layout/`, `tests/DotnetFastFormat.Tests/Layout/` | Each primitive has a unit test; a group that fits stays flat and one that does not breaks outermost first; 10,000 nested groups do not overflow the stack | `dotnet test -- --filter-class "*DocPrinter*"` (done) |
| T-102 | `RoslynFormatter : IFormatter`: parse at `LanguageVersion.Preview` through one shared setting, report syntax errors, print every node verbatim; the golden tests switch from the identity formatter to it | FMT-004, FMT-005, FMT-008, FMT-009, FMT-011, FMT-012, FMT-013 | `src/DotnetFastFormat.Core/`, `tests/DotnetFastFormat.Tests/GoldenTests.cs` | `Formatter.UnsupportedNodesAreVerbatim`, `Formatter.NodesWithCommentsAreVerbatim`, `Formatter.ParseErrorIsReported`, `Formatter.FilesWithConditionalDirectivesAreVerbatim`, `Formatter.FilesWithoutDeclarationsAreVerbatim` pass; input nesting brackets more than 1,000 deep is rejected with an error (Roslyn's parser overflows the stack on deep input, and an overflow cannot be caught), and everything else runs on a 64 MB stack; invariants hold on every fixture, including `#if` files | `dotnet test -- --filter-class "*FormatterTests*"` (done) |
| T-103 | Using directives and namespaces (block and file-scoped); starts `docs/style.md` | FMT-001, FMT-002, FMT-003, FMT-013 | `src/DotnetFastFormat.Core/Printers/`, `tests/golden/namespaces/`, `docs/style.md` | Fixtures `namespaces/*` pass, including a comment before a `using` (printed verbatim) next to formatted siblings (idempotent at that boundary), a CRLF file and a file without a final newline (`Formatter.OutputEndsWithOneNewline`, `Formatter.UsesTheDominantLineEnding`) | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-104 | Type declarations: class, struct, interface and record with modifiers, type parameters and base lists; members stay verbatim until T-105 | FMT-001, FMT-002, FMT-003 | `src/DotnetFastFormat.Core/Printers/`, `tests/golden/types/` | Fixtures `types/*` pass, including a base list that must wrap at 100 columns | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-105 | Members: fields, properties, and method and constructor signatures with parameter lists wrapped to the width; bodies stay verbatim until M2; a blank line between members | FMT-001, FMT-002, FMT-003 | `src/DotnetFastFormat.Core/Printers/`, `tests/golden/members/` | Fixtures `members/*` pass, including a long parameter list and a commented member | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-106 | CLI formats files in place: file and directory arguments, atomic writes, BOM preserved, no line-ending conversion, a parse error leaves the file untouched, the verifier blocks a bad output (`Cli.SelfCheckBlocksBadOutput`) | CLI-003, CLI-004, CLI-005, CLI-006, FMT-004, FMT-007 (interim), FMT-010 | `src/DotnetFastFormat.Cli/`, `tests/DotnetFastFormat.Tests/CliTests.cs` | `Cli.DiscoversCSharpFiles`, `Cli.NoProjectNeeded`, `Cli.WritesAreAtomic`, `Cli.ParseErrorLeavesFileUntouched` pass | `dotnet test -- --filter-class "*Cli*"` (done) |
| T-107 | Output verifier: one `OutputVerifier` in Core, shared with the test helper `Invariants` (no new error diagnostics by ID, same tokens by kind and text, same comment and directive sequence, same parse options; skipped for input with errors). The CLI blocks writes with it in T-106 | FMT-010 | `src/DotnetFastFormat.Core/`, `tests/DotnetFastFormat.Tests/Invariants.cs` | `OutputVerifier` tests cover each kind of difference, and `Invariants` calls it instead of its own copy | `dotnet test -- --filter-class "*OutputVerifier*"` (done) |
| T-108 | Slow-tier test formats every file of the pinned corpus and asserts the invariants and no crashes | FMT-001, FMT-002, FMT-003, FMT-005 | `tests/DotnetFastFormat.Tests/Corpus/` | `-p:TestTier=slow` passes with 0 crashes and 100% idempotency and tree equivalence | `dotnet test -p:TestTier=slow` (done: all six repositories pass) |
| T-109 | Add the formatter to the benchmark harness; record its first row and the cost of the self-check against the ADR 0006 target | PERF-004, PERF-005 | `benchmarks/`, `docs/decisions/0006-formatting-speed-baseline.md` | A benchmark run lists the formatter with and without the self-check | `dotnet run --project benchmarks/DotnetFastFormat.Benchmarks -c Release` (done: 1.22x CSharpier; the check is 50–71% of format plus check, see ADR 0006) |

Risks: comment and trivia ownership (deferred to M3 by the verbatim fallback), stack depth in a recursive
printer (T-101 tests it), and idempotency at the boundary between formatted and verbatim regions (every
printer's fixtures include one).

## M3: comments and the preprocessor

Goal: a file with comments and `#if` blocks is formatted instead of copied: own-line comments, end-of-line
comments, comments before `}` and at the end of a file, and directives are kept in place; every other position
copies the node that owns it ([ADR 0010](decisions/0010-keep-comments-at-node-boundaries.md),
[ADR 0011](decisions/0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md)). The four invariants hold
on every fixture, on comments inserted at token boundaries of this repository's files, and on the corpus.

One pull request, one commit per task.

| ID | Task | Requirements | Files | Acceptance | Verify |
|---|---|---|---|---|---|
| T-300 | Decide the trivia model and the preprocessor strategy: ADRs 0010 and 0011 | FMT-009, FMT-011 | `docs/decisions/` | Both ADRs Accepted (provisional) | done under delegation after an independent review |
| T-301 | A document that prints its contents from column 0 (for directives and disabled text) | none (internal) | `src/DotnetFastFormat.Core/Layout/`, `tests/DotnetFastFormat.Tests/Layout/` | A directive inside an indented group starts at column 0 and the next line returns to its indent; trailing spaces before it are removed | `dotnet test -- --filter-class "*DocPrinter*"` (done) |
| T-302 | Trivia classifier: parse a token's leading trivia into own-line items with blank-line counts, and its trailing trivia into at most one comment; report unclean shapes | FMT-014, FMT-015 | `src/DotnetFastFormat.Core/Printers/`, `tests/DotnetFastFormat.Tests/Printers/` | Unit tests for every trivia shape, including `/* */` followed by code, two comments after one token, `///`, `/** */` and disabled text | `dotnet test -- --filter-class "*Trivia*"` (done) |
| T-303 | Print leading lines and trailing comments for list items; verbatim children never repeat them | FMT-014, FMT-015, FMT-018 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `comments/leading-*`, `comments/trailing-*`, `comments/text-*` pass | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-304 | Closing lines: comments and directives before `}` and at the end of the file; empty bodies with comments | FMT-016 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `comments/closing-*` pass | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-305 | Preprocessor: remove the whole-file rule for conditional directives, print disabled text from column 0, indent `#region` with the code | FMT-011, FMT-012, FMT-017 | `src/DotnetFastFormat.Core/` | Fixtures `directives/*` pass, including a `#if` that splits a header (verbatim node) and nested `#if` | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-306 | Mutation test: insert comments and directives at token boundaries of this repository's files, format, check every invariant | FMT-001, FMT-002, FMT-003 | `tests/DotnetFastFormat.Tests/` | Fixed-seed run passes, with no crash and no invariant violation | `dotnet test -- --filter-class "*CommentMutationTests*"` (done; the same mutations run over the corpus in the slow tier) |
| T-307 | Corpus run and docs: `docs/style.md`, `docs/style-changelog.md`, architecture and AGENTS.md status | FMT-001 to FMT-003 | `docs/`, `AGENTS.md` | `-p:TestTier=slow` passes; the style changelog lists the changed output | `dotnet test -p:TestTier=slow` |

Risks: a trivia shape the classifier wrongly calls clean (the output check blocks the write, but the file is then
unformatted and exits 2); hidden dependence on the no-symbols parse; `#line` after reflow.

## Later milestones

| Milestone | Scope |
|---|---|
| M2 | Statements, expressions, string literals |
| M3 | Comments and trivia; preprocessor strategy decided (ADR): planned above |
| M4 | `.editorconfig` resolution and the first supported keys; BOM and line endings |
| M5 | Parallelism, cache, benchmarks and the performance gate |
| M6 | `--check`, `--stdin`, packaging as a .NET tool |
| After | Stretch: the parts of `dotnet format`'s style pass that need no semantic model |

Each milestone gets its tasks written when the previous one finishes, so the plan stays
accurate.
