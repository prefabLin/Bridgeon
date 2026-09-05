using Bridgeon.Core.Scheduling;
using FluentAssertions;

namespace Bridgeon.Core.Tests;

/// <summary>
/// Decision 0003: every schedule passes the validator before an event can use
/// it, whatever produced it. So the validator must accept what the generator
/// makes and name every planted corruption — a validator that cannot fail is
/// not a gate.
/// </summary>
public class ScheduleValidatorTests
{
    [Fact]
    public void GeneratedSchedulesHaveNoViolations()
    {
        for (var teams = 2; teams <= 60; teams++)
            ScheduleValidator.Violations(RoundRobin.Generate(teams))
                .Should().BeEmpty("the generator's output must pass its own gate, {0} teams", teams);

        for (var teams = 3; teams <= 59; teams += 2)
            ScheduleValidator.Violations(RoundRobin.GenerateCounter(teams))
                .Should().BeEmpty("counter form, {0} teams", teams);
    }

    [Fact]
    public void ASwappedTeamIsNamedThreeWays()
    {
        // Replace one pairing's away team, so one pair meets twice, another
        // never, and the substitute is seated twice in that round.
        var schedule = RoundRobin.Generate(8);
        var round = schedule.Rounds[0];
        var victim = round.Pairings[0];
        var substitute = round.Pairings[1].Home;
        var corrupted = Replace(schedule, 0, round with
        {
            Pairings = [victim with { Away = substitute }, .. round.Pairings.Skip(1)],
        });

        var violations = ScheduleValidator.Violations(corrupted);

        violations.Should().Contain(v => v.Contains("twice in round 1"),
            "the substitute now plays two tables at once");
        violations.Should().Contain(
            v => v.Contains($"{victim.Home}") && v.Contains($"{victim.Away}"),
            "the abandoned opponents never meet");
        violations.Should().Contain(
            v => v.Contains($"{victim.Home}") && v.Contains($"{substitute}"),
            "the new pair now meets twice");
    }

    [Fact]
    public void AMissingRoundBreaksTheMeetingCountAndTheNumbering()
    {
        var schedule = RoundRobin.Generate(6);
        var truncated = schedule with { Rounds = [.. schedule.Rounds.Skip(1)] };

        var violations = ScheduleValidator.Violations(truncated);

        violations.Should().Contain(v => v.Contains("Round numbers"),
            "rounds must run 1..R with nothing missing");
        violations.Should().NotBeEmpty();
    }

    [Fact]
    public void ATeamPairedAgainstItselfIsNamed()
    {
        var schedule = RoundRobin.Generate(4);
        var round = schedule.Rounds[0];
        var corrupted = Replace(schedule, 0, round with
        {
            Pairings = [new Pairing(2, 2), .. round.Pairings.Skip(1)],
        });

        ScheduleValidator.Violations(corrupted)
            .Should().Contain(v => v.Contains("itself"));
    }

    [Fact]
    public void ATeamNumberOutsideTheFieldIsNamed()
    {
        var schedule = RoundRobin.Generate(4);
        var round = schedule.Rounds[0];
        var corrupted = Replace(schedule, 0, round with
        {
            Pairings = [round.Pairings[0] with { Away = 99 }, .. round.Pairings.Skip(1)],
        });

        ScheduleValidator.Violations(corrupted)
            .Should().Contain(v => v.Contains("99"));
    }

    [Fact]
    public void AWrongByeIsNamed()
    {
        // Point the bye at a team that is actually seated; the team that truly
        // sits out goes unaccounted for.
        var schedule = RoundRobin.Generate(5);
        var round = schedule.Rounds[0];
        var corrupted = Replace(schedule, 0, round with { Bye = round.Pairings[0].Home });

        ScheduleValidator.Violations(corrupted).Should().NotBeEmpty();
    }

    [Fact]
    public void UnevenByesAreNamed()
    {
        // Swap one round's bye onto a team that already byed elsewhere.
        var schedule = RoundRobin.Generate(5);
        var first = schedule.Rounds[0];
        var second = schedule.Rounds[1];
        var doubleByed = first.Bye!.Value;
        // Seat the second round's bye team in place of the double-byed team.
        var reseated = second.Pairings
            .Select(p => p.Home == doubleByed
                ? p with { Home = second.Bye!.Value }
                : p.Away == doubleByed ? p with { Away = second.Bye!.Value } : p)
            .ToArray();
        var corrupted = Replace(schedule, 1, second with
        {
            Pairings = reseated, Bye = doubleByed,
        });

        ScheduleValidator.Violations(corrupted)
            .Should().Contain(v => v.Contains("bye"),
                "byes must land on every team equally");
    }

    private static Schedule Replace(Schedule schedule, int index, Round round)
    {
        var rounds = schedule.Rounds.ToArray();
        rounds[index] = round;
        return schedule with { Rounds = rounds };
    }
}
