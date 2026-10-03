# Corpus

Real-world C# repositories the formatter is checked against (idempotent, tree-preserving, no loss;
see `docs/requirements/core.md`, FMT-001 to FMT-005). The list is pinned so every run, local or CI,
sees the same source.

`corpus.json` lists each repository with:

| Field | Meaning |
|---|---|
| `name` | Directory name under `.corpus/` (lowercase letters, digits, `.`, `-`, `_`) |
| `url` | `https://github.com/<owner>/<repo>` |
| `tag` | Release tag the pin was taken from. Informational |
| `sha` | Full 40-character commit hash that is fetched. This is the pin |
| `license` | SPDX identifier, checked against the repository's license file |

The files are fetched at test time into `.corpus/` (git-ignored) and are never committed or
redistributed. The tests validate the manifest on every run (`CorpusManifestTests`).

## Updating a pin or adding a repository

1. Pick a stable release tag and resolve its commit: `git ls-remote https://github.com/<owner>/<repo>.git "refs/tags/<tag>^{}"`
   (use the `refs/tags/<tag>` line when the tag is not annotated).
2. Read the repository's license file and record its SPDX identifier. Use only permissive licenses
   (MIT, Apache-2.0, BSD).
3. Edit `corpus.json`, then run `dotnet test`. Moving a pin changes what the corpus checks, so do it
   in its own change and say why.
