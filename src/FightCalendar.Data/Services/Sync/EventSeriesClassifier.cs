using System.Text.RegularExpressions;

namespace FightCalendar.Data.Services.Sync;

// Discovers a promotion's recurring sub-series (e.g. "Friday Fights" within
// ONE, "Fight Night" within UFC) directly from event titles, with no
// hardcoded list of known series names per promotion. If a promotion
// launches a new named sub-series tomorrow, this picks it up on the next
// sync with no code change - the frontend renders whatever series
// distinctly show up in the data.
public static partial class EventSeriesClassifier
{
    public static string? ExtractSubSeries(string promotionCode, string title)
    {
        if (!StartsWithWholeWord(title, promotionCode))
        {
            return null;
        }

        var rest = title[promotionCode.Length..].TrimStart();

        // Drop everything from the first colon onward - that's the matchup/
        // location descriptor (e.g. ": Van vs. Pantoja 2"), not part of the
        // series identity.
        var colonIndex = rest.IndexOf(':');
        if (colonIndex >= 0)
        {
            rest = rest[..colonIndex];
        }

        rest = rest.Trim();

        // Truncate at the first standalone number, wherever it falls -
        // "Friday Fights 170" -> "Friday Fights", but also "LANDMARK 16 in
        // NAGASAKI" -> "LANDMARK" (the city name after the number is more
        // like the colon-separated descriptor above, just without a colon).
        // A bare numbered flagship event ("331") truncates down to nothing,
        // meaning no sub-series at all.
        var match = FirstNumberPattern().Match(rest);
        var name = match.Success ? rest[..match.Index] : rest;
        name = name.Trim();

        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    // Prevents "MVP" from matching inside "MVPW" - the character right after
    // the promotion code must be whitespace or the end of the string.
    private static bool StartsWithWholeWord(string title, string prefix)
    {
        if (!title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return title.Length == prefix.Length || char.IsWhiteSpace(title[prefix.Length]);
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex FirstNumberPattern();
}
