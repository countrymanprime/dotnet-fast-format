# 0008. Print unsupported syntax and trivia-bearing nodes verbatim

- **Status:** Accepted
- **Acceptance:** accepted provisionally on 2026-10-04 under the author's delegation while they were away, after an
  independent review that asked for the changes below (adopted). Pending the author's review.
- **Date:** 2026-10-04
- **Amended:** 2026-10-05. Rule 2 (nodes with comments) is narrowed by [ADR 0010](0010-keep-comments-at-node-boundaries.md) and rule 3
  (whole-file cases) is narrowed by [ADR 0011](0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md). The rest stands.
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

1. **Print such nodes verbatim, per node, with a few whole-file cases** (chosen).
2. Reject the whole file with an error when it contains anything unsupported.
3. Print the whole file verbatim whenever it contains anything unsupported.

## Decision outcome

**Chosen option: verbatim fallback, per node, with whole-file cases for the hazards that span nodes**, because it keeps the invariants true by construction for the
copied text and lets each milestone shrink the fallback instead of rewriting it.

**The unit.** The fallback applies to a syntax node and is never finer than a node: a declaration (using
directive, namespace, type, member) or a node inside one that has no printer, such as a method body. It never
applies to a single token.

**What triggers it.**

1. A node kind with no printer yet.
2. A node that carries a comment or a directive: its leading trivia, its trailing trivia, or the trivia on any
   of its own tokens (not the tokens of its child nodes, which are judged on their own) contains a comment
   (single-line, multi-line or documentation) or a preprocessor directive. Whitespace-only trivia does not
   trigger it. The smallest such node is the one printed verbatim, so a class with one commented member prints the
   class normally and that member verbatim.
3. Whole file, because the hazard spans nodes: the compilation unit is printed verbatim when the file contains a
   conditional or region directive (`#if`, `#elif`, `#else`, `#endif`, `#region`, `#endregion`) anywhere, when the
   end-of-file token carries a comment or a directive, or when the file has no declarations (empty,
   whitespace-only or comment-only). Standalone directives (`#nullable`, `#pragma`, `#line` and so on) trigger
   rule 2 only.

**What verbatim means.**

- A node's verbatim text is its full source text with leading and trailing whitespace removed. Comments before
  the node stay with it, and a comment that trails its last token stays with it.
- The parent prints every separator between its children: one line break, or a blank line where the style
  requires one or the author had one. Blank lines are counted from the end-of-line trivia in the node's leading
  trivia before its first comment. A node's trivia is never printed twice.
- Verbatim text is copied as written. It is not re-indented, and its line terminators are not converted,
  because converting them inside a multi-line string literal would change the string's value.
- The line breaks the printer writes use the file's dominant line terminator (LF unless CRLF is more common), and
  formatted output ends with exactly one line terminator. Both are stable across passes.
- The fallback is normal behavior, not an error: the exit code is 0.

### Consequences

- **Good:** comments and directives cannot be dropped or moved by M1; each later printer is a self-contained
  change that removes cases from the fallback; partial formatting is available from M1.
- **Bad:** formatted and verbatim regions can look inconsistent in one file; a file mostly made of the fallback
  changes little, so M1's benchmark row will say little about speed.
- **Bad:** a bug in deciding what counts as "has a comment" could still lose one, so every fixture with
  comments in unusual positions is mandatory (the `add-formatting-rule` skill already asks for them), and each
  printer's fixtures must include a formatted node next to a verbatim sibling to prove idempotency at the boundary.
- **Bad:** a file with any `#if` or `#region` is not formatted at all in M1, which covers many real files; M3 and
  the preprocessor ADR lift this.
- **Neutral:** idempotency still has to be tested at the boundary between formatted and verbatim regions.

### Confirmation

FMT-008 and FMT-009 in `docs/requirements/core.md`, and the invariants on every fixture and the corpus.

## More information

Revisit when M3 lands trivia handling; the comment trigger then goes away, and the preprocessor strategy ADR
decides what happens to directives.
