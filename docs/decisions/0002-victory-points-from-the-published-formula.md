# 0002 — Victory points from the published formula

**Status:** accepted

## Decision

Victory points are computed from the World Bridge Federation's published
continuous formula, not from a stored lookup table.

```
VP_win = 10 + 10 * (1 - tau^(d/B)) / (1 - tau)
tau    = phi^3, where phi = (sqrt(5) - 1) / 2
B      = 15 * sqrt(boards per round)
```

saturating at 20:0 once `d >= B`, published to two decimal places, rounded half
away from zero. `d` is the net IMP difference for the round.

## Why

A formula is a specification: it can be read, argued with, and implemented by
anyone. It also extends to board counts no published table happens to cover,
which the schedule engine needs, since a director may ask for a round length
nobody has tabulated.

## Consequences

- Printed tables in circulation may differ from the formula by a cent of a VP at
  scattered points, depending on how they were rounded. Bridgeon follows the
  formula, and the ruleset name printed on the results says which rule produced
  the numbers.
- If an association requires exactly the values from a particular published
  table, that becomes a named ruleset selecting a table it supplies, rather than
  a change to the default.
