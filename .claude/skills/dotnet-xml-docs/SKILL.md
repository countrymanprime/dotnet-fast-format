---
name: dotnet-xml-docs
description: Write, review, and enforce C# XML documentation comments (/// summary, param, returns, exception, remarks, inheritdoc) and inline // comments to the standard used by the .NET runtime and API docs. Use when adding or changing public or protected C# APIs, when asked to document a type or member, when reviewing comments in a diff, or when setting up build enforcement (GenerateDocumentationFile, CS1591, StyleCop SA16xx, PublicApiAnalyzers) in a .NET repo.
---

# .NET XML documentation and code comments

XML doc comments are the **contract** of an API: they feed IntelliSense, the NuGet `.xml`
file, and generated API reference (DocFX). Inline `//` comments explain **why** code is the
way it is. This skill covers writing both, reviewing them, and making the build enforce them.

Reference files (load when needed):

- [references/tag-guide.md](references/tag-guide.md): phrasing templates for each kind of member, every tag with rules, cref syntax,
  and a full gold-standard example. **Read it before documenting more than one or two members.**
- [references/enforcement.md](references/enforcement.md): verified `Directory.Build.props`/`.targets`, `.editorconfig`,
  `stylecop.json`, and PublicApiAnalyzers setup, plus a rollout plan for existing repos.

## Step 0: Detect the repo's conventions first

Before writing, look at what the repo already does and match it:

1. Read 2–3 well-documented public types in the same project. Note the exception phrasing
   (`Thrown when …` vs. a bare condition), whether `<remarks>` are used, and the `<see langword>` style.
2. Check `Directory.Build.props`, `*.csproj`, `.editorconfig`, and `stylecop.json` for
   `GenerateDocumentationFile`, `NoWarn`/`WarningsAsErrors` containing `CS1591`, and StyleCop
   `documentInternalElements`/`documentPrivateElements`.
3. Check for `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` next to the project files.

If the repo has a convention, follow it even where this skill's defaults differ. If it has
none, use the defaults below.

## What must be documented

| Scope | Default |
| --- | --- |
| `public` and `protected` members of **packable / shipped** projects | **Required.** Every type, member, parameter, type parameter, and return value. |
| `internal` members | Optional. Document the non-obvious ones (invariants, threading, ownership). |
| `private` members | Only when the contract is non-obvious. Prefer a clear name. |
| Test projects, samples, benchmarks | Not required. Name tests so they describe the behavior. |
| Overrides and interface implementations | `/// <inheritdoc/>`, unless the override changes the contract. |

## Writing rules (defaults)

1. **The summary is one sentence** in present tense, third person, ending with a period. **Never restate the
   name**: "Gets the name." on `Name` adds nothing. Say what it is *for* or what it *means*.
   Use the fixed openings in `references/tag-guide.md` (e.g. constructors:
   `Initializes a new instance of the <see cref="X"/> class.`).
2. **Document the contract, not the implementation.** For each member, check:
   - **Inputs**: valid range, units, format, what `null` or empty means.
   - **Output**: what the return value means, including for the "not found" and "empty" cases.
   - **Exceptions**: every exception the member throws **directly**, written as a condition
     (see the tag guide). For `async` members whose argument validation throws synchronously, say so.
     For exceptions surfaced by the returned task, add "This exception is stored into the returned task."
   - **Side effects and state**: mutation, I/O, events raised, caching.
   - **Thread safety**: when it's not the type's default.
   - **Ownership and disposal**: who disposes what the member returns or accepts.
   - **Cancellation**: what happens when the `CancellationToken` fires.
   - **Performance**: only when it's surprising (e.g. O(n) on something that looks like a property).
3. **Keep docs and nullability consistent.** If the docs say `null` throws, the parameter must
   be non-nullable, and vice versa. If `null` is meaningful, the docs must say what it means.
4. **Use semantic tags**, not plain text: `<see langword="null"/>`, `<see langword="true"/>`,
   `<paramref name="x"/>`, `<typeparamref name="T"/>`, `<see cref="Type.Member"/>`, and `<c>` for inline code.
