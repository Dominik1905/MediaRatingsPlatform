using System.Collections.Specialized;
using DatabaseObjects;
using DatabaseObjects.Service;

namespace MediaRatingsPlatform;

public static class MediaQueryOptionsParser
{
    public static MediaQueryOptions Parse(System.Collections.Specialized.NameValueCollection qs)
    {
        var opt = new MediaQueryOptions();
        
        opt.TitlePart = (qs["titlePart"] ?? qs["title"] ?? "").Trim();
        opt.Genre = (qs["genre"] ?? "").Trim();

        if (int.TryParse(qs["type"], out var t)) opt.Type = (MediaType)t;
        if (int.TryParse(qs["year"], out var y)) opt.ReleaseYear = y;
        if (int.TryParse(qs["yearFrom"], out var yf)) opt.ReleaseYearFrom = yf;
        if (int.TryParse(qs["yearTo"], out var yt)) opt.ReleaseYearTo = yt;
        if (int.TryParse(qs["age"], out var a)) opt.AgeRestriction = a;

        if (double.TryParse(qs["minRating"], System.Globalization.CultureInfo.InvariantCulture, out var mr))
            opt.MinAverageRating = mr;

        opt.SortBy = (qs["sortBy"] ?? "").Trim();
        var sortOrder = (qs["sortOrder"] ?? "").Trim().ToLowerInvariant();
        opt.SortDescending = sortOrder == "desc";

        if (int.TryParse(qs["limit"], out var limit)) opt.Limit = limit;
        if (int.TryParse(qs["offset"], out var offset)) opt.Offset = offset;

        return opt;
    }


    private static bool TryParseInt(string? s, out int value)
        => int.TryParse(s, out value);

    private static bool TryParseDouble(string? s, out double value)
        => double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);

    private static bool TryParseMediaType(string s, out MediaType mediaType)
    {
        mediaType = MediaType.Movie;

        if (int.TryParse(s, out var i))
        {
            if (i < 0 || i > 2) return false;
            mediaType = (MediaType)i;
            return true;
        }

        switch (s.ToLowerInvariant())
        {
            case "movie":
                mediaType = MediaType.Movie;
                return true;
            case "series":
                mediaType = MediaType.Series;
                return true;
            case "game":
                mediaType = MediaType.Game;
                return true;
            default:
                return false;
        }
    }
}
