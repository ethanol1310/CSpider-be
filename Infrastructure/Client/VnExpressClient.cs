namespace CSpider.Infrastructure.Client;

using AngleSharp;
using AngleSharp.Dom;
using Abot2.Core;
using Abot2.Poco;
using Bogus;
using Newtonsoft.Json;
using Serilog;
using System.Globalization;

public interface IVnExpressClient
{
    Task<(IHtmlCollection<IElement> Articles, bool HasNextPage)> GetArticlesByCategoryAsync(
        int categoryId,
        long fromDateUnix,
        long toDateUnix,
        int pageNumber);

    Task<(string ObjectId, string ObjectType, DateTime PublishedDate)?> GetArticleAsync(string url, string title);

    Task<int> GetCommentsAsync(
        string objectId,
        string objectType,
        int offset = 0,
        int limit = 1000);
}

public class VnExpressClient : IVnExpressClient
{
    private readonly string _baseUrl;
    private readonly string _commentApiUrl;
    private readonly IBrowsingContext _context;
    private PageRequesterCustom _pageRequester;

    public VnExpressClient(string baseUrl, string commentApiUrl, IWebContentExtractor contentExtractor)
    {
        _baseUrl = baseUrl;
        _commentApiUrl = commentApiUrl;
        _context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
        _pageRequester = new PageRequesterCustom(new CrawlConfiguration(), contentExtractor);
    }


    public async Task<(IHtmlCollection<IElement> Articles, bool HasNextPage)> GetArticlesByCategoryAsync(
        int categoryId,
        long fromDateUnix,
        long toDateUnix,
        int pageNumber)
    {
        var url =
            $"{_baseUrl}/category/day/cateid/{categoryId}/fromdate/{fromDateUnix}/todate/{toDateUnix}/allcate/0/page/{pageNumber}";
        var response = await _pageRequester.MakeRequestAsync(new Uri(url));

        if (!response.HttpResponseMessage.IsSuccessStatusCode)
        {
            Log.Warning(
                $"Failed to get articles for category {categoryId}, page {pageNumber}. Status code: {response.HttpResponseMessage.StatusCode}");
            return (null, false);
        }

        var document = await _context.OpenAsync(req => req.Content(response.Content.Text));
        var articles = document.QuerySelectorAll("article.item-news.item-news-common");

        return (articles, articles.Length > 0);
    }

    public async Task<(string ObjectId, string ObjectType, DateTime PublishedDate)?> GetArticleAsync(string url,
        string title)
    {
        var response = await _pageRequester.MakeRequestAsync(new Uri(url));
        if (!response.HttpResponseMessage.IsSuccessStatusCode)
        {
            Log.Warning("Failed to get article {Title} at {Url}. Status code: {StatusCode}", title, url,
                response.HttpResponseMessage.StatusCode);
        }
        
        var document = await _context.OpenAsync(req => req.Content(response.Content.Text));

        var commentSection = document.QuerySelector("span.number_cmt.txt_num_comment.num_cmt_detail");
        var publishedDateData = document.QuerySelector("meta[name='pubdate']");

        if (commentSection == null)
        {
            Log.Warning("No comment section found for {Url}", url);
            return null;
        }

        var objectId = commentSection.GetAttribute("data-objectid");
        var objectType = commentSection.GetAttribute("data-objecttype");

        if (string.IsNullOrEmpty(objectId) || string.IsNullOrEmpty(objectType) ||
            !DateTime.TryParseExact(publishedDateData?.GetAttribute("content")?.Trim(), "yyyy-MM-ddTHH:mm:sszzz",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime publishedDate))
        {
            return null;
        }

        return (objectId, objectType, publishedDate);
    }

    public async Task<int> GetCommentsAsync(string objectId, string objectType, int offset = 0, int limit = 1000)
    {
        var apiUrl =
            $"{_commentApiUrl}?offset={offset}&limit={limit}&sort_by=like&objectid={objectId}&objecttype={objectType}&siteid=1000000";

        var response = await _pageRequester.MakeRequestAsync(new Uri(apiUrl));

        if (string.IsNullOrEmpty(response.Content.Text))
        {
            Log.Error("Empty response from VnExpress API for objectId {ObjectId}", objectId);
            return 0;
        }

        var commentData = JsonConvert.DeserializeObject<CommentResponse>(response.Content.Text);
        if (commentData?.Data?.Items == null || commentData.Data.Items.Count == 0)
        {
            return 0;
        }

        return commentData.Data.Items.Sum(comment => comment.UserLike);
    }

    private class CommentResponse
    {
        public CommentData Data { get; set; }
    }

    private class CommentData
    {
        [JsonProperty("items")] public List<Comment> Items { get; set; }
    }

    private class Comment
    {
        [JsonProperty("userlike")] public int UserLike { get; set; }
    }
}