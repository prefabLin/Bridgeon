namespace Bridgeon.Core.Scoring;

/// <summary>
/// What happened on one board: a contract that was played, or a board passed
/// out. <see cref="Notation"/> is the canonical form of Bridgeon's contract
/// notation (wiki/rules/contract-notation.md) and parses back to an equal entry.
/// </summary>
public abstract record BoardEntry
{
    private protected BoardEntry() { }

    /// <summary>The canonical notation naming this entry.</summary>
    public abstract string Notation { get; }
}

/// <summary>A contract that was played, and the tricks the declaring side took.</summary>
public sealed record PlayedContract : BoardEntry
{
    public PlayedContract(Contract contract, int tricksTaken)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentOutOfRangeException.ThrowIfLessThan(tricksTaken, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tricksTaken, 13);
        Contract = contract;
        TricksTaken = tricksTaken;
    }

    public Contract Contract { get; }

    /// <summary>Tricks the declaring side took, 0 through 13.</summary>
    public int TricksTaken { get; }

    public override string Notation
    {
        get
        {
            var strain = Contract.Strain switch
            {
                Strain.Clubs => "C",
                Strain.Diamonds => "D",
                Strain.Hearts => "H",
                Strain.Spades => "S",
                _ => "NT",
            };
            var doubling = Contract.Doubling switch
            {
                Doubling.Doubled => "X",
                Doubling.Redoubled => "XX",
                _ => "",
            };
            var seat = Contract.Declarer switch
            {
                Seat.North => "N",
                Seat.East => "E",
                Seat.South => "S",
                _ => "W",
            };
            var relative = TricksTaken - Contract.TricksNeeded;
            var result = relative switch
            {
                0 => "=",
                > 0 => $"+{relative}",
                _ => $"-{-relative}",
            };
            return $"{Contract.Level}{strain}{doubling} {seat} {result}";
        }
    }
}

/// <summary>A board on which all four players passed. Scores zero to both sides.</summary>
public sealed record PassedOut : BoardEntry
{
    public override string Notation => "PASS";
}
