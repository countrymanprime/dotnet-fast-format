---
name: dotnet-repo-docs
description: Scaffold, audit, or restructure the documentation set of a .NET repository (README, docs/ index, architecture overview with diagrams, CONTRIBUTING, CHANGELOG, NuGet package README, PR template, CODEOWNERS, agent instructions) and add docs CI (markdownlint, link checking, Mermaid validation, optional DocFX API site). Use when starting or overhauling docs for a .NET repo, when asked to audit or improve a repo's documentation, when a repo has little or stale documentation, or when adding documentation checks to CI. Sizes the doc set to the repo (library or application, small or large).
---

# .NET repository documentation set

Good repo docs are **small, layered, and current**:

- The README gets someone from zero to a running build.
- `docs/` holds everything deeper, organized by what the reader is trying to do.
- Reference material is generated from code.
- Explanations and decisions are written by hand.
- Every doc changes in the same PR as the code it describes.

Reference files:

- [references/doc-set.md](references/doc-set.md): what each document must contain (READMEs for libraries and
  apps, package README, CHANGELOG, CONTRIBUTING, architecture, docs index, agent files, DocFX).
- [references/docs-ci.md](references/docs-ci.md): docs CI jobs, their config files, and running the checks locally.

Templates:

- [templates/architecture.md](templates/architecture.md)
- [templates/pull_request_template.md](templates/pull_request_template.md)
- [templates/docs.yml](templates/docs.yml) (GitHub Actions workflow)

Related skills:

- `mermaid-diagrams`: every diagram.
- `architecture-decision-records`: the decision log.
- `dotnet-xml-docs`: API comments and their build enforcement.
- `dotnet-docs-sync`: keeping all of this current as code changes.

## Principles

1. **Adopt, don't replace.** If the repo already has a docs structure, naming scheme, or
   site generator, extend it. Propose a restructure only when the current one clearly fails its
   readers, and ask before moving files.
2. **Proportional.** A 2-project library needs a README, a CHANGELOG and XML docs, not twelve
   files. Create a doc only when there is real content for it (see the table in Step 2).
3. **Grounded.** Every command, version, path and claim comes from the repo:
   - `global.json`
   - `*.csproj` (`TargetFramework(s)`, `IsPackable`, `PackageId`)
   - CI workflows
   - `Dockerfile`, AppHost and `appsettings*.json`

   Run the **safe** commands you document: restore, build, test, format, and local run (start
   it, confirm it comes up, then stop it). **Never run** deploy or publish steps, migrations
   against shared databases, or anything that needs real secrets. Mark those as "not run" in
   your report. Never write from memory or leave placeholder text.
4. **Organize by purpose** (Diátaxis):
   - *tutorials / getting started*: learning
   - *how-to guides*: doing a task
   - *reference*: looking something up
   - *explanation*: understanding, including architecture and decisions

   Don't mix these in one page.
5. **Link, don't duplicate.** Each fact lives in one place, and other docs link to it.
6. **Keep working notes out of the repo root.** Plans, scratch notes and dated release plans belong
   in issues, or in `docs/` if they're durable, and nowhere else.

## Step 1: Survey the repo

Collect these facts before writing anything:

- **Kind of repo:**
  - **Library**: projects with `IsPackable` true or a `PackageId`, or a NuGet publish step in CI.
  - **Application**: `Microsoft.NET.Sdk.Web`/`Worker`, `OutputType Exe`, a `Dockerfile`,
    or an Aspire AppHost.
  - It can be **both**.
- **Size:** the number of projects and bounded areas, and whether it's public or has external
  contributors.
- **Existing docs:** `git ls-files '*.md'`, plus `.github/`, `docs/`, an ADR folder,
  `docfx.json`, `mdsnippets.json`, and `CODEOWNERS`.
- **Existing Mermaid diagrams:** `git grep -l '```mermaid' -- '*.md'`
- **Build, test and run commands:** from the CI workflow files and existing docs. Run the
  safe ones (see principle 3).
- **CI system and default branch:** GitHub Actions (`.github/workflows/`) or Azure Pipelines
  (`azure-pipelines.yml`), and the default branch name.
- **Doc-comment setup:** `GenerateDocumentationFile` and CS1591 handling in
  `Directory.Build.props` / csproj files.

