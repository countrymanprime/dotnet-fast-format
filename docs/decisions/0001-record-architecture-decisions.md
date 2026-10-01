# 0001. Record architecture decisions

- **Status:** Accepted
- **Date:** 2026-10-01
- **Deciders:** countrymanprime

## Context and problem

The project is starting from nothing, and several choices (how to print, how to gate changes) are
expensive to reverse. The reasons behind them are easy to lose, especially when much of the code
is written with AI assistance and each session starts without memory of earlier ones.

## Decision drivers

- Reasons must live next to the code and be reviewed in pull requests.
- Cheap enough that a solo maintainer actually keeps it up.
- Readable by an AI agent at the start of a session.

## Considered options

1. MADR-style ADRs in `docs/decisions/`
2. Decisions only in the PRD and commit messages
3. No formal record

## Decision outcome

**Chosen option: MADR-style ADRs in `docs/decisions/`**, because they are versioned and reviewed with
the code, and an append-only log stays trustworthy as the code changes. The `architecture-decision-records`
skill in `.claude/skills/` holds the template and workflow.

### Consequences

- **Good:** the reasoning behind hard-to-reverse choices is recorded and linkable.
- **Bad:** a small amount of process for each significant decision.
- **Neutral:** the PRD keeps scope and intent; ADRs keep the "why" of individual choices.

### Confirmation

Review checklist: a pull request that makes a hard-to-reverse decision includes its ADR.

## More information

Format and workflow: [`.claude/skills/architecture-decision-records/SKILL.md`](../../.claude/skills/architecture-decision-records/SKILL.md).
