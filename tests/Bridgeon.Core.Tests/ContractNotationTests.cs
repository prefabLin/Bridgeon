using Bridgeon.Core.Scoring;
using FluentAssertions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Bridgeon.Core.Tests;

/// <summary>
/// Contract notation is Bridgeon's own specification, written down in
/// wiki/rules/contract-notation.md. These tests transcribe that page: the form,
/// the leniencies, and every typed rejection. The parser must never throw —
/// a typo is an expected input.
/// </summary>
public class ContractNotationTests
{
    /// <summary>Cases per property, kept small so mutation testing stays affordable.</summary>
    private const int PropertyBudget = 200;

    // ---------------------------------------------------------------- accepted

    [Theory]
    [InlineData("4S N +1", 4, Strain.Spades, Doubling.None, Seat.North, 11)]
    [InlineData("3NTX W =", 3, Strain.NoTrump, Doubling.Doubled, Seat.West, 9)]
    [InlineData("7CXX S =", 7, Strain.Clubs, Doubling.Redoubled, Seat.South, 13)]
    [InlineData("1D E -7", 1, Strain.Diamonds, Doubling.None, Seat.East, 0)]
    [InlineData("2H S -2", 2, Strain.Hearts, Doubling.None, Seat.South, 6)]
    [InlineData("6NT N +1", 6, Strain.NoTrump, Doubling.None, Seat.North, 13)]
    public void WellFormedEntriesParse(
        string input, int level, Strain strain, Doubling doubling, Seat declarer, int tricks)
    {
        var entry = Accept(input);

        var played = entry.Should().BeOfType<PlayedContract>().Subject;
        played.Contract.Should().Be(new Contract(level, strain, doubling, declarer));
        played.TricksTaken.Should().Be(tricks);
    }

    [Theory]
    [InlineData("4s n +1")]
    [InlineData("4SN+1")]
    [InlineData("  4 S N + 1  ")]
    [InlineData("4S\tN\t+1")]
    public void CaseAndWhitespaceDoNotMatter(string variant) =>
        Accept(variant).Should().Be(Accept("4S N +1"));

    [Theory]
    [InlineData("4S X N -1")] // whitespace before the doubling
    [InlineData("4 S XX N -1", "4SXX N -1")]
    public void WhitespaceBeforeTheDoublingIsAlsoAllowed(string spaced, string compact = "4SX N -1") =>
        Accept(spaced).Should().Be(Accept(compact));

    [Fact]
    public void NAloneReadsAsNoTrump()
    {
        var played = Accept("4N S =").Should().BeOfType<PlayedContract>().Subject;
        played.Contract.Strain.Should().Be(Strain.NoTrump);
        played.Contract.Declarer.Should().Be(Seat.South);
    }

    [Fact]
    public void ALoneNStillTakesItsDoublingFromWhatFollows()
    {
        // The X after a lone N is a doubling, never a swallowed T.
        var played = Accept("4NXX W =").Should().BeOfType<PlayedContract>().Subject;
        played.Contract.Should().Be(
            new Contract(4, Strain.NoTrump, Doubling.Redoubled, Seat.West));
    }

    [Fact]
    public void ANullContractIsRejectedByItsParameterName()
    {
        var construct = () => new PlayedContract(null!, 9);
        construct.Should().Throw<ArgumentNullException>().WithParameterName("contract");
    }

    [Fact]
    public void StrainsAreReadBeforeSeatsSoSIsNeverAmbiguous()
    {
        var played = Accept("4SS=").Should().BeOfType<PlayedContract>().Subject;
        played.Contract.Strain.Should().Be(Strain.Spades);
        played.Contract.Declarer.Should().Be(Seat.South);
    }

    [Theory]
    [InlineData("PASS")]
    [InlineData("pass")]
    [InlineData("  Pass ")]
    public void APassedOutBoardIsItsOwnEntry(string input) =>
        Accept(input).Should().BeOfType<PassedOut>();

    [Fact]
    public void EqualsMeansExactlyTheTricksTheContractNeeds() =>
        Accept("5H E =").Should().BeOfType<PlayedContract>()
            .Which.TricksTaken.Should().Be(11);

    // --------------------------------------------------------------- rejected

    [Theory]
    [InlineData("", RejectionReason.EmptyInput)]
    [InlineData("   ", RejectionReason.EmptyInput)]
    [InlineData("8S N =", RejectionReason.UnknownLevel)]
    [InlineData("0S N =", RejectionReason.UnknownLevel)]
    [InlineData("S N =", RejectionReason.UnknownLevel)]
    [InlineData("4Q N =", RejectionReason.UnknownStrain)]
    [InlineData("4 N =", RejectionReason.UnknownDeclarer)] // N is the strain, so the seat is missing
    [InlineData("4SXXX N =", RejectionReason.UnknownDoubling)]
    [InlineData("4S =", RejectionReason.UnknownDeclarer)]
    [InlineData("4S Q =", RejectionReason.UnknownDeclarer)]
    [InlineData("4S N", RejectionReason.MissingResult)]
    [InlineData("4S N +", RejectionReason.UnknownResult)]
    [InlineData("4S N -0", RejectionReason.UnknownResult)]
    [InlineData("4S N +0", RejectionReason.UnknownResult)]
    [InlineData("4S N ~1", RejectionReason.UnknownResult)]
    [InlineData("4S N +4", RejectionReason.ImpossibleOvertricks)]
    [InlineData("7C S +1", RejectionReason.ImpossibleOvertricks)]
    [InlineData("4S N +14", RejectionReason.ImpossibleOvertricks)] // well-formed, merely impossible
    [InlineData("4S N +1000", RejectionReason.ImpossibleOvertricks)]
    [InlineData("7NT N -131", RejectionReason.ImpossibleUndertricks)] // clamp must not truncate to -13
    [InlineData("PAS", RejectionReason.UnknownLevel)] // too short to be PASS, and P is no level
    [InlineData("4S N -11", RejectionReason.ImpossibleUndertricks)]
    [InlineData("4S N -14", RejectionReason.ImpossibleUndertricks)]
    [InlineData("1C W -8", RejectionReason.ImpossibleUndertricks)]
    [InlineData("4S N +1 junk", RejectionReason.UnexpectedTrailingInput)]
    [InlineData("PASS junk", RejectionReason.UnexpectedTrailingInput)]
    public void EachMistakeIsNamed(string input, RejectionReason reason) =>
        Reject(input).Reason.Should().Be(reason);

