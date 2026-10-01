# What each document contains

Use these as checklists, not as fill-in-the-blank templates. Omit any section that has no
real content for this repo.

## README.md for a library

1. **Name and one-sentence purpose.** Say what problem it solves, not which technology it uses.
2. **Badges.** Only real, working ones: CI status and NuGet version. No vanity badges.
3. **Install:** `dotnet add package <PackageId>`, for each package if there are several (a table is fine).
4. **Supported frameworks:** copied from `TargetFramework(s)` in the csproj files.
5. **Quick start:** the smallest complete example that does something useful, including DI
   registration if the library needs it. It must compile. Prefer pulling it from a test with
   MarkdownSnippets (see `docs-ci.md`).
6. **Key concepts and next steps:** links into `docs/` or the docs site.
7. **Contributing, license, and security:** one line each, linking to the files.

## README.md for an application or service

1. **Name and purpose.** What it does, and for whom.
2. **Architecture at a glance:** link to `docs/architecture.md`, or embed the container view if it's small.
3. **Prerequisites:** the exact .NET SDK version from `global.json`, plus Docker, Node, cloud CLIs and
   anything else, as a numbered list.
4. **Run locally:** ideally one command (`dotnet run --project src/AppHost`, `docker compose up`).
   Give the URL or port it serves on.
5. **Configuration:** where settings live, which are required, and how to supply secrets locally
   (`dotnet user-secrets`, environment variables). **Never include real secret values.**
   Link to `docs/reference/configuration.md` if there are many.
6. **Test:** `dotnet test`, plus how to run integration tests if they need extra setup.
7. **Project layout:** a table mapping `src/*` and `tests/*` projects to one-line responsibilities.
8. **Deploy:** link to the pipeline or runbook, not a copy of it.
9. **Troubleshooting:** only problems people have actually hit.

## package-readme.md (NuGet)

NuGet.org renders a restricted Markdown subset:

- **Relative links and images don't work.** Use absolute URLs.
- **Images must come from NuGet's allow-listed domains**, e.g. `img.shields.io`, `raw.githubusercontent.com`.

Keep it to purpose, install, a minimal example, and links. Wire it up in the packable project
(this was checked with `dotnet pack` on SDK 10):

```xml
<PropertyGroup>
  <PackageReadmeFile>package-readme.md</PackageReadmeFile>
</PropertyGroup>
<ItemGroup>
  <None Include="$(MSBuildThisFileDirectory)../../package-readme.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

With `GenerateDocumentationFile` on, the package automatically includes `lib/<tfm>/<Assembly>.xml`,
which IntelliSense reads for consumers.

## CHANGELOG.md

Use the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format:

```markdown
# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `OrderClient.CancelAsync` for cancelling unshipped orders. (#123)

### Changed
- **Breaking:** `OrderStatus.Pending` renamed to `OrderStatus.Placed`. See [migration notes](docs/migration/v3.md). (#130)

## [2.4.0] - 2026-09-01
...
```

- **Sections:** Added, Changed, Deprecated, Removed, Fixed, Security.
- **Newest release first.** Every entry explains its impact on users and links the PR or issue.
- **Mark breaking changes with `**Breaking:**`** and link a migration note.
- **Don't paste raw commit logs.**
- **Linking from the package:** `PackageReleaseNotes` is free text, so point it at the changelog
  URL rather than duplicating the content.
- **Can't find it written down?** If the repo uses GitHub-generated release notes instead,
  follow that convention and skip CHANGELOG.md.

## CONTRIBUTING.md

1. **Prerequisites:** SDK version and tools, including local dotnet tools (`dotnet tool restore`).
2. **Build and test commands**, copy-paste ready. Include how to run one test project.
3. **Code style:** the `.editorconfig` and analyzers in use, and `dotnet format` if it's part of the flow.
4. **Documentation expectations:**
   - public API changes include XML docs
   - behavior changes update `docs/` and diagrams **in the same PR**
   - significant decisions get an ADR
   - how to run the docs checks locally
5. **Branch, commit and PR conventions**, and what CI checks must pass.

## docs/README.md (index)

```markdown
# Documentation

## Get started
- [Getting started](getting-started.md): install and first use

## How-to guides
- [Configure retries](how-to/configure-retries.md)

## Reference
- [Configuration](reference/configuration.md)
- [API reference](https://…) (DocFX site, if any)

## Explanation
- [Architecture](architecture.md)
- [Architecture decisions](decisions/README.md)
```

List only pages that exist. Update the index whenever you add or remove a page.

## docs/architecture.md

Start from `templates/architecture.md`, a lightweight arc42 structure:

1. Purpose and scope
2. System context view
3. Container view and a project map
4. Key flows, as 1–3 sequence diagrams of the most important or least obvious paths
5. Cross-cutting concerns: configuration, logging and telemetry, error handling, security,
   persistence, messaging. Only the ones with a real, repo-specific answer.
6. Links to the ADR log
7. Constraints and quality goals (optional)

For **libraries**, replace the context and container views with:

- design principles
- the public API shape and extension points
- threading and ownership model
- versioning and compatibility policy (SemVer; `PublicAPI.*.txt` if used)

A library may call this file `docs/design.md`, or split it into `docs/design/*.md`.

## AGENTS.md / CLAUDE.md

Put these at the repo root. Keep them **short, well under 200 lines**.

Studies of agent context files find that repository overviews don't help, and that following
explicit instructions for non-standard practices does. So include only:

- build, test, format and docs-check commands
- conventions an agent couldn't infer from the code, e.g. "all DB access goes through
  `IOrderStore`; never use `DbContext` from controllers"
- pointers to `docs/architecture.md`, the ADR log, and CONTRIBUTING

Don't restate the README. If both files exist, one should point to the other rather than duplicate it.

## DocFX site (libraries that want hosted API docs)

These commands were checked with DocFX 2.81.0 on SDK 10:

```bash
dotnet new tool-manifest        # only if the repo has no dotnet-tools.json yet
dotnet tool install docfx
dotnet docfx init -y -o docs    # creates docs/docfx.json, docs/index.md, docs/toc.yml, docs/docs/
dotnet docfx docs/docfx.json --serve                 # local preview
dotnet docfx docs/docfx.json --warningsAsErrors      # CI: exits non-zero on broken xrefs
```

- `docs/docfx.json` points `metadata.src` at `../src/**/*.csproj`. Narrow it to the packable
  projects, or exclude test projects.
- The generated config uses the `modern` template, which renders Mermaid. Its PDF output does not.
- Add `docs/_site/` and `docs/api/` to `.gitignore`. Both are generated.
- Link to API pages from Markdown with `<xref:Namespace.Type>`. Broken xrefs fail the
  `--warningsAsErrors` build.
- DocFX only picks up types inside a namespace; types in the global namespace are skipped.
- Publish with GitHub Pages: `actions/upload-pages-artifact` + `actions/deploy-pages` on push to `main`.
