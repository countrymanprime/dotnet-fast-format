# 0002. Build our own doc-printer on Roslyn syntax trees

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** countrymanprime
- **Related:** [PRD](../PRD.md), [architecture](../architecture.md)

## Context and problem

The goal is a formatter that is fast, needs no MSBuild project load, and honors a team's existing
`.editorconfig`. `dotnet format` is slow because it loads an MSBuild workspace
([sdk#39124](https://github.com/dotnet/sdk/issues/39124)). CSharpier is fast but
[does not accept option requests](https://csharpier.com/docs/Configuration). We need to choose how
the layout itself is produced.

## Decision drivers

- Speed without MSBuild.
- Control over layout so `.editorconfig` keys can change output.
- Correctness: no meaning changes, no lost comments.
- Effort and long-term maintenance.

## Considered options

1. **Own doc-printer** over Roslyn syntax trees (Prettier-style document tree and printer).
2. **Wrap Roslyn's `Formatter`** and drive it from `.editorconfig`.
3. **Fork CSharpier** and add `.editorconfig` options. A community fork already does this for some
   keys: [TobiStr/csharpier-editorconfig](https://github.com/TobiStr/csharpier-editorconfig).
4. **Keep using `dotnet format`.**

## Decision outcome

**Chosen option: own doc-printer on Roslyn syntax trees**, because it gives full control over layout
while staying syntax-only and MSBuild-free, which is what the speed and configurability goals need.

The doc-printer is the core product. If the stretch goal of replacing `dotnet format`'s wider style
pass proves out of reach, the doc-printer formatter still delivers goals 1 to 3 of the PRD on its own.

### Consequences

- **Good:** layout is ours to configure; no dependency on Roslyn's formatter limits or on CSharpier's
  option policy.
- **Bad:** this is the highest-effort option. Comment and trivia attachment, preprocessor directives,
  and every syntax kind must be handled and kept up to date as C# grows.
- **Bad:** correctness rests on our tests, so the invariant checks (idempotent, tree-preserving,
  no-loss) and a pinned real-code corpus are mandatory from the first milestone.
- **Neutral:** we can still borrow ideas and test inputs from CSharpier and others.

### Confirmation

The invariant tests and corpus run in `dotnet test` ([ADR 0003](0003-use-dotnet-test-as-the-single-gate.md)).

## Pros and cons of the options

### Own doc-printer

- Good, because full control over layout and options.
- Bad, because the most work and the most edge cases.

### Wrap Roslyn's `Formatter`

- Good, because least code.
- Bad, because the formatter is [only configurable by a predefined option set](https://github.com/dotnet/roslyn/issues/31691),
  can be [very slow on huge files](https://github.com/dotnet/roslyn/issues/36157), and has
  [long-standing whitespace bugs](https://github.com/dotnet/roslyn/issues/2583).

### Fork CSharpier

- Good, because a proven printer and test suite.
- Bad, because a fork must track upstream changes, and the upstream maintainer's stance on options
  means our direction would diverge permanently.

### Keep `dotnet format`

- Good, because no work.
- Bad, because it does not meet the speed goal.

## More information

Revisit if the open `dotnet format` speed PR ([sdk#56394](https://github.com/dotnet/sdk/pull/56394))
lands and removes most of the speed gap, or if the doc-printer cannot pass the invariants on the
corpus by the end of milestone M3.
