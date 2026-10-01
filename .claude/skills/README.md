# .NET documentation skills for Claude Code

Five repo-agnostic [Claude Code skills](https://code.claude.com/docs/en/skills) for creating
and maintaining best-in-class documentation in any .NET repository: XML doc comments,
Mermaid diagrams, ADRs, the repo doc set, and keeping all of it in sync as code changes.

| Skill | Use it to | Triggers on |
| --- | --- | --- |
| [`dotnet-docs-sync`](dotnet-docs-sync/SKILL.md) | Turn a diff into doc updates, in the same change | Finishing a change, before a commit or PR, "check docs drift" |
| [`dotnet-xml-docs`](dotnet-xml-docs/SKILL.md) | Write, review and enforce `///` docs and `//` comments | Adding or changing public API, "document this", setting up CS1591 |
| [`mermaid-diagrams`](mermaid-diagrams/SKILL.md) | Draw, update and validate diagrams that render everywhere | Architecture, flow, state or data-model changes, "diagram this" |
| [`architecture-decision-records`](architecture-decision-records/SKILL.md) | Record and supersede decisions (MADR, append-only) | Significant or hard-to-reverse decisions, "write an ADR" |
| [`dotnet-repo-docs`](dotnet-repo-docs/SKILL.md) | Scaffold or audit the doc set, and add docs CI | New repo, docs overhaul or audit, adding doc checks to CI |

`dotnet-docs-sync` is the one to use day to day. It hands off to the other four, and the skills
refer to each other by name, so **install all five together**.

## Install

These skills live in this repo under `.claude/skills/`, so they already work here. To use them in
every repo, copy them to your user-level skills folder.

macOS/Linux:

```bash
mkdir -p ~/.claude/skills
cp -r .claude/skills/{dotnet-docs-sync,dotnet-xml-docs,mermaid-diagrams,architecture-decision-records,dotnet-repo-docs} ~/.claude/skills/
```

Windows (PowerShell):

```powershell
$dest = "$HOME\.claude\skills"; New-Item -ItemType Directory -Force $dest | Out-Null
'dotnet-docs-sync','dotnet-xml-docs','mermaid-diagrams','architecture-decision-records','dotnet-repo-docs' |
  ForEach-Object { Copy-Item -Recurse -Force ".claude\skills\$_" $dest }
```

When the same skill name exists in both places, Claude Code uses the **personal** copy
(`~/.claude/skills/`) over the repo's `.claude/skills/` copy. After copying, keep one source
of truth: edit the personal copies, or delete the ones in the repo.

## Make syncing a habit

Skills load when their description matches the task. To make the sync pass happen on every
change, add a line like this to each repo's `CLAUDE.md`, or to `~/.claude/CLAUDE.md` for all repos:

```markdown
- Before committing a code change, run the `dotnet-docs-sync` skill and include its doc updates in the same commit.
```

## What the skills rely on

Everything is optional and detected per repo. The skills adapt to whatever a repo already uses.

| Tool | Used for | Install |
| --- | --- | --- |
| .NET SDK | Build-time doc checks (CS15xx), `dotnet format` for PublicAPI files | — |
| Node.js (`npx`) | `@mermaid-js/mermaid-cli` (diagram validation), `markdownlint-cli2` | nodejs.org |
| `gh` CLI | Reading PR diffs | cli.github.com |
| lychee | Link checking (optional) | `cargo install lychee` / `brew install lychee` / `winget install lycheeverse.lychee` |
| DocFX, MarkdownSnippets | API site and compiled snippets (optional, local dotnet tools) | `dotnet new tool-manifest` (once), then `dotnet tool install docfx` / `dotnet tool install MarkdownSnippets.Tool` |

## Provenance

The guidance comes from:

- Microsoft Learn: C# XML documentation, NuGet, and SourceLink
- `dotnet/runtime` coding guidelines and the `dotnet/dotnet-api-docs` style wiki
- the C4 model and MADR
- Keep a Changelog, Diátaxis, and Google's documentation best practices
- repos such as Polly, eShop, Aspire, and semantic-kernel

The build snippets, CLI commands, templates, and Mermaid examples were tested in September 2026:

- .NET SDK 10.0.101
- StyleCop.Analyzers 1.1.118, PublicApiAnalyzers 5.6.0, SauceControl.InheritDoc 2.0.2
- DocFX 2.81.0, MarkdownSnippets.Tool 28.4.4, EfToMermaid 2.1.1
- mermaid-cli 12.0.0 and Mermaid 11

Package and action versions drift, so check for current ones when applying these to a repo.
