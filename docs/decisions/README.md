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
| [0006](0006-formatting-speed-baseline.md) | Set the formatting speed target relative to CSharpier | Accepted | 2026-10-04 |
| [0007](0007-default-style-is-microsoft-conventions-at-100-columns.md) | Use Microsoft conventions at 100 columns as the default style | Accepted (provisional) | 2026-10-04 |
| [0008](0008-print-unsupported-syntax-verbatim.md) | Print unsupported syntax and trivia-bearing nodes verbatim | Accepted (provisional); rules 2 and 3 amended by 0010 and 0011 | 2026-10-04 |
| [0009](0009-self-check-output-before-writing.md) | Check the output before writing a file | Accepted (provisional) | 2026-10-04 |
| [0010](0010-keep-comments-at-node-boundaries.md) | Keep comments at node boundaries and copy every other position verbatim | Accepted (provisional) | 2026-10-05 |
| [0011](0011-format-the-no-symbols-parse-and-keep-directives-as-lines.md) | Format the no-symbols parse and keep preprocessor directives as lines | Accepted (provisional) | 2026-10-05 |
