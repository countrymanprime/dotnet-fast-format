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

## M2: statements and expressions

Goal: a method body is formatted. Blocks, statements, expressions and string literals have printers, so the
statement lists inside methods, constructors, lambdas and top-level code are laid out to the width instead of being
copied. Statement lists reuse the list machinery and the trivia model of M3
([ADR 0010](decisions/0010-keep-comments-at-node-boundaries.md)): own-line comments and directives above a
statement, a trailing comment after it, closing lines before `}`. Anything without a printer is copied per statement or
per expression, never per file ([ADR 0008](decisions/0008-print-unsupported-syntax-verbatim.md),
[ADR 0014](decisions/0014-statements-and-expressions-reuse-the-trivia-model.md)). Layout rules are in
[ADR 0013](decisions/0013-lay-out-statements-and-expressions-with-groups.md) and `docs/style.md`. Tokens are never
added or removed, so braces, parentheses and commas stay as written. The four invariants hold on every fixture, on
comments inserted into this repository's files, and on the corpus, and the formatter stays within 1.5 times
CSharpier's time ([ADR 0006](decisions/0006-formatting-speed-baseline.md)).

One branch, one commit per task. Each fixture is written from `docs/style.md`, the received output is reviewed
against the rule, and only then accepted.

| ID | Task | Requirements | Files | Acceptance | Verify |
|---|---|---|---|---|---|
| T-200 | Decide the layout and the trivia ownership of statements and expressions: ADRs 0013 and 0014; add the M2 requirements | FMT-006, FMT-020 to FMT-030 | `docs/decisions/`, `docs/requirements/core.md` | Both ADRs Accepted (provisional) | done under delegation; the author's review is pending |
| T-201 | Doc printer: a group that always breaks, and a conditional group that tries a list of layouts in order (for "hug the last argument") | none (internal) | `src/DotnetFastFormat.Core/Layout/`, `tests/DotnetFastFormat.Tests/Layout/` | A conditional group picks the first layout that fits (a layout with a forced break fits when the text before the break fits) and falls back to the last one broken; 10,000 nested ones do not overflow the stack | `dotnet test -- --filter-class "*DocPrinter*"` (done) |
| T-202 | Statement lists: block, expression statement, local declaration, `return`, `throw`, `yield`, `break`, `continue`, empty statement; bodies of methods, constructors and top-level statements use them; expressions still copied | FMT-020, FMT-029 | `src/DotnetFastFormat.Core/Printers/`, `tests/golden/statements/` | Fixtures `statements/blocks`, `statements/simple`, `statements/comments` pass, including a comment above, after and before `}` of a statement, a comment inside a statement (that statement copied, its neighbours formatted) and `#if` around statements | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-203 | Invocations, member access, element access, argument lists that wrap at the width, member chains, hugging a last-argument lambda | FMT-024, FMT-025, FMT-026 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `expressions/invocations`, `expressions/chains`, `expressions/lambda-arguments` pass | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-204 | Object, array and collection creation, initializers, anonymous objects, tuples, `with`; the trailing comma that forces a break | FMT-027, FMT-030 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `expressions/creation`, `expressions/initializers` pass | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-205 | Binary and logical, conditional, assignment, unary, cast, `await`, parenthesized, `typeof`, `default`, `throw` expressions, lambdas; assignment layout for declarators, field and property initializers and expression bodies | FMT-024, FMT-028 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `expressions/operators`, `expressions/assignments`, `expressions/lambdas` pass | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-206 | Embedded statements (a body that is not a block, and the last token it shares with its parent), `if` with `else` and `else if` chains, `for`, `foreach`, `while`, `do` | FMT-021, FMT-022 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `statements/if`, `statements/loops` pass, including comments between `}` and `else` (the `if` copied) | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-207 | `using`, `lock`, `try`/`catch`/`finally`, `switch` statements | FMT-023 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `statements/using-lock`, `statements/try`, `statements/switch` pass | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-208 | String literals: regular, verbatim, interpolated and raw are never reflowed, in every position | FMT-006 | `src/DotnetFastFormat.Core/Printers/`, `tests/golden/strings/` | Fixtures `strings/*` pass: a multi-line verbatim and raw string as an argument, an initializer and a return value, with and without width pressure | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-209 | Members use the new printers: attributes on members, expression bodies, initializer values, constructor initializers, local functions | FMT-020, FMT-024 | `src/DotnetFastFormat.Core/Printers/` | Fixtures `members/*` updated; `members/attributes`, `statements/local-functions` pass | `dotnet test -- --filter-class "*Golden*"` (done) |
| T-211 | Copy less: attribute lists and parameter attributes and defaults, directives and comments around attribute lists and opening braces, enums, delegates, events, indexers, operators and destructors; measure the share of the corpus still copied as written | FMT-020, FMT-029 | `src/DotnetFastFormat.Core/Printers/`, `tests/DotnetFastFormat.Tests/Corpus/` | Fixtures `members/attributes*`, `members/other-members` pass; `CorpusVerbatimShareTests` writes `.bench/verbatim-share.md` and guards against a collapse | `dotnet test -- --filter-class "*Golden*"` and `dotnet test -p:TestTier=slow -- --filter-class "*CorpusVerbatimShare*"` (done) |
| T-212 | Verification: the repository and mutation tests, the corpus run, a verbatim counter, the benchmark row, docs (`style.md`, style changelog, architecture, AGENTS.md status) | FMT-001 to FMT-003, PERF-004 | `docs/`, `tests/` | `-p:TestTier=slow` passes with no crash and 100% idempotency and tree equivalence; the ratio is recorded in ADR 0006 | `dotnet test -p:TestTier=slow` and the benchmark command (done: corpus format and mutation tests pass on all six repositories; 0.58 times CSharpier, see ADR 0006; 2.4 percent of the corpus is still copied code) |

