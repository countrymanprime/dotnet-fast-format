# Output style

What the formatter prints with no `.editorconfig` key applying (see
[ADR 0007](decisions/0007-default-style-is-microsoft-conventions-at-100-columns.md)). One section per construct,
added with the fixture that pins it. Fixtures live in `tests/DotnetFastFormat.Tests/golden/`.

## Whole file

- Indent four spaces; the width is 100 columns (UTF-16 code units).
- Lines end with the file's dominant line ending (LF unless CRLF is more common); the file ends with exactly one.
- An empty or whitespace-only file stays empty.
- A file is kept as written when it holds nothing but whitespace, comments and directives, or when it has a
  `#line` directive that remaps line numbers (`#line 100`, `#line (1,1)-(2,2)`; `#line hidden` and `#line default`
  are fine) ([ADR 0008](decisions/0008-print-unsupported-syntax-verbatim.md), [ADR 0011](decisions/0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md)).
- A node with a comment or directive in a position the next section does not list is kept as written, with
  everything inside it. Its neighbours are still formatted.

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

## Type declarations

Classes, structs, interfaces and records (including `record struct`).

- Modifiers, keyword, name and type parameters are separated by single spaces; no space inside `<...>`, and one
  after each comma.
- The base list is `: A, B` on the header line. When the header (name, type parameters, parameters, base list and
  constraints) is longer than 100 columns, every base type goes on its own line, indented one level, after
  `Name :`.
- Constraint clauses follow on the same line when everything fits, and otherwise each `where` clause is on its
  own line, indented one level, after the base list.
- An empty body prints `{ }` on the header's last line, or on its own line below a header that wrapped.
- A non-empty body puts `{` and `}` on their own lines; members are indented one level and separated by the
  blank-line rule above.
- A record with a parameter list (positional record) prints the list like method parameters: `(int A, int B)` on
  one line when it fits, otherwise one parameter per line, with `)` on its own line. A record without a body ends
  in `;`.
- Parameter defaults print only when they are a literal, a name, a member access or a signed number; a record with
  any other default is kept as written, and so is a type with attributes, a primary-constructor base call
  (`: Base(x)`) or attributes on a type parameter.
- Members are kept as written until their printer exists; only the first line of a kept member is re-indented, its
  later lines keep the indentation they had.

## Members

Fields, properties with no accessor bodies (or an expression body), methods and constructors. Every other member
(events, indexers, operators, enums, delegates, destructors) and any member with attributes is kept as written.

- Fields print `modifiers Type name = value;` with single spaces, `a, b` for several declarators, and the value
  kept as written.
- An auto-property prints `Type Name { get; set; }` on one line, then `= value;` when it has an initializer. A
  property with an accessor that has a body is kept as written.
- A method or constructor signature stays on one line when it fits in 100 columns. When it does not, the
  parameters go one per line, indented one level, with `)` on its own line. Parameters follow the default rules
  under "Type declarations".
- Constraint clauses of a method follow the signature on the same line when everything fits, and otherwise sit
  one per line, indented one level.
- A method or constructor with no body ends in `;`. An expression body prints ` => value;` on the signature's last
  line, with the value kept as written.
- A block body is printed on the line below the signature (Allman) and kept as written until statements are
  supported (M2): its lines keep the indentation they had, so a body written for a different indent looks
  misaligned. An empty block prints `{ }` on the signature's last line (or on its own line below a wrapped
  signature).
- A constructor initializer (`: base(x)`) goes on its own indented line, kept as written.

## Comments and directives

[ADR 0010](decisions/0010-keep-comments-at-node-boundaries.md) and [ADR 0011](decisions/0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md).
The text of a comment is never changed.

- **Above an item** (a using, member or top-level statement): own-line `//` and `///` comments, block comments that
  end their line, directives and disabled text are printed on their own lines above it, in order. Comments are
  indented with the code; the lines of a `///` run are each re-indented; the first line of a multi-line block
  comment is re-indented and its other lines are copied.
- **Directives** are written at column 0, except `#region` and `#endregion`, which are indented with the code.
  Disabled text (an inactive `#if` branch) is copied from column 0 as it is; only the active branch, the one the
  parse without symbols sees, is formatted.
- **Blank lines** around comments: the author's blank line is kept (more than one becomes one) between comments,
  directives and the item; none is added between a comment and the item below it.
- **After an item**: one comment on the same line as the item's last token (a `//` comment, or a block comment that
  ends the line) is printed after it, separated by one space. It never makes the item wrap.
- **Before a closing `}` and at the end of the file**: own-line comments and directives are the last lines of the
  body or file, indented with the body. A body that holds only comments is printed open.
- **Everything else** copies the node that owns the comment: a comment inside a header or parameter list, after a
  `{` on its line, between a `)` and a `{`, a block comment followed by code, two comments after one token, a
  directive inside a header or expression. A comment inside a method body or an initializer value is part of
  the text that is copied.
- A `#if` that splits a declaration (`#if A class X : B #else class X : C #endif`) copies that declaration.
