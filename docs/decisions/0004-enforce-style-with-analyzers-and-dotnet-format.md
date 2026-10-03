# 0004. Enforce code style with analyzers and `dotnet format`

- **Status:** Accepted
- **Date:** 2026-10-02
- **Deciders:** countrymanprime
- **Related:** [ADR 0003](0003-use-dotnet-test-as-the-single-gate.md), [PR 3](https://github.com/countrymanprime/dotnet-fast-format/pull/3)

## Context and problem

This project's job is to format C#, but its own source cannot be formatted by itself yet, and
should not be while the formatter is unfinished: a formatter that rewrites its own source can hide
its own bugs. We still need the repository's code kept consistent and clean, by machine, so
reviewers and agents do not argue about style.

## Decision drivers

- One standard, enforced automatically, with no reliance on reviewer attention.
- Keep `dotnet test` as the single correctness gate ([ADR 0003](0003-use-dotnet-test-as-the-single-gate.md)).
- Do not let a half-built formatter format its own code.

## Considered options

1. **Analyzers in the build plus a `dotnet format` CI check.**
2. Analyzers only.
3. Dogfood: run our own formatter over our own code now.

## Decision outcome

**Chosen option: analyzers in the build plus a `dotnet format --verify-no-changes` CI job**, with
the root `.editorconfig` as the single source of style.

- `StyleCop.Analyzers` (1.2.0-beta line, because the 2019 stable release predates file-scoped
  namespaces and primary constructors) and `Meziantou.Analyzer` apply to every project through
  `GlobalPackageReference` in `Directory.Packages.props`. They run in `dotnet build`, so
  `TreatWarningsAsErrors` makes them part of the `dotnet test` gate.
- `dotnet format DotnetFastFormat.slnx --verify-no-changes` runs as a separate required CI job
  (whitespace, `.editorconfig` style and analyzer fixes).
- XML documentation is generated for `src` projects, so StyleCop's documentation rules and `CS1591`
  enforce documented public API there. Tests are exempt (not public API).
- Disabled rules, each on purpose: `SA1633` (no per-file license header; the license is
  repo-level) and, for `tests/**`, StyleCop's documentation category and `SA0001`.

**Dogfooding is deferred, not rejected.** Once the formatter is stable (target: after milestone
M4, with `.editorconfig` support) run a pinned, released build of it over this repo in a
non-gating job, then promote it. Pinning to a released version avoids the bootstrap problem, and
a new ADR would supersede this one when that happens.

### Consequences

- **Good:** consistent code with no reviewer effort; analyzers catch real issues early (the first
  run found an unbounded regex, a missing string comparer and a `Process` misconfiguration).
- **Bad:** `dotnet format` loads MSBuild, so the format job is slower than the build; the StyleCop
  beta is a prerelease dependency to track.
- **Neutral:** adding or loosening analyzer severities is a deliberate change that needs a reason
  in the PR, not a way to get a build green.

### Confirmation

The `dotnet test` gate (analyzers) and the `dotnet format` CI job. Both are required checks on `main`.

## More information

Revisit when the formatter can format itself, or when StyleCop ships a stable 1.2.

## Amendment 2026-10-03

`SA1101` (prefix local calls with `this`) is also disabled: it contradicts `dotnet_style_qualification_*`
= `false` in `.editorconfig`, which `dotnet format` enforces (`IDE0003`). The repository style is no `this.`.
