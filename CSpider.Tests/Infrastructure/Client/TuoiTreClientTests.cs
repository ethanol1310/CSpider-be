using System;
using System.Net.Http;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Poco;
using CSpider.Config;
using CSpider.Infrastructure.Client;
using Moq;
using Xunit;

namespace CSpider.CSpider.Tests.Infrastructure.Client;

public class TuoiTreClientTests
{
    private readonly Mock<IWebContentExtractor> _mockContentExtractor;
    private readonly TuoiTreClient _client;
    private const string BaseUrl = "https://tuoitre.vn";
    private const string CommentApiUrl = "https://id.tuoitre.vn/api/getlist-comment.api";

    public TuoiTreClientTests()
    {
        _mockContentExtractor = new Mock<IWebContentExtractor>();
        _client = new TuoiTreClient(_mockContentExtractor.Object, new TuoiTreConfig
        {
            BaseUrl = BaseUrl,
            CommentApiUrl = CommentApiUrl,
            HttpClientConfig = new HttpClientConfig
            {
                MaxRetry = 3,
                MinRetryDelayInMilliseconds = 100
            }
        });
    }

    [Fact]
    public async Task GetArticlesByDateAsync_ValidResponse_ReturnsArticlesAndHasNextPage()
    {
        // Arrange
        var dateStr = "01-01-2024";
        var pageNumber = 1;
        var htmlContent = @"
                <html>
                    <body>
                        <li class='news-item'>
                            <a href='/test-article-1' title='Test Article 1'>Test Article 1</a>
                        </li>
                        <li class='news-item'>
                            <a href='/test-article-2' title='Test Article 2'>Test Article 2</a>
                        </li>
                    </body>
                </html>";

        SetupMockResponse(htmlContent);

        // Act
        var (articles, hasNextPage) = await _client.GetArticlesByDateAsync(dateStr, pageNumber);

        // Assert
        Assert.NotNull(articles);
        Assert.Equal(2, articles.Length);
        Assert.True(hasNextPage);
    }

    [Fact]
    public async Task GetArticleAsync_ValidResponse_ReturnsArticleDetails()
    {
        // Arrange
        var url = "https://tuoitre.vn/test-article";
        var title = "Test Article";
        var htmlContent = @"
                <html>
                    <body>
                        <section class='comment-wrapper' 
                                data-objectid='123456' 
                                data-objecttype='1'>
                        </section>
                        <div data-role='publishdate'>01/01/2024 12:00 GMT+7</div>
                    </body>
                </html>";

        SetupMockResponse(htmlContent);

        // Act
        var result = await _client.GetArticleAsync(url, title);

        // Assert
        Assert.NotNull(result);
        var (objectId, objectType, publishedTime) = result.Value;
        Assert.Equal("123456", objectId);
        Assert.Equal("1", objectType);
        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), publishedTime);
    }

    [Fact]
    public async Task GetArticleAsync_InvalidResponse_ReturnsArticleDetails()
    {
        // Arrange
        var url = "https://tuoitre.vn/test-article";
        var title = "Test Article";
        var htmlContent = @"
                <html>
                    <body>
                        <section class='comment-wrapper' 
                                data-objectid='123456' 
                                data-objecttype='1'>
                        </section>
                    </body>
                </html>";

        SetupMockResponse(htmlContent);

        // Act
        var result = await _client.GetArticleAsync(url, title);

        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task GetCommentsAsync_ValidResponse_ReturnsTotalLikes()
    {
        // Arrange
        var objectId = "123456";
        var objectType = "1";
        var url = "https://tuoitre.vn/test-article";
        var jsonResponse = @"{
                'data': '[{""reactions"":{""1"":10,""3"":5}},{""reactions"":{""1"":15,""3"":10}}]'
            }";

        SetupMockResponse(jsonResponse);

        // Act
        var totalLikes = await _client.GetCommentsAsync(objectId, objectType, url);

        // Assert
        Assert.Equal(40, totalLikes);
    }

    [Fact]
    public async Task GetCommentsAsync_EmptyResponse_ReturnsZero()
    {
        // Arrange
        var objectId = "123456";
        var objectType = "1";
        var url = "https://tuoitre.vn/test-article";
        var jsonResponse = @"{ 'data': '[]' }";

        SetupMockResponse(jsonResponse);

        // Act
        var totalLikes = await _client.GetCommentsAsync(objectId, objectType, url);

        // Assert
        Assert.Equal(0, totalLikes);
    }

    private void SetupMockResponse(string content)
    {
        _mockContentExtractor
            .Setup(x => x.GetContentAsync(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(new PageContent { Text = content, Bytes = System.Text.Encoding.UTF8.GetBytes(content) });
    }
}