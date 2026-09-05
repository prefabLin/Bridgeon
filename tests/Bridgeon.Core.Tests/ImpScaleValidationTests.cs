using Bridgeon.Core.Scoring;
using FluentAssertions;

namespace Bridgeon.Core.Tests;

/// <summary>
/// A scale is supplied by whoever configures a ruleset, so a malformed one must
/// be rejected where it is built rather than discovered mid-event. Each rejection
/// names the offending band, so these tests assert the message too.
/// </summary>
public class ImpScaleValidationTests
{
    [Fact]
    public void ASingleOpenBandIsAValidScale()
    {
        var scale = ImpScale.FromBands([new ImpBand(0, null, 0)]);

        scale.Bands.Should().HaveCount(1);
        scale.MaxImp.Should().Be(0);
        scale.ImpFor(int.MaxValue).Should().Be(0);
    }

    [Fact]
    public void NullBandsAreRejected() =>
        FluentActions.Invoking(() => ImpScale.FromBands(null!))
            .Should().Throw<ArgumentNullException>();

    [Fact]
    public void AnEmptyScaleIsRejected() => Reject([], "at least one band");

    [Fact]
    public void AScaleNotStartingAtZeroIsRejected() =>
        Reject([new ImpBand(1, null, 0)], "must start at 0");

    [Fact]
    public void AClosedFinalBandIsRejectedBecauseSomeDifferenceWouldBeUncovered() =>
        Reject([new ImpBand(0, 19, 0)], "open-ended");

    [Fact]
    public void ANegativeAwardIsRejected() =>
        Reject([new ImpBand(0, null, -1)], "negative");

    [Fact]
    public void ABandEndingBeforeItStartsIsRejected() =>
        Reject([new ImpBand(0, 9, 0), new ImpBand(10, 5, 1), new ImpBand(20, null, 2)],
            "before it starts");

    [Fact]
    public void TwoBandsSharingALowerBoundAreRejected() =>
        Reject([new ImpBand(0, 9, 0), new ImpBand(0, null, 1)], "Bands must ascend");

    [Fact]
    public void ADescendingBandIsRejected() =>
        Reject([new ImpBand(0, 9, 0), new ImpBand(10, 19, 1), new ImpBand(5, null, 2)],
            "Bands must ascend");

    [Fact]
    public void AGapBetweenBandsIsRejected() =>
        Reject([new ImpBand(0, 9, 0), new ImpBand(20, null, 1)], "gap or an overlap");

    [Fact]
    public void AnOverlapBetweenBandsIsRejected() =>
        Reject([new ImpBand(0, 15, 0), new ImpBand(10, null, 1)], "gap or an overlap");

    [Fact]
    public void TwoBandsAwardingTheSameImpAreRejected() =>
        Reject([new ImpBand(0, 9, 0), new ImpBand(10, null, 0)], "Awards must ascend");

    [Fact]
    public void ADescendingAwardIsRejected() =>
        Reject([new ImpBand(0, 9, 1), new ImpBand(10, null, 0)], "Awards must ascend");

    private static void Reject(ImpBand[] bands, string expectedInMessage) =>
        FluentActions.Invoking(() => ImpScale.FromBands(bands))
            .Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedInMessage}*",
                "a rejection must say which band is wrong and why");
}