    [Fact]
    public void NullIsRejectedNotThrown() =>
        ContractNotation.Parse(null).Should().BeOfType<NotationResult.Rejected>()
            .Which.Reason.Should().Be(RejectionReason.EmptyInput);

    [Fact]
    public void ARejectionMessageQuotesTheOffendingText()
    {
        Reject("4Q N =").Message.Should().Contain("Q");
        Reject("4S N +9").Message.Should().Contain("+9");
        // Exactly what was typed — never a truncation of it.
        Reject("4S N +1000").Message.Should().Contain("+1000");
        Reject("4S N +x").Message.Should().Contain("+x");
        Reject("4S N -0").Message.Should().Contain("-0");
        // The quotes hug the offence: no swallowed neighbours, no stray spaces.
        Reject("4S N +x  ").Message.Should().Contain("'+x'");
        Reject("4S N +1 junk  ").Message.Should().Contain("'junk'");
    }

    [Fact]
    public void TheBoundaryResultsAreStillPossible()
    {
        // Down everything: declarer took zero tricks.
        Accept("7NT N -13").Should().BeOfType<PlayedContract>()
            .Which.TricksTaken.Should().Be(0);
        // Maximum overtricks on a one-level contract.
        Accept("1C S +6").Should().BeOfType<PlayedContract>()
            .Which.TricksTaken.Should().Be(13);
    }

    // ------------------------------------------------------------- properties

    [Fact]
    public void EveryPossibleEntryRoundTripsThroughItsOwnNotation()
    {
        // The whole space: 7 levels x 5 strains x 3 doublings x 4 seats x 14
        // trick counts = 5,880 entries, plus the passed-out board.
        foreach (var level in Enumerable.Range(1, 7))
            foreach (var strain in Enum.GetValues<Strain>())
                foreach (var doubling in Enum.GetValues<Doubling>())
                    foreach (var seat in Enum.GetValues<Seat>())
                        foreach (var tricks in Enumerable.Range(0, 14))
                        {
                            var entry = new PlayedContract(
                                new Contract(level, strain, doubling, seat), tricks);
                            Accept(entry.Notation).Should().Be(entry,
                                $"the notation {entry.Notation} names this entry");
                        }

        Accept(new PassedOut().Notation).Should().Be(new PassedOut());
    }

    [Property(MaxTest = PropertyBudget)]
    public Property NoStringWhatsoeverMakesTheParserThrow() =>
        Prop.ForAll(ArbMap.Default.ArbFor<string>(), input =>
        {
            var result = ContractNotation.Parse(input);
            return result is NotationResult.Accepted or NotationResult.Rejected;
        });

    // ------------------------------------------------------------ model rules

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void AContractLevelOutsideOneToSevenIsAProgrammingError(int level)
    {
        var construct = () => new Contract(level, Strain.Spades, Doubling.None, Seat.North);
        construct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(14)]
    public void TricksTakenOutsideZeroToThirteenIsAProgrammingError(int tricks)
    {
        var contract = new Contract(4, Strain.Spades, Doubling.None, Seat.North);
        var construct = () => new PlayedContract(contract, tricks);
        construct.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AnUndefinedEnumArgumentIsAProgrammingError()
    {
        // A cast-in garbage enum must throw, not score plausibly and wrongly.
        var strain = () => new Contract(4, (Strain)99, Doubling.None, Seat.North);
        var doubling = () => new Contract(4, Strain.Spades, (Doubling)7, Seat.North);
        var seat = () => new Contract(4, Strain.Spades, Doubling.None, (Seat)9);

        strain.Should().Throw<ArgumentOutOfRangeException>();
        doubling.Should().Throw<ArgumentOutOfRangeException>();
        seat.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AContractNeedsSixTricksMoreThanItsLevel() =>
        new Contract(4, Strain.Spades, Doubling.None, Seat.North)
            .TricksNeeded.Should().Be(10);

    // ---------------------------------------------------------------- helpers

    private static BoardEntry Accept(string input) =>
        ContractNotation.Parse(input).Should().BeOfType<NotationResult.Accepted>(
            $"'{input}' is well-formed").Subject.Entry;

    private static NotationResult.Rejected Reject(string input) =>
        ContractNotation.Parse(input).Should().BeOfType<NotationResult.Rejected>(
            $"'{input}' is malformed").Subject;
}
