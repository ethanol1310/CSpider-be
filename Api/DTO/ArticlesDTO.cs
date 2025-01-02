using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CSpider.Models;
using Microsoft.AspNetCore.Mvc;

namespace CSpider.Api.DTO;

public class GetArticlesRequest
{
    [FromQuery(Name = "from_date")]
    public DateTime? FromDate { get; set; }

    [FromQuery(Name = "to_date")]
    public DateTime? ToDate { get; set; }

    [FromQuery(Name = "limit")]
    public int? Limit { get; set; } = 10;

    [Required]
    [FromQuery(Name = "source")]
    public Source Source { get; set; }
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

    [JsonPropertyName("source")]
    public required string Source { get; set; }
}