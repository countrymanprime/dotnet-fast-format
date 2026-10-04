# 0008. Print unsupported syntax and trivia-bearing nodes verbatim

- **Status:** Proposed
- **Date:** 2026-10-04
- **Deciders:** countrymanprime
- **Related:** [ADR 0002](0002-build-a-doc-printer-on-roslyn-syntax-trees.md), requirements FMT-008 and FMT-009

## Context and problem

Milestone M1 prints namespaces, usings, type declarations and member signatures. Statements and expressions
arrive in M2 and comments and trivia in M3, and the preprocessor strategy is not decided (PRD open question 2).
Real files contain all of it, so the formatter needs a defined behavior for syntax it cannot print yet, without
breaking the invariants (idempotent, tree-preserving, no loss) or waiting until every construct exists.

## Decision drivers

- Never lose or move a comment or directive, before the milestone that handles them.
- Be useful early: format what is supported in a file that also contains unsupported code.
- Keep each later milestone a small step, so a new printer only removes cases from the fallback.

## Considered options

1. **Print such nodes verbatim, per node** (recommended).
2. Reject the whole file with an error when it contains anything unsupported.
3. Print the whole file verbatim whenever it contains anything unsupported.

## Decision outcome (proposed)

**Chosen option: verbatim fallback, per node**, because it keeps the invariants true by construction for the
copied text and lets each milestone shrink the fallback instead of rewriting it.

- A node kind with no printer yet is emitted as its original source text, exactly.
- A node whose leading or trailing trivia contains a comment or a preprocessor directive is emitted as its
  original source text, including that trivia, until trivia handling lands (M3). Whitespace-only trivia does not
  trigger the fallback.
- The fallback applies at the smallest node that has the problem: a class with one commented member prints the
  class normally and that one member verbatim.
- Verbatim text is copied as written, including its own indentation; it is not re-indented. A body printed
  verbatim next to a reformatted signature may look inconsistent until the milestone that formats it.
- The fallback is a normal, documented behavior, not an error: the exit code is 0.

### Consequences

- **Good:** comments and directives cannot be dropped or moved by M1; each later printer is a self-contained
  change that removes cases from the fallback; partial formatting is available from M1.
- **Bad:** formatted and verbatim regions can look inconsistent in one file; a file mostly made of the fallback
  changes little, so M1's benchmark row will say little about speed.
- **Bad:** a bug in deciding what counts as "has a comment" could still lose one, so every fixture with
  comments in unusual positions is mandatory (the `add-formatting-rule` skill already asks for them).
- **Neutral:** idempotency still has to be tested at the boundary between formatted and verbatim regions.

### Confirmation

FMT-008 and FMT-009 in `docs/requirements/core.md`, and the invariants on every fixture and the corpus.

## More information

Revisit when M3 lands trivia handling; the comment trigger then goes away, and the preprocessor strategy ADR
decides what happens to directives.