Risks: a comment position inside a statement that makes a large statement fall back (the verbatim share in the
corpus is measured in T-212); the output check blocking a file because a printer dropped or moved a token; formatting
cost growing past the 1.5 times target; layouts that differ from other tools, which is a style choice and not a bug.

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
| T-307 | Corpus run and docs: `docs/style.md`, `docs/style-changelog.md`, architecture and AGENTS.md status | FMT-001 to FMT-003 | `docs/`, `AGENTS.md` | `-p:TestTier=slow` passes; the style changelog lists the changed output | `dotnet test -p:TestTier=slow` (done: corpus format and corpus mutation tests pass on all six repositories) |

Risks: a trivia shape the classifier wrongly calls clean (the output check blocks the write, but the file is then
unformatted and exits 2); hidden dependence on the no-symbols parse; `#line` after reflow.

## M4: `.editorconfig`

Goal: `dotnet fast-format` reads the `.editorconfig` files that apply to each file (nearest first, stopping at
`root = true`, with no MSBuild) and applies the keys `indent_style`, `indent_size`, `tab_width`,
`max_line_length`, `end_of_line`, `insert_final_newline` and `charset`. Defaults with no key stay as in
[ADR 0007](decisions/0007-default-style-is-microsoft-conventions-at-100-columns.md). The set of keys, how invalid
values and tabs behave, and the choice of a hand-written parser and matcher are decided in
[ADR 0012](decisions/0012-read-editorconfig-with-a-small-parser-in-core.md), written first. The four invariants
hold under every supported configuration, on every fixture and on the corpus, including the corpus files'
own `.editorconfig` files.

One pull request, one commit per task. T-401 and T-402 do not need the parser; T-403 to T-405 do not touch the
printers.

