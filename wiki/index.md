# The Bridgeon wiki

This is the project's knowledge base, maintained by the agents that build the
code — read it before answering any question about a bridge rule, a scoring
formula, or a schedule property, and update it in the same change that touches
the behaviour it describes. The wiki is the specification; the code is its
current implementation. When they disagree, one of them has a bug, and the
page's cited source decides which.

Every rule page cites the published source it restates. A page with no citation
is a defect.

## Map

| Page | What it holds |
|---|---|
| `rules/` | One page per scoring or tournament rule, citing its published source. |
| `log.md` | Append-only work log: what was done, when, and what was learned. |

Decision records live in `docs/decisions/`, not here — a decision is about this
project; the wiki is about the domain.

## Maintenance discipline

- **Update with the change.** A `feat:` commit that alters rule behaviour
  updates the wiki page in the same commit. A rules page that lags the code is
  worse than no page.
- **`log.md` is append-only.** Entries are never edited or removed; a wrong
  entry is corrected by a later entry saying so.
- **Cite, don't copy.** Pages restate published laws and formulas in this
  project's own words, with the source named. Nothing is transcribed from any
  other program's text (see `CLAUDE.md`, "Original content only").
