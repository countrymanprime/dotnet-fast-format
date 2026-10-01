# Core requirements

Draft v0.1, 2026-10-01. Written in [EARS](https://en.wikipedia.org/wiki/Easy_Approach_to_Requirements_Syntax)
form (sentence templates such as "When X, the formatter shall Y"). **No tests exist yet**, so every test anchor below is
planned; a requirement stays a draft until its anchor is a real, passing test.

Priority: Must / Should / Could. IDs are stable and never reused.

## Formatting (FMT)

| ID | Requirement | Priority | Scenario | Test anchor (planned) |
|---|---|---|---|---|
| FMT-001 | The formatter shall produce identical text when run on its own output. | Must | Format a file twice; compare. | `Invariants.Idempotent`, every fixture and corpus file (M0) |
| FMT-002 | The formatter shall produce output whose syntax tree equals the input's, ignoring trivia. | Must | Parse both; compare tokens. | `Invariants.TreePreserving` (M0) |
| FMT-003 | The formatter shall keep every comment and preprocessor directive, in order. | Must | Count and compare before and after. | `Invariants.NoLoss` (M0) |
| FMT-004 | If a file fails to parse, then the formatter shall leave it unchanged and report an error. | Must | Format a file with a syntax error. | `Cli.ParseErrorLeavesFileUntouched` (M1) |
| FMT-005 | The formatter shall produce output that parses with no new diagnostics. | Must | Parse output; compare diagnostics. | `Invariants.ValidOutput` (M0) |
| FMT-006 | The formatter shall not reflow the contents of verbatim, interpolated or raw string literals. | Must | Fixtures for each literal kind. | `tests/golden/strings/*` (M2) |
| FMT-007 | The formatter shall preserve a file's BOM and its line-ending style unless `.editorconfig` sets `end_of_line`. | Should | Fixtures with BOM and CRLF/LF. | `tests/golden/encoding/*` (M4) |

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
| CLI-003 | If an error occurs for an input, then the formatter shall exit 2 and modify nothing for that input. | Must | Unreadable path, parse error. | `Cli.ErrorExitCode` (M1) |
| CLI-004 | The formatter shall not require an MSBuild project, solution or restore to run. | Must | Format a folder of loose `.cs` files. | `Cli.NoProjectNeeded` (M1) |

## Performance (PERF)

| ID | Requirement | Priority | Scenario | Test anchor (planned) |
|---|---|---|---|---|
| PERF-001 | The formatter shall format files in parallel. | Should | Multi-file run on the benchmark corpus. | Benchmark project (M5) |
| PERF-002 | Where caching is enabled, the formatter shall skip files unchanged since the last successful run. | Could | Re-run on an unchanged tree. | `Cache.SkipsUnchanged` (M5) |
| PERF-003 | The formatter shall not regress wall-clock time on the benchmark corpus by more than the agreed threshold. | Should | Scheduled benchmark job. | Benchmark CI job (M5); threshold UNRESOLVED |
