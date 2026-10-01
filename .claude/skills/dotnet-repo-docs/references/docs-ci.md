# Docs CI and local checks

The workflow template is at `templates/docs.yml`. It has three always-on jobs (markdownlint,
links, Mermaid) and two optional ones (DocFX, MarkdownSnippets).

**Where doc-comment errors are caught:** in the normal `dotnet build` in the repo's main CI, once the
`dotnet-xml-docs` enforcement is in `Directory.Build.props`. They don't need a separate job.

**Required checks and path filters:** if you make the docs jobs required in branch protection,
don't add `paths:` filters to the workflow. A required check that never runs blocks the PR.

## markdownlint

Put `.markdownlint-cli2.jsonc` at the repo root:

```jsonc
{
  "config": {
    "default": true,
    "MD013": false,                                  // line length: prose wraps differently per editor
    "MD024": { "siblings_only": true },              // allow repeated headings in different sections (CHANGELOG)
    "MD033": { "allowed_elements": ["a", "sup", "br", "details", "summary", "img"] }, // MarkdownSnippets emits <a>/<sup>
    "MD031": true,                                   // set to false if using MarkdownSnippets: it puts an <a> tag directly above each fence
    "MD060": false                                   // table pipe spacing: newer rule that flags the common |---| style; too noisy
  },
  "globs": ["**/*.md"],
  "ignores": ["**/bin/**", "**/obj/**", "**/node_modules/**", "**/_site/**", "docs/api/**", "**/*.verified.md", "**/*.received.md"]
}
```

To run it locally, pass no globs so that it uses the config's `globs` and `ignores`:

```bash
npx -y markdownlint-cli2
npx -y markdownlint-cli2 --fix     # auto-fix what it can, then review the diff
```

## Links (lychee)

- **CI:** `lycheeverse/lychee-action`. On pull requests it checks relative links only
  (`--offline`), so a third-party outage can't block unrelated PRs. External URLs are checked on
  pushes and on a weekly schedule.
- **URLs to skip:** list them, one regex per line, in `.lycheeignore`, e.g. `^https://localhost` or links behind auth.
- **Locally:** if lychee is installed (`cargo install lychee`, `brew install lychee`, or `winget install lycheeverse.lychee`):

  ```bash
  lychee --offline --no-progress './**/*.md'   # relative links only: fast, and no network needed
  lychee --no-progress './**/*.md'             # include external URLs
  ```

- **Without lychee:** check that each relative Markdown link target exists before committing.

## Mermaid

The CI job renders every tracked Markdown file that contains a mermaid block, using
mermaid-cli, and fails if any block doesn't parse. See the `mermaid-diagrams` skill for the local command.
mermaid-cli bundles a newer Mermaid than GitHub, so a pass means the syntax is valid, not
that GitHub already supports every feature used.

## Code samples that can't rot (MarkdownSnippets)

MarkdownSnippets copies code regions from source files, usually tests, into Markdown, so
documented examples are always code that compiles and runs. This was checked with
MarkdownSnippets.Tool 28.4.4.

```bash
dotnet new tool-manifest                           # only if the repo has no dotnet-tools.json yet
dotnet tool install MarkdownSnippets.Tool          # adds it to the local tool manifest
echo '{ "Convention": "InPlaceOverwrite" }' > mdsnippets.json
```

In a test or sample file:

```csharp
// begin-snippet: CreateOrder
var order = await client.CreateOrderAsync(new CreateOrder("SKU-1", Quantity: 2));
// end-snippet
```

In any Markdown file, put `snippet: CreateOrder` on its own line, then run `dotnet mdsnippets`.
The line becomes the code block, with a link to its source. Re-running refreshes it in place.
`#region CreateOrder` / `#endregion` also works as a marker.

In CI, run `dotnet mdsnippets`, then `git diff --exit-code -- '*.md'`. This is the optional
`snippets` job in the template.

**Alternatives:**

- **DocFX sites:** `[!code-csharp[](../src/Samples/Orders.cs#CreateOrder)]` includes a
  `#region` at build time.
- **XML doc `<example>`s:** `<code source="../samples/X.cs" region="Y"/>`, which DocFX supports.

## Optional prose and spelling checks

Add these only if the team wants them. They create more review noise than the checks above.

- **Vale** (prose style), with `errata-ai/vale-action`. Add `.vale.ini` with `Packages = Microsoft`
  and `vale sync`. The Microsoft package is community-maintained and not endorsed by Microsoft.
  Use `filter_mode: added` so only changed lines are flagged.
- **cspell**, with `streetsidesoftware/cspell-action`. Add `cspell.json` with a `words` list for
  domain and .NET terms (`dotnet`, `csproj`, `nullable`, product names). Use
  `incremental_files_only: true` on PRs.

## Before committing docs changes, locally

```bash
dotnet build                                   # CS15xx doc-comment errors, if enforced
npx -y markdownlint-cli2
out=$(mktemp -d) && npx -y @mermaid-js/mermaid-cli -i <changed-file>.md -o "$out/out.md"   # per file with Mermaid
dotnet mdsnippets && git diff --stat           # if the repo uses MarkdownSnippets
dotnet docfx docs/docfx.json --warningsAsErrors  # if the repo uses DocFX
```
