# 0010. Keep comments at node boundaries and copy every other position verbatim

- **Status:** Accepted
- **Acceptance:** accepted provisionally on 2026-10-05 under the author's delegation ("decide, I will review later"),
  after an independent review (see the pull request). Pending the author's review.
- **Date:** 2026-10-05
- **Deciders:** countrymanprime
- **Related:** [ADR 0008](0008-print-unsupported-syntax-verbatim.md) (rule 2 amended here), [ADR 0011](0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md), [ADR 0009](0009-self-check-output-before-writing.md), requirements FMT-003, FMT-009, FMT-014 to FMT-018

## Context and problem

[ADR 0008](0008-print-unsupported-syntax-verbatim.md) made M1 safe by copying any node with a comment or directive on
one of its own tokens, whole. That is correct but weak: a type with a doc comment above it is not formatted at all,
and neither are its members, so most real files stay largely untouched. Roslyn attaches trivia to tokens, and
deciding who owns a comment once line breaks change is the main risk in the PRD. M3 has to format code that has
comments without ever dropping, duplicating or moving one across code (invariant 3).

## Evidence

Measured on 2026-10-05 with CSharpier 1.3.0 on probe files, as in ADR 0007:

| Case | CSharpier 1.3.0 |
|---|---|
| Own-line `//` and `///` comments above a member | kept above it, re-indented |
| `int x;   // note` | `int x; // note` (one space; the comment's own text is untouched) |
| Blank lines above a comment | one kept, more collapsed to one |
| Comment before `}` | kept, inside the body |
| `#if` / `#pragma` / `#nullable` | printed at column 0; `#region` / `#endregion` indented with the code |
| Comment inside a parameter list | kept on its line, the list is broken |
| `/* before */ int c;` | `/* before */int c;` |

## Decision drivers

- Invariant 3 must hold by construction, with the output check ([ADR 0009](0009-self-check-output-before-writing.md))
  as backstop, not as the mechanism.
- Format the common shapes (the ones that make up nearly every real comment) and leave the rest alone.
- One mechanism for every list of siblings (declarations now, statements in M2).

## Considered options

1. **Classify trivia by position; format the common positions, copy the rest** (chosen).
2. Attach every comment to a token and re-emit it wherever that token goes (Prettier's model). Most complete, and the
   most code and the most risk; every printer must then handle comments in every position.
3. Keep ADR 0008 as it is until a later milestone.

## Decision outcome

**Chosen option 1.** Trivia is read in four positions, for a node that is an item of a list of siblings
(a using, extern alias, member or top-level statement):

1. **Leading lines.** The trivia before the first token of an item, when it consists only of whitespace, line ends
   and *own-line* items: a `//` or `///` comment, a `/* */` comment that ends its line, a preprocessor directive, or
   disabled text. Each is printed on its own line above the item, in order.
2. **Trailing comment.** One comment that follows the last token of the item on the same line (a `//` comment, or
   a `/* */` comment on one line). It is printed after the item, separated by one space.
3. **Closing lines.** The leading trivia of a closing `}` (and of the end of the file), when it is own-line items as
   in 1. They are printed as the last lines of the body, at the body's indentation.
4. **Anything else is verbatim.** A comment inside a header, between a `)` and a `{`, inside a parameter list, after
   a `{` on its line, two comments after one token, a comment followed by code on its line, or a `/* */` comment
   that spans lines in trailing position. The item that owns it prints as ADR 0008 says: its full text, trimmed.

**Printing rules.**

- Blank lines: the author's choice of none or one is kept between an own-line item and its neighbours (more than one
  becomes one); none is added. A blank line is never inserted between a comment and the item below it.
- Indentation: own-line comments are re-indented to the item's level. The first line of a multi-line `/* */`
  comment is re-indented; its other lines are not touched (a re-indent could change a comment that is aligned on
  purpose, and the text is data we do not own).
- The text of a comment is never changed, including its inner whitespace. Directives are trimmed of trailing
  whitespace only.
- Directives are written at column 0, except `#region` and `#endregion`, which are indented with the code. This is
  CSharpier's behavior, and it keeps a `#if` block readable when the code inside is indented.
- A node whose own tokens (those between the first and last, excluding items that are separately printed) carry a
  comment or directive is verbatim as a whole, as before.

**Where verbatim children sit.** A verbatim child inside a formatted node (a method body, an initializer value) never
includes the trailing comment of the node's last token, because the list prints it.

### Consequences

- **Good:** a type, namespace or member with a doc comment, an end-of-line comment or comments before `}` is now
  formatted, and everything but position 4 stays formatted. The invariants hold by construction for the formatted
  positions and by copying for the rest.
- **Good:** statements (M2) reuse the same list mechanism.
- **Bad:** comments aligned in a column (`int a;   // x`) lose their alignment; comments in headers and parameter
  lists keep their node verbatim until a later milestone.
- **Bad:** more states means more places to get wrong; this is why the output check stays on and a mutation test
  (comments inserted at many token boundaries) is part of the change.
- **Neutral:** printed output changes for files that used to be verbatim, so it needs a style-changelog entry.

### Confirmation

Fixtures for every position and for every case that falls back to verbatim; a mutation test that inserts comments at
token boundaries of this repository's own files and checks every invariant; the corpus run.

## More information

Revisit when a milestone formats comments inside headers and parameter lists, and when `/* */` continuation lines
need re-indenting.
