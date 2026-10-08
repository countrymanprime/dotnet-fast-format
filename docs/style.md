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
  body, event without accessors, statement (top-level or in a block), accessor, switch section}: the author's choice,
  none or one.
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
- Parameters print with their attributes, modifiers, type, name and default value (any expression, printed like the value
  of an assignment). A type with a primary-constructor base call (`: Base(x)`) or attributes on a type parameter is kept as
  written. Attributes on the type itself are printed as under "Members".
- A member that is kept as written (a comment or directive in a position not listed under "Comments and directives") has
  only its first line re-indented; its later lines keep the indentation they had.

## Members

Fields, properties, accessors, methods, constructors, local functions, enums, delegates, events, indexers, operators and
destructors.

- **Attributes** on a type, member, accessor, enum member or local function print each attribute list on its own line
  above it (`[A] [B]` becomes two lines, `[A, B]` stays one). An attribute list prints `[Name(1, "two", Named = 3)]` on one line
  when it fits and otherwise like an argument list (one argument per line, `)` on its own line); a target
  (`[return: X]`, `[assembly: Y]`) stays. A list with a comment or directive inside it is copied as written. A comment
  after a list on its line stays after it; own-line comments and directives between the lists, or between the last list and
  the member (an `#if` around an attribute), stay on lines of their own, directives at column 0. A comment between the
  last list and the member on the same line copies the member. Attributes on a parameter print inline
  (`[In] int x`); on a type parameter or a namespace they copy the whole declaration.
- Fields print `modifiers Type name = value;` with single spaces and `a, b` for several declarators. The value follows
  the assignment rule under "Expressions: operators, assignments and lambdas".
- An auto-property prints `Type Name { get; set; }` on one line, then `= value;` when it has an initializer. A
  property whose accessors have bodies or attributes prints `{` on its own line, each accessor on its own line
  indented one level, and `}` on its own line; no blank line is added between accessors, and the author's choice of
  none or one is kept. An accessor with a block prints `get`, then the block on the lines below; `get => value;`
  and `get;` print on one line; an empty block is `set { }`.
- A method, constructor or local function signature stays on one line when it fits in 100 columns. When it does not,
  the parameters go one per line, indented one level, with `)` on its own line. Parameters follow the default rules
  under "Type declarations".
- Constraint clauses of a method follow the signature on the same line when everything fits, and otherwise sit
  one per line, indented one level.
- A method or constructor with no body ends in `;`. An expression body prints ` => value;` on the signature's last
  line when it fits, otherwise the value goes on the next line, indented one level (`=>` stays on the signature's
  line). A value that opens with braces or brackets (creation with an initializer, collection expression, lambda)
  stays on the `=>` line and breaks inside itself.
- A block body is printed on the line below the signature (Allman), with its statements as described under
  "Statements". An empty block prints `{ }` on the signature's last line (or on its own line below a wrapped
  signature).
- A constructor initializer (`: base(x)`) goes on its own indented line; its arguments wrap like any argument list.
- A local function prints like a method, as a statement (no blank lines are added around it).
- An enum prints `{`, one member per line indented one level (a comma after each member that has one in the source, so a
  trailing comma stays and none is added), and `}`; the author's choice of none or one blank line between members is
  kept, and comments above or after a member (after its comma) are kept. An empty enum is `enum E { }`. `enum E : byte`
  prints the base type with one space on each side of `:`.
- A delegate prints like a method signature ending in `;`. An event field prints like a field, and an event with
  accessors like a property with accessors. An indexer prints like a property with a bracketed parameter list
  (`public int this[int index]`); an operator, a conversion operator (`implicit operator int(C value)`) and a destructor
  (`~C()`) print like a method with `operator +`, `operator <` and `~C` spaced as shown.
- A `{` that is preceded by comments or directives on lines of their own (an `#if` between a header and its body), or followed by
  a comment on its line (`class C { // note`), keeps them: they are printed above the brace and after it. An empty body
  with such a comment prints `{ // note` and `}` on the next line.

## Statements

[ADR 0013](decisions/0013-lay-out-statements-and-expressions-with-groups.md) and
[ADR 0014](decisions/0014-statements-and-expressions-reuse-the-trivia-model.md). Statements are the items of a list, so
the comment rules under "Comments and directives" apply to them as they do to members.

- A block prints `{`, its statements one per line indented one level, and `}` on its own line (Allman), wherever it
  appears: a method body, a nested block, a top-level program. An empty block prints `{ }`; a block that holds only a
  comment is printed open.
- Between two statements the author's choice is kept: none or one blank line (more than one becomes one). There is
  none after `{` or before `}`.
- Expression statements, local declarations (`const`, `using`, `await using`, several declarators `int a = 1, b = 2;`),
  `return`, `throw`, `yield return`, `yield break`, `break`, `continue` and `;` print on one line with single spaces.
