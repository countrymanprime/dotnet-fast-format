# Glossary

Terms used across these docs, each with a link to its source definition.

| Term | Meaning here | Source |
|---|---|---|
| **ADR** | Architecture Decision Record: a short file recording one significant decision, its context and consequences | [adr.github.io](https://adr.github.io/) |
| **MADR** | Markdown Architectural Decision Records: the ADR template this repo uses in [`decisions/`](decisions/README.md) | [adr.github.io/madr](https://adr.github.io/madr/) |
| **EARS** | Easy Approach to Requirements Syntax: sentence templates ("When X, the formatter shall Y") that make requirements testable; used in [`requirements/`](requirements/core.md) | [Wikipedia](https://en.wikipedia.org/wiki/Easy_Approach_to_Requirements_Syntax) |
| **Roslyn** | The .NET compiler platform; this project uses only its C# syntax-tree parsing | [dotnet/roslyn](https://github.com/dotnet/roslyn) |
| **Syntax tree** | The parsed structure of a source file, including comments and whitespace as trivia | [Roslyn syntax analysis](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis) |
| **Trivia** | Whitespace, comments and preprocessor directives attached to tokens in a Roslyn syntax tree | [Roslyn syntax analysis](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis) |
| **Doc IR / doc-printer** | A Prettier-style intermediate document tree (groups, indents, line breaks) and the printer that lays it out to a line width | [Prettier doc commands](https://github.com/prettier/prettier/blob/main/commands.md) |
| **`.editorconfig`** | The cross-editor settings file this tool reads for indentation, line endings and C# layout keys | [editorconfig.org](https://editorconfig.org/) |
| **Verify** | The snapshot-testing library used for golden fixtures | [VerifyTests/Verify](https://github.com/VerifyTests/Verify) |
| **Golden fixture** | An input file paired with its expected formatted output; the executable spec for formatting behavior | [PRD](PRD.md), [`formatter-verification`](../.claude/skills/formatter-verification/SKILL.md) |
| **Idempotent** | Formatting already-formatted output changes nothing: `format(format(x)) == format(x)` | [`../AGENTS.md`](../AGENTS.md) |
| **Conventional Commits** | Commit message convention (`feat:`, `fix:`, `docs:`) used in this repo | [conventionalcommits.org](https://www.conventionalcommits.org/en/v1.0.0/) |
| **BenchmarkDotNet** | The .NET benchmarking library planned for the performance project | [benchmarkdotnet.org](https://benchmarkdotnet.org/) |
| **Mermaid** | Text-based diagram syntax that GitHub renders in Markdown | [mermaid.js.org](https://mermaid.js.org/) |
