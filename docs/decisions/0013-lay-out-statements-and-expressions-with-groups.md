# 0013. Lay out statements and expressions with groups, leading operators and last-argument hugging

- **Status:** Accepted
- **Acceptance:** accepted provisionally on 2026-10-05, decided under delegation while the author was away. Pending the
  author's review; the layouts below are choices, not facts, and each is cheap to change before a stable release.
- **Date:** 2026-10-05
- **Deciders:** countrymanprime
- **Related:** [ADR 0002](0002-build-a-doc-printer-on-roslyn-syntax-trees.md), [ADR 0007](0007-default-style-is-microsoft-conventions-at-100-columns.md),
  [ADR 0014](0014-statements-and-expressions-reuse-the-trivia-model.md), requirements FMT-020 to FMT-030

## Context and problem

M2 prints statements and expressions. ADR 0007 fixed the defaults (Allman braces, four spaces, 100 columns, wrap
the outermost group first) but no construct below the member level has a layout yet, and a layout has to be chosen for
every one before the first fixture exists: where a long condition breaks, where a binary operator goes, what happens
to a call whose last argument is a lambda with a body, how a chain of calls wraps. The formatter also may not add or
remove a token ([ADR 0009](0009-self-check-output-before-writing.md) compares tokens), which rules out the choices
other formatters make by adding braces, parentheses or trailing commas.

## Evidence

Behavior of CSharpier 1.3.0 and Prettier's published algorithms (the model ADR 0002 names), as known when this was
decided; no probe was run for each case, so treat them as inputs to a choice, not as a specification:

| Case | Prettier / CSharpier |
|---|---|
| Group that does not fit | Breaks the outermost group first; inner groups are measured again after the break. |
| Binary operator | Prettier ends the line with the operator; CSharpier starts the continuation line with it. |
| Last argument is a function | Prettier tries three layouts in order: everything flat, all arguments but the last flat and the last one broken open, every argument on its own line. |
| Call chain | Prettier breaks every `.call()` onto its own line when the chain has more than two call groups and does not fit, or any group but the last must break; shorter chains stay on one line and break inside the last call. |
| Assignment | Prettier picks a layout from the right-hand side: break after `=` for binary and long string values, keep on the line for calls, objects and functions. |

## Decision drivers

- A layout depends only on the document and the width, so formatting twice gives the same text.
- Stay close to what Microsoft-style C# looks like and to what CSharpier users already have; do not copy either exactly.
- No added or removed tokens.
- Every layout must be expressible in the doc printer with a small number of new primitives.

## Considered options

1. **Reflow by width with Prettier-style groups, leading operators, hugging and a member-chain rule** (chosen).
2. Keep the author's line breaks and only fix indentation and spacing (what `dotnet format` does). No wrapping, so no
   doc printer, which ADR 0007 already rejected.
3. Reproduce CSharpier's output exactly. Ties the output to a tool that changes on its own schedule (ADR 0007).

## Decision outcome

**Chosen option 1.** The layouts are listed in `docs/style.md`; the decisions that shape them are these.

1. **Statements.** One statement per line. A block is `{`, the statements indented one level, `}`, with the braces on
   their own lines (Allman) wherever it appears: after `if (...)`, `else`, `try`, `catch`, a lambda's `=>`. An empty
   block is `{ }` on the line of its header. A body that is not a block is indented one level on the next line. `else if`
   stays on one line.
2. **Headers.** The parenthesized part of `if`, `while`, `for`, `foreach`, `switch`, `using`, `lock` and `catch` is one
   group: flat when it fits, otherwise its content on its own indented line with `)` after it, so the content then gets
   the width again.
3. **Operators.** A chain of the same binary operator is one group. When it breaks, every operator breaks, and the
   operator starts the continuation line (`&& b`). In a header's parentheses the continuation lines are not indented
   further; elsewhere they are indented one level. A `//` comment cannot be swallowed because a comment inside the
   expression makes the whole statement verbatim (ADR 0014).
