namespace Bridgeon.Core.Scoring;

/// <summary>
/// One band of an IMP scale: point differences from <paramref name="Lower"/> to
/// <paramref name="Upper"/> inclusive are worth <paramref name="Imp"/> IMPs.
/// </summary>
/// <param name="Lower">Smallest point difference in the band.</param>
/// <param name="Upper">Largest point difference, or null for the final open band.</param>
/// <param name="Imp">IMPs awarded.</param>
public sealed record ImpBand(int Lower, int? Upper, int Imp);

/// <summary>
/// Converts a point difference into International Match Points.
/// </summary>
/// <remarks>
/// <see cref="Law78"/> is the scale published as Law 78 of the Laws of Duplicate
/// Bridge. The type is immutable, does no I/O, and accepts a caller-supplied
/// table so that the scale in force is part of a named ruleset rather than a
/// constant compiled into the engine.
/// </remarks>
public sealed class ImpScale
{
    private static readonly int[] Law78Thresholds =
    [
        0, 20, 50, 90, 130, 170, 220, 270, 320, 370, 430, 500, 600, 750, 900,
        1100, 1300, 1500, 1750, 2000, 2250, 2500, 3000, 3500, 4000,
    ];

    private readonly ImpBand[] _bands;

    private ImpScale(ImpBand[] bands) => _bands = bands;

    /// <summary>The scale published as Law 78 of the Laws of Duplicate Bridge.</summary>
    public static ImpScale Law78 { get; } = FromBands(
        Law78Thresholds.Select((lower, index) => new ImpBand(
            lower,
            index + 1 < Law78Thresholds.Length ? Law78Thresholds[index + 1] - 1 : null,
            index)));

    /// <summary>The bands, ascending.</summary>
    public IReadOnlyList<ImpBand> Bands => _bands;

    /// <summary>The largest award this scale can give.</summary>
    public int MaxImp => _bands[^1].Imp;

    /// <summary>
    /// Builds a scale, rejecting any table that is not a well-formed partition of
    /// the non-negative point differences.
    /// </summary>
    /// <exception cref="ArgumentException">The bands do not form a valid scale.</exception>
    public static ImpScale FromBands(IEnumerable<ImpBand> bands)
    {
        ArgumentNullException.ThrowIfNull(bands);
        var ordered = bands.ToArray();

        if (ordered.Length == 0)
            throw new ArgumentException("A scale needs at least one band.", nameof(bands));
        if (ordered[0].Lower != 0)
            throw new ArgumentException(
                $"The first band must start at 0 but starts at {ordered[0].Lower}.",
                nameof(bands));
        if (ordered[^1].Upper is not null)
            throw new ArgumentException(
                "The final band must be open-ended so that every difference is covered.",
                nameof(bands));

        for (var i = 0; i < ordered.Length; i++)
        {
            var band = ordered[i];
            if (band.Imp < 0)
                throw new ArgumentException(
                    $"Band {i} awards a negative {band.Imp} IMPs.", nameof(bands));
            if (band.Upper is { } upper && upper < band.Lower)
                throw new ArgumentException(
                    $"Band {i} ends at {upper}, before it starts at {band.Lower}.",
                    nameof(bands));
            if (i == 0) continue;

            var previous = ordered[i - 1];
            if (band.Lower <= previous.Lower)
                throw new ArgumentException(
                    $"Band {i} starts at {band.Lower}, not above the previous band's "
                    + $"{previous.Lower}. Bands must ascend.", nameof(bands));
            if (previous.Upper is { } previousUpper && previousUpper != band.Lower - 1)
                throw new ArgumentException(
                    $"Band {i - 1} ends at {previousUpper} but band {i} starts at "
                    + $"{band.Lower}, leaving a gap or an overlap.", nameof(bands));
            if (band.Imp <= previous.Imp)
                throw new ArgumentException(
                    $"Band {i} awards {band.Imp} IMPs, not more than the previous "
                    + $"{previous.Imp}. Awards must ascend.", nameof(bands));
        }

        return new ImpScale(ordered);
    }

    /// <summary>
    /// The IMPs earned by a point difference. The sign is ignored: a difference is
    /// a magnitude, and which side earns the IMPs is the caller's concern.
    /// </summary>
    public int ImpFor(int pointDifference)
    {
        var magnitude = pointDifference == int.MinValue
            ? int.MaxValue
            : Math.Abs(pointDifference);

        var low = 0;
        var high = _bands.Length - 1;
        while (low < high)
        {
            var mid = low + ((high - low + 1) / 2);
            if (_bands[mid].Lower <= magnitude) low = mid;
            else high = mid - 1;
        }

        return _bands[low].Imp;
    }
}
