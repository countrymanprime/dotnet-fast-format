# 0012. Read `.editorconfig` with a small parser in Core and support seven keys

- **Status:** Accepted (provisional)
- **Acceptance:** accepted provisionally on 2026-10-05 by the agent implementing M4, under the milestone brief
  ("decide the exact set in an ADR"). Pending the author's review.
- **Date:** 2026-10-05
- **Deciders:** countrymanprime
- **Related:** [PRD](../PRD.md) (configuration surface), [ADR 0002](0002-build-a-doc-printer-on-roslyn-syntax-trees.md), [ADR 0007](0007-default-style-is-microsoft-conventions-at-100-columns.md), [ADR 0008](0008-print-unsupported-syntax-verbatim.md), [ADR 0009](0009-self-check-output-before-writing.md), requirements CFG-001 to CFG-013, FMT-007, FMT-013

## Context and problem

Until M3 every setting is hard-coded: four spaces, 100 columns, the file's dominant line ending, one final newline,
and the file's own BOM. The product promise (PRD) is behavior driven by the `.editorconfig` a team already has, with
no MSBuild. M4 has to decide four things before any code exists, because AGENTS.md says a formatting option needs an
ADR and the option set is intentionally small: which keys are supported and what each means for the printer; what
happens to a value or a file that cannot be used; how tabs interact with width and with text the formatter copies
as written; and how the files are read (own code or a package).

