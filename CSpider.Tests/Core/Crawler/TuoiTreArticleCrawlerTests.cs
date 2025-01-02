using System.Linq;
using CSpider.Core.Crawler;
using CSpider.Core.Interface;
using CSpider.Core.Spider;
using CSpider.Infrastructure.Store;
using CSpider.Tests.Seed;
using System;
using System.Threading.Tasks;
using CSpider.Tests.Fixtures;
using Moq;
using Xunit;

namespace CSpider.Tests.Core.Crawler;

public class TuoiTreArticleCrawlerTests : IClassFixture<ArticleStoreFixture>
{
    private readonly Mock<ITuoiTreSpider> _mockSpider;
    private readonly ArticleStore _articleStore;
    private readonly TuoiTreArticleCrawler _crawler;

    public TuoiTreArticleCrawlerTests(ArticleStoreFixture fixture)
    {
        _articleStore = fixture.Store;
        _mockSpider = new Mock<ITuoiTreSpider>();
        _crawler = new TuoiTreArticleCrawler(_mockSpider.Object, _articleStore);
    }

    [Fact]
    public void CrawlArticle_ShouldProcessAndStoreArticles()
    {
        // Arrange
        var fromDate = DateTime.Now.AddDays(-1);
        var toDate = DateTime.Now;
        var articles = ArticleSeed.CreateArticlesInDateRange(fromDate, toDate, 5).ToList();

        _mockSpider.Setup(s => s.ListArticle)
            .Returns(new ListArticle { Articles = articles });
        _mockSpider.Setup(s => s.CrawlAsync(fromDate, toDate))
            .Returns(Task.CompletedTask);

        // Act
        _crawler.CrawlArticle(fromDate, toDate);

        // Assert
        _mockSpider.Verify(s => s.CrawlAsync(fromDate, toDate), Times.Once);
        foreach (var article in articles)
        {
            var result = _articleStore.FindByUrl(article.Url);
            Assert.Single(result);
            Assert.Equal(article.Title, result[0].Title);
        }
    }

    [Fact]
    public void CrawlArticle_NoArticles()
    {
        // Arrange
        var fromDate = DateTime.Now.AddDays(-1);
        var toDate = DateTime.Now;

        _mockSpider.Setup(s => s.ListArticle)
            .Returns(new ListArticle());
        _mockSpider.Setup(s => s.CrawlAsync(fromDate, toDate))
            .Returns(Task.CompletedTask);

        // Act
        _crawler.CrawlArticle(fromDate, toDate);

        // Assert
        _mockSpider.Verify(s => s.CrawlAsync(fromDate, toDate), Times.Once);
    }

    [Fact]
    public void CrawlArticle_WhenSpiderFails_ShouldNotCrash()
    {
        // Arrange
        var fromDate = DateTime.Now.AddDays(-1);
        var toDate = DateTime.Now;

        _mockSpider.Setup(s => s.CrawlAsync(fromDate, toDate))
            .ThrowsAsync(new Exception("Spider error"));

        // Act & Assert
        var exception = Record.Exception(() => _crawler.CrawlArticle(fromDate, toDate));
        Assert.Null(exception);
    }
}
