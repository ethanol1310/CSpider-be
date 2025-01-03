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

    public VnExpressArticleCrawler(
        IVnExpressSpider spider)
    {
        _spider = spider;
    }

    public void CrawlArticle(DateTime fromDate, DateTime toDate)
    {
        Log.Information("Start Crawling VnExpress articles from {fromDate} to {toDate}", fromDate, toDate);

        try
        {
            var currentDate = toDate;
            while (currentDate > fromDate)
            {
                var chunkStartDate = currentDate.AddHours(-12) < fromDate ? fromDate : currentDate.AddHours(-12);
                Log.Information("Crawling VnExpress articles from {chunkStartDate} to {currentDate}", chunkStartDate, currentDate);
        
                _spider.CrawlAsync(chunkStartDate, currentDate).Wait();
                Log.Information(
                    "VnExpress Finished chunk: {chunkStartDate} to {currentDate}.",
                    chunkStartDate, currentDate);

                currentDate = chunkStartDate;
            }

            Log.Information("VnExpress Completed full crawl from {fromDate} to {toDate}", fromDate, toDate);
        }
        catch (Exception e)
        {
            Log.Error($"Error crawling TuoiTre articles from {fromDate} to {toDate}: {e.Message}");
        }
    }
}