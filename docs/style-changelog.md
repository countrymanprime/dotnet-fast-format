# Style changelog

Every change to what the formatter prints for the same input, newest first. A change to stable output needs an
entry here ([AGENTS.md](../AGENTS.md)). Until a stable release, entries record what moved so a reader of a diff
knows it was deliberate.

## M2 (2026-10-05)

Output that changed for files that already formatted in M3 (bodies, values and attributes used to be copied as written):

- Method, constructor, accessor, lambda and top-level bodies are printed: Allman braces, four-space indent, one statement
  per line, `if`/`for`/`while`/`foreach`/`using`/`lock`/`try`/`switch` with their bodies below, and expressions wrapped to
  100 columns (see "Statements" and "Expressions" in [style.md](style.md)). A body that was written for another indent or
  brace style moves; a one-line `if (x) return;` becomes two lines.
- Initializer values and expression bodies of fields and properties are printed (a long value breaks after `=` or `=>`,
  an initializer with braces goes Allman).
- A property with accessor bodies prints its accessors one per line (it used to be copied).
- Attribute lists are printed one per line above their member (`[A] [B] void M()` is two lines), also on types; attribute arguments
  are spaced and wrapped like call arguments. Parameter attributes and any parameter default are printed (a list with
  a default that was not a literal used to be copied).
- Enums, delegates, events, indexers, operators and destructors are printed (they were copied).
- A comment after an opening brace (`class C { // note`) stays after the brace on its line, and an `#if` between a header
  and its `{` stays above the brace; both used to copy the declaration.
- Multi-line string literals are measured to their first line break and no longer make the call around them break.

Unchanged: string literals are never reflowed; patterns, query expressions, `stackalloc`, and a comment inside a statement
(except above, after or before the `}` of one) are copied as written, as one expression or statement.

## M3 (2026-10-05)

Output that changed for files that already formatted in M1:

- A node that carries a comment or directive in a position [ADR 0010](decisions/0010-keep-comments-at-node-boundaries.md)
  handles is now formatted. In M1 it was copied whole, so its spacing and its members' spacing change: a type with
  a `///` comment above it, a member with a `// note` after it, a class whose last line before `}` is a comment.
- A file that contains `#if`, `#else`, `#endif`, `#region` or `#endregion` is now formatted outside the directives.
  In M1 it was copied whole.
- A file that holds only usings and comments is formatted. In M1 a file without declarations was copied.
- A comment at the end of a file is kept after the last item with one blank line where the author had one or more;
  in M1 the file was copied.

Unchanged: a file that holds only whitespace, comments and directives is still copied; a node with a comment in
any other position is still copied.

## M4 (2026-10-05)

Output that depends on `.editorconfig`; a file whose configuration sets none of these keys prints as in M3:

- `indent_style`, `indent_size`, `tab_width`, `max_line_length`, `end_of_line` and `insert_final_newline` now change the
  output as described in `docs/style.md`. A repository that already sets them in `.editorconfig` sees its files change
  the first time they are formatted, for example two-space indentation, tabs, CRLF or no final newline.
- `charset = utf-8` removes a byte order mark that a file has, and `charset = utf-8-bom` adds one. In M3 a byte order
  mark was always kept. Not set: still kept.
- `end_of_line = lf` or `crlf` converts the line breaks of copied text, comments, directives and disabled text too;
  string literals are never touched. Without the key the dominant terminator is still used and mixed input stays mixed
  inside copied text.

Unchanged: the defaults, the layout rules, and the handling of comments and directives.
