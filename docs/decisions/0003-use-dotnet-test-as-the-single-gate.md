# 0003. Use `dotnet test` as the single gate

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** countrymanprime
- **Related:** [AGENTS.md](../../AGENTS.md), `formatter-verification` skill

## Context and problem

Work on this project is checked many times a day, by people and by AI agents. We need one command
that says whether a change breaks any rule, and CI must enforce it before anything reaches `main`.
A separate verify script would duplicate what the test runner already does and add a second place to
maintain.

## Decision drivers

- One command locally and in CI.
- Failures are machine-readable and cannot be argued with.
- Few moving parts for a solo maintainer.

## Considered options

1. `dotnet test` runs everything: Verify snapshot tests, the invariants and the corpus run, with
   warnings as errors set in `Directory.Build.props`.
2. A wrapper script (for example `build.ps1 verify`) that runs build, format check and tests.

## Decision outcome

**Chosen option: `dotnet test` as the single gate**, because snapshots, invariants and the corpus run
are all expressible as tests, and `TreatWarningsAsErrors` makes warnings fail the build. Once CI
exists, the `dotnet test` check is required on `main`.

### Consequences

- **Good:** one command; CI is a single step; nothing extra to keep in sync.
- **Bad:** the corpus needs a fetch step that runs inside or before the test run, and slow or noisy
  checks (benchmarks, fuzzing, revision diffs) must live in separate jobs outside the gate.
- **Neutral:** the matrix (Windows and Linux) is workflow configuration, not a script.

### Confirmation

The required status check on the `Pull Request` ruleset (task T-004 in [TASKS.md](../TASKS.md)).

## More information

Revisit if the test run becomes too slow for local use; then split fast and full suites rather than
adding a wrapper script.
