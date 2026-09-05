# Round robin schedules

**Source:** the circle method, the standard combinatorial construction for
round-robin tournaments (also called the polygon method; it dates to
19th-century whist scheduling and is the textbook construction in
combinatorial design theory). The balance properties are from
`docs/decisions/0003-schedules-are-validated-not-trusted.md`.
**Implementation:** `src/Bridgeon.Core/Scheduling/RoundRobin.cs`.
**Tests:** `tests/Bridgeon.Core.Tests/RoundRobinTests.cs` asserts the balance
properties directly — counting meetings, not replaying the construction.

## The properties a schedule must satisfy

For N teams playing a single round robin:

- every pair of teams meets **exactly once**;
- no team plays twice in one round;
- N even: exactly **N − 1 rounds**, each with N/2 pairings and no byes;
- N odd: exactly **N rounds**, each with (N−1)/2 pairings and exactly one bye,
  and every team byes **exactly once**.

A **counter round robin** doubles the cycle: every pair meets exactly twice,
once in each seating, over twice the rounds.

A schedule that satisfies these properties is provably a round robin, whatever
produced it. That is what the validator checks and the generator's output must
pass — two independent statements, per decision 0003.

## The circle method

Fix one team; arrange the rest in a circle and rotate them one position per
round. With teams numbered 1…N (N even):

- in round r (r = 1…N−1), the fixed team N meets team r;
- every other pairing puts together the two rotated positions that sum to the
  same value mod N−1 — each round uses each difference exactly once, which is
  why no pair can repeat.

For odd N, a phantom team is added; whoever is paired with the phantom sits
out, which distributes the byes one per team.

## Provenance

Every schedule carries how it came to be (decision 0003): a **verified** named
movement, **generated** and fully validated, or **generated with warnings**
naming each property that could not be satisfied and needs the director's
acceptance. The generator emits `Generated`; the warnings state exists for the
event layer, where a director may insist on a combination nobody can verify.

## Not yet specified

Home/away (open/closed room) assignment policy and table allocation beyond
pairing order are ruleset concerns, deliberately not fixed by this page.