- Top-level statements are printed the same way, at the left margin, with the same blank-line rule.
- A statement kind that has no printer is copied as written, as one statement; an expression kind that has no printer
  (query expressions `from x in xs select x`, `stackalloc`, `checked(...)`, ...) is copied as written inside a statement
  that is still formatted. Neighbours are formatted.
- A comment or directive inside a statement, in a position the comment rules do not list (after the `=` of a
  declaration, inside an argument list), copies that statement as written.

## Expressions: names, calls and chains

[ADR 0013](decisions/0013-lay-out-statements-and-expressions-with-groups.md). Every expression that is not listed in these
sections is copied as written, as one expression; the statement around it is still formatted.

- Names, literals, `this` and `base` print as written. String literals (regular, verbatim, interpolated, raw) are
  never reflowed (see "String literals").
- An argument list prints `(a, b)` on one line when it fits in 100 columns. Otherwise every argument goes on its own
  line, indented one level, and `)` is on its own line, as for parameter lists. There is no space inside the
  parentheses, and one space after each comma; an empty list is `()`. Named arguments print `name: value`, and
  `ref`, `out` and `in` stay before the value. The same rule applies to element access `a[i, j]`.
- A chain such as `a.B(x).C(y)` stays on one line when it fits. When it does not fit and has up to two groups after the
  head (three when the root is `this`, `base`, a predefined type like `string`, a capitalized name like
  `Enumerable`, or, as a statement, a name of at most four characters), the line breaks inside the argument lists,
  outermost first. With more groups, each group after the head goes on its own line, indented one level, starting
  with `.` (or `?.`):

  ```csharp
  var result = collection
      .Where(item => item.IsActive)
      .Select(item => item.DisplayName)
      .ToList();
  ```

  A group is a member access plus the calls and indexers after it. The head is the root with the calls right after it and
  any member accesses that are followed by another member access (`a.b.c.D()` has the head `a.b.c`). Member
  accesses after the last call stay on its line.
- A conditional access `a?.B` and the null-forgiving `a!` are part of the chain.

## Expressions: creation and initializers

- `new T(args)`, `new(args)`, `new T[n]`, `new[] { ... }`, `new { A = 1 }`, collection expressions `[a, ..b]`, tuples
  `(a, b)` and `x with { A = 1 }` print with single spaces and no space inside brackets or parentheses.
- A creation with an initializer stays on one line when it fits: `new Foo { A = 1, B = 2 }` (a space inside the braces).
  When it does not fit, `{` goes on its own line below the creation, at the creation's indent, with one element per line
  indented one level and `}` on its own line (Allman):

  ```csharp
  var a = new Foo
  {
      FirstProperty = firstValueWithALongName,
      SecondProperty = secondValueWithALongName
  };
  ```

  The same applies to `new[]`, `new T[] { ... }`, anonymous objects (`new` then the braces) and `with`. A collection
  expression breaks as `[` on the line of its owner, one element per line, `]` on its own line. An empty initializer
  prints `{ }`.
- A trailing comma is a token and is kept, never added: an initializer written with a trailing comma always prints one
  element per line, with the comma kept after the last element.
- An initializer that is a value (`int[] a = { 1, 2 };`) or an element of one (`{ "one", 1 }`) prints its braces on the
  line of its owner: `{ 1, 2 }` when it fits, otherwise one element per line.
- When the last argument of a call is a creation with an initializer (or a lambda with a block body), the call
  keeps its opening line when the text before the last argument fits, and the last argument opens on it:

  ```csharp
  Register(firstArgument, new Options
  {
      Name = "name",
      Description = "a description that is long enough"
  });
  ```

  Otherwise every argument goes on its own line.

## Expressions: operators, assignments and lambdas

- Binary and logical operators have one space on each side; unary operators, casts, `typeof(T)`, `default(T)`, `a[^1]`
  and `a..b` have none. `x is T`, `x as T`, `x is pattern` print with single spaces; a pattern is copied as written.
  Two signs that would touch stay apart (`- -x`).
- A chain of the same operator (`&&`, `||`, `??`, `+`/`-`, `*`/`/`, and so on) is one group. When it does not fit, every
  operator starts a continuation line, and the operands go one per line:

  ```csharp
  return firstConditionWithALongName
      && secondConditionWithALongName
      && thirdConditionWithALongName;
  ```

  Continuation lines are indented one level from the first operand, except after `=` (below) and inside the parentheses
  of a header, where the group around the parentheses already indents. A mixed expression nests: the operand that
  is itself a chain of another operator is its own group.
- A conditional expression `c ? a : b` is one group. When it does not fit, `?` and `:` start lines indented one level
  under the condition.
