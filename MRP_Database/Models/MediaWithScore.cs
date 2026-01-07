using System.Text.Json.Serialization;

namespace DatabaseObjects;

public sealed class MediaWithScore
{
    public Media Media { get; set; } = new();

    public double AvgScore { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double RecommendationScore { get; set; }
}
