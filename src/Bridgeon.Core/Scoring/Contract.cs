namespace Bridgeon.Core.Scoring;

/// <summary>The denomination a contract is played in.</summary>
public enum Strain { Clubs, Diamonds, Hearts, Spades, NoTrump }

/// <summary>Whether the contract was doubled, and how far.</summary>
public enum Doubling { None, Doubled, Redoubled }

/// <summary>A seat at the table.</summary>
public enum Seat { North, East, South, West }

/// <summary>
/// A contract: the level and strain the declaring side committed to, how far it
/// was doubled, and who declares.
/// </summary>
public sealed record Contract
{
    public Contract(int level, Strain strain, Doubling doubling, Seat declarer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 7);
        Level = level;
        Strain = strain;
        Doubling = doubling;
        Declarer = declarer;
    }

    /// <summary>The level, 1 through 7.</summary>
    public int Level { get; }

    public Strain Strain { get; }

    public Doubling Doubling { get; }

    public Seat Declarer { get; }

    /// <summary>The tricks the declaring side must take: six more than the level.</summary>
    public int TricksNeeded => Level + 6;
}
