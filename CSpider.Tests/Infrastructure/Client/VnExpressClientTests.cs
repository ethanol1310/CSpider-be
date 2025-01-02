using System;
using System.Net.Http;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Poco;
using CSpider.Infrastructure.Client;
using Moq;
using Serilog;
using Xunit;

namespace CSpider.CSpider.Tests.Infrastructure.Client;

public class VnExpressClientTests
{
    private readonly Mock<IWebContentExtractor> _mockContentExtractor;
    private readonly VnExpressClient _client;
    private const string BaseUrl = "https://vnexpress.net";
    private const string CommentApiUrl = "https://usi-saas.vnexpress.net/index/get";

    public VnExpressClientTests()
    {
        _mockContentExtractor = new Mock<IWebContentExtractor>();
        _client = new VnExpressClient(BaseUrl, CommentApiUrl, _mockContentExtractor.Object);
    }

    [Fact]
    public async Task GetArticlesByCategoryAsync_ValidResponse_ReturnsArticlesAndHasNextPage()
    {
        // Arrange
        var categoryId = 1001005;
        var fromDateUnix = ((DateTimeOffset)DateTime.Now.AddDays(-7)).ToUnixTimeSeconds();
        var toDateUnix = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        var pageNumber = 1;

        var htmlContent = @"
                <html>
                    <body>
                        <article class='item-news item-news-common'>
                            <h3 class='title-news'>
                                <a href='/test-article-1' title='Test Article 1'>Test Article 1</a>
                            </h3>
                        </article>
                        <article class='item-news item-news-common'>
                            <h3 class='title-news'>
                                <a href='/test-article-2' title='Test Article 2'>Test Article 2</a>
                            </h3>
                        </article>
                    </body>
                </html>";

        SetupMockResponse(htmlContent);

        // Act
        var (articles, hasNextPage) = await _client.GetArticlesByCategoryAsync(
            categoryId, fromDateUnix, toDateUnix, pageNumber);

        // Assert
        Assert.NotNull(articles);
        Assert.Equal(2, articles.Length);
        Assert.True(hasNextPage);
    }

    [Fact]
    public async Task GetArticlesByCategoryAsync_EmptyResponse_ReturnsNullAndNoNextPage()
    {
        // Arrange
        var categoryId = 1001005;
        var fromDateUnix = ((DateTimeOffset)DateTime.Now.AddDays(-7)).ToUnixTimeSeconds();
        var toDateUnix = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        var pageNumber = 1;

        var htmlContent = "<html><body></body></html>";
        SetupMockResponse(htmlContent);

        // Act
        var (articles, hasNextPage) = await _client.GetArticlesByCategoryAsync(
            categoryId, fromDateUnix, toDateUnix, pageNumber);

        // Assert
        Assert.NotNull(articles);
        Assert.Empty(articles);
        Assert.False(hasNextPage);
    }

    [Fact]
    public async Task GetArticleAsync_ValidResponse_ReturnsArticleDetails()
    {
        // Arrange
        var url = "https://vnexpress.net/test-article";
        var title = "Test Article";
        var htmlContent = @"
                <html>
                    <body>
                        <span class='number_cmt txt_num_comment num_cmt_detail' 
                              data-objectid='123456' 
                              data-objecttype='1'>
                        </span>
                        <meta name='pubdate' content='2024-01-01T12:00:00+07:00'>
                    </body>
                </html>";

        SetupMockResponse(htmlContent);

        // Act
        var result = await _client.GetArticleAsync(url, title);

        // Assert
        Assert.NotNull(result);
        var (objectId, objectType, publishedDate) = result.Value;
        Assert.Equal("123456", objectId);
        Assert.Equal("1", objectType);
        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), publishedDate);
    }

    [Fact]
    public async Task GetArticleAsync_InvalidResponse_ReturnsArticleDetails()
    {
        // Arrange
        var url = "https://vnexpress.net/test-article";
        var title = "Test Article";
        var htmlContent = @"
                <html>
                    <body>
                        <span class='number_cmt txt_num_comment num_cmt_detail' 
                              data-objecttype='1'>
                        </span>
                        <meta name='pubdate' content='2024-01-01T12:00:00+07:00'>
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
        var jsonResponse = @"{
          ""error"": 0,
          ""errorDescription"": """",
          ""iscomment"": 1,
          ""data"": {
            ""total"": 19,
            ""totalitem"": 32,
            ""items"": [
              {
                ""comment_id"": 58245554,
                ""parent_id"": 58245554,
                ""article_id"": 4832065,
                ""content"": ""Mô hình sản xuất và kinh doanh tốt quá. Chúc doanh nghiệp làm ăn bền vững, phát đạt, đóng góp ổn định cho đất nước, giữ vững và tạo thêm công việc chất lượng cho người dân."",
                ""full_name"": ""cần kiệm liêm chính"",
                ""userlike"": 421,
                ""t_r_1"": 420,
                ""t_r_2"": 1,
                ""t_r_3"": 0,
                ""t_r_4"": 0,
                ""userid"": 1076709892,
              },
              {
                ""comment_id"": 58245531,
                ""parent_id"": 58245531,
                ""article_id"": 4832065,
                ""content"": ""Thật tuyệt vời, mang lại công việc ổn định cho nhiều gia đình lao đông."",
                ""full_name"": ""Dầu Nhớt Quận 12"",
                ""creation_time"": 1735441456,
                ""time"": ""6h trước"",
                ""userlike"": 257,
                ""t_r_1"": 257,
                ""t_r_2"": 0,
                ""t_r_3"": 0,
                ""t_r_4"": 0,
                ""userid"": 1061179726,
              }
            ],
            ""items_pin"": [],
            ""offset"": 0
          }
        }";

        SetupMockResponse(jsonResponse);

        // Act
        var totalLikes = await _client.GetCommentsAsync(objectId, objectType);

        // Assert
        Assert.Equal(678, totalLikes);
    }

    private void SetupMockResponse(string content)
    {
        _mockContentExtractor
            .Setup(x => x.GetContentAsync(It.IsAny<HttpResponseMessage>()))
            .ReturnsAsync(new PageContent { Text = content, Bytes = System.Text.Encoding.UTF8.GetBytes(content) });
    }
}