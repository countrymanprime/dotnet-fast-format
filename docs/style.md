# Output style

What the formatter prints with no `.editorconfig` key applying (see
[ADR 0007](decisions/0007-default-style-is-microsoft-conventions-at-100-columns.md)). One section per construct,
added with the fixture that pins it. Fixtures live in `tests/DotnetFastFormat.Tests/golden/`.

## Whole file

- Indent four spaces; the width is 100 columns (UTF-16 code units).
- Lines end with the file's dominant line ending (LF unless CRLF is more common); the file ends with exactly one.
- An empty or whitespace-only file stays empty.
- A file is kept as written when it has `#if`, `#elif`, `#else`, `#endif`, `#region` or `#endregion`, no
  declarations, or a comment or directive after its last token ([ADR 0008](decisions/0008-print-unsupported-syntax-verbatim.md)).
- A node with a comment or directive on one of its own tokens (including a comment above it) is kept as written,
  with everything inside it. Its neighbours are still formatted. This goes away when comment handling lands (M3).

## Using directives and extern aliases

- Tokens are separated by single spaces except around `.`, `::`, `,`, `<`, `>`, `;`.
- Directives are never sorted or moved.
- Between two neighbouring directives the author's choice is kept: no blank line or one (more than one becomes one).
  Between a directive and anything else there is one blank line.

## Namespaces

- A block namespace puts `{` and `}` on their own lines and indents the contents one level.
- An empty block namespace prints as `namespace N { }`.
- A file-scoped namespace prints `namespace N;`, a blank line, then its contents at the left margin.
- A namespace with attributes (a syntax error in practice) is kept as written.

## Blank lines between siblings

The rule depends on the kind of the two neighbours, not on how they wrap:

- Two neighbours of the same kind in {using directive, field, property without accessor bodies, method without a
  body, event without accessors, top-level statement}: the author's choice, none or one.
- Any other pair: exactly one blank line.
- None before the first or after the last item in a block.