- A parenthesized expression prints `(` value `)` with no line break of its own.
- **After `=` and the other assignment operators, and after `=>` of a switch arm:** a value that can break inside itself
  (call, chain, creation, initializer, collection expression, lambda, switch expression, cast, parenthesized, a
  conditional whose condition is not a binary or logical expression) stays on the operator's line. A binary or logical
  value, a conditional whose condition is one, a string that is on one line and a plain member access (`a.b.c`) move to
  the next line, indented one level, when the whole does not fit, and the value then wraps as above without further
  indent:

  ```csharp
  var longSum =
      firstOperandWithALongName
      + secondOperandWithALongName
      + thirdOperandWithALongName;
  ```

  `a = b = c`, `x += 1`, `??=` and the other compound forms print the same way. Declarators, assignments, field and
  property initializers use the same rule.
- A lambda prints its parameters, ` =>` and its body. A block body goes on the lines below at the lambda's indent
  (Allman), or is `{ }` after `=>` when empty. An expression body stays after `=>` when it fits, and goes on the next
  line, indented one level, when it does not; a body that opens with braces or brackets (creation with an initializer,
  collection expression, anonymous object, switch expression, another lambda) stays on the `=>` line and breaks
  inside itself. `async`, `static` and typed or discarded parameters print as written. Lambdas with attributes or a
  return type are copied as written. `delegate (int x) { ... }`
  follows the same rule as a block lambda.
- A switch expression prints `value switch`, then `{`, one arm per line indented one level with a comma after each arm
  that has one in the source, and `}` on its own line. It is always printed on several lines. The patterns and
  `when` guards are printed as written; the arm's result follows the assignment rule above.
- A call whose last argument is a lambda with a block body, or an anonymous method, keeps its opening line when the text
  before the block fits, and the block goes below it. A lambda with an expression body that does not fit on the line is
  opened the same way: `x =>` stays on the call's line, the body goes on the next line indented one level, and `)` on the
  line after it:

  ```csharp
  var measurements = _columns.Select(column =>
      measurer.MeasureColumn(column, totalCellWidth, additionalArgument)
  );
  ```

  The same applies to a last argument that is an object creation or anonymous object with an initializer (`Register(a, new Options`
  then the braces below it) and to a lambda whose body is one. A last argument that is an array creation or collection
  expression is not opened this way: every argument goes on its own line.

  ```csharp
  Task.Run(async () =>
  {
      await Work();
  });
  ```

  With two lambdas with blocks, or when an earlier argument holds a block, every argument goes on its own line. When
  the text before the opened argument does not fit either, every argument goes on its own line.
  A block lambda in the middle of a chain breaks the chain:

  ```csharp
  var x = items
      .Where(i => i.IsValid)
      .Select(i =>
      {
          return i.Name;
      })
      .ToList();
  ```

### `if`, `else`, loops

- The body of `if`, `else`, `for`, `foreach`, `while` and `do` goes on the line below its header. A block is on its own
  lines at the statement's indent (Allman); any other statement is indented one level. Braces are never added or removed.
  An empty block is `{ }` on the header's line (`for (;;) { }`, `if (a) { }`).
- `else` starts its own line at the statement's indent; `else if` stays on one line and the chain continues at the
  same indent:

  ```csharp
  if (a)
      return;
  else if (b)
  {
      Run();
  }
  else
      throw new Exception();
  ```
- The parentheses of a header are one group. When the header does not fit in 100 columns, its content goes on its own
  line, indented one level, and `)` on the line after it; the content then wraps as an expression (operators start
  continuation lines, calls wrap their arguments). `for` breaks after each `;`:

  ```csharp
  for (
      int index = 0;
      index < collectionWithALongName.Count && !cancellationToken.IsCancellationRequested;
      index++
  ) { }
  ```
- `for` prints `for (int i = 0, j = 10; i < j; i++, j--)`, `for (;;)`, `for (; i < n;)` and `for (i = 0; ; i++)`;
  `foreach` prints `foreach (var x in xs)`, `foreach (var (a, b) in map)` and `await foreach (...)`.
- `do` prints its body, then `while (condition);` on its own line at the statement's indent.
- A comment between `else` and `if`, between `}` and `else`, or after the `)` of a header copies the whole `if`. A
  comment after a block's `}` that is followed by `else` stays after the `}`.

### `using`, `lock`, `try`, `switch`

- `using`, `await using`, `lock`, `fixed`, `checked` and `unsafe` follow the rule for `if`: header on one line when it
  fits, body on the lines below. A `using` whose body is another `using` stays at the same indent (a stack, not a
  nesting). `using var x = ...;` is a declaration and prints like one.
