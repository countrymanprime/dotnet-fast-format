---
name: formatter-verification
description: Run, extend and interpret the checks that prove a code formatter is correct - idempotency, syntax-tree equivalence, no lost comments, corpus runs on real repositories, golden-snapshot triage, revision-to-revision diffs and performance regression. Use after any formatter change, when a snapshot or invariant test fails, when adding a corpus or benchmark, or before declaring a formatting task done.
---

# Formatter verification

A formatter's correctness is checkable by machine. Use the checks below instead of judging
output by eye. Prose rules drift; failing tests do not.

## The invariants

| # | Invariant | Check |
|---|---|---|
| 1 | Idempotent | Format the output again; the text must be identical. |
| 2 | Tree-preserving | Parse input and output with Roslyn; compare token streams / trees with trivia ignored. |
| 3 | No loss | Count comments and preprocessor directives before and after; they must match, in order. |
| 4 | Fail safe | On a parse error or internal failure, leave the file unchanged and exit non-zero. |
| 5 | Valid output | The output parses with no new diagnostics. |

Implement 1 to 3 and 5 as one reusable assertion helper that every test layer calls, so no test
can format without also checking the invariants.

## Test layers

1. **Golden fixtures per construct.** `tests/golden/<area>/<name>.in.cs` plus the expected
   output, compared with Verify snapshots. One construct per fixture; name the fixture after the
   behavior.
2. **Doc-printer unit tests.** The printer primitives (group, indent, line, fill) get their own
   tests, independent of C#. Include a deep-nesting test for stack depth.
3. **Corpus run.** Format a pinned set of real repositories (record commit SHAs) with `--check`
   style semantics. Assert the invariants on every file and that no file crashes. Run it in CI
   on changes to formatter code.
4. **Revision diff.** Format the corpus with the base revision and with the PR, and diff the two
   outputs. A style change should show up as an intended, reviewable diff; an unexpected diff is
   a bug. Post the summary on the PR.
5. **Performance.** Benchmark on a named corpus, cold and warm. Gate CI on a regression
   threshold, not an absolute number, because shared runners are noisy.
6. **Later:** fuzzing by mutating fixtures (removing or adding line breaks, truncating files) and
   asserting the invariants still hold or the failure is clean.

## Snapshot triage

When a snapshot test fails:

1. Read the `.received.` file against the `.verified.` file.
2. Decide intent. Is the new output the behavior you meant to ship?
   - **Yes:** accept it deliberately, and add a style-changelog entry if stable output changed.
   - **No:** fix the code. Never edit a `.verified.` file by hand to make a test pass.
3. Scrub non-deterministic values (paths, timestamps) rather than accepting them.
4. In non-interactive runs, disable diff tools (`DiffEngine_Disabled=true`).

## Reporting results

State what ran and what it covered: number of fixtures, number of corpus files, which
invariants were asserted, and any skipped or filtered tests. Do not report success for a gate
that did not run. A failing invariant on the corpus is a blocker, not a flake: save the smallest
reproducing input as a new fixture before fixing.

## Known C# pitfalls to cover (verify each against current Roslyn behavior)

- Trivia and comment ownership when line breaks change
- Preprocessor directives (`#if` branches may be incomplete code; decide the strategy and test it)
- Verbatim, interpolated and raw string literals must never be reflowed
- BOM and mixed line endings
- Very deep expressions and long operator chains (stack depth)
- Newer syntax: file-scoped namespaces, records, primary constructors, collection expressions,
  raw strings, top-level statements, `.csx`

## Pairs with

- `add-formatting-rule` for the step-by-step procedure to add a rule
- `prd-and-requirements` for turning an invariant into a requirement with a test anchor
