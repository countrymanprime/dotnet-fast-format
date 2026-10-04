# PRD: dotnet-fast-format

- **Status:** Draft v0.1
- **Date:** 2026-10-01
- **Owner:** countrymanprime

## Summary

`dotnet-fast-format` is a command-line C# formatter for developers and CI. It formats a solution
in a fraction of the time `dotnet format` needs because it parses files with [Roslyn](https://github.com/dotnet/roslyn) and never loads
an MSBuild project. Unlike CSharpier, it takes its style from the [`.editorconfig`](https://editorconfig.org/) a team already
maintains instead of asking them to adopt one fixed style. It must never change what the code
does, and running it twice must give the same result as running it once.

## Problem and gap

| Tool | Strength | Gap for this project |
|---|---|---|
| `dotnet format` | Official, reads `.editorconfig`, also applies analyzer fixes | Slow: loads an MSBuild workspace. A user reported [27.9 s vs 2.4 s for the older tool](https://github.com/dotnet/sdk/issues/39124); that issue was closed as not planned. An [open PR](https://github.com/dotnet/sdk/pull/56394) proposes a faster whitespace path and could narrow the gap. |
| CSharpier | Fast, no MSBuild, Prettier-style printer | [Option requests are out of scope](https://csharpier.com/docs/Configuration) and `.editorconfig` support is limited; open issues include [#1214](https://github.com/belav/csharpier/issues/1214) and [#1002](https://github.com/belav/csharpier/issues/1002). |
| [lyssieth/dotnet-fastformat](https://github.com/lyssieth/dotnet-fastformat) | Fast, reads common `.editorconfig` keys | Early project (0 stars at the time of writing). It builds on Roslyn's own formatter and describes itself as an intermediary, not a replacement for `dotnet format`. |

The gap: **speed close to CSharpier, with behavior driven by existing `.editorconfig` rules.**

## Goals

1. Format C# files without MSBuild, in parallel, skipping unchanged files.
2. Honor the common formatting keys in `.editorconfig`.
3. Guarantee safety: idempotent, tree-preserving, no lost comments, fail safe on error.
4. **Stretch:** cover enough of `dotnet format`'s whitespace and style pass to replace it for
   most repositories.

If the stretch goal falls short, the doc-printer formatter in goals 1 to 3 still stands on its own.

## Non-goals for the first release

- Analyzer-based code fixes that need a compilation or semantic model.
- Linting, import sorting, or any rewrite that changes program meaning.
- A language server or IDE plugin. Editor use is covered by `--stdin`.
- Formatting languages other than C#.
- A large option surface. Every option must be justified (see Configuration).

## Users and scenarios

| User | Scenario |
|---|---|
| Developer | Run `dotnet fast-format .` before committing. |
| CI | Run `dotnet fast-format --check .` and fail the build when files would change. |
| Editor or pre-commit hook | Pipe a buffer through `--stdin` and read the result from stdout. |

## Formatting principles

Written before any code, to be revisited with a rationale doc once the printer exists.

- **Correctness first.** Formatting never changes meaning. When unsure, leave code as written.
- **Print, don't transform.** The formatter lays out existing syntax; it does not rewrite it.
- **Honor the author where it is safe.** Line breaks and blank lines the author chose are kept
  where the `.editorconfig` keys allow.
- **Comments are never lost or moved across code.**

## Configuration surface

The tool reads `.editorconfig` (nearest file first, honoring `root = true`). Initial keys:

- General: `indent_style`, `indent_size`, `end_of_line`, `insert_final_newline`, `max_line_length`
- C# layout: the `csharp_new_line_*`, `csharp_indent_*`, `csharp_space_*` and
  `csharp_preserve_single_line_*` families from Microsoft's
  [C# formatting options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/csharp-formatting-options)

Which keys are honored, ignored, or deliberately deviate from `dotnet format` will be tracked in a
support table once implemented. A key is added only with a fixture that shows its effect.

## CLI contract (proposed)

```text
dotnet fast-format [paths...] [--check] [--stdin] [--config <path>] [--no-cache]
```

| Exit code | Meaning |
|---|---|
| 0 | Success; with `--check`, no file would change |
| 1 | With `--check`, at least one file would change |
| 2 | Error (parse failure, I/O error, bad arguments); no file was modified for the failing input |

Formatted text goes to stdout only with `--stdin`; diagnostics go to stderr.

## Success metrics

The speed targets come from the M0 baseline ([ADR 0006](decisions/0006-formatting-speed-baseline.md)).

| Metric | Target |
|---|---|
| Idempotency on the pinned corpus | 100% |
| Tree-equivalence on the pinned corpus | 100% |
| Crashes on the pinned corpus | 0 |
| Wall-clock on the pinned corpus, relative to CSharpier in the same run | At most 1.5 times (stretch: parity) |
| Wall-clock on the pinned corpus, relative to `dotnet format` (MSBuild) | At least 10 times faster (the M0 baseline gap to CSharpier is about 18 times) |
| Wall-clock relative to `dotnet format whitespace` | Never slower |
| `.editorconfig` keys covered by a fixture | Tracked in the support table |

## Risks and rabbit holes

- **Comment and trivia attachment.** Roslyn attaches trivia to tokens; deciding who owns a comment
  when line breaks change is the main source of bugs.
- **Preprocessor directives.** `#if` branches can contain incomplete code and Roslyn parses one
  branch per symbol set. The strategy is undecided (Open question 2).
- **Strings.** Verbatim, interpolated and raw string literals must never be reflowed.
- **Encoding.** BOM and mixed line endings must round-trip.
- **Language growth.** New C# syntax must be covered each release.
- **A faster `dotnet format`.** If the open sdk PR lands, the speed advantage shrinks and the
  `.editorconfig` fidelity and correctness story matter more.

## Prior art

Studied for process and structure, not copied: Prettier ([rationale](https://prettier.io/docs/rationale)),
rustfmt, Ruff, Black, Biome, gofmt, and CSharpier. Ideas adopted: tests as the spec (golden fixtures
plus idempotency and tree-equivalence checks), a stability policy for stable output, and a small option set.

## Open questions

1. **Default style.** When no `.editorconfig` key applies, does output follow `dotnet format`
   defaults, CSharpier's style, or something else? Recommendation in
   [ADR 0007](decisions/0007-default-style-is-microsoft-conventions-at-100-columns.md) (Proposed). UNRESOLVED
2. **Preprocessor strategy.** Format per branch, skip regions, or run a multi-symbol pass? Until M3,
   nodes with directives print verbatim ([ADR 0008](decisions/0008-print-unsupported-syntax-verbatim.md)). UNRESOLVED
3. **Name of the command and tool package.** `dotnet fast-format` is proposed; confirm. UNRESOLVED
4. **Target framework and AOT.** Which .NET versions to support, and whether Roslyn syntax-only
   packages permit Native AOT (a related project reported AOT failures with Roslyn Workspaces,
   which this project does not use). UNRESOLVED
5. **Speed target.** RESOLVED by [ADR 0006](decisions/0006-formatting-speed-baseline.md): ratios to
   CSharpier measured in the same run, not seconds. See the success metrics.

## Milestones and definition of done

See [`TASKS.md`](TASKS.md). A milestone is done when `dotnet test` passes, including the
idempotency, tree-equivalence and no-loss checks and the pinned corpus run, with no new warnings
and docs and ADRs updated in the same change.
