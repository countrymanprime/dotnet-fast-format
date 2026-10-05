# Architecture (planned)

> **Planned design, no code yet.** This describes the intended structure so the first milestones
> have a target. It is replaced by a description of the real code as each part lands. Names are
> provisional.

## Purpose and scope

`dotnet-fast-format` reads C# source files and rewrites them with consistent layout, using the
style from `.editorconfig`. It works on syntax only: no compilation, no MSBuild, no semantic model.
It never changes program meaning (see the invariants in [`../AGENTS.md`](../AGENTS.md)). Linting
and analyzer fixes are out of scope for the first release ([PRD](PRD.md)).

## Pipeline

```mermaid
flowchart LR
  accTitle: Formatting pipeline for one file
  accDescr {
    A source file is read, its settings are resolved from .editorconfig, and it is parsed
    with Roslyn into a syntax tree. Comments and trivia are attached to nodes, a document
    tree is built from the syntax tree, and a printer lays that document out to a line
    width. The result is checked against the invariants and then written, compared in
    check mode, or returned on standard output.
  }
  src["Source file"] --> cfg["Resolve .editorconfig settings"]
  cfg --> parse["Parse with Roslyn (syntax only)"]
  parse --> trivia["Attach comments and trivia"]
  trivia --> doc["Build document tree (Doc IR)"]
  doc --> print["Print to line width"]
  print --> check["Check invariants"]
  check --> out["Write file, compare for --check, or stdout for --stdin"]
```

| Stage | Responsibility |
|---|---|
| Resolve settings | Walk up from the file to find `.editorconfig` files, nearest first, stop at `root = true`; map keys to formatter options |
| Parse | `Microsoft.CodeAnalysis.CSharp` syntax tree for the latest language version; a parse error stops the file |
| Attach trivia | Decide which node owns each comment, blank line and directive so none is lost or moved across code |
| Build Doc IR | One builder per syntax node kind (the [Prettier-style document tree](https://github.com/prettier/prettier/blob/main/commands.md); see the [glossary](glossary.md)), composed from printer primitives (group, indent, line, softline, fill) |
| Print | Lay the Doc IR out to `max_line_length` and the indent settings |
| Check invariants | Idempotent, tree-preserving, no-loss, valid output (also asserted in tests) |

Until each construct has a printer, the builder falls back to the node's original text, and so does any node
carrying a comment or directive until trivia handling lands ([ADR 0008](decisions/0008-print-unsupported-syntax-verbatim.md)).
Before a file is written, its output is re-parsed and compared with the input ([ADR 0009](decisions/0009-self-check-output-before-writing.md)).

The pipeline runs per file and files are independent, so a run formats files in parallel.

## Planned layout

```text
src/DotnetFastFormat.Core/       doc IR, printer, per-node builders, trivia handling
src/DotnetFastFormat.Config/     .editorconfig resolution and option mapping
src/DotnetFastFormat.Cli/        the dotnet tool: arguments, file discovery, exit codes
tests/                           golden fixtures (Verify), invariant helper, corpus run
tests/DotnetFastFormat.Corpus/   pinned corpus manifest and fetch step, shared by tests and benchmarks
benchmarks/                      speed harness that times other formatters on the corpus, outside the test gate
```

Core has no dependency on the CLI or on file I/O, so the printer can be tested with in-memory
strings.

## Decisions

- [0002](decisions/0002-build-a-doc-printer-on-roslyn-syntax-trees.md): build our own doc-printer on Roslyn syntax trees
- [0003](decisions/0003-use-dotnet-test-as-the-single-gate.md): `dotnet test` is the single gate

## Open design questions

Preprocessor strategy, default style, and target frameworks are open; see the
[PRD](PRD.md#open-questions).
