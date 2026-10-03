# Architecture decisions

This folder records significant architecture and design decisions as
[Architecture Decision Records](https://adr.github.io/), using the [MADR](https://adr.github.io/madr/) format.

- **One decision per file:** `NNNN-short-imperative-title.md`, numbered in order.
- **Append-only.** An accepted record is never rewritten. To change a decision, add a new record
  that supersedes it, and update the old record's status line.
- **Statuses:** Proposed, Accepted, Rejected, Deprecated, Superseded.

## Index

| ADR | Title | Status | Date |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted | 2026-10-01 |
| [0002](0002-build-a-doc-printer-on-roslyn-syntax-trees.md) | Build our own doc-printer on Roslyn syntax trees | Accepted | 2026-10-01 |
| [0003](0003-use-dotnet-test-as-the-single-gate.md) | Use `dotnet test` as the single gate | Accepted | 2026-10-01 |
| [0004](0004-enforce-style-with-analyzers-and-dotnet-format.md) | Enforce code style with analyzers and `dotnet format` | Accepted | 2026-10-02 |
| [0005](0005-tier-tests-with-a-slow-trait.md) | Tier tests with a `Slow` trait | Accepted | 2026-10-03 |
