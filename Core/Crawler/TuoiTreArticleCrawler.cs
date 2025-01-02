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
        Log.Information("Crawling TuoiTre articles from {fromDate} to {toDate}", fromDate, toDate);

        try
        {
            _spider.CrawlAsync(fromDate, toDate).Wait();
            var now = DateTime.Now;
            var articles = _spider.ListArticle.Articles.Select(article =>
            {
                article.Source = Source.TuoiTre;
                article.CreatedTime = now;
                return article;
            });

            _articleStore.UpsertBatch(articles);

            Log.Information(
                "Finished crawling TuoiTre articles from {fromDate} to {toDate}. Stored {count} articles in batch.",
                fromDate, toDate, _spider.ListArticle.Articles.Count);
        }
        catch (Exception e)
        {
            Log.Error($"Error crawling VnExpress articles from {fromDate} to {toDate}: {e.Message}");
            return;
        }
    }
}