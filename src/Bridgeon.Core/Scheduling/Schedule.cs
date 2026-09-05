namespace Bridgeon.Core.Scheduling;

/// <summary>Two teams meeting at a table.</summary>
public sealed record Pairing(int Home, int Away);

/// <summary>One round: its 1-based number, its pairings in table order, and the
/// team sitting out, if the count is odd.</summary>
public sealed record Round(int Number, IReadOnlyList<Pairing> Pairings, int? Bye);

/// <summary>How a schedule came to be (decision 0003).</summary>
public enum ScheduleOrigin
{
    /// <summary>A named movement, validated.</summary>
    VerifiedMovement,

    /// <summary>Produced by a generator and fully validated.</summary>
    Generated,

    /// <summary>Generated, but a property could not be satisfied; each warning
    /// names one, for the director to accept.</summary>
    GeneratedWithWarnings,
}

/// <summary>A schedule's provenance, stored with the event and printed on the
/// results (decision 0003).</summary>
public sealed record ScheduleProvenance(ScheduleOrigin Origin, IReadOnlyList<string> Warnings)
{
    public static readonly ScheduleProvenance Generated =
        new(ScheduleOrigin.Generated, []);
}

/// <summary>
/// A complete schedule and its declared contract: how many teams, how often
/// each pair must meet, and how many matches each team plays per round. The
/// validator checks the rounds against exactly that contract.
/// </summary>
public sealed record Schedule(
    int Teams,
    int MeetingsPerPair,
    int MatchesPerTeamPerRound,
    IReadOnlyList<Round> Rounds,
    ScheduleProvenance Provenance);
