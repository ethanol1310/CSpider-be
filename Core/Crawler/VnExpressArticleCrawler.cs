using System;
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
        Log.Information("Start Crawling VnExpress articles from {fromDate} to {toDate}", fromDate, toDate);

        try
        {
            var currentDate = toDate;
            while (currentDate > fromDate)
            {
                var chunkStartDate = currentDate.AddHours(-2) < fromDate ? fromDate : currentDate.AddHours(-2);
                Log.Information("Crawling VnExpress articles from {chunkStartDate} to {currentDate}", chunkStartDate, currentDate);
        
                _spider.CrawlAsync(chunkStartDate, currentDate).Wait();
                var now = DateTime.Now;
                var articles = _spider.ListArticle.Articles.Select(article =>
                {
                    article.Source = Source.VnExpress;
                    article.CreatedTime = now;
                    return article;
                });

                _articleStore.UpsertBatch(articles);

                Log.Information(
                    "Finished chunk: {chunkStartDate} to {currentDate}. Stored {count} articles.",
                    chunkStartDate, currentDate, _spider.ListArticle.Articles.Count);

                currentDate = chunkStartDate;
            }

            Log.Information("Completed full crawl from {fromDate} to {toDate}", fromDate, toDate);
        }
        catch (Exception e)
        {
            Log.Error($"Error crawling TuoiTre articles from {fromDate} to {toDate}: {e.Message}");
        }
    }
}