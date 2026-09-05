using Bridgeon.Core.Scoring;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Bridgeon.Core.Tests;

/// <summary>
/// The IMP scale is Law 78 of the Laws of Duplicate Bridge. It is a published
/// specification, so these tests restate it independently of the implementation
/// and check the two against each other.
/// </summary>
public class ImpScaleTests
{
    /// <summary>Cases per property, kept small so mutation testing stays affordable.</summary>
    private const int PropertyBudget = 200;

    /// <summary>
    /// Law 78: the smallest point difference earning each IMP award, 0 through 24.
    /// Transcribed from the Law, not from any implementation of it.
    /// </summary>
    private static readonly int[] Law78LowerBounds =
    [
        0, 20, 50, 90, 130, 170, 220, 270, 320, 370, 430, 500, 600, 750, 900,
        1100, 1300, 1500, 1750, 2000, 2250, 2500, 3000, 3500, 4000,
    ];

    [Fact]
    public void TheBuiltInScaleIsLaw78()
    {
        var scale = ImpScale.Law78;

        scale.Bands.Select(b => b.Lower).Should().Equal(Law78LowerBounds);
        scale.Bands.Select(b => b.Imp).Should().Equal(Enumerable.Range(0, 25));
        scale.MaxImp.Should().Be(24);
        // Each band ends one short of the next; only the last is open.
        scale.Bands.SkipLast(1).Select(b => b.Upper)
            .Should().Equal(Law78LowerBounds.Skip(1).Select(lower => (int?)(lower - 1)));
        scale.Bands[^1].Upper.Should().BeNull();
    }

    [Fact]
    public void AGapNamesTheBandsOnBothSidesOfIt()
    {
        var build = () => ImpScale.FromBands(
            [new ImpBand(0, 10, 0), new ImpBand(12, null, 1)]);

        build.Should().Throw<ArgumentException>()
            .WithMessage("*Band 0 ends at 10 but band 1 starts at 12*");
    }

    [Fact]
    public void EveryBandBoundaryAwardsTheRightImps()
    {
        var scale = ImpScale.Law78;

        for (var imp = 0; imp < Law78LowerBounds.Length; imp++)
        {
            scale.ImpFor(Law78LowerBounds[imp]).Should().Be(imp,
                "the smallest difference in a band earns that band's IMPs");

            if (imp + 1 < Law78LowerBounds.Length)
                scale.ImpFor(Law78LowerBounds[imp + 1] - 1).Should().Be(imp,
                    "one point short of the next band still earns this band's IMPs");
        }
    }

    [Fact]
    public void TheScaleSaturatesAtTwentyFour()
    {
        ImpScale.Law78.ImpFor(4000).Should().Be(24);
        ImpScale.Law78.ImpFor(999_999).Should().Be(24);
    }

    [Property(MaxTest = PropertyBudget)]
    public Property TheAwardNeverFallsAsTheDifferenceGrows()
    {
        var difference = Gen.Choose(0, 8000).ToArbitrary();
        return Prop.ForAll(difference, difference, (a, b) =>
        {
            var (lower, higher) = a <= b ? (a, b) : (b, a);
            return ImpScale.Law78.ImpFor(lower) <= ImpScale.Law78.ImpFor(higher);
        });
    }

    [Property(MaxTest = PropertyBudget)]
    public Property SignIsIgnoredBecauseADifferenceIsAMagnitude() =>
        Prop.ForAll(Gen.Choose(0, 8000).ToArbitrary(),
            (int d) => ImpScale.Law78.ImpFor(d) == ImpScale.Law78.ImpFor(-d));

    [Fact]
    public void TheMostNegativeDifferenceSaturatesRatherThanOverflowing() =>
        // Math.Abs(int.MinValue) throws, so the magnitude has to be clamped.
        ImpScale.Law78.ImpFor(int.MinValue).Should().Be(24);

    [Fact]
    public void ANullBandTableIsRejectedByItsParameterName()
    {
        var build = () => ImpScale.FromBands(null!);
        build.Should().Throw<ArgumentNullException>().WithParameterName("bands");
    }
}
