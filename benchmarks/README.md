# Benchmarks

Times other formatters on the pinned [corpus](../corpus/README.md) so we know what speed we have to beat.
It is not part of the `dotnet test` gate ([ADR 0003](../docs/decisions/0003-use-dotnet-test-as-the-single-gate.md)).

```text
dotnet run --project benchmarks/DotnetFastFormat.Benchmarks -c Release -- [--runs N] [--repo NAME]... [--tool NAME]... [--output DIR]
```

| Tool | What it runs |
|---|---|
| `dotnet-format-whitespace` | `dotnet format whitespace . --folder`: the whitespace pass, no MSBuild project load |
| `dotnet-format` | `dotnet format <solution> --no-restore`: loads the projects through MSBuild (the restore is done first, untimed) |
| `csharpier` | `csharpier format .`, installed at a pinned version under `.bench/tools` |

For each repository and tool the harness copies the pinned checkout, runs the tool on the copy and
records wall-clock time and how many tracked files it changed, then deletes the copy. The files
changed count matters: CSharpier rewrites layout wholesale, `dotnet format whitespace` only fixes
violations of the repository's `.editorconfig`, so they are not doing the same work. A repository
with no solution file shows `n/a` for `dotnet-format`; a tool that errors shows `failed`.

`--output DIR` also writes `baseline.md` and `baseline.json`. The default is 3 runs; the report shows
the median and the min–max range. Working files live in `.bench/` (git-ignored).

The harness adjusts the environment so the comparison is about formatting, not setup: every `global.json` in a
working copy is deleted (the installed SDK is used), Windows targeting is enabled, and NuGet vulnerability
warnings (`NU1901`–`NU1904`) are not errors during restore. Working copies live in the system temp directory, never inside this repository, because MSBuild
and `.editorconfig` lookups search upward and would otherwise pick up this repository's own settings.
`DOTNET_FAST_FORMAT_BENCH_DIR` overrides that directory.

Timings depend on the machine, so compare numbers only from the same environment. The report records
the OS, processor count, SDK version, tool versions and the commit it ran from. The
`Benchmarks` workflow runs it on a GitHub-hosted runner.

A new formatter will be added as a tool here once it can format a real file; the speed target in
[ADR 0006](../docs/decisions/0006-formatting-speed-baseline.md) is measured against these numbers.