5. **Examples must compile.** Put short `<example><code>` blocks only on entry-point APIs.
   Longer samples belong in tested code or in docs pulled from tested code.
6. **Don't document what the signature already says.** `<param name="cancellationToken">The
   token to monitor for cancellation requests.</param>` is the accepted standard phrasing. Use
   it, then move on.

### Anti-patterns to fix on sight

- A summary that only restates the member name: `/// <summary>Gets the order.</summary>` on `GetOrder`.
- Empty or placeholder tags: `<param name="x"></param>`, `<returns></returns>`, "TODO", "The x."
- A `<param>` that doesn't match the signature after a rename. The compiler flags this as CS1572 or CS1573.
- `<exception>` entries for exceptions the member does not throw, or missing ones it does.
- `<remarks>` that narrate the implementation line by line.
- Copy-pasted docs that still name the original member or type.
- Hand-copied docs on overrides where `<inheritdoc/>` would do.

## Inline comments

- **Explain why, never what.** Write one when the code can't say it: a workaround, an invariant,
  a non-obvious ordering, a performance trade-off, or a link to the spec or issue that forced it.
  `// Read _count before the CAS: a concurrent Clear() can reset it (#1234).`
- Put the comment on its own line above the code, starting with a capital letter.
- **A TODO carries an owner or an issue link:** `// TODO(#1234): remove once EF Core supports X.`
  A TODO without either is noise. Create the issue or delete the comment.
- **Every suppression gets a reason:** `#pragma warning disable CA2000 // Ownership transfers to the caller.`
- **Delete commented-out code.** Git history keeps it.
- When you change code, **re-read the comments around it**. A stale comment is worse than none.

## Review mode

When asked to review comments, or when reviewing a diff, check each changed public or protected member
against the rules above and the anti-patterns list. Report findings as:

```text
<file>:<line> <Member> - <problem> -> <concrete fix, with the replacement text>
```

Group them by severity:

- **wrong**: the docs contradict the code.
- **missing**: a required tag or exception is absent.
- **weak**: restates the name, or is vague.
- **style**: phrasing or formatting.

A "wrong" doc is a bug.

- **When you're writing code,** fix it in the same change.
- **When you're only reviewing,** report it, and don't edit.

## Enforcement

Tooling catches **structural** drift: missing docs, renamed parameters, broken `cref`s. It
**cannot** catch prose that no longer matches behavior; only review catches that.

Change build settings only when asked to set up enforcement. If you notice a repo has
`GenerateDocumentationFile` off, **offer** to enable it; don't do it silently. When setting up,
follow [references/enforcement.md](references/enforcement.md). Summary:

1. Turn on `GenerateDocumentationFile=true` in `Directory.Build.props`, **build, and count** the
   existing structural doc warnings (CS1570–CS1587, CS17xx, CS0419). An existing repo usually has some.
2. Fix them, then promote those IDs to errors. They are silent on correct docs, so after the
   cleanup they cost nothing.
3. Suppress CS1591 (missing docs) for test and non-packable projects in
   `Directory.Build.targets`, not in `.props`.
4. Keep CS1591 as a warning while backfilling, then promote it to an error.
5. Optionally, add StyleCop's **documentation rules only** for content quality, and
   PublicApiAnalyzers so every new public API shows up in the diff.

Never switch on repo-wide `TreatWarningsAsErrors` as a side effect of documentation work. It
can break unrelated code. Promote specific IDs instead, and tell the user what changed.

## Verify before finishing

- `dotnet build --no-incremental` shows no new warnings for the structural IDs listed in
  `references/enforcement.md`, no new `CS1591` for the projects in scope, and no new `SA16xx`.
- Every `cref` resolves. The build proves it once CS1574 is an error.
- If the repo has `PublicAPI.Unshipped.txt`, new public symbols are listed there.
- Re-read each doc you wrote next to its code. Does it describe what the code *actually*
  does, including edge cases?
