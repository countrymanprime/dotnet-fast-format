# 0006. Set the formatting speed target relative to CSharpier

- **Status:** Proposed
- **Date:** 2026-10-04
- **Deciders:** countrymanprime
- **Related:** [PRD](../PRD.md) (open question 5), [PERF-003](../requirements/core.md), [benchmarks/README.md](../../benchmarks/README.md), [PR 3](https://github.com/countrymanprime/dotnet-fast-format/pull/3)

## Context and problem

The [PRD](../PRD.md) names the gap this project fills: speed close to CSharpier, with behavior driven by
existing `.editorconfig` rules. Its open question 5 says the speed target is fixed after the M0 baseline,
and PERF-003 has an unresolved regression threshold. Task T-006 measured the tools we compare against on the
pinned [corpus](../../corpus/README.md) so the target rests on numbers.

## Method

`benchmarks/DotnetFastFormat.Benchmarks` formats a fresh copy of each pinned repository with each tool and
records wall-clock time (median of 3 runs, min–max) and how many tracked files the tool changed. The three
tools: `dotnet format whitespace . --folder` (no project load), `dotnet format <solution> --no-restore`
(MSBuild project load; the restore is done first and not timed) and CSharpier 1.3.0 (`csharpier format .`).
The environment is adjusted so the comparison is about formatting, not setup: every `global.json` in the copy
is deleted, Windows targeting is enabled and NuGet vulnerability warnings are not errors during restore.
Working copies live outside this repository, because MSBuild and `.editorconfig` lookups search upward
(an early version ran inside the repository and silently picked up its own settings).

## Baseline

Measured by the `Benchmarks` workflow on a GitHub-hosted `ubuntu-latest` runner (4 logical processors,
.NET SDK 10.0.401, CSharpier 1.3.0), [run 37190414730](https://github.com/countrymanprime/dotnet-fast-format/actions/runs/37190414730),
corpus pinned as in `corpus/corpus.json`:

| Repository | .cs files | MB | `dotnet format whitespace` | `dotnet format` (MSBuild) | CSharpier |
|---|---:|---:|---|---|---|
| newtonsoft-json | 944 | 6.8 | 8.8 s (8.4–9.6), 72 files changed | 94.2 s (91.6–94.3), 77 files changed | 10.6 s (10.3–12.1), 920 files changed |
| dapper | 157 | 1.1 | 3.2 s (3.2–3.3), 29 files changed | 38.1 s (37.6–39.0), 49 files changed | 2.4 s (2.4–2.5), 140 files changed |
| serilog | 216 | 0.9 | 2.7 s (2.7–2.7), 23 files changed | 36.6 s (35.7–36.8), 27 files changed | 1.8 s (1.8–1.8), 172 files changed |
| humanizer | 534 | 2.0 | 4.3 s (4.2–4.6), 189 files changed | 63.6 s (62.9–63.9), 195 files changed | 2.8 s (2.5–3.2), 518 files changed |
| spectre-console | 466 | 2.5 | 3.9 s (3.9–4.0), 50 files changed | 66.4 s (66.3–67.7), 43 files changed | 2.0 s (2.0–2.0), 447 files changed |
| communitytoolkit-dotnet | 385 | 3.7 | 4.9 s (4.9–5.0), 350 files changed | 120.4 s (117.5–130.4), 349 files changed | 4.1 s (3.9–4.2), 347 files changed |
| **Total** | 2702 | 17.0 | **27.8 s** | **419.3 s** | **23.8 s** |

What the numbers say:

- Full `dotnet format` is about **18 times slower than CSharpier** (419.3 s against 23.8 s) and about 15 times
  slower than its own whitespace pass. Loading projects through MSBuild is the cost this project avoids.
- The whitespace pass without a project load is close to CSharpier (1.17 times its total), so the speed gap the
  PRD describes is against the MSBuild path, not against `dotnet format whitespace`.
- **The tools do different work.** CSharpier rewrites layout wholesale (920 of 944 files in Newtonsoft.Json);
  the `dotnet format` passes only fix `.editorconfig` violations (72 of 944). Time is comparable only as
  "time to format this repository", not as time per change.
- **Absolute times are not stable on shared runners; ratios are.** A later run of the same harness on the same
  runner type took 40 percent longer for CSharpier over the same five repositories (15.0 s, then 21.0 s; Humanizer
  is left out because it failed to restore in the earlier run), yet full `dotnet format` over CSharpier was 17.9
  then 16.9, and the whitespace pass over CSharpier was 1.10 then 1.12. A target in seconds would be
  meaningless; a ratio is not.
- A run on a different machine (a 4-core cloud container, SDK 10.0.112) gave the same ordering of the tools.
  Humanizer's full `dotnet format` could not restore there because NuGet's signature revocation check could
  not reach its server. That is environmental, and the runner measures it.

## Decision outcome (proposed)

Express the speed target and the regression gate as **ratios measured in the same run**, never in seconds:

1. **Target:** formatting the whole corpus takes **at most 1.5 times CSharpier's time** (about 36 s on the
   runner above), with **at most 1.0 times** (parity) as the stretch goal. This is what "close to CSharpier"
   means in the PRD, and it is far inside the headline: at least 10 times faster than full `dotnet format`.
2. **Floor:** never slower than `dotnet format whitespace`, the tool this project replaces.
3. **Regression gate (PERF-003, milestone M5):** the benchmark job fails if the new formatter's time as a ratio
   of CSharpier's, measured in the same job, worsens by more than a threshold chosen when the formatter exists
   (start at 15 percent, judged against the run-to-run spread of the ratio). The new formatter is added as a
   tool in the harness once it can format a real file.

If accepted, this resolves PRD open question 5 and sets PERF-003's threshold basis. Status moves to Accepted
and the PRD and requirements are updated in the same change.

### Consequences

- **Good:** the target survives changing runners, SDK versions and noisy neighbours; it is defined by the
  comparison the PRD already makes; the benchmark is reproducible by anyone with `dotnet run`.
- **Bad:** a ratio to CSharpier moves when CSharpier releases a faster version, so the pinned CSharpier
  version is part of the baseline and changing it is a deliberate, recorded step.
- **Bad:** the workload differs (see above), so the target compares time to format a repository, not like for
  like. A formatter that rewrites less than CSharpier has to parse the same amount, so the floor on its time is
  similar, but this should be re-checked once it exists.
- **Neutral:** the corpus is six repositories, 2,702 files, 17 MB, so absolute numbers say little about a
  million-line solution. M5 adds a larger input before the gate is trusted.

### Confirmation

The `Benchmarks` workflow (not part of the PR gate; ADR 0003) and the tests of the harness's statistics and
report. The first formatter build (M1) adds a row to this table.

## Considered options

1. **Ratios to CSharpier, measured in the same run** (proposed).
2. A fixed time budget in seconds on the runner. Rejected: the same harness varied by about 40 percent in
   absolute time between two runs.
3. Parity with `dotnet format whitespace` only. Rejected as the target: it is the floor, not the goal, and
   it is not the tool the PRD says to be close to.

## More information

Revisit when the formatter first runs on the corpus, when CSharpier or the .NET SDK changes major version, and
when `dotnet format`'s MSBuild path gets faster (the PRD notes an open SDK pull request that might).
