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

public class VnExpressArticleCrawlerTests : IClassFixture<ArticleStoreFixture>
{
    private readonly Mock<IVnExpressSpider> _mockSpider;
    private readonly VnExpressArticleCrawler _crawler;

    public VnExpressArticleCrawlerTests()
    {
        _mockSpider = new Mock<IVnExpressSpider>();
        _crawler = new VnExpressArticleCrawler(_mockSpider.Object);
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
