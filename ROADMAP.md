# Roadmap

## The product

One process a director starts on a venue laptop. It serves a browser interface to
that laptop and to phones on the venue network, runs a whole tournament without
an internet connection, and publishes results afterwards when a connection
returns. Offline is the operating mode, not a fallback.

## v1: a round robin teams event, end to end

Round robin first for a reason beyond simplicity. A round-robin schedule is a
mathematical object, so correctness is checkable from first principles: every
team meets every other the right number of times, nobody plays twice in a round,
board sets balance. Those are properties a test can assert. A Swiss pairing has
no such test — it can only be compared against something else — so it comes
later, once the scoring and schedule layers underneath it are proven.

### In v1

- **Schedule engine** — circle-method generator for any team count, a validator
  every schedule must pass whatever its origin, and the counter round robin
  (each team playing twice per round).
- **Free configuration with warnings** — any team count, boards per round and
  round count. An unverified combination is allowed and produces a named
  warning, never a silent refusal.
- **Contract entry and scoring** — contract notation with typed rejections, the
  Law 77 score table, the Law 78 IMP scale, victory points from the WBF formula.
- **Ranking** — VP totals with a documented, swappable tiebreak.
- **Score capture two ways** — director keyboard entry, and a table client on
  players' own phones.
- **Corrections and adjustments** — every one an entry in an append-only log.
- **Displays** — seating, live standings, per-player scorecard by QR.
- **Output** — seating cards, results sheet, USEBIO 1.3 export, PBN import.
- **An MCP server** — so a director can set an event up by describing it.
- **Crash recovery** — kill the process mid-event and replay to identical state.

### Deferred

Swiss pairing, pairs events (Mitchell, Howell, individual), knockout,
multi-session, dedicated scoring hardware, master points.

## Design principles

**Provenance is a first-class field.** Every event records how its schedule was
produced: a verified named movement, generated and fully validated, or generated
with warnings naming the property that could not be satisfied. It is stored with
the event, returned by the MCP tools, and printed on the results sheet. This is
what makes "the director asks for whatever they want" safe rather than reckless.

**Local rules are named rulesets.** Where an association's practice differs from
a published default — its tiebreak order, its handicap policy, how it treats an
absent pair — that becomes a named ruleset built from the association's stated
rules, stored with the event, and printed on the results. Each rule is an
individually tested, individually documented object, never a branch buried in
the scoring code.

**The event log is the truth.** Every director action is an immutable entry;
standings are a projection. That buys undo, an audit trail when a player disputes
a score, and crash recovery that is just replay.

**One SQLite file per event.** A file the director can email, archive, or hand
over when something goes wrong.

**The wiki is the specification.** Rule pages in `wiki/rules/` restate the
published source each behaviour implements and move in the same commit as the
behaviour; `wiki/log.md` records the work append-only (decision 0004).

**UI comes from design files.** Screens are designed in `design/` before they
are implemented, following the workflows directors already know (decision
0005). P4 and P5 each begin with their screens designed, not with code.

## Phases

Each gate is machine-checkable, which is what makes unattended work safe.

| | Phase | Gate |
|---|---|---|
| **P0** | Foundations: content gates, solution, IMP scale | ✅ gates fail a planted violation; suites green |
| **P1** | Scoring core: contract notation, Law 77 table, VP | the 2,940-case score space enumerated; mutation ≥90% |
| **P2** | Schedule engine: generator, validator, provenance | balance properties hold for every team count 2–60 |
| **P3** | Event log, HTTP API, MCP server | killed mid-event, replays to identical state, 100 runs |
| **P4** | Director console, from design files | a full event driven end to end by Playwright, offline |
| **P5** | Table clients, from design files | event scored from simulated phones, network dropped mid-round |
| **P6** | First real event | results accepted; USEBIO validates against the published XSD |

## Where things stand

**P0 is complete.** The repository has its content and TDD gates, the solution
scaffold, and the Law 78 IMP scale implemented test-first.

**P1 is complete.** Contract notation with typed rejections that never throw
(wiki/rules/contract-notation.md is its specification); the Law 77 score table,
all 2,940 cases enumerated against an oracle transcribed from the Law, with
monotonicity in tricks taken asserted across the whole space; the WBF
continuous VP scale, asserted against its hand-derived golden-ratio fixed
points; and the printed 16-board vulnerability cycle. 134 tests pass. The
mutation gate runs in CI.

**P2 is next**: the schedule engine — the circle-method generator, the
validator every schedule must pass whatever its origin (decision 0003), the
counter round robin, and provenance as data. The gate: balance properties hold
for every team count 2–60.

<details><summary>P1 as it was planned</summary>

1. **Contract notation** — a parser turning what a director types into a
   contract. Typed rejections that name what is wrong; it must never throw,
   because a typo at eight in the morning is an expected input.
2. **The Law 77 score table** — pure arithmetic over a contract and a
   vulnerability. Enumerate the whole space rather than sampling it: 7 levels ×
   5 strains × 3 penalties × 2 vulnerabilities × 14 trick counts = 2,940 cases.
   Assert also that the score never falls as declarer takes another trick.
3. **Victory points** — the WBF continuous formula, and the standard
   sixteen-board vulnerability cycle.

</details>

## Open questions for the director

These need answers from someone who runs the events; none of them block P2.

- **Ruleset.** Which tiebreak order is in force? Which handicap policy, and how
  is a handicap earned? How is an absent team treated? All variants can be
  implemented; the question is which one a ruleset named for the association
  should select by default.
- ~~**Typical event size.**~~ Answered 2026-09-05: events run 2 to 30 tables,
  occasionally more. The validated limits (60 teams, 30 tables) cover that;
  raising them is a one-constant change plus rerunning the property tests over
  the larger range, to be done against a real requirement rather than
  speculatively.
- **Even-count counter round robin.** Each team plays two matches per round,
  but an even field leaves an odd number of opponents, so one round must be an
  ordinary one. Whether the association ever runs this format with an even
  count — and how its printed schedule arranges that odd round — decides
  whether the generator's refusal is already the right behaviour.
- **A real event to run at P6**, with printed movement cards as the backup.
