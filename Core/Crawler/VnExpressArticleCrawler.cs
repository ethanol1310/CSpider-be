using CSpider.Core.Interface;
using CSpider.Interface;
using CSpider.Core.Spider;
using CSpider.Infrastructure.Store;
using CSpider.Models;
using Serilog;

namespace CSpider.Core.Crawler;

public class VnExpressArticleCrawler : IArticleCrawler
{
    private readonly IVnExpressSpider _spider;
    private readonly ArticleStore _articleStore;

    public VnExpressArticleCrawler(
        IVnExpressSpider spider,
        ArticleStore articleStore)
    {
        _spider = spider;
        _articleStore = articleStore;
    }

    public void CrawlArticle(DateTime fromDate, DateTime toDate)
    {
        Log.Information("Crawling VnExpress articles from {fromDate} to {toDate}", fromDate, toDate);

        try
        {
            _spider.CrawlAsync(fromDate, toDate).Wait();
            var now = DateTime.Now;
            var articles = _spider.ListArticle.Articles.Select(article =>
            {
                article.Source = Source.VnExpress;
                article.CreatedTime = now;
                return article;
            }).ToList();

            _articleStore.UpsertBatch(articles);

            Log.Information(
                "Finished crawling VnExpress articles from {fromDate} to {toDate}. Stored {count} articles in batch.",
                fromDate, toDate, articles.Count);
        }
        catch (Exception e)
        {
            Log.Error($"Error crawling VnExpress articles from {fromDate} to {toDate}: {e.Message}");
        }
    }
}