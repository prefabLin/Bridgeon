# The duplicate score

**Source:** Law 77, *The Laws of Duplicate Bridge* (WBF, 2017) — the duplicate
scoring table.
**Implementation:** `src/Bridgeon.Core/Scoring/DuplicateScore.cs`.
**Tests:** `tests/Bridgeon.Core.Tests/DuplicateScoreTests.cs` enumerates the
whole space — 7 levels × 5 strains × 3 doublings × 2 vulnerabilities × 14 trick
counts = 2,940 cases — against an oracle transcribed from the Law's tables.

The score is from the declaring side's perspective: positive when the contract
makes, negative when it goes down, zero for a passed-out board. Only the
declaring side's vulnerability matters.

## Contract made (tricks taken ≥ level + 6)

**Trick score** — for the contracted tricks only:

| Strain | Per trick |
|---|---|
| Clubs, diamonds | 20 |
| Hearts, spades | 30 |
| No-trump | 40 for the first, 30 for each later trick |

Doubling multiplies the trick score by 2, redoubling by 4.

**Overtricks** — per trick over the contract:

| | Not vulnerable | Vulnerable |
|---|---|---|
| Undoubled | trick value (20 or 30) | trick value |
| Doubled | 100 | 200 |
| Redoubled | 200 | 400 |

**Bonuses** — all that apply, judged on the (doubled) trick score:

- Game (trick score ≥ 100): 300 not vulnerable, 500 vulnerable. Otherwise the
  part-score bonus of 50. A doubled part-score can be "doubled into game".
- Small slam (level 6): 500 / 750. Grand slam (level 7): 1000 / 1500, each in
  addition to the game bonus.
- Making a doubled contract: 50 ("the insult"); redoubled: 100.

## Contract down (n undertricks)

| Undertrick | Undoubled NV | Undoubled V | Doubled NV | Doubled V |
|---|---|---|---|---|
| 1st | 50 | 100 | 100 | 200 |
| 2nd, 3rd | 50 each | 100 each | 200 each | 300 each |
| 4th onward | 50 each | 100 each | 300 each | 300 each |

Redoubled undertricks cost exactly twice the doubled amounts. The total goes to
the defending side, so the declaring side's score is its negation.

## Fixed points of the table

`7NTXX= vulnerable` is the largest score, 2,980; thirteen down redoubled
vulnerable is the smallest, −7,600. The score never falls as declarer takes one
more trick.
