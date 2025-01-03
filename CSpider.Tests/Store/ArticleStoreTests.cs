using System;
using System.Linq;
using CSpider.Models;
using CSpider.Tests.Fixtures;
using CSpider.Tests.Seed;
using Xunit;

namespace CSpider.Tests.Store;

public class ArticleStoreTests : IClassFixture<ArticleStoreFixture>
{
    private readonly ArticleStoreFixture _fixture;

    public ArticleStoreTests(ArticleStoreFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void UpsertBatch_WithNewArticles_ShouldInsertAll()
    {
        // Arrange
        var articles = new[]
        {
            ArticleSeed.CreateTestArticle(title: "Article 1", url: "http://test.com/1"),
            ArticleSeed.CreateTestArticle(title: "Article 2", url: "http://test.com/2")
        };

        // Act
        _fixture.Store.UpsertBatch(articles);

        // Assert
        foreach (var article in articles)
        {
            var result = _fixture.Store.FindByUrl(article.Url);
            Assert.Single(result);
            Assert.Equal(article.Title, result[0].Title);
        }
    }

    [Fact]
    public void UpsertBatch_WithDuplicateUrls_ShouldUpdate()
    {
        // Arrange
        var originalArticle = ArticleSeed.CreateTestArticle(
            title: $"{Guid.NewGuid()}",
            totalCommentLikes: 10,
            source: Source.VnExpress
        );
        var updatedArticle = ArticleSeed.CreateArticleWithSameUrl(
            originalArticle,
            newTitle: $"{Guid.NewGuid()}",
            newTotalCommentLikes: 20
        );

        // Act
        updatedArticle.Id = originalArticle.Id;
        _fixture.Store.UpsertBatch(new[] { originalArticle });
        _fixture.Store.UpsertBatch(new[] { updatedArticle });

        // Assert
        var result = _fixture.Store.FindByUrl(originalArticle.Url);
        Assert.Single(result);
        Assert.Equal(updatedArticle.Title, result[0].Title);
        Assert.Equal(20, result[0].TotalCommentLikes);
        Assert.Equal(originalArticle.Url, result[0].Url);
    }

    [Fact]
    public void FindByDateRange_WithValidRange_ShouldReturnMatchingArticles()
    {
        // Arrange
        var fromDate = DateTime.Now.AddDays(-10000);
        var toDate = DateTime.Now.AddDays(-9000);
        var articles = ArticleSeed.CreateArticlesInDateRange(fromDate, toDate, 5).ToList();
        var outOfRangeArticle = ArticleSeed.CreateTestArticle(
            publishTime: DateTime.Now
        );

        _fixture.Store.UpsertBatch(articles.Concat(new[] { outOfRangeArticle }));

        // Act
        var result = _fixture.Store.FindByPublishTimeRange(fromDate, toDate);

        // Assert
        Assert.Equal(5, result.Count);
        Assert.All(result, article =>
        {
            Assert.True(article.PublishTime >= fromDate);
            Assert.True(article.PublishTime <= toDate);
        });
        Assert.DoesNotContain(result, a => a.Url == outOfRangeArticle.Url);
        foreach (var article in articles)
        {
            var foundByUrl = _fixture.Store.FindByUrl(article.Url);
            Assert.Single(foundByUrl);
            Assert.Equal(article.Title, foundByUrl[0].Title);
        }
    }
}