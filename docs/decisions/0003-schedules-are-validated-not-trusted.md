# 0003 — Schedules are validated, never trusted

**Status:** accepted

## Decision

Movements and schedules come from generators — the circle method and its
relatives — and every schedule passes a validator before an event can use it,
whatever produced it. The event records which.

## Why

A movement is a combinatorial object with properties that can be checked:

- every pair of teams meets the required number of times;
- no team appears twice in one round;
- board sets distribute evenly, and no board is played twice at a table;
- for a plain round robin of N teams, exactly N−1 rounds.

A schedule that passes is **provably** correct. That is a stronger guarantee than
any comparison against another program could give, and it holds for team counts
nobody has tabulated.

## Consequences

- Three provenance states, stored with the event and printed on its results:
  **verified** (a named movement, validated), **generated** (produced and fully
  validated), and **generated with warnings** (a property could not be satisfied,
  named, and accepted by the director).
- Odd and awkward team counts work, because the generator does not depend on
  anyone having tabulated them in advance.
