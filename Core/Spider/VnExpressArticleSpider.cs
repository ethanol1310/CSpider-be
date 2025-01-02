using CSpider.Interface;

namespace CSpider.Core.Spider;
using Models;
using Serilog;
using System.Diagnostics;
using Infrastructure.Client;
using AngleSharp.Dom;
using Config;
using Interface;
using Microsoft.Extensions.Options;

public class VnExpressArticleSpider : IVnExpressSpider
{
    private readonly SemaphoreSlim _categorySemaphore;
    private readonly SemaphoreSlim _pageSemaphore;
    private readonly int _maxConcurrentArticles;
    private readonly IVnExpressClient _vnExpressClient;
    
    public ListArticle ListArticle { get; }
    
    public VnExpressArticleSpider(
        IVnExpressClient vnExpressClient,
        IOptions<Config> config)
    {
        var cfg = config.Value.VnExpressConfig;
        _vnExpressClient = vnExpressClient;
        _categorySemaphore = new SemaphoreSlim(cfg.MaxConcurrentCategories);
        _pageSemaphore = new SemaphoreSlim(cfg.MaxConcurrentPages);
        _maxConcurrentArticles = cfg.MaxConcurrentArticles;
        ListArticle = new ListArticle();
    }

    public async Task CrawlAsync(DateTime fromDate, DateTime toDate)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var categories = GetCategories();
            var categoryTasks = categories.Select(category => 
                ProcessCategoryWithSemaphoreAsync(category, fromDate, toDate));
            await Task.WhenAll(categoryTasks);
            
