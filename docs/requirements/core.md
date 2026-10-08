# Core requirements

Draft v0.1, 2026-10-01. Written in [EARS](https://alistairmavin.com/ears/)
form (sentence templates such as "When X, the formatter shall Y"). **No tests exist yet**, so every test anchor below is
planned; a requirement stays a draft until its anchor is a real, passing test.

Priority: Must / Should / Could. IDs are stable and never reused.

## Formatting (FMT)

| ID | Requirement | Priority | Scenario | Test anchor (planned) |
|---|---|---|---|---|
| FMT-001 | The formatter shall produce identical text when run on its own output. | Must | Format a file twice; compare. | `Invariants.Idempotent`, every fixture and corpus file (M0) |
| FMT-002 | The formatter shall produce output whose syntax tree equals the input's, ignoring trivia. | Must | Parse both; compare tokens. | `Invariants.TreePreserving` (M0) |
| FMT-003 | The formatter shall keep every comment and preprocessor directive, in order. | Must | Count and compare before and after. | `Invariants.NoLoss` (M0) |
| FMT-004 | If a file fails to parse, then the formatter shall leave it unchanged and report an error. | Must | Format a file with a syntax error. | `CliFormatTests.ParseErrorLeavesFileUntouched` (M1) |
| FMT-005 | The formatter shall produce output that parses with no new diagnostics. | Must | Parse output; compare diagnostics. | `Invariants.ValidOutput` (M0) |
| FMT-006 | The formatter shall not reflow the contents of verbatim, interpolated or raw string literals. | Must | Fixtures for each literal kind. | `tests/golden/strings/*` (M2) |
| FMT-007 | The formatter shall preserve a file's BOM and its line-ending style unless `.editorconfig` sets `end_of_line`. | Should | Fixtures with BOM and CRLF/LF. | `tests/golden/encoding/*` (M4; M1 already preserves both, see T-106) |
| FMT-008 | When a syntax node kind has no printer yet, the formatter shall emit that node's source text unchanged. | Must | A file mixing a supported declaration with an unsupported statement. | `Formatter.UnsupportedNodesAreVerbatim` (M1; [ADR 0008](../decisions/0008-print-unsupported-syntax-verbatim.md)) |
| FMT-009 | If a comment or preprocessor directive is in a position that [ADR 0010](../decisions/0010-keep-comments-at-node-boundaries.md) does not format (inside a header or parameter list, after a `{` on its line, several comments after one token, a comment followed by code on its line), then the formatter shall emit the text of the node that owns it unchanged, the trivia on its own tokens included. | Must | A member with a comment inside its parameter list, next to formatted siblings. | `Formatter.NodesWithCommentsInOtherPositionsAreVerbatim` (M3; replaces the M1 rule) |
| FMT-010 | Before writing a file, the formatter shall re-parse its output and, if the tokens, comments or directives differ from the input's or the output has new syntax errors, leave the file unchanged and exit 2. | Should | A deliberately broken formatter. | `CliFormatTests.SelfCheckBlocksBadOutput` (M1; [ADR 0009](../decisions/0009-self-check-output-before-writing.md)) |
| FMT-011 | When a file contains `#if`, `#elif`, `#else`, `#endif`, `#region` or `#endregion`, the formatter shall keep every directive and every line of disabled text in order, and shall format only the code the no-symbols parse contains ([ADR 0011](../decisions/0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md)). | Must | `#if` around members, a `#if` that splits a header, nested `#if`. | `Formatter.ConditionalDirectivesAreKept` (M3; replaces the M1 whole-file rule) |
| FMT-012 | When a file contains nothing but whitespace, comments and directives, the formatter shall emit the whole file unchanged. | Must | An empty file and a comment-only file. | `Formatter.FilesWithoutDeclarationsAreVerbatim` (M1; narrowed in M3) |
| FMT-013 | If the formatter changes a file, then the output shall end with exactly one line terminator, and the line breaks the formatter writes shall use the input's dominant line terminator (LF unless CRLF is more common). | Must | A CRLF file without a final newline; an LF file with three. | `Formatter.OutputEndsWithOneNewline`, `Formatter.UsesTheDominantLineEnding` (M1) |
| FMT-014 | The formatter shall print each own-line comment, documentation comment and directive that precedes a using, member or top-level statement on its own line above it, in order, re-indented to the item's level (directives at column 0, `#region` and `#endregion` with the code), and shall keep one blank line where the author left one or more. | Must | Comments and directives above members, with and without blank lines. | `tests/golden/comments/leading-*` (M3) |
| FMT-015 | The formatter shall print a comment that follows the last token of a using, member or top-level statement on the same line after it, separated by one space. | Must | `int x;   // note` and `int y; /* note */`. | `tests/golden/comments/trailing-*` (M3) |
| FMT-016 | The formatter shall print the own-line comments and directives before a closing `}` and at the end of a file as the last lines of that body or file, including in an otherwise empty body. | Must | A comment before `}`, in an empty class, and at the end of the file. | `tests/golden/comments/closing-*` (M3) |
| FMT-017 | The formatter shall copy the text of disabled preprocessor branches unchanged, in place, from column 0. | Must | An `#if` branch with odd indentation. | `tests/golden/directives/disabled-*` (M3) |
| FMT-018 | The formatter shall never change the text of a comment (the first line of a multi-line comment is re-indented; its other lines are not touched). | Must | Comments with unusual inner whitespace and a multi-line block comment. | `tests/golden/comments/text-*` (M3) |

## Configuration (CFG)

| ID | Requirement | Priority | Scenario | Test anchor (planned) |
|---|---|---|---|---|
| CFG-001 | Where `.editorconfig` sets `indent_style` and `indent_size`, the formatter shall indent accordingly. | Must | Same input, two configs. | `tests/golden/config/indent-*` (M4) |
| CFG-002 | The formatter shall resolve `.editorconfig` files nearest first and stop at `root = true`. | Must | Nested config directories. | `Config.ResolutionOrder` (M4) |
| CFG-003 | If `.editorconfig` contains a key the formatter does not support, then the formatter shall ignore it without failing. | Should | Config with unknown key. | `Config.UnknownKeysIgnored` (M4) |

## Command line (CLI)

| ID | Requirement | Priority | Scenario | Test anchor (planned) |
|---|---|---|---|---|
| CLI-001 | When `--check` is set, the formatter shall write no files and exit 1 if any file would change. | Must | Unformatted file with `--check`. | `Cli.CheckExitsOneWithoutWriting` (M6) |
| CLI-002 | When `--stdin` is set, the formatter shall read source from stdin and write the result to stdout. | Must | Pipe a buffer. | `Cli.StdinToStdout` (M6) |
| CLI-003 | If an error occurs for an input, then the formatter shall exit 2 and modify nothing for that input. | Must | Unreadable path, parse error. | `CliFormatTests.ParseErrorLeavesFileUntouched`, `CliFormatTests.AMissingPathIsAnError` (M1) |
| CLI-004 | The formatter shall not require an MSBuild project, solution or restore to run. | Must | Format a folder of loose `.cs` files. | `CliFormatTests.NoProjectNeeded` (M1) |
| CLI-005 | The formatter shall write each changed file atomically, so an interrupted or failed run never leaves a partly written file. | Must | Fail the write of the second of two files. | `CliFormatTests.WritesAreAtomic` (M1) |
| CLI-006 | The formatter shall format each `.cs` file named on the command line and every `.cs` file under each named directory. | Must | A folder tree with `.cs` and other files. | `CliFormatTests.DiscoversCSharpFiles` (M1) |

## Performance (PERF)

| ID | Requirement | Priority | Scenario | Test anchor (planned) |
|---|---|---|---|---|
| PERF-001 | The formatter shall format files in parallel. | Should | Multi-file run on the benchmark corpus. | Benchmark project (M5) |
| PERF-002 | Where caching is enabled, the formatter shall skip files unchanged since the last successful run. | Could | Re-run on an unchanged tree. | `Cache.SkipsUnchanged` (M5) |
| PERF-003 | The formatter shall not regress wall-clock time on the benchmark corpus by more than the agreed threshold. | Should | Scheduled benchmark job. | Benchmark CI job (M5); the threshold is a ratio to CSharpier measured in the same job, starting at 15 percent ([ADR 0006](../decisions/0006-formatting-speed-baseline.md)); the exact value is set when the formatter exists |
| PERF-004 | The formatter shall format the pinned corpus in at most 1.5 times CSharpier's wall-clock time, measured in the same run. | Should | Benchmark run on the pinned corpus. | Benchmark CI job (M5) |
| PERF-005 | The formatter shall not be slower than `dotnet format whitespace` on the pinned corpus, measured in the same run. | Should | Benchmark run on the pinned corpus. | Benchmark CI job (M5) |
