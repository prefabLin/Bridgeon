using Bridgeon.Core.Scoring;
using FluentAssertions;

namespace Bridgeon.Core.Tests;

/// <summary>
/// The duplicate score is Law 77 of the Laws of Duplicate Bridge. The oracle
/// here transcribes the Law's tables as data — per-trick values, undertrick
/// schedules, bonus amounts — and sums them per component, so it is an
/// independent statement of the same table the implementation computes with
/// arithmetic. The whole space is enumerated: 7 levels x 5 strains x
/// 3 doublings x 2 vulnerabilities x 14 trick counts = 2,940 cases.
/// </summary>
public class DuplicateScoreTests
{
    // ------------------------------------------------- the oracle, from the Law

    /// <summary>Law 77 trick values: what each contracted trick is worth.</summary>
    private static int ContractedTrickValue(Strain strain, int trickNumber) =>
        strain switch
        {
            Strain.Clubs or Strain.Diamonds => 20,
            Strain.Hearts or Strain.Spades => 30,
            _ => trickNumber == 1 ? 40 : 30,
        };

    /// <summary>Law 77 undertrick schedule: what the nth undertrick costs the
    /// declaring side, before the redoubled doubling-again.</summary>
    private static int UndertrickValue(int n, Doubling doubling, Vulnerability vul)
    {
        var vulnerable = vul == Vulnerability.Vulnerable;
        if (doubling == Doubling.None) return vulnerable ? 100 : 50;

        var doubled = (n, vulnerable) switch
        {
            (1, false) => 100,
            (2 or 3, false) => 200,
            (_, false) => 300,
            (1, true) => 200,
            (_, true) => 300,
        };
        return doubling == Doubling.Redoubled ? doubled * 2 : doubled;
    }

    private static int Oracle(Contract contract, Vulnerability vul, int tricksTaken)
    {
        var vulnerable = vul == Vulnerability.Vulnerable;

        if (tricksTaken < contract.TricksNeeded)
        {
            var penalty = 0;
            for (var n = 1; n <= contract.TricksNeeded - tricksTaken; n++)
                penalty += UndertrickValue(n, contract.Doubling, vul);
            return -penalty;
        }

        var multiplier = contract.Doubling switch
        {
            Doubling.Doubled => 2,
            Doubling.Redoubled => 4,
            _ => 1,
        };

        var trickScore = 0;
        for (var trick = 1; trick <= contract.Level; trick++)
            trickScore += ContractedTrickValue(contract.Strain, trick);
        trickScore *= multiplier;

        var overtrickValue = contract.Doubling switch
        {
            Doubling.None => ContractedTrickValue(contract.Strain, 2),
            Doubling.Doubled => vulnerable ? 200 : 100,
            _ => vulnerable ? 400 : 200,
        };
        var overtricks = (tricksTaken - contract.TricksNeeded) * overtrickValue;

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

        return trickScore + overtricks + gameBonus + slamBonus + insult;
    }

    private static IEnumerable<(Contract Contract, Vulnerability Vul, int Tricks)> WholeSpace()
    {
        foreach (var level in Enumerable.Range(1, 7))
            foreach (var strain in Enum.GetValues<Strain>())
                foreach (var doubling in Enum.GetValues<Doubling>())
                    foreach (var vul in Enum.GetValues<Vulnerability>())
                        foreach (var tricks in Enumerable.Range(0, 14))
                            yield return (
                                new Contract(level, strain, doubling, Seat.North), vul, tricks);
    }

    // ------------------------------------------------------------- enumeration

    [Fact]
    public void TheWholeScoreSpaceAgreesWithTheLaw()
    {
        var cases = 0;
        foreach (var (contract, vul, tricks) in WholeSpace())
        {
            var entry = new PlayedContract(contract, tricks);
            DuplicateScore.For(entry, vul).Should().Be(Oracle(contract, vul, tricks),
                $"{entry.Notation} {vul} taking {tricks} tricks is scored by Law 77");
            cases++;
        }
        cases.Should().Be(2940);
    }