            stopwatch.Stop();
            Log.Information(
                "VnExpress crawling completed: Processed {ArticleCount} articles in {ElapsedTime:hh\\:mm\\:ss}",
                ListArticle.Articles.Count,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in main crawl process");
            throw;
        }
    }

    private async Task ProcessCategoryWithSemaphoreAsync(VnExpressCategory category, DateTime fromDate, DateTime toDate)
    {
        await _categorySemaphore.WaitAsync();
        try
        {
            await ProcessCategoryAsync(category, fromDate, toDate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error processing category {category.Id}");
        }
        finally
        {
            _categorySemaphore.Release();
        }
    }

    private async Task ProcessCategoryAsync(VnExpressCategory category, DateTime fromDate, DateTime toDate)
    {
        var fromDateUnix = ((DateTimeOffset)fromDate).ToUnixTimeSeconds();
        var toDateUnix = ((DateTimeOffset)toDate).ToUnixTimeSeconds();
        var pageProcessingTasks = new List<Task>();
        var pageNumber = 1;

        while (true)
        {
            await _pageSemaphore.WaitAsync();
            try
            {
                var (articles, hasNextPage) = await _vnExpressClient.GetArticlesByCategoryAsync(
                    category.Id,
                    fromDateUnix,
                    toDateUnix,
                    pageNumber);

                if (articles != null && articles.Length > 0)
                {
                    pageProcessingTasks.Add(ProcessArticlesInParallelAsync(articles, category.Id));
                }

                if (!hasNextPage) break;
                pageNumber++;
            }
            finally
            {
                _pageSemaphore.Release();
            }
        }

        await Task.WhenAll(pageProcessingTasks);
    }

    private async Task ProcessArticlesInParallelAsync(IHtmlCollection<IElement> articles, int categoryId)
    {
        await Parallel.ForEachAsync(
            articles,
            new ParallelOptions { MaxDegreeOfParallelism = _maxConcurrentArticles },
            async (article, ct) =>
            {
                var linkElement = article.QuerySelector("a");
                var articleUrl = linkElement?.GetAttribute("href");
                var title = linkElement?.GetAttribute("title");

                if (string.IsNullOrEmpty(articleUrl) || string.IsNullOrEmpty(title)) return;

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await ProcessArticle(articleUrl, title);
                    Log.Debug($"Processed article '{title}' in category {categoryId} in {stopwatch.Elapsed.TotalMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing article {title} in category {categoryId}");
                }
            });
    }

    private async Task ProcessArticle(string url, string title)
    {
        try
        {
            var articleDetails = await _vnExpressClient.GetArticleAsync(url, title);
            if (!articleDetails.HasValue)
            {
                return;
            }

            var (objectId, objectType, publishedDate) = articleDetails.Value;
            var totalLikes = await ProcessComments(objectId, objectType, title, url);
            AddArticle(new Article
            {
                Title = title,
                Url = url,
                TotalCommentLikes = totalLikes,
                PublishTime = publishedDate,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error processing article {Url}: {Title}", url, title);
        }
    }

    private async Task<int> ProcessComments(string objectId, string objectType, string title, string url, 
        int offset = 0, int limit = 1000, int totalCommentLikes = 0)
    {
        try
        {
            var currentPageLikes = await _vnExpressClient.GetCommentsAsync(objectId, objectType, offset, limit);

            if (currentPageLikes > 0)
            {
                return await ProcessComments(objectId, objectType, title, url, offset + limit, limit + 1000,
                    totalCommentLikes + currentPageLikes);
            }

            return totalCommentLikes + currentPageLikes;
        } 
        catch (Exception e)
        {
            Log.Error(e, "Error processing comments for article {Url}, page {Page}", url);
            return totalCommentLikes; 
        }
    }

    private void AddArticle(Article article)
    {
        if (ListArticle.Articles.Count != 0 && ListArticle.Articles.Count % 100 == 0)
        {
            Log.Information("VnExpress: Added {Count} articles", ListArticle.Articles.Count);
        }
        Log.Debug($"Added article: {article.TotalCommentLikes} - {article.Title} - {article.Url}");
        ListArticle.AddArticle(article);
    }

    private List<VnExpressCategory> GetCategories()
    {
        return new List<VnExpressCategory>
        {
            new VnExpressCategory { Name = "Thời sự", Id = 1001005, ShareUrl = "/thoi-su" },
            new VnExpressCategory
                { Name = "Góc nhìn", Id = 1003450, ShareUrl = "/goc-nhin" },
            new VnExpressCategory
                { Name = "Thế giới", Id = 1001002, ShareUrl = "/the-gioi" },
            new VnExpressCategory
                { Name = "Kinh doanh", Id = 1003159, ShareUrl = "/kinh-doanh" },
            new VnExpressCategory
                { Name = "Podcasts", Id = 1004685, ShareUrl = "/podcast" },
            new VnExpressCategory
                { Name = "Bất động sản", Id = 1005628, ShareUrl = "/bat-dong-san" },
            new VnExpressCategory
                { Name = "Khoa học", Id = 1001009, ShareUrl = "/khoa-hoc" },
            new VnExpressCategory
                { Name = "Giải trí", Id = 1002691, ShareUrl = "/giai-tri" },
            new VnExpressCategory
                { Name = "Thể thao", Id = 1002565, ShareUrl = "/the-thao" },
            new VnExpressCategory
                { Name = "Pháp luật", Id = 1001007, ShareUrl = "/phap-luat" },
            new VnExpressCategory
                { Name = "Giáo dục", Id = 1003497, ShareUrl = "/giao-duc" },
            new VnExpressCategory
                { Name = "Sức khỏe", Id = 1003750, ShareUrl = "/suc-khoe" },
            new VnExpressCategory
                { Name = "Đời sống", Id = 1002966, ShareUrl = "/doi-song" },
            new VnExpressCategory { Name = "Du lịch", Id = 1003231, ShareUrl = "/du-lich" },
            new VnExpressCategory { Name = "Số hóa", Id = 1002592, ShareUrl = "/so-hoa" },
            new VnExpressCategory { Name = "Xe", Id = 1001006, ShareUrl = "/oto-xe-may" },
            new VnExpressCategory { Name = "Ý kiến", Id = 1001012, ShareUrl = "/y-kien" },
            new VnExpressCategory { Name = "Tâm sự", Id = 1001014, ShareUrl = "/tam-su" },
            new VnExpressCategory { Name = "Thư giãn", Id = 1001011, ShareUrl = "/thu-gian" },
        };
    }
}
