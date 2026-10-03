# 0005. Tier tests with a `Slow` trait

- **Status:** Accepted
- **Date:** 2026-10-03
- **Deciders:** countrymanprime
- **Related:** [ADR 0003](0003-use-dotnet-test-as-the-single-gate.md), [PR 3](https://github.com/countrymanprime/dotnet-fast-format/pull/3)

## Context and problem

[ADR 0003](0003-use-dotnet-test-as-the-single-gate.md) makes `dotnet test` the single gate and says
slow or noisy checks live in separate jobs. The first test that needs the network (fetching the
pinned corpus, task T-005) forces the question of how those tests are separated. The pull-request
gate must stay fast and independent of network outages, and the exhaustive runs must still happen
on a schedule. A tier that nothing runs would be a way to skip tests, which `AGENTS.md` forbids.

## Decision drivers

- A fast, offline-safe default for local runs and the PR gate.
- No test that silently never runs.
- One mechanism, readable in the test code.

## Considered options

1. **A `[Trait("Category", "Slow")]` tier**, excluded by default and run by a scheduled workflow.
2. A separate test project for slow tests.
3. Skip slow tests at run time unless an environment variable is set.

## Decision outcome

**Chosen option: the `Slow` trait**, because it keeps one test project and one runner, shows the
tier in the code, and the filter is explicit.

- The test project sets `TestTier` (default `fast`): plain `dotnet test` runs everything except
  `Category=Slow`. `dotnet test -p:TestTier=slow` runs only the slow tier and
  `-p:TestTier=all` runs both.
- `.github/workflows/slow.yml` runs the slow tier nightly, on demand, and on pull requests that
  touch the corpus or the fetch step, on Linux and Windows.
- A fast test (`TestTierPolicyTests`) fails if a category other than `Slow` is used, or if the slow
  workflow loses its schedule or its `TestTier=slow` run. A trait therefore cannot exist without a
  job that runs it.
- A failure in the slow tier is a blocker, not a flake.
- Selecting only slow tests under the default tier exits with code 8 ("zero tests ran"), so the
  mistake is loud.

**Filtering syntax changes.** xunit v3 on Microsoft.Testing.Platform rejects a VSTest-style
`dotnet test --filter "..."` together with a trait filter. To run one test, use the native filters,
which combine with the tier: `dotnet test -- --filter-method "*<name>*"` or
`--filter-class "*<name>*"`.

### Consequences

- **Good:** fast default, offline-safe PR gate, and the exhaustive tier is guaranteed to run.
- **Bad:** the documented one-test command differs from the common `--filter` form, and the slow
  tier is not a required check on `main` (path-filtered checks cannot be required), so nightly
  failures need someone to watch them.
- **Neutral:** the corpus fetch is slow only in network terms; the fetch logic itself is covered
  offline by fast tests against a local git repository.

### Confirmation

`TestTierPolicyTests` and the `Slow tests` workflow.

## More information

Revisit if the slow tier grows (fuzzing, large inputs): add categories only together with a job that
runs them, and extend `KnownCategories` in the policy test.
