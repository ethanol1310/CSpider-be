using System;
using CSpider.Core.Interface;
using CSpider.Core.Spider;
using CSpider.Infrastructure.Store;
using CSpider.Interface;
using CSpider.Models;
using Serilog;

namespace CSpider.Core.Crawler;

public class TuoiTreArticleCrawler : IArticleCrawler
{
    private readonly ITuoiTreSpider _spider;
    private readonly ArticleStore _articleStore;

    public TuoiTreArticleCrawler(
        ITuoiTreSpider spider,
        ArticleStore articleStore)
    {
        _spider = spider;
        _articleStore = articleStore;
    }

    public void CrawlArticle(DateTime fromDate, DateTime toDate)
    {
        Log.Information("Start Crawling TuoiTre articles from {fromDate} to {toDate}", fromDate, toDate);

        try
        {
            var nextDate = toDate.AddSeconds(1);
            var currentDate = nextDate;
            while (currentDate > fromDate)
            {
                var chunkStartDate = currentDate.AddDays(-1) < fromDate ? fromDate : currentDate.AddDays(-1);
                Log.Information("Crawling TuoiTre articles from {chunkStartDate} to {currentDate}", chunkStartDate, currentDate);
        
                _spider.CrawlAsync(chunkStartDate, currentDate).Wait();
                var now = DateTime.Now;
                var articles = _spider.ListArticle.Articles.Select(article =>
                {
                    article.Source = Source.TuoiTre;
                    article.CreatedTime = now;
                    return article;
                });

                _articleStore.UpsertBatch(articles);

                Log.Information(
                    "Finished chunk: {chunkStartDate} to {currentDate}. Stored {count} articles.",
                    chunkStartDate, currentDate, _spider.ListArticle.Articles.Count);

                currentDate = chunkStartDate;
            }

            Log.Information("Completed full crawl from {fromDate} to {nextDate}", fromDate, nextDate);
        }
        catch (Exception e)
        {
            Log.Error($"Error crawling TuoiTre articles from {fromDate} to {toDate}: {e.Message}");
        }
    }
}