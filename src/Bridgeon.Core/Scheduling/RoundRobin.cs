namespace Bridgeon.Core.Scheduling;

/// <summary>
/// Round robin schedules from the circle method and its relatives
/// (wiki/rules/round-robin-schedule.md). Every output satisfies the balance
/// properties of decision 0003; the validator is the independent check.
/// </summary>
public static class RoundRobin
{
    /// <summary>The validated hard limit on the field size.</summary>
    public const int TeamLimit = 60;

    /// <summary>
    /// A single round robin: every pair meets exactly once. Even counts play
    /// N−1 rounds; odd counts play N rounds with one bye per round, each team
    /// sitting out exactly once.
    /// </summary>
    public static Schedule Generate(int teams)
    {
        ValidateTeamCount(teams);

        // The circle method, with a phantom team when the count is odd:
        // whoever meets the phantom sits out.
        var even = teams % 2 == 0;
        var circle = even ? teams : teams + 1;
        var rotating = Enumerable.Range(1, circle - 1).ToArray();

        var rounds = new List<Round>();
        for (var number = 1; number < circle; number++)
        {
            var pairings = new List<Pairing>();
            int? bye;
            if (even)
            {
                // The fixed team is real: it plays the head of the circle.
                pairings.Add(new Pairing(rotating[0], circle));
                bye = null;
            }
            else
            {
                // The fixed team is the phantom: the head of the circle
                // sits out.
                bye = rotating[0];
            }

            for (var i = 1; i <= (circle / 2) - 1; i++)
                pairings.Add(new Pairing(rotating[i], rotating[circle - 1 - i]));

            rounds.Add(new Round(number, pairings, bye));

            // Rotate right: the last joins the head, the fixed team stays put.
            var last = rotating[^1];
            Array.Copy(rotating, 0, rotating, 1, rotating.Length - 1);
            rotating[0] = last;
        }

        return new Schedule(teams, 1, 1, rounds, ScheduleProvenance.Generated);
    }

    /// <summary>
    /// The counter round robin: every team plays twice per round, halving the
    /// rounds, and every pair still meets exactly once. Exists only for odd
    /// counts — round r pairs each team i with team i+r around the circle, and
    /// the differences 1…(N−1)/2 partition all the pairs. Three teams give
    /// exactly the triangle match.
    /// </summary>
    public static Schedule GenerateCounter(int teams)
    {
        ValidateTeamCount(teams);
        if (teams % 2 == 0)
            throw new ArgumentException(
                $"A counter round robin needs an odd team count; {teams} is even. "
                + "No design seats every team twice per round without a leftover "
                + "ordinary round — see the open questions in ROADMAP.md.",
                nameof(teams));

        var rounds = new List<Round>();
        for (var difference = 1; difference <= (teams - 1) / 2; difference++)
        {
            var pairings = Enumerable.Range(1, teams)
                .Select(home => new Pairing(home, ((home - 1 + difference) % teams) + 1))
                .ToArray();
            rounds.Add(new Round(difference, pairings, null));
        }

        return new Schedule(teams, 1, 2, rounds, ScheduleProvenance.Generated);
    }

    private static void ValidateTeamCount(int teams)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(teams, 2);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(teams, TeamLimit);
    }
}
