# Contract notation

**Source:** Bridgeon's own design — this page is the specification.
**Implementation:** `src/Bridgeon.Core/Scoring/ContractNotation.cs`.
**Tests:** `tests/Bridgeon.Core.Tests/ContractNotationTests.cs` transcribes this
page.

What a director or a table client types to record one board. The parser never
throws: a typo at eight in the morning is an expected input, so every failure
is a typed rejection naming what is wrong.

## Form

```
<level><strain>[doubling] <declarer> <result>     e.g.  4S N +1   3NTX W =
PASS                                              the board was passed out
```

- **level** — `1` to `7`.
- **strain** — `C`, `D`, `H`, `S`, `NT` (`N` alone also reads as no-trump).
- **doubling** — absent, `X` (doubled) or `XX` (redoubled).
- **declarer** — `N`, `E`, `S`, `W`.
- **result** — `=` (made exactly), `+n` (overtricks), `-n` (undertricks).

Case does not matter, and whitespace between elements is optional everywhere:
`4s n +1`, `4SN+1` and `4 S N + 1` are the same entry. `S` is never ambiguous —
strains are read before seats, so in `4SS=` the first `S` is spades and the
second is South.

## What is accepted

A successful parse yields either a played contract — the contract plus the
tricks the declaring side took, `level + 6` adjusted by the result — or a
passed-out board. Tricks taken always land in 0 through 13.

## Rejections

Each rejection carries a reason and a message quoting the offending text:

| Reason | When |
|---|---|
| `EmptyInput` | Nothing but whitespace. |
| `UnknownLevel` | The entry does not start with `1`–`7`. |
| `UnknownStrain` | The level is not followed by a strain letter. |
| `UnknownDeclarer` | No seat letter where the declarer belongs. |
| `MissingResult` | The entry ends before `=`, `+n` or `-n`. |
| `UnknownResult` | The result is malformed (`+`, `-0`, `+x`, …). |
| `ImpossibleOvertricks` | `+n` would take the trick total past 13. |
| `ImpossibleUndertricks` | `-n` is more tricks than the contract needs. |
| `UnexpectedTrailingInput` | Anything left over after the result. |

`UnknownDoubling` is reserved: more than two `X`s reads as a malformed
doubling, not a declarer error.
