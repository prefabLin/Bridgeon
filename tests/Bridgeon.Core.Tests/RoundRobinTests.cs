using Bridgeon.Core.Scheduling;
using FluentAssertions;

namespace Bridgeon.Core.Tests;

/// <summary>
/// A round robin is a combinatorial object with provable properties
/// (wiki/rules/round-robin-schedule.md, decision 0003). These tests assert the
/// properties directly — counting meetings, appearances and byes — never
/// replaying the generator's construction, so the generator and the checks are
/// two independent statements.
/// </summary>
public class RoundRobinTests
{
    private const int TeamLimit = 60; // the validated hard limit

    // ------------------------------------------------------ single round robin

    [Fact]
    public void EveryTeamCountSatisfiesTheBalanceProperties()
    {
        for (var teams = 2; teams <= TeamLimit; teams++)
        {
            var schedule = RoundRobin.Generate(teams);
            var even = teams % 2 == 0;

            schedule.Teams.Should().Be(teams);
            schedule.Rounds.Should().HaveCount(even ? teams - 1 : teams,
                "an even count plays N-1 rounds; an odd count needs N because of the bye");

            MeetingCounts(schedule).Should().OnlyContain(pair => pair.Value == 1,
                "every pair of teams meets exactly once in a {0}-team round robin", teams);

            foreach (var round in schedule.Rounds)
            {
                round.Pairings.Should().HaveCount(teams / 2);
                var seated = round.Pairings.SelectMany(p => new[] { p.Home, p.Away }).ToArray();
                seated.Should().OnlyHaveUniqueItems(
                    "no team plays twice in round {0} of {1} teams", round.Number, teams);
                if (even)
                    round.Bye.Should().BeNull();
                else
                    seated.Concat([round.Bye!.Value]).Should()
                        .BeEquivalentTo(Enumerable.Range(1, teams));
            }

            if (!even)
                schedule.Rounds.Select(r => r.Bye!.Value).Should()
                    .OnlyHaveUniqueItems("every team byes exactly once");
        }
    }

    [Fact]
    public void RoundsAreNumberedFromOne() =>
        RoundRobin.Generate(8).Rounds.Select(r => r.Number)
            .Should().Equal(Enumerable.Range(1, 7));

    // ----------------------------------------------------- counter round robin

    [Fact]
    public void TheCounterRoundRobinSeatsEveryTeamTwicePerRound()
    {
        for (var teams = 3; teams <= TeamLimit - 1; teams += 2)
        {
            var schedule = RoundRobin.GenerateCounter(teams);

            schedule.Rounds.Should().HaveCount((teams - 1) / 2,
                "playing two matches per round halves the rounds");

            MeetingCounts(schedule).Should().OnlyContain(pair => pair.Value == 1,
                "the counter form still meets every pair exactly once, {0} teams", teams);

            foreach (var round in schedule.Rounds)
            {
                round.Bye.Should().BeNull("everyone plays in every counter round");
                round.Pairings.Should().HaveCount(teams);
                round.Pairings.SelectMany(p => new[] { p.Home, p.Away })
                    .GroupBy(t => t)
                    .Should().OnlyContain(g => g.Count() == 2,
                        "each of the {0} teams plays exactly twice in round {1}",
                        teams, round.Number);
            }
        }
    }

    [Fact]
    public void ThreeTeamsMakeExactlyTheTriangleMatch()
    {
        var schedule = RoundRobin.GenerateCounter(3);

        schedule.Rounds.Should().HaveCount(1);
        MeetingCounts(schedule).Keys.Should().BeEquivalentTo(
            [(1, 2), (1, 3), (2, 3)], "the one round is the whole triangle");
    }

    [Fact]
    public void AnEvenCounterRoundRobinIsRefusedByName()
    {
        // No design exists without a leftover ordinary round; which arrangement
        // the association wants is an open question, not a guess.
        var generate = () => RoundRobin.GenerateCounter(8);
        generate.Should().Throw<ArgumentException>().WithMessage("*even*");
    }

    // -------------------------------------------------------------- provenance

    [Fact]
    public void AGeneratedScheduleSaysSoWithNoWarnings()
    {
        foreach (var schedule in new[] { RoundRobin.Generate(9), RoundRobin.GenerateCounter(9) })
        {
            schedule.Provenance.Origin.Should().Be(ScheduleOrigin.Generated);
            schedule.Provenance.Warnings.Should().BeEmpty();
        }
    }

    [Fact]
    public void GenerationIsDeterministic()
    {
        var first = RoundRobin.Generate(11);
        var second = RoundRobin.Generate(11);

        for (var r = 0; r < first.Rounds.Count; r++)
            first.Rounds[r].Pairings.Should().Equal(second.Rounds[r].Pairings);
    }

    // -------------------------------------------------------------- validation

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(TeamLimit + 1)]
    public void TeamCountsOutsideTheLimitsAreRejected(int teams)
    {
        var single = () => RoundRobin.Generate(teams);
        var counter = () => RoundRobin.GenerateCounter(teams);

        single.Should().Throw<ArgumentOutOfRangeException>();
        counter.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ---------------------------------------------------------------- helpers

    private static Dictionary<(int, int), int> MeetingCounts(Schedule schedule)
    {
        var counts = new Dictionary<(int, int), int>();
        foreach (var pairing in schedule.Rounds.SelectMany(r => r.Pairings))
        {
            var key = pairing.Home < pairing.Away
                ? (pairing.Home, pairing.Away)
                : (pairing.Away, pairing.Home);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        return counts;
    }
}
