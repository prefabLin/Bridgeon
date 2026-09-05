# Bridgeon — working agreement

Offline-first tournament software for duplicate bridge: one process a director
starts on a venue laptop, serving a browser interface to that laptop and to
phones on the venue network. v1 runs a **round robin teams** event end to end.

## What this codebase is built from

Every rule here comes from a published specification, and cites it in the code:

- **The Laws of Duplicate Bridge** — the score table (Law 77), the IMP scale
  (Law 78), adjusted scores, and the conduct of a session.
- **The World Bridge Federation's published formulas** — the continuous
  victory-point scale.
- **Standard combinatorial constructions** — the circle method for round-robin
  schedules, and the balance properties any movement must satisfy.

If a behaviour cannot be justified from a public source, it does not go in. When
the association's own practice differs from the published default — its
tiebreaks, its handicap policy — that becomes a **named ruleset** written from
their stated rules, stored with the event, and printed on the results.

## Original content only

Everything in this repository is written for it.

- **No spreadsheets, databases or imported datasets.** Reference values are
  derived from published formulas and expressed in code, where they can be read,
  tested and argued with.
- **No personal data.** No names, no rosters, no event records, no venues. Test
  data is synthetic and obviously so.
- **Every CJK string is one we authored** for Bridgeon's own interface,
  registered in `tools/zh_vocabulary.txt`.

`tools/check_provenance.py` enforces all three on every CI run. It works as an
allow-list: anything unregistered fails, so admitting content is always a
deliberate, reviewable act rather than an oversight.

## The law of this repository

**No production code without a failing test first.** CI checks that a `feat:`
commit touching `src/X` follows a `test:` commit touching `tests/X.Tests`, and
Stryker breaks the build below 90% mutation score on `Bridgeon.Core`. Line
coverage says a line ran; mutation score says a test would notice if it changed.

**`Bridgeon.Core` stays pure.** No I/O, no hosting, no persistence, no package
references at all. Architecture tests enforce it, which is what keeps the
scoring library separately publishable — the piece another association could
adopt.

**Tests restate the specification.** A test transcribes the Law it encodes, so
the test and the implementation are two independent statements checked against
each other rather than one checking a copy of itself.

## Layout

| Path | What it is |
|---|---|
| `src/Bridgeon.Core` | Domain: contracts, scoring, rules, ranking. Pure. |
| `tests/` | Unit and architecture suites. |
| `tools/` | Development utilities. Not shipped. |

## Toolchain

`dotnet test` runs everything. `python3 tools/check_provenance.py` checks the
content rules. `./tools/check_commit_order.sh` checks the TDD gate locally.
Mutation testing is a merge gate run in CI, not a per-commit tool.

## Limits

60 teams, 30 tables, 24 boards per round — validated limits, chosen for
robustness rather than capacity.