| ID | Task | Requirements | Files | Acceptance | Verify |
|---|---|---|---|---|---|
| T-400 | Decide the supported keys, invalid values, tab handling, `insert_final_newline = false`, `end_of_line` and `charset` semantics, and the no-dependency parser: ADR 0012 | CFG-001, CFG-006 to CFG-011, FMT-007, FMT-013 | `docs/decisions/` | ADR 0012 Accepted (provisional); the index lists it | review |
| T-401 | Doc printer: tab indentation (`UseTabs`, `TabWidth`); a tab counts as `TabWidth` columns in `Fits`, in the position after a break and when trailing indentation is trimmed | CFG-001 | `src/DotnetFastFormat.Core/Layout/`, `tests/DotnetFastFormat.Tests/Layout/` | A line of exactly the width, indented with tabs, fits and one more column breaks; a column-zero directive after a tab-indented line has the right position; `indent_size` 4 with `tab_width` 3 gives a tab and a space | `dotnet test -- --filter-class "*DocPrinter*"` |
| T-402 | `FormatOptions` (public, validated) and `IFormatter.Format(string, FormatOptions)`; `RoslynFormatter` applies indent style and size, width, `end_of_line` (including text inside verbatim nodes and trivia, never inside a token) and `insert_final_newline`; `Format(string)` uses the defaults; `Invariants` takes options; a matrix test runs every golden input under each supported setting | CFG-001, CFG-006 to CFG-008, CFG-013, FMT-013 | `src/DotnetFastFormat.Core/`, `tests/DotnetFastFormat.Tests/` | `FormatterOptionsTests` and `OptionMatrixTests` pass: tabs, two spaces, width 60 and 120, CRLF and LF output for mixed input, no final newline, and a multi-line string literal untouched by `end_of_line` | `dotnet test -- --filter-class "*FormatterOptions*"` and `--filter-class "*OptionMatrix*"` |
| T-403 | `.editorconfig` file parser: lines, sections, preamble `root`, pairs, case rules, no inline comments, `unset`; never throws | CFG-003, CFG-005 | `src/DotnetFastFormat.Core/Config/`, `tests/DotnetFastFormat.Tests/Config/` | `EditorConfigParserTests` pass: every line type in the specification, a BOM, CRLF, `=` in a value, empty values, a header with `]` inside, garbage lines ignored | `dotnet test -- --filter-class "*EditorConfigParser*"` |
| T-404 | Glob matcher: `*`, `**`, `?`, `[seq]`, `[!seq]`, `{a,b}` (nested), `{n..m}`, escapes, slash rule (relative to the `.editorconfig`'s directory, or any level when there is no slash) | CFG-004 | `src/DotnetFastFormat.Core/Config/`, `tests/DotnetFastFormat.Tests/Config/` | `GlobTests` pass with a table built from the examples in the specification and the editorconfig core tests; a pathological pattern finishes in bounded time | `dotnet test -- --filter-class "*Glob*"` |
| T-405 | Resolver: walk up from a file, nearest first, stop at `root = true` (or a ceiling); merge sections in order, nearer files last; `unset`; map keys to `FormatOptions`; invalid values warn and are ignored; unreadable files warn and are skipped; read and parse each file once per run; golden fixtures format one input under two configs | CFG-001 to CFG-006, CFG-010 to CFG-012 | `src/DotnetFastFormat.Core/Config/`, `tests/DotnetFastFormat.Tests/Config/`, `tests/DotnetFastFormat.Tests/golden/config/` | `EditorConfigResolverTests.ResolutionOrder`, `StopsAtRoot`, `UnsetRemovesAValue`, `InvalidValuesWarn`, `EachFileIsReadOnce` pass; fixtures `config/*` pass | `dotnet test -- --filter-class "*EditorConfigResolver*"` and `--filter-class "*ConfigGolden*"` |
| T-406 | CLI: resolve options per file, print each warning once on stderr, apply `charset` (add, remove or keep the BOM), update the help text | CFG-001 to CFG-012, FMT-007, CLI-003 | `src/DotnetFastFormat.Cli/`, `tests/DotnetFastFormat.Tests/CliConfigTests.cs` | `CliConfigTests` pass with temp directories holding nested `.editorconfig` files and `root = true`, a BOM added and removed, CRLF and LF output, an unreadable and an invalid `.editorconfig` (warning, exit code 0) | `dotnet test -- --filter-class "*CliConfig*"` |
| T-407 | Corpus under the files' own `.editorconfig` files (slow tier); docs: `docs/style.md`, `docs/style-changelog.md`, PRD, README, architecture, AGENTS.md status | CFG-001, CFG-002, CFG-013, FMT-001 to FMT-003 | `tests/DotnetFastFormat.Tests/Corpus/`, `docs/` | `-p:TestTier=slow` passes with 0 crashes and 100% idempotency and tree equivalence, with the default options and with each file's resolved options | `DOTNET_FAST_FORMAT_CORPUS_DIR=... dotnet test -p:TestTier=slow` |

Risks: a configuration that changes line endings or the final newline interacts with verbatim text (the
`end_of_line` rule keeps tokens untouched, and the output check proves it); a pattern that backtracks without
bound (T-404 bounds it); `indent_size` that does not match the indentation inside verbatim bodies (they keep the
indentation they had until M2 prints statements); real `.editorconfig` files that use constructs the parser has not
seen (T-407 runs them).

## Later milestones

| Milestone | Scope |
|---|---|
| M2 | Statements, expressions, string literals: done, see above |
| M3 | Comments and trivia; preprocessor strategy decided (ADR): planned above |
| M4 | `.editorconfig` resolution and the first supported keys; BOM and line endings: planned above |
| M5 | Parallelism, cache, benchmarks and the performance gate |
| M6 | `--check`, `--stdin`, packaging as a .NET tool |
| After | Stretch: the parts of `dotnet format`'s style pass that need no semantic model |

Each milestone gets its tasks written when the previous one finishes, so the plan stays
accurate.
