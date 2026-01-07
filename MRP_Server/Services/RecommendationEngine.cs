using System;
using System.Collections.Generic;
using System.Linq;
using DatabaseObjects;

namespace MediaRatingsPlatform;

public static class RecommendationEngine
{
    public static List<MediaWithScore> Recommend(
        IReadOnlyList<MediaWithScore> allMedia,
        IReadOnlyList<Rating> userRatings,
        IReadOnlyCollection<int> favoriteMediaIds,
        int limit = 10,
        int highRatingThreshold = 4)
    {
        if (limit <= 0) limit = 10;

        var ratedIds = new HashSet<int>(userRatings.Select(r => r.MediaId));
        foreach (var fav in favoriteMediaIds)
            ratedIds.Add(fav); // don't recommend already-favorited items

        // Determine "highly rated" media for preference extraction
        var highRated = userRatings.Where(r => r.Stars >= highRatingThreshold).ToList();

        // Fallback: no preference -> recommend by global avg score
        if (highRated.Count == 0)
        {
            return allMedia
                .Where(x => !ratedIds.Contains(x.Media.Id))
                .OrderByDescending(x => x.AvgScore)
                .ThenBy(x => x.Media.Title)
                .Take(limit)
                .Select(x => new MediaWithScore
                {
                    Media = x.Media,
                    AvgScore = x.AvgScore,
                    RecommendationScore = x.AvgScore
                })
                .ToList();
        }

        // Build preference weights
        var genreWeights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var typeWeights = new Dictionary<MediaType, int>();
        var ageWeights = new Dictionary<int, int>();

        foreach (var r in highRated)
        {
            var media = allMedia.FirstOrDefault(m => m.Media.Id == r.MediaId)?.Media;
            if (media == null) continue;

            if (!string.IsNullOrWhiteSpace(media.Genre))
                genreWeights[media.Genre] = genreWeights.GetValueOrDefault(media.Genre) + 1;

            typeWeights[media.Type] = typeWeights.GetValueOrDefault(media.Type) + 1;
            ageWeights[media.AgeRestriction] = ageWeights.GetValueOrDefault(media.AgeRestriction) + 1;
        }

        int preferredAge = ageWeights.Count == 0 ? 0 : ageWeights.OrderByDescending(kv => kv.Value).First().Key;

        // Score all candidates
        var scored = new List<MediaWithScore>();
        foreach (var item in allMedia)
        {
            if (ratedIds.Contains(item.Media.Id)) continue;

            double score = 0;

            // Genre similarity (strong signal)
            if (!string.IsNullOrWhiteSpace(item.Media.Genre) && genreWeights.TryGetValue(item.Media.Genre, out var gw))
                score += gw * 3.0;

            // Type similarity
            if (typeWeights.TryGetValue(item.Media.Type, out var tw))
                score += tw * 1.5;

            // Age restriction similarity
            if (preferredAge != 0 && item.Media.AgeRestriction == preferredAge)
                score += 1.0;

            // Global quality signal
            score += item.AvgScore * 0.75; // 0..3.75

            scored.Add(new MediaWithScore
            {
                Media = item.Media,
                AvgScore = item.AvgScore,
                RecommendationScore = score
            });
        }

        return scored
            .OrderByDescending(x => x.RecommendationScore)
            .ThenByDescending(x => x.AvgScore)
            .ThenBy(x => x.Media.Title)
            .Take(limit)
            .ToList();
    }
}
