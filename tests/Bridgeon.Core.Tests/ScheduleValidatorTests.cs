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

        var violations = ScheduleValidator.Violations(corrupted);

        violations.Should().Contain(v => v.Contains("bye team"),
            "the seated bye is its own offence");
        violations.Should().Contain(v => v.Contains("neither seated"),
            "the true sitter-out is unaccounted for");
    }

    [Fact]
    public void ADuplicatedRoundIsNamedPairByPair()
    {
        // Replace round 2 with a copy of round 1: every round is internally
        // fine, so only the meeting counts can catch it — pair by pair.
        var schedule = RoundRobin.Generate(6);
        var copied = schedule.Rounds[0] with { Number = 2 };
        var original = schedule.Rounds[1];
        var corrupted = Replace(schedule, 1, copied);

        var violations = ScheduleValidator.Violations(corrupted);

        foreach (var pairing in schedule.Rounds[0].Pairings)
            violations.Should().Contain(Meeting(pairing, "twice"),
                "the copied round doubles its meetings");
        foreach (var pairing in original.Pairings)
            violations.Should().Contain(Meeting(pairing, "never"),
                "the discarded round's meetings are gone");
    }

    [Fact]
    public void AHandCraftedDoubleRoundRobinIsValid()
    {
        // Three teams, every pair twice, byes landing twice on everyone. The
        // validator checks the declared contract, not the generator's habits.
        var schedule = new Schedule(3, 2, 1,
            [
                new Round(1, [new Pairing(1, 2)], 3),
                new Round(2, [new Pairing(1, 3)], 2),
                new Round(3, [new Pairing(2, 3)], 1),
                new Round(4, [new Pairing(2, 1)], 3),
                new Round(5, [new Pairing(3, 1)], 2),
                new Round(6, [new Pairing(3, 2)], 1),
            ],
            ScheduleProvenance.Generated);

        ScheduleValidator.Violations(schedule).Should().BeEmpty();
    }

    [Fact]
    public void UnevenByesTripEvenWhenEveryTeamHasByedAtLeastOnce()
    {
        // A fourth round hands team 3 a second bye while 1 and 2 keep one
        // each: the meetings are wrong too, but the bye imbalance must be
        // named in its own right.
        var schedule = new Schedule(3, 1, 1,
            [
                new Round(1, [new Pairing(1, 2)], 3),
                new Round(2, [new Pairing(1, 3)], 2),
                new Round(3, [new Pairing(2, 3)], 1),
                new Round(4, [new Pairing(1, 2)], 3),
            ],
            ScheduleProvenance.Generated);

        ScheduleValidator.Violations(schedule)
            .Should().Contain(v => v.Contains("Uneven byes") && v.Contains("sits out 2"));
    }

    [Fact]
    public void ANullScheduleIsRejectedByItsParameterName()
    {
        var validate = () => ScheduleValidator.Violations(null!);
        validate.Should().Throw<ArgumentNullException>().WithParameterName("schedule");
    }

    private static string Meeting(Pairing pairing, string times)
    {
        var (low, high) = pairing.Home < pairing.Away
            ? (pairing.Home, pairing.Away)
            : (pairing.Away, pairing.Home);
        return $"Teams {low} and {high} meet {times} but must meet once.";
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
            .Should().Contain(v => v.Contains("bye") && v.Contains("sits out 2"),
                "byes must land on every team equally, and the message names "
                + "the team with the most");
    }

    private static Schedule Replace(Schedule schedule, int index, Round round)
    {
        var rounds = schedule.Rounds.ToArray();
        rounds[index] = round;
        return schedule with { Rounds = rounds };
    }
}
