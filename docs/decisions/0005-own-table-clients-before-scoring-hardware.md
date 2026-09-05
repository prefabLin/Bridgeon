# 0005 — Own table clients before scoring hardware

**Status:** accepted

## Decision

Score capture at the table is Bridgeon's own web frontend, served to players'
phones over the venue network, and it ships in v1. Dedicated scoring hardware
(Bridgemate and similar) stays deferred: nothing in v1 depends on it, and when
it arrives it will be an alternative score source behind the same interface the
table clients use.

The UI is developed from design files, not improvised in code. Screens are
designed first — layout, flow, and states — and the implementation follows the
design. The default reference for what a screen should contain and how a
director expects to work is the frontend of the application the association
uses today: its workflows are the habits our users already have. That reference
material lives outside the repository; what is committed is only our own
designs and code, and every interface string is authored for Bridgeon
(enforced by the vocabulary gate).

## Why

- A phone browser is hardware every venue already owns; a hardware integration
  is a Windows-bound dependency none of v1's correctness goals need.
- Designing against the existing frontend's workflow keeps the switching cost
  for directors near zero — the screens feel familiar even though every pixel
  and every string is new.
- Keeping the reference outside the repo preserves the content rules: layout
  conventions and workflow shape are ideas; files and strings are content.

## Consequences

- v1's score sources are exactly two: director keyboard entry and the table
  client. Both feed the same append-only event log.
- The design files are the contract for UI work — a screen not in the designs
  is not built, and a design change precedes the code change.
- When scoring hardware is added later, it must present as a third score
  source, not a special path through the scoring engine.