## Step 2: Choose the target doc set

| Doc | Purpose | Create when |
| --- | --- | --- |
| `README.md` | What it is; install or run; minimal example; links | Always |
| `docs/README.md` | Map of all docs, grouped by purpose | `docs/` has 3 or more pages |
| `docs/architecture.md` | Context and container views, project map, key flows, cross-cutting concerns | Applications; libraries with 3+ projects or non-obvious internals |
| `docs/decisions/` | ADR log | Always offer it. The first ADR adopts the practice. |
| `CONTRIBUTING.md` | Prerequisites, build and test, conventions, PR expectations | Anyone besides the author contributes, or the repo is public |
| `CHANGELOG.md` | Human-written release history (Keep a Changelog) | The repo produces versioned releases or packages |
| `package-readme.md` | NuGet-specific README (`PackageReadmeFile`) | Packable projects. Use a separate file when the repo README has relative links or images. |
| `docs/how-to/*.md` | Task guides: configure X, extend Y, deploy Z | Real recurring tasks exist |
| `docs/reference/configuration.md` | Every setting, its default, and its effect | The app or library has options or `appsettings` sections |
| DocFX site (`docs/docfx.json`) | Published API reference plus conceptual docs | A public library with a meaningful API surface, and the user wants a hosted site |
| `SECURITY.md` | How to report vulnerabilities | Public repos |
| `.github/pull_request_template.md` | Docs checklist on every PR | Always (`templates/pull_request_template.md`) |
| `.github/CODEOWNERS` | Review routing, including for `docs/` | More than one maintainer |
| `AGENTS.md` / `CLAUDE.md` | Commands and non-obvious conventions for coding agents | The repo is worked on with agents. Keep it short; see doc-set. |

Don't create empty stubs, a page per class (that's what API reference is for), or docs
that repeat what the code or `--help` already says.

## Step 3: Write

Work through `references/doc-set.md` for each document you're creating or fixing. Also:

- **Diagrams:** use the `mermaid-diagrams` skill. An application's `docs/architecture.md` should
  have a context view and a container view. The README links to it, or embeds the container
  view if it's small.
- **Decisions:** use the `architecture-decision-records` skill. The architecture doc links to the log.
- **API comments:** use the `dotnet-xml-docs` skill. Turning on build enforcement is a separate
  step; offer it, don't do it silently.
- **Code samples** in docs must compile. The best option is to pull them from tested code; see
  `references/docs-ci.md` (MarkdownSnippets / DocFX code includes). The fallback is to compile
  them yourself once before committing.

## Step 4: Add docs CI (offer, then do it on a yes)

Copy `templates/docs.yml` to `.github/workflows/docs.yml`, and follow `references/docs-ci.md` to:

- add `.markdownlint-cli2.jsonc`
- set the default branch name in `on.push.branches`
- turn on the optional DocFX and snippets jobs if they apply
- run every check locally and fix the findings before committing

Pin action versions to what's current. Check each action's releases page.

**Azure Pipelines instead of GitHub Actions:** port the same steps into script tasks. Each job
in the template is plain shell: `npx markdownlint-cli2`, lychee, the mermaid-cli loop, and
`dotnet docfx`.

## Step 5: Verify

- [ ] Every safe command in the README and CONTRIBUTING was run and works. Anything not run
  is listed in the report.
- [ ] Versions match `global.json` and the csproj files.
- [ ] Relative links resolve. Check with the local link-check command in `docs-ci.md`.
- [ ] markdownlint is clean, or its findings are fixed.
- [ ] Mermaid blocks render. Validate them with mermaid-cli.
- [ ] Nothing contradicts the code. Re-read each doc against the files it describes.

## Audit mode

When asked to audit, don't write anything at first. Produce a table:

| Doc | State | Evidence | Fix |
|---|---|---|---|
| e.g. `README.md` | stale | Says `net6.0`; csproj targets `net8.0`. `dotnet run --project src/Api` fails: the project moved to `src/Orders.Api`. | Update the TFM and the run command |

- **States:** missing, stale, wrong, bloated, ok.
- **Order:** "wrong" first, because it actively misleads, then "missing" for things readers need,
  then "stale", then "bloated".

Then ask which fixes to apply, or apply them if the user already asked for that.
