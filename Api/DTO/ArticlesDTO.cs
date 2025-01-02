using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CSpider.Api.DTO;

public class GetArticlesRequest
{   
    [Range(typeof(DateTime), "2024-01-01", "2100-12-31")]
    [JsonPropertyName("from_date")]
    public DateTime? FromDate { get; set; }
    
    [Range(typeof(DateTime), "2024-01-01", "2100-12-31")]
    [JsonPropertyName("to_date")]
    public DateTime? ToDate { get; set; }
    
    [Range(1, 100)]
    [JsonPropertyName("limit")]
    public int? NTop { get; set; } = 10;
}

public class GetArticlesResponse
{
    [JsonPropertyName("articles")]
    public required List<ArticleDto> Articles { get; set; }
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class ArticleDto
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    
    [JsonPropertyName("url")]
    public required string Url { get; set; }
    
    [JsonPropertyName("total_comment_likes")]
    public required int TotalCommentLikes { get; set; }
    
    [JsonPropertyName("publish_time")]
    public required DateTime PublishTime { get; set; }
}