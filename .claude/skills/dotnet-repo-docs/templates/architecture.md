# Architecture

<!-- Template: replace every {placeholder} (and every UPPER_CASE placeholder inside the Mermaid
     diagrams, which use that form so the template still renders) with what the code actually contains, and delete
     any section that has no real, repo-specific content. Keep this document about structure
     and intent; generated reference (API docs, ERDs) lives elsewhere and is linked. -->

## Purpose and scope

{One or two paragraphs: what the system does, who uses it, and what is deliberately out of scope.}

## System context

{One sentence on what the view shows.}

```mermaid
flowchart LR
  accTitle: SYSTEM_NAME - system context
  accDescr {
    DESCRIBE who uses the system and which external systems it depends on, in plain language.
  }
  user(["USER_ROLE"])
  system["SYSTEM_NAME"]
  ext["EXTERNAL_SYSTEM (PURPOSE)"]

  user -->|"WHAT_THEY_DO"| system
  system -->|"WHAT_FLOWS via PROTOCOL"| ext

  classDef external stroke-dasharray: 5 5
  class ext external
```

## Containers and projects

```mermaid
flowchart LR
  accTitle: SYSTEM_NAME - container view
  accDescr {
    DESCRIBE each runnable part and how they communicate.
  }
  subgraph sys ["SYSTEM_NAME"]
    app["APP_NAME (TECH)"]
    db[("DATABASE_NAME (TECH)")]
  end
  app -->|"EF Core / HTTP / ..."| db
```

| Project | Responsibility | Depends on |
| --- | --- | --- |
| `src/{Project}` | {One line} | {Projects or services} |
| `tests/{Project}.Tests` | {What it covers} | {Projects} |

## Key flows

### {Flow name, e.g. "Placing an order"}

{One sentence on why this flow matters or is non-obvious.}

```mermaid
sequenceDiagram
  accTitle: FLOW_NAME - sequence
  accDescr {
    DESCRIBE the flow step by step, including the important failure path.
  }
  participant A as CALLER
  participant B as SERVICE
  A->>B: CALL
  B-->>A: RESPONSE
```

## Cross-cutting concerns

<!-- Keep only the concerns that have a real, repo-specific answer. -->

- **Configuration:** {where settings live, how they bind (options pattern), and how secrets are supplied}
- **Logging and telemetry:** {logging framework, OpenTelemetry, correlation}
- **Error handling:** {exception strategy, ProblemDetails, retries}
- **Security:** {authentication and authorization model}
- **Persistence:** {data access approach, migrations; link the generated data-model doc if there is one}

## Decisions

Significant decisions are recorded in the [decision log](decisions/README.md). The most
relevant to this architecture are:

- [ADR-{NNNN}](decisions/{NNNN}-{title}.md): {title}

## Constraints and quality goals

{Optional: hard constraints (platform, compliance) and the top quality goals, in priority order.}
