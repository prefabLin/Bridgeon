# 0001 — .NET 10, with a pure domain layer

**Status:** accepted

## Decision

C# on .NET 10 for the domain, server and MCP server; React and TypeScript for
the director console and the table clients; SQLite for the per-event append-only
log. `Bridgeon.Core` holds the domain and depends on nothing.

## Why

The maintainer's depth is in Windows application engineering, and the project
has to be maintainable by them for years rather than by whoever wrote it.

.NET 10.0.400 is the active LTS, supported to 2028-11-14. `global.json` pins it
with `rollForward: latestFeature`, so a newer patch or feature band is fine but a
major-version jump stays a deliberate edit.

## Consequences

- `Directory.Build.props` is the **only** place the target framework is written.
  The project templates put a copy in each csproj, and because the props file is
  imported before the project body those copies win silently.
- `Bridgeon.Core` carries no package references at all, enforced by
  `tests/Bridgeon.Architecture.Tests`. That is what allows the scoring library to
  be published on its own — the piece another association could adopt without
  taking the rest.
