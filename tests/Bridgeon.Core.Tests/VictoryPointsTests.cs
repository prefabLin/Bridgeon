using Bridgeon.Core.Scoring;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Bridgeon.Core.Tests;

/// <summary>
/// The continuous victory-point scale is the WBF's published formula (decision
/// 0002, wiki/rules/victory-points.md). Because tau is the cube of the golden
/// ratio's conjugate, the scale has fixed points derivable by hand — phi
/// squared equals one minus phi, so at a third of B the formula collapses to
/// exactly 15.00. Those derivations, not a reimplementation of the formula,
/// are what these tests assert against.
/// </summary>
public class VictoryPointsTests
{
    /// <summary>Cases per property, kept small so mutation testing stays affordable.</summary>
    private const int PropertyBudget = 200;

    /// <summary>Sixteen boards: B = 15 * sqrt(16) = 60, so the hand-derived
    /// fixed points land on integer IMP differences.</summary>
    private static readonly VictoryPointScale SixteenBoards = VictoryPointScale.Continuous(16);

    // ------------------------------------------------------------ fixed points

    [Theory]
    [InlineData(0, "10.00", "10.00")]   // no difference splits the round evenly
    [InlineData(20, "15.00", "5.00")]   // d = B/3: the golden-ratio identity, exact
    [InlineData(30, "16.73", "3.27")]   // d = B/2: 10 + 10(1 - sqrt(tau))/(1 - tau)
    [InlineData(40, "18.09", "1.91")]   // d = 2B/3: 10 + 5(phi + 1)
    [InlineData(60, "20.00", "0.00")]   // d = B: saturated
    [InlineData(61, "20.00", "0.00")]
    [InlineData(9999, "20.00", "0.00")]
    public void TheHandDerivedFixedPointsHold(int netImps, string own, string opponents)
    {
        var vp = SixteenBoards.For(netImps);

        // Invariant culture: on a comma-decimal locale "15.00" must not read as 1500.
        vp.Own.Should().Be(decimal.Parse(own, System.Globalization.CultureInfo.InvariantCulture));
        vp.Opponents.Should().Be(
            decimal.Parse(opponents, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TheFixedPointsScaleWithTheBoardCount()
    {
        // Nine boards: B = 45, so a third of B is 15 IMPs.
        VictoryPointScale.Continuous(9).For(15).Own.Should().Be(15.00m);
        VictoryPointScale.Continuous(9).For(45).Own.Should().Be(20.00m);
    }

    // -------------------------------------------------------------- properties

    [Property(MaxTest = PropertyBudget)]
    public Property ARoundAlwaysDistributesExactlyTwentyPoints() =>
        Prop.ForAll(Gen.Choose(-100, 100).ToArbitrary(), d =>
        {
            var vp = SixteenBoards.For(d);
            return vp.Own + vp.Opponents == 20.00m;
        });

    [Property(MaxTest = PropertyBudget)]
    public Property LosingIsTheMirrorOfWinning() =>
        Prop.ForAll(Gen.Choose(-100, 100).ToArbitrary(), d =>
        {
            var vp = SixteenBoards.For(d);
            var mirrored = SixteenBoards.For(-d);
            return vp.Own == mirrored.Opponents && vp.Opponents == mirrored.Own;
        });

    [Property(MaxTest = PropertyBudget)]
    public Property WinningByMoreNeverEarnsFewerPoints()
    {
        var difference = Gen.Choose(-100, 100).ToArbitrary();
        return Prop.ForAll(difference, difference, (a, b) =>
        {
            var (lower, higher) = a <= b ? (a, b) : (b, a);
            return SixteenBoards.For(lower).Own <= SixteenBoards.For(higher).Own;
        });
    }

    [Property(MaxTest = PropertyBudget)]
    public Property EveryAwardHasExactlyTwoDecimalPlaces() =>
        Prop.ForAll(Gen.Choose(-100, 100).ToArbitrary(), d =>
        {
            var vp = SixteenBoards.For(d);
            return vp.Own == decimal.Round(vp.Own, 2)
                && vp.Own >= 0.00m && vp.Own <= 20.00m;
        });

    [Fact]
    public void TheMostNegativeDifferenceSaturatesRatherThanOverflowing() =>
        SixteenBoards.For(int.MinValue).Own.Should().Be(0.00m);

    // ------------------------------------------------------------- validation

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ARoundNeedsAtLeastOneBoard(int boards)
    {
        var construct = () => VictoryPointScale.Continuous(boards);
        construct.Should().Throw<ArgumentOutOfRangeException>();
    }
}
