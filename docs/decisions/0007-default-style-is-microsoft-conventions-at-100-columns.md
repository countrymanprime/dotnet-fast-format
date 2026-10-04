# 0007. Use Microsoft conventions at 100 columns as the default style

- **Status:** Accepted
- **Acceptance:** accepted provisionally on 2026-10-04 under the author's delegation while they were away, after an
  independent review that asked for the changes below (adopted). Pending the author's review.
- **Date:** 2026-10-04
- **Deciders:** countrymanprime
- **Related:** [PRD](../PRD.md) (open question 1), [ADR 0002](0002-build-a-doc-printer-on-roslyn-syntax-trees.md), [ADR 0006](0006-formatting-speed-baseline.md)

## Context and problem

A doc-printer wraps lines, so it needs a width and a layout for every construct even when no
`.editorconfig` key says anything about it. The PRD asks (open question 1) what the output follows when no key
applies. Milestone M1 writes the first golden fixtures, so the answer is needed before any expected output
exists. From M4 on, `.editorconfig` keys override these defaults; this ADR only sets what applies without them.

## Evidence

Measured on 2026-10-04 so the options rest on behavior, not memory.

| Source | What it says or does |
|---|---|
| [Microsoft C# coding conventions](https://github.com/dotnet/docs/blob/main/docs/csharp/fundamentals/coding-style/coding-conventions.md) | Four spaces, no tabs; Allman braces; one statement and one declaration per line; file-scoped namespaces; `using` directives outside the namespace; a blank line between method and property definitions. The only line-length figure is **65 characters, and it is for code samples in documentation**, not for real code. |
| CSharpier 1.3.0 (run on probe files) | Default width **100**: a 100-column line stays on one line and a 101-column line breaks. Allman braces, four-space indent, a blank line between members, `) { }` for an empty body. It reads `max_line_length` and `indent_size` from `.editorconfig` (with `max_line_length = 80` and `indent_size = 2`, an 88-column signature broke and indented two spaces). |
| `dotnet format` | Fixes whitespace and style violations against `.editorconfig` but does not re-wrap long lines, so it has no width. |

## Decision drivers

- The PRD goal: speed close to CSharpier with behavior driven by existing `.editorconfig` rules. A team
  migrating from CSharpier should see few surprises.
- Rest on a published convention where one exists.
- Few options: every default is a style decision we then maintain.

## Considered options

1. **Microsoft conventions, wrapping at 100 columns** (recommended).
2. Microsoft conventions, wrapping at 120 columns.
3. No wrapping, like `dotnet format`: whitespace only.
4. Reproduce CSharpier's output exactly.

## Decision outcome

**Chosen option: Microsoft conventions at 100 columns**, because the conventions cover layout and braces,
they leave the width open, and 100 matches the default of the tool the speed target is measured against.

- Layout: four spaces, no tabs; Allman braces; one statement and one declaration per line; an empty body
  printed as `{ }`.
- Blank lines between members: exactly one blank line between members, except between consecutive members of the
  same single-line kind (fields, auto-properties, abstract and interface method signatures, events without
  accessors), where the author's choice is kept: none or one blank line, more than one collapsed to one. The
  Microsoft convention asks for blank lines between method and property definitions, not between every pair of
  members. The rule depends on the member's kind, not on how it happens to wrap, so formatting stays stable when
  a line crosses the width.
- Wrapping: a construct stays on one line when it fits in the width, and otherwise breaks at its outermost group
  first (for example a parameter list breaks one parameter per line before anything inside a parameter does).
- Width: `max_line_length` from `.editorconfig` when set (M4), otherwise 100. Width is measured in UTF-16 code
  units, and a tab counts as the indent size once tabs are supported. Verbatim text ([ADR 0008](0008-print-unsupported-syntax-verbatim.md))
  is never reflowed; it counts towards the width only up to its first line break.
- The formatter does not move or sort `using` directives and does not rewrite namespaces: that is a style fix,
  not layout.
- Each rule is written down per construct in `docs/style.md`, which grows with the fixtures.

Not chosen: 120 columns has no published basis (the only Microsoft figure is 65, for documentation), and no
wrapping would make this a whitespace tool, giving up the doc-printer in ADR 0002. Reproducing CSharpier exactly
would tie our output to a tool that changes on its own schedule.

### Consequences

- **Good:** output is close to what CSharpier users already have; layout follows a published convention.
- **Bad:** 100 is our choice, not Microsoft's; some teams will set `max_line_length` anyway, which is what the
  `.editorconfig` support is for.
- **Bad:** until M4 the defaults are hard-coded, so formatting the corpus before then produces diffs against
  repositories that configure something else.
- **Neutral:** changing a default later is a visible style change and needs a style-changelog entry.

### Confirmation

Golden fixtures under `tests/golden/` (written by hand from `docs/style.md`, not generated) and the invariants
on the corpus.

## More information

Revisit if CSharpier changes its default width, or if usage data shows most users configure a different width.
