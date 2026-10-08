# 0011. Format the no-symbols parse and keep preprocessor directives as lines

- **Status:** Accepted
- **Acceptance:** accepted provisionally on 2026-10-05 under the author's delegation ("decide, I will review later"),
  after an independent review (see the pull request). Pending the author's review.
- **Date:** 2026-10-05
- **Deciders:** countrymanprime
- **Related:** [PRD](../PRD.md) (open question 2), [ADR 0008](0008-print-unsupported-syntax-verbatim.md) (rule 3 replaced here), [ADR 0010](0010-keep-comments-at-node-boundaries.md), requirements FMT-011, FMT-017

## Context and problem

Roslyn parses a file for one set of preprocessor symbols. With none defined, the code in an `#if SYMBOL` branch is
not parsed: it is "disabled text" trivia attached to the next token. The PRD asks (open question 2) whether to format
per branch, skip regions, or run once per symbol set. ADR 0008 chose to copy any file that contains `#if`,
`#region` and their relatives, because the hazard spans nodes. That leaves a large share of real files untouched.

## Decision drivers

- Never change what the file means under any symbol set, not only the one that was parsed.
- Be useful on files that use `#if`.
- Cost: one parse per file, as now ([ADR 0006](0006-formatting-speed-baseline.md)).

## Considered options

1. **Format the parse with no symbols defined; keep directives and disabled text as lines in the trivia model of
   ADR 0010** (chosen).
2. Keep copying the whole file when it contains a conditional directive (ADR 0008 rule 3).
3. Parse once per combination of symbols and merge the results. Correct in principle, exponential in the number of
   symbols, and merging layouts from different trees is unsolved.
4. Format only the code outside conditional blocks.

## Decision outcome

**Chosen option 1**, because layout changes cannot change meaning under any symbol set, as long as every directive
and every disabled line survives in order on lines of its own, and the output check ([ADR 0009](0009-self-check-output-before-writing.md))
compares the sequence of tokens, comments and directives.

- A directive is trivia. It is handled by the positions in ADR 0010: own-line above an item, before a closing `}`,
  or at the end of the file. A directive in any other position (inside a header, a parameter list, an expression)
  makes the node that owns it verbatim; the output check is not what decides this.
- Disabled text is printed as it is, from column 0, in place, with its line endings. It is never re-indented or
  reformatted, because it was not parsed.
- Only the active branch (no symbols) is formatted. Code in an inactive branch is formatted the day someone builds
  with the symbol and runs the tool, or never; this is stated in `docs/style.md`.
- The whole-file rule for `#if`, `#elif`, `#else`, `#endif`, `#region` and `#endregion` is removed. What remains
  of the whole-file rules is: a file with nothing but trivia is copied.
- Directives that change line numbers (`#line`) keep their text; formatting moves physical lines, so a `#line`
  mapping that was relative to the old layout is not adjusted. `#line` is rare and only affects debugger and
  diagnostic positions.

### Consequences

- **Good:** files with `#if` are formatted; no multi-pass cost; no new option.
- **Bad:** inactive branches are never formatted, so a file can look inconsistent; a symbol set that makes the
  structure differ (`#if A class X : B #else class X : C #endif`) falls back to verbatim for that node, which keeps
  it correct but unformatted.
- **Bad:** this relies on the position rules being strict; any case where they wrongly accept a directive would be
  caught only by the output check, so the mutation test and the corpus run matter.
- **Neutral:** a future option to format with chosen symbols can be added without changing this decision.

### Confirmation

Fixtures: `#if` around members, usings, and statements-to-be; `#if` that splits a header; nested `#if`; `#region`
indentation; disabled text with odd indentation; a directive as the last item before `}` and at end of file. The
mutation test and the corpus run.

## More information

Revisit if real files show that unformatted inactive branches are a common complaint (then consider an
`.editorconfig` list of symbols to format with).
