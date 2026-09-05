# 0001 — .NET 10, with a pure domain layer

**Status:** accepted — amended 2026-09-05: the target framework is declared in
every csproj, not in `Directory.Build.props`. Stryker's project analysis finds
no projects at all when the framework lives only in the props file (reproduced
minimally: a fresh classlib + xunit pair works until the framework moves to the
props file), which silently disabled the mutation gate on every CI run. The
"one place to edit" guarantee now comes from
`tests/Bridgeon.Architecture.Tests/TargetFrameworkTests.cs`, which fails if any
project declares a different framework or the props file declares one.

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

- ~~`Directory.Build.props` is the **only** place the target framework is
  written.~~ Amended, see above: each csproj declares it, and the architecture
  suite asserts the declarations never diverge and the props file never grows
  one back.
- `Bridgeon.Core` carries no package references at all, enforced by
  `tests/Bridgeon.Architecture.Tests`. That is what allows the scoring library to
  be published on its own — the piece another association could adopt without
  taking the rest.