4. **Arguments.** One group per argument list: all on one line, or one per line with `)` on its own line, as for
   parameter lists (no trailing comma, since a token cannot be added). When the last argument is a lambda, an
   anonymous method, or an object creation with an initializer, and no earlier argument contains a forced break, the list is a
   conditional group of three layouts: everything flat, the first arguments flat with the last one opened on the call's
   line (a block or initializer opens below it; an expression body goes on the next line with `)` under it), one argument
   per line. That is what makes `Task.Run(() =>` followed by a block, the common shape in real code, print without breaking
   the other arguments. An argument that holds a forced break inside a nested call puts every argument on its own line.
5. **Member chains.** A chain is a head (the root and what is glued to it: calls and indexers directly after the root,
   and member accesses that are followed by another member access) and call groups (a member access followed by calls and
   indexers, up to the next member access after a call). A root that is `this`, `base`, a predefined type, a capitalized
   name (`Enumerable.Range(...)`) or, in an expression statement, a name of at most four characters (`sb.Append(...)`) keeps
   its first group on its line. With at most two groups after the head (three when the first is kept on its line) the
   chain is printed as written, with line breaks only inside argument lists. With more, it is a conditional group: one
   line when it fits, otherwise the head, then each group on its own line indented one level, starting with `.`. A chain whose non-last
   group contains a forced break (a block lambda in the middle) is always broken. Members after the last call stay on that
   call's line. The member-name heuristic of Prettier (a capitalized property means a factory) is not used, because
   every C# member is capitalized.
6. **Assignments.** `lhs = value` keeps the value on the line when it is a call, chain, creation, initializer, lambda,
   cast, parenthesized value, a conditional whose condition is not a binary expression, or anything else that can break
   inside itself. A binary or logical value, a conditional whose condition is one, a single-line string and a plain
   member access (`a.b.c`) move to the next line, indented, when the whole does not fit, with the operators then
   wrapping inside the indented value. The same rule applies to field and property initializers, switch arms (after
   `=>`), and enum members. An expression body (`=> value`) and a lambda body go on the next line when they do not fit,
   unless the value opens with braces or brackets.
7. **Conditionals.** `c ? a : b` is one group; when it breaks, `?` and `:` start lines indented one level under the condition.
8. **Initializers.** `new T { a, b }` is one group: flat when it fits, otherwise `{` on its own line (Allman), one
   element per line. A trailing comma already in the source is a token and stays, and it forces the multi-line form so
   the comma is always where a comma can be.
9. **Strings.** Regular, verbatim, interpolated and raw literals are copied token for token. A multi-line one is verbatim
   text whose own lines are never re-indented; for the layout around it, only its first line counts, and what follows it is
   measured from the column where its last line ends. It does not force the call or operator chain around it to break.
   (Other copied text that spans lines does force the groups around it to break, as in ADR 0008.)
10. **Doc printer.** Two primitives are added: a group that always breaks, and a conditional group that tries a
   list of layouts in order (the first that fits wins; a layout that contains a forced break counts as fitting when the
   text before the break fits) and prints the last one broken if none fits. A conditional group does not pass a forced
   break on to the groups around it, so a call with a block lambda inside another call does not break the outer one
   by itself; a printer that wants that asks whether a document will break (`WillBreak`).
11. **Members.** Attribute lists are printed one per line above their member; a property whose accessors have bodies prints
   them one per line; enums print one member per line. Directives and comments on lines above an opening brace or an
   attribute's successor, and a comment after an opening brace, are printed where they are (ADR 0014).

### Consequences

- **Good:** every long construct has a defined, width-driven layout; the common block-lambda argument is handled; no
  token is added.
- **Good:** one printer for blocks and one for argument lists serve every construct that uses them.
- **Bad:** these are our choices. Some will differ from CSharpier (for example, no trailing commas are added, and
  the break after `=` for a binary value) and a team migrating will see diffs.
- **Bad:** a conditional group measures a layout more than once, so deeply nested hugging arguments cost more than a plain group.
  The benchmark in ADR 0006 tracks it.
- **Neutral:** later `.editorconfig` support may make a few of these configurable; this ADR sets only the defaults.

### Confirmation

Fixtures under `tests/golden/statements/` and `tests/golden/expressions/`, one per construct and one per width case, and the
invariants on every fixture, on this repository's own files and on the corpus.

## More information

Revisit after the first review of corpus diffs by the author, and when `.editorconfig` keys for wrapping arrive (M4).