    [Fact]
    public void TheScoreNeverFallsAsDeclarerTakesAnotherTrick()
    {
        foreach (var (contract, vul, tricks) in WholeSpace())
        {
            if (tricks == 0) continue;
            var fewer = DuplicateScore.For(new PlayedContract(contract, tricks - 1), vul);
            var more = DuplicateScore.For(new PlayedContract(contract, tricks), vul);
            more.Should().BeGreaterThanOrEqualTo(fewer,
                $"one more trick on {contract.Level}{contract.Strain} can never cost points");
        }
    }

    // ----------------------------------------- fixed points, transcribed known

    [Theory]
    // Part-scores and games, undoubled.
    [InlineData("1C N =", Vulnerability.NotVulnerable, 70)]
    [InlineData("1NT N =", Vulnerability.NotVulnerable, 90)]
    [InlineData("1NT N +1", Vulnerability.NotVulnerable, 120)]
    [InlineData("3NT N =", Vulnerability.NotVulnerable, 400)]
    [InlineData("3NT N =", Vulnerability.Vulnerable, 600)]
    [InlineData("4S N =", Vulnerability.NotVulnerable, 420)]
    [InlineData("4S N =", Vulnerability.Vulnerable, 620)]
    [InlineData("5D N =", Vulnerability.Vulnerable, 600)]
    // Doubling.
    [InlineData("2SX N =", Vulnerability.NotVulnerable, 470)] // doubled into game
    [InlineData("4SX N =", Vulnerability.Vulnerable, 790)]
    [InlineData("1CXX N =", Vulnerability.NotVulnerable, 230)]
    [InlineData("3NTX N +1", Vulnerability.Vulnerable, 950)]
    // Slams.
    [InlineData("6C N =", Vulnerability.NotVulnerable, 920)]
    [InlineData("6NT N =", Vulnerability.Vulnerable, 1440)]
    [InlineData("7NT N =", Vulnerability.Vulnerable, 2220)]
    [InlineData("7NTXX N =", Vulnerability.Vulnerable, 2980)] // the largest score
    // Going down.
    [InlineData("4S N -1", Vulnerability.NotVulnerable, -50)]
    [InlineData("4S N -1", Vulnerability.Vulnerable, -100)]
    [InlineData("1NTX N -2", Vulnerability.NotVulnerable, -300)]
    [InlineData("2HX N -5", Vulnerability.NotVulnerable, -1100)]
    [InlineData("3NTX N -3", Vulnerability.Vulnerable, -800)]
    [InlineData("7NTXX N -13", Vulnerability.Vulnerable, -7600)] // the smallest
    public void KnownScoresFromTheTable(string notation, Vulnerability vul, int score)
    {
        var accepted = ContractNotation.Parse(notation)
            .Should().BeOfType<NotationResult.Accepted>().Subject;
        DuplicateScore.For(accepted.Entry, vul).Should().Be(score);
    }

    // ------------------------------------------------------------- passed out

    [Theory]
    [InlineData(Vulnerability.NotVulnerable)]
    [InlineData(Vulnerability.Vulnerable)]
    public void APassedOutBoardScoresZero(Vulnerability vul) =>
        DuplicateScore.For(new PassedOut(), vul).Should().Be(0);

    // -------------------------------------------------------------- sign rules

    [Fact]
    public void MadeContractsScorePositiveAndDownScoresNegative()
    {
        foreach (var (contract, vul, tricks) in WholeSpace())
        {
            var score = DuplicateScore.For(new PlayedContract(contract, tricks), vul);
            if (tricks >= contract.TricksNeeded)
                score.Should().BePositive();
            else
                score.Should().BeNegative();
        }
    }

    [Fact]
    public void VulnerabilityNeverHelpsTheDefendersOfAMadeContract()
    {
        foreach (var (contract, _, tricks) in WholeSpace())
        {
            if (tricks < contract.TricksNeeded) continue;
            var entry = new PlayedContract(contract, tricks);
            DuplicateScore.For(entry, Vulnerability.Vulnerable).Should()
                .BeGreaterThanOrEqualTo(DuplicateScore.For(entry, Vulnerability.NotVulnerable),
                    "every bonus is at least as large vulnerable");
        }
    }
}
