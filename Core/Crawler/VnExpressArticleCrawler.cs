using System;
using CSpider.Core.Interface;
using CSpider.Interface;
using CSpider.Core.Spider;
using CSpider.Infrastructure.Store;
using CSpider.Models;
using System.Diagnostics;
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
        
        var stopWatch = Stopwatch.StartNew();
        try
        {
            var currentDate = toDate;
            while (currentDate > fromDate)
            {
                var chunkStartDate = currentDate.AddHours(-12) < fromDate ? fromDate : currentDate.AddHours(-12);
                Log.Information("Crawling VnExpress articles from {chunkStartDate} to {currentDate}", chunkStartDate, currentDate);
        
                _spider.CrawlAsync(chunkStartDate, currentDate).Wait();

                currentDate = chunkStartDate;
            }

            Log.Information("Completed full crawl VnExpress from {fromDate} to {toDate} in {ElapsedTime:hh\\:mm\\:ss}", fromDate, toDate, stopWatch.Elapsed);
        }
        catch (Exception e)
        {
            Log.Error($"Error crawling TuoiTre articles from {fromDate} to {toDate}: {e.Message}");
        }
    }
}