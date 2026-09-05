# Work log

Append-only. Entries are never edited or removed; correct a wrong entry with a
later entry. Newest at the bottom.

---

## 2026-09-05 — repository reset, wiki seeded

The repository was rebuilt from a fresh root with the content rules in
`CLAUDE.md` in force from the first commit. P0 (content gates, solution
scaffold, Law 78 IMP scale, architecture tests, CI) is complete: 32 tests pass
locally on .NET SDK 10.0.400.

This wiki was seeded today as part of adopting agent-driven development:
`index.md` sets the maintenance discipline, `rules/imp-scale.md` documents the
one rule implemented so far. Decisions 0004 (LLM-maintained wiki) and 0005 (own
table clients before scoring hardware) were recorded at the same time.

Next: P1 — contract notation, the Law 77 score table, victory points.

## 2026-09-05 — P1 complete: the scoring core

Four test-first pairs, each with its rules page written before its tests:

- **Contract notation** (`wiki/rules/contract-notation.md`, our own spec): a
  hand-rolled scanner with typed rejections that never throws; 5,880-entry
  round-trip through canonical notation.
- **Law 77 duplicate score** (`wiki/rules/duplicate-score.md`): all 2,940 cases
  enumerated against a component-wise oracle transcribed from the Law;
  monotonicity in tricks taken asserted across the whole space.
- **Victory points** (`wiki/rules/victory-points.md`): the WBF continuous
  formula, asserted against fixed points derived by hand from φ² = 1 − φ —
  exactly 15.00 VP at d = B/3. The derivations are on the wiki page.
- **Board vulnerability** (`wiki/rules/board-vulnerability.md`): the printed
  16-board cycle transcribed as data, with the projection to the declaring
  side's vulnerability that Law 77 consumes.

134 tests pass in about half a second. Lesson worth keeping: writing the wiki
page first caught two of my own errors before they reached code — a doubling
written on the wrong side of the seat, and a mis-remembered 3NTX+1 value.

Next: P2 — the schedule engine.
