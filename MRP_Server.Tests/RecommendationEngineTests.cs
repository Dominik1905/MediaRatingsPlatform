using System.Collections.Generic;
using DatabaseObjects;
using Xunit;

namespace MediaRatingsPlatform.Tests;

public class RecommendationEngineTests
{
    private static MediaWithScore M(int id, string title, string genre, MediaType type, int age, double avg)
        => new MediaWithScore
        {
            Media = new Media
            {
                Id = id,
                Title = title,
                Genre = genre,
                Type = type,
                AgeRestriction = age,
                Description = "",
                ReleaseYear = 2000,
                CreatedByUserId = 1
            },
            AvgScore = avg
        };

    [Fact]
    public void Recommend_Fallback_WhenNoHighRated_RecommendsByAvgScore()
    {
        var all = new[]
        {
            M(1,"A","Action",MediaType.Movie,16,4.9),
            M(2,"B","Drama",MediaType.Movie,16,3.0),
            M(3,"C","Action",MediaType.Game,18,4.0)
        };
        var userRatings = new[] { new Rating { MediaId = 99, Stars = 2 } };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 2);
        Assert.Equal(2, rec.Count);
        Assert.Equal(1, rec[0].Media.Id);
        Assert.Equal(3, rec[1].Media.Id);
    }

    [Fact]
    public void Recommend_ExcludesAlreadyRated()
    {
        var all = new[] { M(1,"A","Action",MediaType.Movie,16,4.0), M(2,"B","Action",MediaType.Movie,16,4.0) };
        var userRatings = new[] { new Rating { MediaId = 1, Stars = 5 } };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 10);
        Assert.Single(rec);
        Assert.Equal(2, rec[0].Media.Id);
    }

    [Fact]
    public void Recommend_ExcludesFavorites()
    {
        var all = new[] { M(1,"A","Action",MediaType.Movie,16,4.0), M(2,"B","Action",MediaType.Movie,16,4.0) };
        var userRatings = new[] { new Rating { MediaId = 1, Stars = 5 } };
        var fav = new HashSet<int> { 2 };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, fav, limit: 10);
        Assert.Empty(rec);
    }

    [Fact]
    public void Recommend_PrefersSameGenre_FromHighRated()
    {
        var all = new[]
        {
            M(1,"Loved","Action",MediaType.Movie,16,3.0),
            M(2,"Candidate1","Action",MediaType.Movie,16,2.0),
            M(3,"Candidate2","Drama",MediaType.Movie,16,4.8)
        };
        var userRatings = new[] { new Rating { MediaId = 1, Stars = 5 } };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 2);
        Assert.Equal(2, rec[0].Media.Id); // genre bonus beats higher avg score
    }

    [Fact]
    public void Recommend_PrefersSameType_FromHighRated()
    {
        var all = new[]
        {
            M(1,"Loved","Action",MediaType.Game,18,3.0),
            M(2,"Candidate1","Drama",MediaType.Game,18,2.0),
            M(3,"Candidate2","Drama",MediaType.Movie,18,4.8)
        };
        var userRatings = new[] { new Rating { MediaId = 1, Stars = 5 } };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 1);
        Assert.Equal(2, rec[0].Media.Id);
    }

    [Fact]
    public void Recommend_PrefersSameAgeRestriction_Mode()
    {
        var all = new[]
        {
            M(1,"Loved1","Action",MediaType.Movie,16,3.0),
            M(2,"Loved2","Action",MediaType.Movie,16,3.0),
            M(3,"CandidateAge16","Drama",MediaType.Movie,16,3.0),
            M(4,"CandidateAge18","Drama",MediaType.Movie,18,3.0)
        };
        var userRatings = new[]
        {
            new Rating { MediaId = 1, Stars = 5 },
            new Rating { MediaId = 2, Stars = 5 }
        };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 2);
        Assert.Equal(3, rec[0].Media.Id);
    }

    [Fact]
    public void Recommend_RespectsLimit()
    {
        var all = new[]
        {
            M(1,"Loved","Action",MediaType.Movie,16,3.0),
            M(2,"C1","Action",MediaType.Movie,16,3.0),
            M(3,"C2","Action",MediaType.Movie,16,3.0),
            M(4,"C3","Action",MediaType.Movie,16,3.0)
        };
        var userRatings = new[] { new Rating { MediaId = 1, Stars = 5 } };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 2);
        Assert.Equal(2, rec.Count);
    }

    [Fact]
    public void Recommend_SetsRecommendationScore()
    {
        var all = new[] { M(1,"Loved","Action",MediaType.Movie,16,3.0), M(2,"C","Action",MediaType.Movie,16,3.0) };
        var userRatings = new[] { new Rating { MediaId = 1, Stars = 5 } };
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 1);
        Assert.True(rec[0].RecommendationScore > 0);
    }

    [Fact]
    public void Recommend_IgnoresLowRatedForPreferences()
    {
        var all = new[]
        {
            M(1,"Low","Horror",MediaType.Movie,18,1.0),
            M(2,"CandidateHorror","Horror",MediaType.Movie,18,2.0),
            M(3,"CandidateDrama","Drama",MediaType.Movie,18,2.0)
        };
        var userRatings = new[] { new Rating { MediaId = 1, Stars = 1 } }; 
        var rec = MediaRatingsPlatform.RecommendationEngine.Recommend(all, userRatings, new HashSet<int>(), limit: 1);
        Assert.Equal(3, rec[0].Media.Id); // fallback by avg score then title
    }
}
