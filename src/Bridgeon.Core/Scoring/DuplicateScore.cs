namespace Bridgeon.Core.Scoring;

/// <summary>The declaring side's vulnerability. The defenders' never matters.</summary>
public enum Vulnerability { NotVulnerable, Vulnerable }

/// <summary>
/// The duplicate score of a board, from the table published as Law 77 of the
/// Laws of Duplicate Bridge.
/// </summary>
public static class DuplicateScore
{
    /// <summary>
    /// The score from the declaring side's perspective: positive when the
    /// contract makes, negative when it goes down, zero for a passed-out board.
    /// </summary>
    public static int For(BoardEntry entry, Vulnerability vulnerability)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry switch
        {
            PassedOut => 0,
            PlayedContract played => played.TricksTaken >= played.Contract.TricksNeeded
                ? Made(played, vulnerability)
                : Down(played, vulnerability),
            _ => throw new ArgumentException(
                $"Unknown board entry type {entry.GetType().Name}.", nameof(entry)),
        };
    }

    private static int Made(PlayedContract played, Vulnerability vulnerability)
    {
        var contract = played.Contract;
        var vulnerable = vulnerability == Vulnerability.Vulnerable;

        var perTrick = contract.Strain is Strain.Clubs or Strain.Diamonds ? 20 : 30;
        var firstTrickExtra = contract.Strain == Strain.NoTrump ? 10 : 0;
        var multiplier = contract.Doubling switch
        {
            Doubling.Doubled => 2,
            Doubling.Redoubled => 4,
            _ => 1,
        };
        var trickScore = ((perTrick * contract.Level) + firstTrickExtra) * multiplier;

        var overtrickValue = contract.Doubling switch
        {
            Doubling.None => perTrick,
            Doubling.Doubled => vulnerable ? 200 : 100,
            _ => vulnerable ? 400 : 200,
        };
        var overtrickScore =
            (played.TricksTaken - contract.TricksNeeded) * overtrickValue;

        // Game is judged on the doubled trick score, so a doubled part-score
        // can be doubled into game.
        var gameBonus = trickScore >= 100 ? (vulnerable ? 500 : 300) : 50;
        var slamBonus = contract.Level switch
        {
            6 => vulnerable ? 750 : 500,
            7 => vulnerable ? 1500 : 1000,
            _ => 0,
        };
        var insult = contract.Doubling switch
        {
            Doubling.Doubled => 50,
            Doubling.Redoubled => 100,
            _ => 0,
        };

        return trickScore + overtrickScore + gameBonus + slamBonus + insult;
    }

    private static int Down(PlayedContract played, Vulnerability vulnerability)
    {
        var undertricks = played.Contract.TricksNeeded - played.TricksTaken;
        var vulnerable = vulnerability == Vulnerability.Vulnerable;

        int penalty;
        if (played.Contract.Doubling == Doubling.None)
        {
            penalty = undertricks * (vulnerable ? 100 : 50);
        }
        else
        {
            // Doubled: vulnerable 200 then 300 each; not vulnerable 100, then
            // 200 for the second and third, then 300 each.
            penalty = vulnerable
                ? 200 + ((undertricks - 1) * 300)
                : undertricks switch
                {
                    1 => 100,
                    2 or 3 => 100 + ((undertricks - 1) * 200),
                    _ => 500 + ((undertricks - 3) * 300),
                };
            if (played.Contract.Doubling == Doubling.Redoubled) penalty *= 2;
        }

        return -penalty;
    }
}
