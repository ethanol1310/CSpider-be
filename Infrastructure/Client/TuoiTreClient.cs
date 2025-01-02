using System.Threading.RateLimiting;
using CSpider.Config;

namespace CSpider.Infrastructure.Client;

using AngleSharp;
using AngleSharp.Dom;
using Abot2.Core;
using Abot2.Poco;
using Bogus;
using Newtonsoft.Json;
using Serilog;
using System.Globalization;

public interface ITuoiTreClient
{
    Task<(IHtmlCollection<IElement> Articles, bool HasNextPage)> GetArticlesByDateAsync(
        string dateStr,
        int pageNumber);

    Task<(string ObjectId, string ObjectType, DateTime PublishedTime)?> GetArticleAsync(string url, string title);

    Task<int> GetCommentsAsync(
        string objectId,
        string objectType,
        string url,
        int page = 1);
}

public class TuoiTreClient : ITuoiTreClient
{
    private readonly string _baseUrl;
    private readonly string _commentApiUrl;
    private readonly IBrowsingContext _context;
    private PageRequesterCustom _pageRequester;

    public TuoiTreClient(IWebContentExtractor contentExtractor, TuoiTreConfig config)
    {
        _baseUrl = config.BaseUrl;
        _commentApiUrl = config.CommentApiUrl;
        _context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
        _pageRequester = new PageRequesterCustom(new CrawlConfiguration
        {
            MaxRetryCount = config.HttpClientConfig.MaxRetry,
            MinRetryDelayInMilliseconds = config.HttpClientConfig.MinRetryDelayInMilliseconds,
            HttpRequestTimeoutInSeconds = config.HttpClientConfig.HttpRequestTimeoutInSeconds,
        }, contentExtractor);
    }


    public async Task<(IHtmlCollection<IElement> Articles, bool HasNextPage)> GetArticlesByDateAsync(
        string dateStr,
        int pageNumber)
    {
        var url = $"{_baseUrl}/timeline-xem-theo-ngay/0/{dateStr}/trang-{pageNumber}.htm";

        var response = await _pageRequester.MakeRequestAsync(new Uri(url));
        if (!response.HttpResponseMessage.IsSuccessStatusCode)
        {
            Log.Warning(
                $"Failed to get articles for date {dateStr}, page {pageNumber}. Status code: {response.HttpResponseMessage.StatusCode}");
            return (null, false);
        }

        var document = await _context.OpenAsync(req => req.Content(response.Content.Text));
        var articles = document.QuerySelectorAll("li.news-item");

        return (articles, articles.Length > 0);
    }

    public async Task<(string ObjectId, string ObjectType, DateTime PublishedTime)?> GetArticleAsync(string url,
        string title)
    {
        var response = await _pageRequester.MakeRequestAsync(new Uri(url));
        if (!response.HttpResponseMessage.IsSuccessStatusCode)
        {
            Log.Warning(
                $"Failed to get article for url {url}. Status code: {response.HttpResponseMessage.StatusCode}");
            return null;
        }

        var document = await _context.OpenAsync(req => req.Content(response.Content.Text));
        var commentSection = document.QuerySelector("section.comment-wrapper");
        var publishedDateData = document.QuerySelector("div[data-role='publishdate']");

        if (commentSection == null)
        {
            Log.Warning("No comment section found for {Url}", url);
            return null;
        }

        var objectId = commentSection.GetAttribute("data-objectid");
        var objectType = commentSection.GetAttribute("data-objecttype");
        if (string.IsNullOrEmpty(objectId) || string.IsNullOrEmpty(objectType) ||
            !DateTime.TryParseExact(publishedDateData?.TextContent.Trim(), "dd/MM/yyyy HH:mm 'GMT'z",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime publishedDate))
        {
            return null;
        }

        return (objectId, objectType, publishedDate);
    }

    public async Task<int> GetCommentsAsync(string objectId, string objectType, string url, int page = 1)
    {
        var apiUrl = $"{_commentApiUrl}?pageindex={page}&objId={objectId}&objType={objectType}&sort=2";

        var response = await _pageRequester.MakeRequestAsync(new Uri(apiUrl));
        if (!response.HttpResponseMessage.IsSuccessStatusCode)
        {
            Log.Warning(
                $"Failed to get comments for url {url}, page {page}. Status code: {response.HttpResponseMessage.StatusCode}");
            return 0;
        }

        var commentData = JsonConvert.DeserializeObject<CommentResponse>(response.Content.Text);
        if (commentData?.Data == null)
        {
            Log.Warning("No comment data found for {Url}", url);
            return 0;
        }

        var comments = JsonConvert.DeserializeObject<List<Comment>>(commentData.Data);
        if (comments == null || comments.Count == 0)
        {
            return 0;
        }

        return comments.Sum(c => c.Reactions?.Values.Sum() ?? 0);
    }

    private class CommentResponse
    {
        public string Data { get; set; }
    }

    private class Comment
    {
        public Dictionary<string, int> Reactions { get; set; }
    }
}