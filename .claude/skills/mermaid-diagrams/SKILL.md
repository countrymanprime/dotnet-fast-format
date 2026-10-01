---
name: mermaid-diagrams
description: Create, update, review, and validate Mermaid diagrams in repository Markdown, such as system context and container views, sequence flows, state machines, and entity-relationship models, so they render on GitHub, Azure DevOps, and DocFX and stay accurate as code changes. Use when a change alters architecture, request or message flow, a lifecycle, or the data model; when asked to diagram, visualize, or draw how something works; or when reviewing or fixing existing ```mermaid blocks.
---

# Mermaid diagrams in code repositories

A diagram in a repo is only worth having if it is **correct**, **small**, and **maintained with
the code**. This skill decides whether a diagram earns its place, picks the right level and
type, writes it so every target renderer can display it, and validates it.

Reference files:

- [references/templates.md](references/templates.md): ready-to-adapt templates for context, container, sequence,
  state, ER, and pipeline diagrams, with accessibility fields filled in. **Start from these.**
- [references/compatibility-and-tooling.md](references/compatibility-and-tooling.md): what each renderer supports, syntax pitfalls,
  validation commands, and generating diagrams from code (EF Core ERDs and others).

## Step 1: Decide whether a diagram earns its place

Draw one when a reader needs to see **relationships across files or processes**. Examples:
which services talk to which, the order of calls in a flow, the states an entity moves
through, or how tables relate.

Don't draw:

- **Anything at code level.** Class diagrams of most classes duplicate the code and rot fastest.
  At most, show a small, stable domain core.
- **Something a sentence or a bulleted list explains just as well.**
- **Something a tool can generate.** Generate it instead; see the tooling reference.

## Step 2: Detect the context

1. **Find existing diagrams:** `git grep -n '```mermaid' -- '*.md'`. Also look for `*.mmd` files. Update an existing
   diagram rather than adding a near-duplicate. Match its style and naming.
2. **Identify the renderers:**
   - A `github.com` remote means GitHub.
   - A `dev.azure.com` or `visualstudio.com` remote, or a `.wiki` folder, means Azure DevOps.
   - A `docfx.json` means DocFX.

   Write for the **most limited** of them (see the compatibility reference). If Azure DevOps is
   one of them, avoid C4 and `architecture-beta`, and use `graph` instead of `flowchart`.
3. **Read the code the diagram describes.** Every node and arrow must correspond to something
   real: a project, a service, an HTTP call, a queue, a table. Never draw from memory or from
   the old diagram alone.

## Step 3: Pick the level and type

Follow the C4 model's levels. Most repos need only the first two, plus a few dynamic views.

| Question the reader has | Diagram | Mermaid type |
| --- | --- | --- |
| What is this system, and who or what does it talk to? | System context | `flowchart` |
| What are the deployable or runnable parts, and how do they communicate? | Container | `flowchart` with a `subgraph` system boundary |
| What happens, in what order, for request or use case X? | Dynamic / sequence | `sequenceDiagram` |
| What states can entity X be in, and what moves it between them? | State | `stateDiagram-v2` |
| How is the data structured? | ER | `erDiagram`, **generated** where possible |
| What are the stages of a build, pipeline, or process? | Flow | `flowchart LR` |

Prefer `flowchart` over Mermaid's `C4Context`/`C4Container` syntax. The C4 syntax is marked
experimental, has no automatic layout, and Azure DevOps doesn't render it. You still follow C4's
levels and conventions; you just draw them with `flowchart`.

## Step 4: Author it

Rules that apply to every diagram:

1. **One concern per diagram.** Keep it to about 15 nodes, or about 8 participants in a sequence.
   Split anything bigger by level or by use case.
2. **Give every diagram a title and description:** put `accTitle:` and `accDescr { … }` right
   after the diagram-type line. Screen readers use them, and they force you to state the
   diagram's point.
3. **Label every arrow that carries a call, data, or message** with what flows along it and how:
   `-->|HTTPS/JSON|`, `-->|publishes OrderPlaced|`, `-->|EF Core|`. An unlabeled arrow in a
   context or container view is ambiguous. The exceptions are plain "then" steps in a pipeline
   flow, and composition or association lines in a class diagram, which can stay unlabeled.
4. **Name nodes after the real things,** and include the technology in parentheses where it helps:
   `api["Orders API (ASP.NET Core)"]`. Use stable, readable IDs (`api`, `ordersDb`) so diffs
   stay readable.
5. **Quote any label that contains punctuation:** `["Web app (Blazor)"]`. Never use the bare
   lowercase word `end` as a node ID.
6. **Don't hard-code colors or themes.** They break in GitHub dark mode. If you need emphasis,
   use a theme-neutral class, such as `classDef external stroke-dasharray: 5 5` for systems you
   don't own.
7. **Avoid HTML in labels** (`<br/>` and friends). Azure DevOps rejects it.
8. **Put a short prose summary next to the diagram** that says the same thing. The diagram
   supplements the text; it doesn't replace it.

## Step 5: Place it

- **Architecture views** go in `docs/architecture.md` or `docs/architecture/*.md`, following the
  repo's existing layout. The top-level context or container view should also be linked from the
  README.
- **Flow, state and ER diagrams** go in the doc for that feature or area, next to the prose about it.
  They can also go in the relevant ADR, if the diagram explains that decision.
- **Keep the source in the Markdown file** so it diffs with the code. Don't commit exported PNG or
  SVG files unless a renderer can't handle Mermaid, and if you do, keep the `.mmd` source next to
  them.

## Step 6: Validate

Always validate after writing or editing a diagram:

```bash
out=$(mktemp -d)   # mmdc does not create the output directory, so use an existing one
npx -y @mermaid-js/mermaid-cli -i docs/architecture.md -o "$out/architecture.md"
```

This renders every Mermaid block in the file to an SVG, and exits non-zero on a syntax error.
The first run downloads a headless Chromium. If you can't run it, say so explicitly; don't claim the
diagram is valid. Also re-check the rules above by eye: labeled arrows, titles, and no
colors.

## Keeping diagrams current

When code changes, search the docs for the names of the things that changed, such as
services, projects, queues, entities, and endpoints:
`git grep -n -i "<name>" -- '*.md' '*.mmd'`. For each diagram that mentions them:

- **A component was added or removed:** update the context or container view.
- **A call order, protocol, or message changed:** update the sequence diagram.
- **A status or transition changed:** update the state diagram.
- **The schema changed:** regenerate the ERD. Don't hand-edit a generated one.

Update the diagram in the **same change** as the code. If a diagram is no longer worth
maintaining, delete it. A wrong diagram is worse than none.

## Review checklist

- [ ] Every node and arrow matches the current code.
- [ ] Every arrow that carries a call, data, or message is labeled.
- [ ] `accTitle` and `accDescr` are present.
- [ ] It has one concern and 15 nodes or fewer.
- [ ] There are no colors, themes, or HTML.
- [ ] It renders (mermaid-cli, or a preview on the target platform).
- [ ] Prose next to the diagram says the same thing.
- [ ] Nothing below the component level unless it's generated.
