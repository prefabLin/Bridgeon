# 0004 — An LLM-maintained wiki is the specification

**Status:** accepted

## Decision

The repository carries a wiki (`wiki/`) maintained by the agents that write the
code. Rule pages restate the published law or formula a behaviour implements,
in this project's own words, with the source cited. Agents read the relevant
page before answering any question about a bridge rule, and update it in the
same commit that changes the behaviour. `wiki/log.md` is an append-only work
log.

## Why

This project is developed agent-first, and an agent session starts with no
memory of the last one. Code states *what* the current implementation does;
tests state what it *must* do; neither states the domain understanding that
produced them — why a band table is a partition, what balance property a
schedule must satisfy. Writing that down once, next to its citation, is cheaper
than re-deriving it every session, and it gives a human reviewer one place to
check the project's understanding of a rule against the published text.

## Consequences

- A rules page without a citation is a defect, same as a failing test.
- The wiki obeys the content rules: nothing in it is transcribed from any other
  program; CJK text follows the vocabulary gate.
- A `feat:` commit that changes rule behaviour without touching the
  corresponding wiki page should be challenged in review.
