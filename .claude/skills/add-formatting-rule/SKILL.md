---
name: add-formatting-rule
description: Step-by-step procedure for teaching the formatter to handle a C# syntax construct or style rule - write the fixture first, implement the printer for that node kind, handle comments and trivia, verify the invariants, review the corpus diff, and update docs. Use when adding or changing how a node kind is printed, when adding an .editorconfig-driven rule, or when a bug report shows a construct formatted wrongly.
---

# Add or change a formatting rule

Follow the steps in order. Each ends with a check; do not move on until it passes.

## 0. Frame it

- Name the construct (for example `ConditionalExpression`) and the requirement ID it satisfies. If
  there is none, add one (`prd-and-requirements`).
- If this adds an option or changes stable output style, write the ADR first
  (`architecture-decision-records`) and plan a style-changelog entry.

## 1. Write the fixture first

- Create `tests/golden/<area>/<name>.in.cs` with the input, covering: the common shape, a long
  line that must break, a short line that must not, and comments in awkward places.
- Write the expected output by hand, from the style rules, not by running the formatter.
- Run the test. It must fail for the right reason.

Check: the test fails and the failure matches the gap.

## 2. Implement the printer

- Add or change the printer for the node kind. Build the output from doc-printer primitives
  (group, indent, line, softline, fill); do not concatenate strings.
- Decide break behavior explicitly: what stays on one line when it fits, and what breaks first
  when it does not.
- Keep one construct per change. If you find a second problem, write it down and fix it in its
  own change.

Check: the new fixture passes.

## 3. Handle comments and trivia

- Add fixtures with a comment before, inside and after each child of the construct, and with
  blank lines between them.
- Make sure no comment moves across code or is dropped, and that blank-line handling matches the
  rationale doc.
- If the construct can contain preprocessor directives, add a fixture for each position.

Check: no-loss invariant passes on all fixtures.

## 4. Run the invariants

Use the `formatter-verification` skill: idempotency, tree equivalence and valid output must hold
for every new fixture. If idempotency fails, the printer is deciding differently on its own
output; fix the decision, not the test.

## 5. Review the corpus diff

Format the pinned corpus with the base revision and with this change, and read the diff. Every
changed region should be explainable by this rule. Unexpected changes are bugs or a missing
fixture; add the fixture and fix the code.

## 6. Options and `.editorconfig`

If the rule is driven by an `.editorconfig` key, document whether the key is honored or ignored
and why, in the deviations table. Prefer reusing an existing key over inventing one. Test both
the default and the configured value.

## 7. Finish

- [ ] Fixture added, passing, and named after the behavior
- [ ] Comment and trivia fixtures added
- [ ] Invariants pass on all fixtures; corpus run has no crashes
- [ ] Corpus diff reviewed and explained
- [ ] Style-changelog entry if stable output changed
- [ ] Docs, XML comments and ADR updated (`dotnet-docs-sync`)
- [ ] The full verify gate from `AGENTS.md` passes
