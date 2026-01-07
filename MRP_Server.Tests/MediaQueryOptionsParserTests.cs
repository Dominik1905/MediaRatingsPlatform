using System.Collections.Specialized;
using DatabaseObjects;
using Xunit;

namespace MediaRatingsPlatform.Tests;

public class MediaQueryOptionsParserTests
{
    [Fact]
    public void Parse_Defaults_WhenEmpty()
    {
        var q = new NameValueCollection();
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal("title", o.SortBy);
        Assert.False(o.SortDescending);
        Assert.Null(o.TitlePart);
    }

    [Fact]
    public void Parse_Title_UsesPartialMatchParam()
    {
        var q = new NameValueCollection { { "title", "star" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal("star", o.TitlePart);
    }

    [Fact]
    public void Parse_Genre_SetsGenre()
    {
        var q = new NameValueCollection { { "genre", "Action" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal("Action", o.Genre);
    }

    [Fact]
    public void Parse_Type_AcceptsStringMovie()
    {
        var q = new NameValueCollection { { "type", "movie" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal(MediaType.Movie, o.Type);
    }

    [Fact]
    public void Parse_Type_AcceptsNumeric()
    {
        var q = new NameValueCollection { { "type", "2" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal(MediaType.Game, o.Type);
    }

    [Fact]
    public void Parse_Year_SetsReleaseYear()
    {
        var q = new NameValueCollection { { "year", "1999" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal(1999, o.ReleaseYear);
    }

    [Fact]
    public void Parse_YearFromTo_SetsRange()
    {
        var q = new NameValueCollection { { "yearFrom", "2000" }, { "yearTo", "2005" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal(2000, o.ReleaseYearFrom);
        Assert.Equal(2005, o.ReleaseYearTo);
    }

    [Fact]
    public void Parse_AgeRestriction_AcceptsAgeAlias()
    {
        var q = new NameValueCollection { { "age", "16" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal(16, o.AgeRestriction);
    }

    [Fact]
    public void Parse_MinRating_ParsesInvariantDouble()
    {
        var q = new NameValueCollection { { "minRating", "3.5" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal(3.5, o.MinAverageRating);
    }

    [Fact]
    public void Parse_SortByScore_AndDesc()
    {
        var q = new NameValueCollection { { "sortBy", "score" }, { "sortOrder", "desc" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal("score", o.SortBy);
        Assert.True(o.SortDescending);
    }

    [Fact]
    public void Parse_InvalidSortBy_FallsBackToTitle()
    {
        var q = new NameValueCollection { { "sortBy", "hacker" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal("title", o.SortBy);
    }

    [Fact]
    public void Parse_LimitOffset_Validates()
    {
        var q = new NameValueCollection { { "limit", "25" }, { "offset", "0" } };
        var o = MediaRatingsPlatform.MediaQueryOptionsParser.Parse(q);
        Assert.Equal(25, o.Limit);
        Assert.Equal(0, o.Offset);
    }
}
