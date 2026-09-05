# Victory points

**Source:** the World Bridge Federation's continuous victory-point scale, as
adopted in `docs/decisions/0002-victory-points-from-the-published-formula.md`.
**Implementation:** `src/Bridgeon.Core/Scoring/VictoryPointScale.cs`.
**Tests:** `tests/Bridgeon.Core.Tests/VictoryPointsTests.cs` pins the fixed
points derived below and the scale's properties.

A round's net IMP difference `d` converts to a 20-point VP split:

```
VP_win  = 10 + 10 · (1 − τ^(d/B)) / (1 − τ)
VP_lose = 20 − VP_win
τ = φ³ where φ = (√5 − 1)/2,   B = 15·√(boards per round)
```

saturating at 20:0 once `d ≥ B`. The winner's VP is published to two decimal
places, rounded half away from zero; the loser's is the exact complement to
20.00, so a round always distributes exactly 20 VPs.

## Fixed points, derived by hand

φ is the golden-ratio conjugate, so **φ² = 1 − φ**. That identity collapses the
formula at thirds of B, which is exactly why the WBF chose τ = φ³:

- `d = 0` → 10.00 : 10.00.
- `d = B/3` → τ^(1/3) = φ, and 1 − τ = (1 − φ)(1 + φ + φ²) = 2(1 − φ), so
  VP = 10 + 10/2 = **15.00 exactly**.
- `d = 2B/3` → τ^(2/3) = φ² = 1 − φ, so VP = 10 + 10φ / 2φ² = 10 + 5/φ =
  10 + 5(φ + 1) = **18.09** (1/φ = φ + 1).
- `d = B/2` → 10 + 10(1 − √τ)/(1 − τ) = **16.73**.
- `d ≥ B` → **20.00 : 0.00**.

With 16 boards, B = 60, so d = 20, 30, 40, 60 hit these points at integer IMP
differences; with 9 boards, B = 45 and d = 15 gives 15.00 again.

## Notes

- Printed tables in circulation can differ from the formula by a cent of a VP
  at scattered points depending on how they were rounded (decision 0002).
  Bridgeon follows the formula; a ruleset can select a supplied table instead.
- The formula extends to any board count, which the schedule engine relies on —
  a director may ask for a round length nobody has tabulated.
