# The IMP scale

**Source:** Law 78B, *The Laws of Duplicate Bridge* (WBF, 2017).
**Implementation:** `src/Bridgeon.Core/Scoring/ImpScale.cs` (`ImpScale.Law78`).
**Tests:** `tests/Bridgeon.Core.Tests/ImpScaleTests.cs` transcribes the Law's
thresholds independently of the implementation.

In International Match Point scoring, the difference between two total-point
scores on a board is converted to IMPs on the published scale. The scale is a
partition of the non-negative differences into 25 contiguous bands awarding 0
through 24 IMPs; the final band (4000 and up) is open-ended.

Band lower bounds, in order — a difference earns the IMPs of the last band at
or below it:

```
0, 20, 50, 90, 130, 170, 220, 270, 320, 370, 430, 500, 600, 750, 900,
1100, 1300, 1500, 1750, 2000, 2250, 2500, 3000, 3500, 4000
```

## Properties the tests assert

- Every band boundary maps correctly from both sides.
- Awards are monotonic: more difference never earns fewer IMPs.
- Sign-independent: a difference is a magnitude; which side earns the IMPs is
  the caller's concern.
- Saturation: every difference of 4000 or more earns 24.

## Design notes

`ImpScale` takes a caller-supplied band table, validated as a well-formed
partition, so the scale in force is part of a named ruleset rather than a
constant baked into the engine. `Law78` is the only table shipped.
