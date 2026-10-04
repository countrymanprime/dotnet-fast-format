# 0009. Check the output before writing a file

- **Status:** Accepted
- **Acceptance:** accepted provisionally on 2026-10-04 under the author's delegation while they were away, after an
  independent review that asked for the changes below (adopted). Pending the author's review.
- **Date:** 2026-10-04
- **Deciders:** countrymanprime
- **Related:** [AGENTS.md](../../AGENTS.md) (invariants 2 and 4), [ADR 0006](0006-formatting-speed-baseline.md), requirement FMT-010

## Context and problem

The invariants are checked by tests, but a test can only cover the inputs it has. A formatter bug that changes
meaning on a file nobody tested would write that file. Invariant 4 says an error leaves the file untouched; a
silent wrong output is not an error unless something detects it. Detecting it costs a second parse of every
output.

## Decision drivers

- Formatting must never change program meaning (the first rule in `AGENTS.md`).
- The speed target in [ADR 0006](0006-formatting-speed-baseline.md): at most 1.5 times CSharpier, never slower
  than `dotnet format whitespace`.
- Few options and flags (the option set is intentionally small).

## Considered options

1. **Always re-parse the output and compare tokens with the input's before writing** (recommended).
2. Do it only when a flag asks for it.
3. Never; rely on tests and the corpus.

## Decision outcome

**Chosen option: always check, no flag**, because the check is what turns invariant 2 from a test-time property
into a guarantee for every file, and the cost is bounded by one extra parse.

- The check is one function, `OutputVerifier` in Core. The command line calls it before writing, and the test
  helper `Invariants` calls the same function, so there is one definition of "unchanged apart from layout".
- It requires, comparing the input with the output: (1) no error diagnostics the input did not have, counted by
  diagnostic ID; (2) the same token sequence, comparing each token's kind and text (text alone would miss a `>>`
  that became `> >`); (3) the same sequence of code tokens interleaved with comment and directive trivia, so a
  comment that moves across code fails, with each line trimmed. In M1 comments are copied verbatim so nothing
  inside them changes; the trimming is revisited in M3 when the formatter may re-indent a multi-line comment.
  String, verbatim-string and raw-string contents are tokens, so they are compared exactly.
- Both sides are parsed with the same options, one shared constant (`LanguageVersion.Preview`), or the check
  would raise false alarms.
- The check is skipped when the input has syntax errors, because such a file is rejected before printing.
- On a difference the file is left unchanged, the file is named, and the exit code is 2.
- Idempotency is checked by the tests, not at run time: formatting twice would double the cost.
- Measure the cost with the benchmark harness before M1 closes (task T-109). If it breaks the ADR 0006 target,
  reconsider options 2 or 3 in a new ADR; do not add the flag preemptively.

### Consequences

- **Good:** a formatter bug cannot silently change a file's meaning; failures name the file, which makes bug
  reports cheap.
- **Bad:** roughly one extra parse per file. How much that is of total time is unknown until measured.
- **Neutral:** the same comparison code is shared with the test helper `Invariants`, so there is one definition
  of "unchanged apart from layout".

### Confirmation

FMT-010 (a deliberately broken formatter must be blocked and exit 2) and the speed measurement.

## More information

Revisit if the measured cost threatens the speed target.
