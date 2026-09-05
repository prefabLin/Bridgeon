# Bridgeon

A bridge scoring and tournament hosting kit.

Offline-first: one process a director starts on a venue laptop, serving a browser
interface to that laptop and to phones on the venue network. No internet is
needed to run an event.

Scoring implements the Laws of Duplicate Bridge and the World Bridge
Federation's published victory-point formulas. Movements are generated from
standard combinatorial constructions and validated against the balance
properties a movement must satisfy, so a schedule is checked rather than trusted.

## Status

Early. The scoring core exists and is tested; there is no application yet.

## Building

Requires the .NET 10 SDK.

```bash
dotnet test                          # unit and architecture suites
python3 tools/check_provenance.py    # content rules
```

## Contributing

`CLAUDE.md` describes how the project is built: test-first, every rule citing a
published specification, and a pure domain layer with no I/O.
