---
name: dotnet-docs-sync
description: Keep documentation in step with code by checking a change set (uncommitted work, a branch, or a PR diff) for documentation impact, then updating XML doc comments, inline comments, README and docs pages, Mermaid diagrams, CHANGELOG, generated docs, and ADRs in the same change. Use after finishing a feature, fix, or refactor in a .NET repo, before committing or opening a PR, when asked "what docs need updating?" or to "check docs drift", or when reviewing a PR for missing documentation.
---

# Keep docs in sync with a change

Docs rot when they're updated "later". This skill turns a diff into a concrete list of
documentation impacts and resolves them **in the same change**. It uses these skills:

- `dotnet-xml-docs`: API comments
- `mermaid-diagrams`: diagrams
- `architecture-decision-records`: decisions
- `dotnet-repo-docs`: README, docs pages, CHANGELOG, and docs CI

Run it at the end of every non-trivial code change. For a change with no public surface and no
behavior change, the pass takes a minute: check the comments next to the changed lines, and stop.

## Step 1: Establish the change set

```bash
base=""
default=$(git symbolic-ref --short refs/remotes/origin/HEAD 2>/dev/null)
for c in "$default" origin/main origin/master main master; do
  [ -n "$c" ] && base=$(git merge-base HEAD "$c" 2>/dev/null) && break
done
[ -n "$base" ] || echo "No base branch found. Ask which branch or commit to diff against." >&2

git diff --name-status "$base" HEAD        # committed on this branch (skip if base is empty)
git diff --name-status                     # unstaged
git diff --name-status --cached            # staged
git ls-files --others --exclude-standard   # new, untracked files
```

For a PR, use `gh pr diff <n> --name-only` and `gh pr diff <n>`. Read the actual hunks, not
just the file names.

**An empty change set is only "no impact" if you found the base.** If no base was found, say so
and ask; don't report "nothing to update".

## Step 2: Classify each change and derive its doc impact

| Change in the diff | Documentation to update |
| --- | --- |
| **Public/protected API added** | XML docs on every new member (`dotnet-xml-docs`). `PublicAPI.Unshipped.txt` if the repo uses it: `dotnet format analyzers <proj> --diagnostics RS0016 --severity info`. CHANGELOG *Added*. README or how-to if it's a new entry point. |
| **Public API renamed, changed, or removed** | Update its docs. Search for the old name everywhere (Step 3). CHANGELOG *Changed/Removed*, marked **Breaking**, with a migration note. Update samples and snippets. |
| **Behavior changed, signature unchanged** (the dangerous case) | Re-read the `<summary>`, `<returns>`, `<remarks>` and `<exception>` of every public member whose body changed, and fix anything no longer true. Update docs pages that describe the behavior. CHANGELOG *Changed/Fixed*. |
| **Thrown exceptions changed** (new or removed `throw`, `ThrowIfNull`, and similar) | `<exception>` tags on the affected members. |
| **Nullability changed** (`?` added or removed on a public signature) | Docs must say what `null` means now, or stop mentioning it. |
| **Configuration changed** (options classes, `appsettings` keys, env vars, CLI flags, defaults) | The configuration reference, the README configuration section, and example settings files. |
| **Project, service, external dependency, queue, or database added or removed** | Container and context diagrams, the project table in the architecture doc, the README project layout, and CONTRIBUTING prerequisites. Offer an ADR. |
| **Flow changed** (endpoint, message type, handler order, protocol, retry behavior) | The affected sequence diagram, API or how-to docs, and any `.http` sample files. |
| **Enum or state transition changed** | The state diagram, and XML docs on the enum members. |
| **EF Core model or migration changed** | Regenerate the ERD. Don't hand-edit a generated one. Update data docs. |
| **Build, test, or run changed** (`global.json`, TFMs, CI, scripts, Dockerfile, AppHost) | README prerequisites and run steps, CONTRIBUTING, and AGENTS.md/CLAUDE.md commands. |
| **New framework or pattern, or a reversal of an earlier approach** | An ADR (`architecture-decision-records`). Search the decision log for the topic; if an accepted ADR is contradicted, supersede it. |
| **Code deleted** | Remove the doc sections, diagram nodes, how-tos and snippets that describe it. |
| **Files moved or renamed** | Relative links and snippet source paths. |
| **Any edited method** | Inline `//` comments within and next to the changed lines. Are they still true? |

These are the docs to update **if they exist**. When the right home for a fact doesn't exist
(e.g. there is no configuration reference), don't create new documents as part of a sync pass.
List the gap in the report, and suggest the `dotnet-repo-docs` skill.

## Step 3: Hunt for stale references

For every renamed or removed identifier, moved path, and changed setting key:

```bash
git grep -n -i -e "OldName" -e "old/path"
```

`git grep` searches every tracked file, so build output is already excluded. That covers Markdown,
`///` and `//` comments, `cref`s, csproj/props, YAML, `.http` files, Dockerfiles, and `.mmd` files. With doc enforcement on, the build
catches stale `cref`s and `<param>` names (CS1572/CS1574), **but not prose**. Also search the ADR
log and diagrams for component names that changed.

## Step 4: Apply the updates

- **Read the code, then write.** Never describe behavior you haven't confirmed in the code or tests.
- **Follow each sibling skill's rules** for its artifact, and the repo's existing conventions.
- **Change only what the diff affects.** Don't rewrite unrelated docs in passing. If you notice
  unrelated drift, list it in the report instead.
- **Never edit the body of an accepted ADR.** Supersede it instead.
- **Regenerate generated docs** instead of editing them: snippets (`dotnet mdsnippets`), ERD
  tests, `PublicAPI.Unshipped.txt`, DocFX.

## Step 5: Verify

Run whichever of these the repo supports:

```bash
dotnet build                                          # doc-comment errors (CS15xx), RS0016
npx -y markdownlint-cli2                              # if the repo has a markdownlint config
out=$(mktemp -d) && npx -y @mermaid-js/mermaid-cli -i <changed.md> -o "$out/out.md"   # each changed file with Mermaid
dotnet mdsnippets && git diff --exit-code -- '*.md'   # if MarkdownSnippets is used
lychee --offline --no-progress './**/*.md'            # if lychee is installed
```

If a check can't run, say which one and why. Don't report the docs as verified.

## Step 6: Report

End with a short table so the author can review the doc changes alongside the code:

| Impact | Action | Files |
| --- | --- | --- |
| New `OrderClient.CancelAsync` | XML docs added; CHANGELOG *Added* | `src/…/OrderClient.cs`, `CHANGELOG.md` |
| Fulfilment now via Service Bus | Container + sequence diagrams updated; ADR-0009 proposed | `docs/architecture.md`, `docs/decisions/0009-….md` |
| `RetryCount` default 3 → 5 | Configuration reference updated | `docs/reference/configuration.md` |
| No impact | Private refactor of `PriceCalculator`; comments re-checked | — |
| **Needs input** | Why was Service Bus chosen over Storage Queues? ADR context incomplete | `docs/decisions/0009-….md` |

Put anything you couldn't resolve without the author under **Needs input**. Don't guess.
