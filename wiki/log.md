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
