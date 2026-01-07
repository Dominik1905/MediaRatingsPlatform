namespace DatabaseObjects.Service;

public sealed class MediaQueryOptions
{
    // Search
    public string? TitlePart { get; set; }

    // Filters
    public string? Genre { get; set; }
    public DatabaseObjects.MediaType? Type { get; set; }
    public int? ReleaseYear { get; set; }
    public int? ReleaseYearFrom { get; set; }
    public int? ReleaseYearTo { get; set; }
    public int? AgeRestriction { get; set; }
    public double? MinAverageRating { get; set; }

    // Sorting
    public string SortBy { get; set; } = "title";
    public bool SortDescending { get; set; } = false;

    // Optional paging
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}
