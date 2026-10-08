# 0014. Statements and expressions reuse the trivia model; copy per statement or per expression

- **Status:** Accepted
- **Acceptance:** accepted provisionally on 2026-10-05, decided under delegation while the author was away. Pending the
  author's review.
- **Date:** 2026-10-05
- **Deciders:** countrymanprime
- **Related:** [ADR 0008](0008-print-unsupported-syntax-verbatim.md), [ADR 0010](0010-keep-comments-at-node-boundaries.md),
  [ADR 0011](0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md), [ADR 0013](0013-lay-out-statements-and-expressions-with-groups.md),
  requirements FMT-020, FMT-029

## Context and problem

ADR 0010 handles comments and directives for the items of a list of siblings (usings, members, top-level statements)
and says statements reuse that mechanism. Statements add two things it did not have to settle: a statement can be the
only child of another statement (`if (x) return;`), and the last token of a statement is also the last token of
the statement that contains it (`}` of an `else` block ends the `if`). Whoever prints the comment after that token must
be exactly one printer, or the comment is printed twice or not at all. Expressions add comments in the middle of a
line, where moving the line break would change which token a `//` comment ends.

## Decision drivers

- Invariant 3 (no loss) by construction, with the output check as backstop only.
- One mechanism for comments (ADR 0010), not a second one for statements.
- The fallback unit is small: a statement, an expression, never a file.

## Considered options

1. **Statement lists are lists; an embedded statement is a list of one; the statement that ends on a token owns
   its trailing comment, and a child that ends on the same token leaves it to that statement; any comment on an inner
   token copies the statement** (chosen).
2. Attach every comment inside a statement to a token and print it wherever that token goes (ADR 0010 option 2).
3. Copy every statement that contains a comment anywhere inside it.

## Decision outcome

**Chosen option 1.**

- **Lists.** The statements of a block, a switch section and a top-level program are printed by the same list as
  members: own-line comments and directives above each, one trailing comment after each, closing lines before `}`.
  A non-block child of `if`, `else`, `for`, `foreach`, `while`, `do`, `using`, `lock`, a label, a `catch` or `finally`
  block is printed as a list of one, so a comment above it is kept above it.
- **One owner for the last token.** A node's trailing comment is printed by the outermost node that ends on that token.
  A child that shares its last token with its parent does not read that comment. A child that ends before the parent's
  last token (the `if` body when there is an `else`) prints its own.
- **Inner tokens.** The tokens a printer writes itself (keywords, parentheses, operators, names) must carry no comment
  or directive except the node's first token (printed above by the list) and last token (printed after by the list). If one does, the
  node is copied as written. Lists nested inside the node (a lambda's block) are independent and keep formatting.
- **Fallback units.** A statement kind with no printer is copied as one statement. An expression kind with no printer
  is copied as one expression and the statement around it is still printed. A copied child never repeats trivia
  the list prints, and a copied child whose last token carries a `//` comment, followed by more code on its line, makes
  the enclosing node copied instead (the comment would swallow the code).
- **Lines above a brace or after an attribute list.** Own-line comments and directives before an opening brace (an `#if`
  between a header and its body) or before the token that follows an attribute list (an `#if` around an attribute) are
  printed on lines of their own, directives at column 0, and a comment after an opening brace or after an attribute
  list stays on its line. This extends ADR 0011: a `#if` between a header and its `{` no longer copies the declaration.
  The same shapes inside a header, parameter list or expression still copy the node.
- **Multi-line tokens.** A token that holds a line break (verbatim, raw and interpolated strings) is emitted as
  verbatim text and never makes the node copied.

### Consequences

- **Good:** no second comment mechanism; the statement fixtures reuse the M3 fixtures' rules; invariants hold by construction.
- **Good:** a comment inside a lambda body does not stop the call around it from being formatted.
- **Bad:** one comment in an argument list copies the whole statement that contains the call, which keeps long
  commented calls unformatted until a later milestone formats comments inside expressions.
- **Neutral:** `NodePrinter` gains a parameter for a node whose last token is printed by an ancestor.

### Confirmation

Fixtures for a comment above, after, between `}` and `else`, inside a header and inside an argument list; the mutation test over this repository's files and
the corpus; the output check.

## More information

Revisit when a milestone formats comments inside expressions and headers.