- `try`, each `catch` and `finally` start their own line at the statement's indent, with their blocks below. A `catch`
  prints `catch`, `catch (Type)`, `catch (Type name)` and `catch (Type name) when (condition)`; the filter's
  parentheses wrap like a header's.
- `switch` prints `switch (expression)`, then `{`, the sections indented one level, and `}`. An empty switch is
  `switch (x) { }`. A section prints each label on its own line (`case 1:`, `case 2:`, `default:`), then its
  statements indented one level; a section that is a single block prints the block under its labels at the labels'
  indent. Between sections the author's choice is kept: none or one blank line. Patterns after `case` and `when` guards
  are copied as written. `goto`, `goto case x;`, `goto default;` and labels (`name:` and then its statement on the next
  line at the same indent) print as written.
- A comment after a label's colon (`case 1: // note`), or between a `}` and `catch` or `finally`, copies the section or the `try`.

## String literals

Regular, verbatim (`@"..."`), interpolated (`$"..."`, `$@"..."`) and raw (`"""..."""`) string literals are copied token
for token and never reflowed, re-indented or split, whatever the line width: the text of a literal, including the
whitespace and the line breaks inside it, is its value.

- A literal on one line is an ordinary operand: it can move to the next line (after `=`, after `(`, or as an operand
  of `+`) when the line does not fit, and it stays whole when it does not fit even there.
- A literal that spans lines is emitted as it is, from the column where it starts. Its first line counts towards the
  width; the text after it is measured from the column where its last line ends. It does not make the call, creation
  or operator chain around it break by itself, so `Log(@"a` newline `b", second);` keeps its shape.
- A multi-line literal as the value of `=` stays on the line of `=`.
- Interpolated strings are copied as a whole, holes included.

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
- **Next to a brace**: a comment after a `{` on its line stays there, and comments and directives on lines of their
  own between a header and its `{` stay above it (see "Members").
- **Everything else** copies the node that owns the comment: a comment inside a header or parameter list, a block comment
  followed by code, two comments after one token, a directive inside a header or expression. A comment inside a statement
  (not above, after or before the `}` of one) copies that statement; the statements around it are still formatted.
- A `#if` that splits a header (`#if A class X : B #else class X : C #endif {`) keeps its directives and disabled text as lines;
  only the branch the parse without symbols sees is formatted. A `#if` inside a header or a parameter list copies the
  declaration.

## Settings from `.editorconfig`

Everything above is the output with no key applying. These keys change it
([ADR 0012](decisions/0012-read-editorconfig-with-a-small-parser-in-core.md)); fixtures are in
`tests/DotnetFastFormat.Tests/golden/config/`, one input formatted under several configurations.

- **Which files apply.** For each file the formatter reads `.editorconfig` in its directory and every parent, nearest
  first, and stops after a file with `root = true` in its preamble. Within a file the sections whose glob matches the
  file's path apply in order, so a later section beats an earlier one, and a nearer file beats a farther one. A
  section name with a `/` is relative to the directory of its `.editorconfig`; one without matches at any depth.
  Keys are case-insensitive, and so are the values of the keys below. `unset` removes a key's earlier values.
- **`indent_style`, `indent_size`, `tab_width`.** A level is `indent_size` columns. With `space` it is that many
  spaces. With `tab` the printer writes as many tabs as fit in the level's columns (`columns / tab_width`) and then
  the rest as spaces, so `indent_size = 4` with `tab_width = 8` writes four spaces at level 1, a tab at level 2 and a
  tab and four spaces at level 3. With `tab` and no `indent_size`, `indent_size` is `tab_width` (default 4); a
  `tab_width` that is not set is the `indent_size`. A tab counts as `tab_width` columns when deciding whether a line fits.
- **Copied text is never re-indented.** Only the first line of a node kept as written gets the indentation; its other
  lines, block comments and disabled text keep the whitespace they had, spaces or tabs. A tab character inside text
  counts as one column, like any other character.
- **`max_line_length`.** The width in columns, or `off` to never wrap (a line is still broken where the layout forces
  a break). Default 100.
- **`end_of_line`.** `lf` or `crlf`: every line break the formatter writes uses it, and so do the line breaks in
  comments, directives, disabled text and copied code. A line break inside a token, which means a multi-line verbatim,
  raw or interpolated string literal, is never changed. Not set: the most common terminator of the input, as before.
- **`insert_final_newline`.** `true` or not set: exactly one line terminator at the end of a non-empty file. `false`:
  none. An empty file stays empty.
- **`charset`.** `utf-8` writes no byte order mark and `utf-8-bom` writes one (a non-empty file). Not set: the file's own
  is kept.
- **Values that cannot be used** (a word that is not an option, a number out of range, `end_of_line = cr`, a charset
  other than the two above) are reported once on stderr and ignored; an earlier valid value of the key still applies.
  Keys the formatter does not know are ignored without a message.
