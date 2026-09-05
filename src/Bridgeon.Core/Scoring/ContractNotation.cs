namespace Bridgeon.Core.Scoring;

/// <summary>Why an entry was rejected. Each reason names one mistake.</summary>
public enum RejectionReason
{
    EmptyInput,
    UnknownLevel,
    UnknownStrain,
    UnknownDoubling,
    UnknownDeclarer,
    MissingResult,
    UnknownResult,
    ImpossibleOvertricks,
    ImpossibleUndertricks,
    UnexpectedTrailingInput,
}

/// <summary>The outcome of parsing an entry: accepted with its board entry, or
/// rejected with a reason and a message quoting the offending text.</summary>
public abstract record NotationResult
{
    private NotationResult() { }

    public sealed record Accepted(BoardEntry Entry) : NotationResult;

    public sealed record Rejected(RejectionReason Reason, string Message) : NotationResult;
}

/// <summary>
/// Parses Bridgeon's contract notation (wiki/rules/contract-notation.md).
/// </summary>
/// <remarks>
/// Never throws: a typo is an expected input, so every failure is a typed
/// rejection. Case does not matter and whitespace between elements is optional.
/// Strains are read before seats, which is what keeps <c>S</c> unambiguous.
/// </remarks>
public static class ContractNotation
{
    public static NotationResult Parse(string? input)
    {
        var text = input ?? "";
        var at = 0;
        SkipWhitespace(text, ref at);

        if (at >= text.Length)
            return Reject(RejectionReason.EmptyInput, "Nothing was entered.");

        if (TryReadPass(text, ref at))
            return Finish(text, at, new PassedOut());

        // Level.
        var levelChar = text[at];
        if (levelChar is < '1' or > '7')
            return Reject(RejectionReason.UnknownLevel,
                $"'{levelChar}' is not a level; an entry starts with 1 through 7.");
        var level = levelChar - '0';
        at++;
        SkipWhitespace(text, ref at);

        // Strain. NT must be contiguous; a lone N also reads as no-trump.
        if (at >= text.Length)
            return Reject(RejectionReason.UnknownStrain,
                "The entry ended where the strain belongs.");
        Strain strain;
        switch (char.ToUpperInvariant(text[at]))
        {
            case 'C': strain = Strain.Clubs; at++; break;
            case 'D': strain = Strain.Diamonds; at++; break;
            case 'H': strain = Strain.Hearts; at++; break;
            case 'S': strain = Strain.Spades; at++; break;
            case 'N':
                strain = Strain.NoTrump;
                at++;
                if (at < text.Length && char.ToUpperInvariant(text[at]) == 'T') at++;
                break;
            default:
                return Reject(RejectionReason.UnknownStrain,
                    $"'{text[at]}' is not a strain; expected C, D, H, S or NT.");
        }
        SkipWhitespace(text, ref at);

        // Doubling: a contiguous run of X.
        var crosses = 0;
        while (at < text.Length && char.ToUpperInvariant(text[at]) == 'X')
        {
            crosses++;
            at++;
        }
        if (crosses > 2)
            return Reject(RejectionReason.UnknownDoubling,
                $"{new string('X', crosses)} is not a doubling; only X and XX exist.");
        var doubling = crosses switch
        {
            1 => Doubling.Doubled,
            2 => Doubling.Redoubled,
            _ => Doubling.None,
        };
        SkipWhitespace(text, ref at);

        // Declarer.
        if (at >= text.Length)
            return Reject(RejectionReason.UnknownDeclarer,
                "The entry ended where the declarer's seat belongs.");
        Seat declarer;
        switch (char.ToUpperInvariant(text[at]))
        {
            case 'N': declarer = Seat.North; break;
            case 'E': declarer = Seat.East; break;
            case 'S': declarer = Seat.South; break;
            case 'W': declarer = Seat.West; break;
            default:
                return Reject(RejectionReason.UnknownDeclarer,
                    $"'{text[at]}' is not a seat; expected N, E, S or W.");
        }
        at++;
        SkipWhitespace(text, ref at);

        // Result.
        if (at >= text.Length)
            return Reject(RejectionReason.MissingResult,
                "The entry ended before the result; expected =, +n or -n.");

        var contract = new Contract(level, strain, doubling, declarer);
        int tricksTaken;
        var resultStart = at;
        var sign = text[at];
        if (sign == '=')
        {
            tricksTaken = contract.TricksNeeded;
            at++;
        }
        else if (sign is '+' or '-')
        {
            at++;
            SkipWhitespace(text, ref at);
            var digits = 0;
            var count = 0;
            while (at < text.Length && char.IsAsciiDigit(text[at]) && digits <= 13)
            {
                digits = (digits * 10) + (text[at] - '0');
                count++;
                at++;
            }
            var token = ResultToken(text, resultStart, at);
            if (count == 0 || digits == 0 || digits > 13)
                return Reject(RejectionReason.UnknownResult,
                    $"'{token}' is not a result; expected =, +n or -n.");
            if (sign == '+' && contract.TricksNeeded + digits > 13)
                return Reject(RejectionReason.ImpossibleOvertricks,
                    $"'{token}' would take the trick total past 13.");
            if (sign == '-' && digits > contract.TricksNeeded)
                return Reject(RejectionReason.ImpossibleUndertricks,
                    $"'{token}' is more tricks than the contract needs.");
            tricksTaken = sign == '+'
                ? contract.TricksNeeded + digits
                : contract.TricksNeeded - digits;
        }
        else
        {
            return Reject(RejectionReason.UnknownResult,
                $"'{sign}' is not a result; expected =, +n or -n.");
        }

        return Finish(text, at, new PlayedContract(contract, tricksTaken));
    }

    private static NotationResult Finish(string text, int at, BoardEntry entry)
    {
        SkipWhitespace(text, ref at);
        if (at < text.Length)
            return Reject(RejectionReason.UnexpectedTrailingInput,
                $"'{text[at..].TrimEnd()}' was left over after the entry.");
        return new NotationResult.Accepted(entry);
    }

    private static bool TryReadPass(string text, ref int at)
    {
        const string word = "PASS";
        if (at + word.Length > text.Length) return false;
        for (var i = 0; i < word.Length; i++)
            if (char.ToUpperInvariant(text[at + i]) != word[i])
                return false;
        at += word.Length;
        return true;
    }

    private static string ResultToken(string text, int start, int end) =>
        string.Concat(text[start..end].Where(c => !char.IsWhiteSpace(c)));

    private static void SkipWhitespace(string text, ref int at)
    {
        while (at < text.Length && char.IsWhiteSpace(text[at])) at++;
    }

    private static NotationResult Reject(RejectionReason reason, string message) =>
        new NotationResult.Rejected(reason, message);
}