The [EditorConfig specification](https://spec.editorconfig.org/) (the text on its `master` branch, read on 2026-10-05) defines the file
format, the glob dialect, the lookup and the override order. It defines the keys `indent_style`, `indent_size`,
`tab_width`, `end_of_line`, `charset`, `trim_trailing_whitespace`, `insert_final_newline` and `root`, and says that
plugins "shall ignore unrecognized keys and invalid/unsupported values". `max_line_length` is not in the
specification but is the widely used width key (CSharpier reads it, see ADR 0007).

## Decision drivers

- The output must never change program meaning (AGENTS.md invariants), so a setting that touches text inside a
  token (a multi-line string literal) must leave the token alone.
- Few options: each key is a style commitment. A key is supported only if it has a clear effect on this printer.
- No MSBuild and no semantic model; a run over thousands of files must not re-read or re-parse `.editorconfig`
  for each of them.
- A broken or hostile `.editorconfig` must never crash a run or corrupt a file (invariant 4).
- Speed is a goal (ADR 0006): avoid a new dependency's load cost and surface area where a few hundred lines do.

## Considered options

For the way the files are read:

1. **A small parser and matcher in `DotnetFastFormat.Core`** (chosen).
2. A package: a .NET port of the EditorConfig core (not evaluated in depth) or Roslyn's own `AnalyzerConfig`
   (in `Microsoft.CodeAnalysis`, already referenced, but designed for the analyzer configuration dialect, whose
   section matching and override rules differ from the specification in places).
3. Shell out to the `editorconfig` command line tool.

## Decision outcome

### How the files are read

**Option 1**, hand-written, in `src/DotnetFastFormat.Core/Config/`. The format is a dozen lines of rules, and
the glob dialect is a page; both are covered by a table of examples from the specification. A package would add a
dependency for code that is about as small as the glue needed to adapt it, and `AnalyzerConfig` follows Roslyn's
own variant of the rules, so it cannot be relied on for the specification's `root`, `unset` and override behavior.
The parser reads spans of the text without allocating per character, each section's glob is compiled once per file, and
the lookup is cached (below). Core stays free of file I/O: the resolver takes a function that reads a path, so tests
run in memory and the CLI supplies the disk.

### Lookup and precedence

For a file, the search starts in its directory and goes up, nearest first, and stops after the first file whose
preamble has `root = true` (case-insensitive) or at the file system root. Every found file is parsed once per run and
kept (CFG-012). For each key the assignments are applied from the farthest file to the nearest and, inside one file,
from the first section to the last, so the last assignment wins; `unset` ends the key's history and the default
applies. Keys are lowercased; values of supported keys are compared case-insensitively. A resolved value that cannot
be used (below) is skipped as if it had not been written, so an earlier valid assignment still applies. The
resolver takes an optional ceiling directory so tests can keep a repository's configuration separate from the
configuration above it.

Resolved options are computed per file from the cached, already parsed files: matching a file name against a few
compiled section globs is cheap, and a cache keyed by directory alone would be wrong because sections can match
a file name (`[Program.cs]`).

Glob details where the specification leaves room: matching is case-sensitive; `[a-z]` ranges work inside brackets
and a `/` inside brackets does not count as a separator; `**/` has no special meaning, so `[**/*.cs]` needs a
directory below the `.editorconfig` and does not match a file next to it, while `[*.cs]` and `[**.cs]` do; `{n..m}`
matches an integer written without leading zeros and needs `n < m`, any other braces with no comma are literal; a
leading `/` is dropped when the name has a separator. A name longer than 4,096 characters, nested alternations more
than 32 deep, or alternations that expand to more than 1,024 patterns are not matched (with a warning, below).
Matching remembers the states that failed, so a pattern such as `*a*a*a*b` cannot take exponential time.

### Supported keys

| Key | Values | Effect |
|---|---|---|
| `indent_style` | `space`, `tab` | Each indentation level is written with spaces, or with tabs (see below). Default `space`. |
| `indent_size` | a whole number 1 to 256, or `tab` | Columns per indentation level. `tab` means the `tab_width` value, or 4. Default 4. With `indent_style = tab` and no `indent_size`, the level is `tab_width` columns. |
| `tab_width` | a whole number 1 to 256 | Columns one tab counts for. Default: the `indent_size`, or 4. Only used with `indent_style = tab`. |
| `max_line_length` | a whole number 1 to 1,000,000, or `off` | The width at which groups break (ADR 0007). `off` never wraps. Default 100. |
| `end_of_line` | `lf`, `crlf` | The line terminator the formatter writes. Unset: the file's dominant one. |
| `insert_final_newline` | `true`, `false` | `true` (and unset): exactly one line terminator at the end of a non-empty file. `false`: none. |
| `charset` | `utf-8`, `utf-8-bom` | Remove or add a UTF-8 byte order mark. Unset: the file's own is kept (FMT-007). |

Every other key is ignored without a message (CFG-003), including `root` outside the preamble,
`trim_trailing_whitespace` (the printer already removes trailing whitespace it writes, and text inside tokens and
comments is never touched), `csharp_*` and `dotnet_*` keys (later milestones add them one by one, each with an ADR
amendment and a fixture), and `spelling_language`.

**Not supported, with a documented rule:** `end_of_line = cr` (a lone carriage return as the line terminator has
not been a real terminator on any current platform; Roslyn accepts it but nobody writes it) and the `charset` values
`latin1`, `utf-16be` and `utf-16le` (the CLI reads and writes UTF-8 only and fails on a file that is not valid
UTF-8). Both are treated as invalid values (below): a warning, and the key is ignored. `indent_size = 0` and a width
of 0 are invalid for the same reason.

### Invalid values and unreadable files

A supported key with a value outside the table is **ignored with one warning on stderr** and the run
continues; the exit code does not change (CFG-010). The warning names the file and line, for example
`warning: /repo/.editorconfig(12): indent_size = abc is not a whole number from 1 to 256, or tab; ignored`. It is
printed only for an assignment that applies to a file being formatted, and once per run. Failing the run instead was
considered and rejected: the tool formats code and is not a configuration linter, the editors the team already
uses ignore the same values, and a typo in a section for another language must not block a build.

A `.editorconfig` that cannot be read (an `IOException`, a permissions error) produces one warning, is treated as
absent, and the search continues in the parent directory (CFG-011). Invalid UTF-8 inside it is decoded leniently.
A line that is not blank, a comment, a section header or a pair is skipped with a warning. Nothing in the parser or
matcher throws; a section name that is too long or too complex for the matcher is skipped with a warning. These
warnings, like the unreadable-file one, are printed for every file whose search reaches the `.editorconfig`, once per
run.

### Tabs and the printer

`indent_style = tab` changes how the printer writes the whitespace at the start of a line it breaks. A level is
`indent_size` columns; the printer writes `columns / tab_width` tab characters and then `columns % tab_width`
spaces, which is what the specification asks for (`indent_size = 4`, `tab_width = 3` gives a tab and a space; with the
usual `indent_size = tab_width` it is one tab per level). The width accounting counts those columns: the printer's
column position after a line break is the number of columns, so a tab is `tab_width` columns for `Fits` and for the
`max_line_length` comparison. A tab character inside text (a token or a comment) still counts as one UTF-16 code
unit, as everything else does (ADR 0007).

Text copied as written (ADR 0008) is never re-indented: only the first line of a copied node gets the printer's
indentation, and its other lines keep the whitespace they had, so a body written with four spaces under
`indent_style = tab` stays as it was until the statement printer lands (M2). Block comments, disabled text and
documentation comments follow the same rule as before.

### `insert_final_newline = false`

The formatted file ends without a line terminator: the one newline the printer writes after the last item is not
written. An empty file stays empty. A file that ends in a `//` comment or a directive is valid without a final
newline. FMT-013 is amended: exactly one final terminator, or none when this key is `false`.

### `end_of_line`

With `lf` or `crlf`, the printer writes that terminator for every break it writes, and the formatter also converts
the terminators of text it copied as written: comments, directives, disabled text and whitespace. Terminators
inside a token, which today means a multi-line verbatim, raw or interpolated string literal, are never changed:
doing so would change the string's value and break invariant 2. A file can therefore still hold both terminators
after the conversion, but only inside such literals. The conversion runs as a pass over the output's trivia and is
skipped when the printed text already holds only the wanted terminator.
Unset keeps today's behavior (the dominant terminator; mixed input stays mixed inside copied text).

### `charset`

The CLI decodes UTF-8 (a BOM is detected and removed before formatting). On writing, `utf-8-bom` writes a BOM and
`utf-8` writes none, for a non-empty output; unset writes the BOM the file had. A file whose only change is the BOM is
written. This follows the specification (`utf-8` means no BOM); teams that want BOMs kept leave the key unset or choose
`utf-8-bom`.

### Consequences

- **Good:** the main `.editorconfig` keys work with no new dependency; the behavior is specified, tested against
  the specification's examples, and safe by the existing output check (token text is compared exactly).
- **Good:** the formatter ignores everything it does not support, so a team's full C# `.editorconfig` can be used
  as is.
- **Bad:** we maintain a parser and a glob matcher. The core-tests of the EditorConfig project are the reference for
  extending them; unusual patterns there that the table does not cover may match differently.
- **Bad:** the options interact with copied text until M2: with `indent_size` other than 4, or tabs, a copied
  member body keeps the indentation it was written with and looks misaligned (documented in `docs/style.md`).
- **Neutral:** removing a BOM under `charset = utf-8` changes bytes of files in repositories that use the key and
  still carry BOMs; this is announced in the style changelog.

### Confirmation

Table-driven tests of the parser and the glob matcher from the specification; resolver tests for order, `root`,
`unset`, case, invalid values and one read per file; golden fixtures that format one input under two
configurations; a matrix test that runs every golden input under every supported setting through the invariants;
CLI tests with nested configurations in temp directories; and the slow corpus test under each file's own
`.editorconfig`.

## Pros and cons of the options

### Small parser and matcher in Core

- Good, because no dependency, no I/O in Core, full control of invalid-value and unreadable-file behavior, and the
  per-file match is allocation-light.
- Bad, because we own correctness of the glob dialect.

### A package (`EditorConfig.Core` or Roslyn `AnalyzerConfig`)

- Good, because the glob and file rules are someone else's to maintain.
- Bad, because of the dependency, because the fit is uncertain (above), because neither is known to report per-line
  warnings for invalid values, and because the code we would write around it is about as large as the parser.

### Shell out to the `editorconfig` tool

- Bad, because a process per file defeats the speed goal and needs the tool installed.

## More information

Revisit when the first `csharp_*` key is added (an amendment to this ADR or a new one), if real configurations
use `end_of_line = cr` or non-UTF-8 charsets, or if the EditorConfig specification adds keys we should honor.
