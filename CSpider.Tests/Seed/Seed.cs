using CSpider.Models;
using System;
using System.Collections.Generic;

namespace CSpider.Tests.Seed;

public static class ArticleSeed
{
    public static Article CreateTestArticle(
        Source source = Source.VnExpress,
        string? title = null,
        string? url = null,
        int? totalCommentLikes = null,
        DateTime? publishTime = null,
        DateTime? createdTime = null)
    {
        return new Article
        {
            Id = Guid.NewGuid().ToString(),
            Source = source,
            Title = title ?? $"Test Article {Guid.NewGuid()}",
            Url = url ?? $"http://test.com/{Guid.NewGuid()}",
            TotalCommentLikes = totalCommentLikes ?? Random.Shared.Next(1, 1000),
            PublishTime = publishTime ?? DateTime.Now.AddDays(-Random.Shared.Next(1, 30)),
            CreatedTime = createdTime ?? DateTime.Now
        };
    }

    public static Article CreateArticleWithSameUrl(Article original, string? newTitle = null, int? newTotalCommentLikes = null)
    {
        return CreateTestArticle(
            source: original.Source,
            title: newTitle ?? original.Title,
            url: original.Url,
            totalCommentLikes: newTotalCommentLikes ?? original.TotalCommentLikes + 10,
            publishTime: original.PublishTime,
            createdTime: DateTime.Now
        );
    }

    public static IEnumerable<Article> CreateArticlesInDateRange(DateTime startDate, DateTime endDate, int count = 5)
    {
        var timeSpan = endDate - startDate;
        var articles = new List<Article>();

        for (int i = 0; i < count; i++)
        {
            var randomDays = Random.Shared.NextDouble() * timeSpan.TotalDays;
            var publishTime = startDate.AddDays(randomDays);

            articles.Add(CreateTestArticle(publishTime: publishTime));
        }

        return articles;
    }
}