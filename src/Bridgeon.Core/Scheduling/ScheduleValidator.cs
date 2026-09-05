namespace Bridgeon.Core.Scheduling;

/// <summary>
/// Checks a schedule against its own declared contract (decision 0003): every
/// pair meets the declared number of times, every team is seated the declared
/// number of times per round or is that round's bye, byes land evenly, and the
/// rounds are numbered without gaps. Runs on every schedule before an event
/// can use it, whatever produced it.
/// </summary>
public static class ScheduleValidator
{
    /// <summary>Every violated property, one message each; empty means valid.</summary>
    public static IReadOnlyList<string> Violations(Schedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        var violations = new List<string>();

        if (!schedule.Rounds.Select(r => r.Number)
                .SequenceEqual(Enumerable.Range(1, schedule.Rounds.Count)))
            violations.Add(
                $"Round numbers must run 1..{schedule.Rounds.Count} without gaps.");

        var byes = new Dictionary<int, int>();
        foreach (var round in schedule.Rounds)
            CheckRound(schedule, round, violations, byes);

        CheckMeetings(schedule, violations);

        if (byes.Values.Distinct().Count() > 1
            || (byes.Count > 0 && byes.Count < schedule.Teams))
        {
            var most = byes.MaxBy(b => b.Value);
            violations.Add(
                $"Uneven byes: team {most.Key} sits out {most.Value} round(s) "
                + "while another sits out fewer.");
        }

        return violations;
    }

    private static void CheckRound(
        Schedule schedule, Round round, List<string> violations, Dictionary<int, int> byes)
    {
        var seatings = new Dictionary<int, int>();
        foreach (var pairing in round.Pairings)
        {
            if (pairing.Home == pairing.Away)
                violations.Add(
                    $"Round {round.Number}: team {pairing.Home} is paired against itself.");
            foreach (var team in new[] { pairing.Home, pairing.Away })
            {
                if (team < 1 || team > schedule.Teams)
                {
                    violations.Add(
                        $"Round {round.Number}: team {team} is not in the field "
                        + $"of {schedule.Teams}.");
                    continue;
                }

                seatings[team] = seatings.GetValueOrDefault(team) + 1;
            }
        }

        foreach (var (team, count) in seatings)
            if (count != schedule.MatchesPerTeamPerRound)
                violations.Add(
                    $"Team {team} is seated {Times(count)} in round {round.Number} "
                    + $"but should be seated {Times(schedule.MatchesPerTeamPerRound)}.");

        if (round.Bye is { } bye)
        {
            if (seatings.ContainsKey(bye))
                violations.Add($"Round {round.Number}: the bye team {bye} is seated.");
            else
                byes[bye] = byes.GetValueOrDefault(bye) + 1;
        }

        foreach (var team in Enumerable.Range(1, schedule.Teams))
            if (!seatings.ContainsKey(team) && team != round.Bye)
                violations.Add(
                    $"Team {team} is neither seated nor the bye in round {round.Number}.");
    }

    private static void CheckMeetings(Schedule schedule, List<string> violations)
    {
        var meetings = new Dictionary<(int Low, int High), int>();
        foreach (var pairing in schedule.Rounds.SelectMany(r => r.Pairings))
        {
            var key = pairing.Home < pairing.Away
                ? (pairing.Home, pairing.Away)
                : (pairing.Away, pairing.Home);
            meetings[key] = meetings.GetValueOrDefault(key) + 1;
        }

        for (var low = 1; low <= schedule.Teams; low++)
            for (var high = low + 1; high <= schedule.Teams; high++)
            {
                var count = meetings.GetValueOrDefault((low, high));
                if (count != schedule.MeetingsPerPair)
                    violations.Add(
                        $"Teams {low} and {high} meet {Times(count)} "
                        + $"but must meet {Times(schedule.MeetingsPerPair)}.");
            }
    }

    private static string Times(int count) => count switch
    {
        0 => "never",
        1 => "once",
        2 => "twice",
        _ => $"{count} times",
    };
}
