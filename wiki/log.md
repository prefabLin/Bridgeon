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

## 2026-09-05 — the mutation gate repaired, and a review round

Two findings from running the gates for real rather than trusting them:

- **The mutation gate had never actually run.** Stryker's project analysis
  finds zero source projects when the TargetFramework lives only in
  Directory.Build.props — reproduced minimally with a fresh classlib pair.
  Every CI run had failed at that step since the first push. Fixed by declaring
  the framework in each csproj, with two architecture tests replacing the
  "one place to edit" guarantee (decision 0001, amended).
- **An independent review agent audited the P1 diff** and returned seven
  confirmed findings, all now fixed test-first: culture-sensitive
  decimal.Parse in the VP tests, parser rejections that violated their own
  wiki page (+14 must be an impossible overtrick, messages must quote exactly
  what was typed), an int overflow in the vulnerability cycle, missing enum
  guards on Contract, a dead int.MinValue branch, an open BoardEntry
  hierarchy, and thin fixed-point coverage for shared overtrick values.

First honest mutation score: 88.32. After killing the genuine survivors and
configuring string-literal mutants out (message wording is cosmetic; the
reason enums and quoted fragments are asserted): **97.57**, with only provably
equivalent boundary mutants surviving. 154 tests.

Process note: the review agent caught real defects the enumeration missed —
the whole-space oracle shares blind spots with the implementation it checks.
Keep the pattern: an independent review pass after every phase.
