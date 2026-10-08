# Style changelog

Every change to what the formatter prints for the same input, newest first. A change to stable output needs an
entry here ([AGENTS.md](../AGENTS.md)). Until a stable release, entries record what moved so a reader of a diff
knows it was deliberate.

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
