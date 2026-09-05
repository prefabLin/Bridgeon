namespace Bridgeon.Core.Scoring;

/// <summary>One side's share of a round's 20 victory points, and its opponents'.</summary>
public sealed record VictoryPoints(decimal Own, decimal Opponents);

/// <summary>
/// The World Bridge Federation's continuous victory-point scale (decision 0002):
/// <c>VP = 10 + 10·(1 − τ^(d/B))/(1 − τ)</c> with τ = φ³ and B = 15·√boards,
/// saturating at 20:0 once the IMP difference reaches B.
/// </summary>
public sealed class VictoryPointScale
{
    private static readonly double Phi = (Math.Sqrt(5) - 1) / 2;
    private static readonly double Tau = Phi * Phi * Phi;

    private readonly double _saturationDifference;

    private VictoryPointScale(int boardsPerRound) =>
        _saturationDifference = 15 * Math.Sqrt(boardsPerRound);

    /// <summary>The continuous scale for a round of the given length.</summary>
    public static VictoryPointScale Continuous(int boardsPerRound)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(boardsPerRound, 1);
        return new VictoryPointScale(boardsPerRound);
    }

    /// <summary>
    /// The VP split for a net IMP difference, from the perspective of the side
    /// whose difference it is. The winner's award carries two decimal places,
    /// rounded half away from zero; the loser's is the exact complement, so a
    /// round always distributes exactly 20 VPs.
    /// </summary>
    public VictoryPoints For(int netImps)
    {
        // The cast happens before Math.Abs, so int.MinValue cannot overflow here.
        var magnitude = Math.Abs((double)netImps);

        var winner = magnitude >= _saturationDifference
            ? 20.00m
            : (decimal)Math.Round(
                10 + (10 * (1 - Math.Pow(Tau, magnitude / _saturationDifference)) / (1 - Tau)),
                2, MidpointRounding.AwayFromZero);
        var loser = 20.00m - winner;

        return netImps >= 0
            ? new VictoryPoints(winner, loser)
            : new VictoryPoints(loser, winner);
    }
}
