# dotnet-fast-format

A fast C# formatter that reads the [`.editorconfig`](https://editorconfig.org/) you already have.
It parses with [Roslyn](https://github.com/dotnet/roslyn) syntax trees, needs no MSBuild project
load, and prints through its own [doc-printer](https://github.com/prettier/prettier/blob/main/commands.md).
Unfamiliar terms are defined in the [glossary](docs/glossary.md).

> **Status: pre-code.** This repository currently holds the plan: product requirements, design
> and decisions. There is nothing to install yet. See [`docs/TASKS.md`](docs/TASKS.md) for what
> comes first.

## Why

- `dotnet format` loads an MSBuild workspace, which makes it slow on large repositories.
  One user measured [27.9 s against 2.4 s for the older standalone tool](https://github.com/dotnet/sdk/issues/39124).
- [CSharpier](https://csharpier.com/) is fast but deliberately has [very few options](https://csharpier.com/docs/Configuration)
  and limited `.editorconfig` support.
- This project aims to sit in the gap: speed close to CSharpier, behavior driven by the
  `.editorconfig` rules a team already has.

## Goals

1. Format C# files quickly, without MSBuild.
2. Honor common formatting keys from `.editorconfig`.
3. Never change program meaning: output is idempotent and tree-preserving.
4. Stretch: cover enough of `dotnet format`'s whitespace and style pass to replace it.

Details, non-goals and open questions are in the [PRD](docs/PRD.md).

## Install

Not published yet. Planned: `dotnet tool install -g dotnet-fast-format`, run as
`dotnet fast-format`.

## Documentation

- [`docs/README.md`](docs/README.md): index of everything below
- [`docs/PRD.md`](docs/PRD.md): product requirements
- [`docs/requirements/`](docs/requirements/core.md): testable requirements with IDs
- [`docs/TASKS.md`](docs/TASKS.md): milestones and tasks
- [`docs/architecture.md`](docs/architecture.md): planned design
- [`docs/decisions/`](docs/decisions/README.md): architecture decision records
- [`AGENTS.md`](AGENTS.md): conventions and commands for AI coding agents

## License

[MIT](LICENSE)
