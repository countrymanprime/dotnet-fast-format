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
