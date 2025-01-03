using System;
using System.Diagnostics;
using CSpider.Core.Interface;
using CSpider.Core.Spider;
using CSpider.Infrastructure.Store;
using CSpider.Interface;
using CSpider.Models;
using CSpider.Utils;
using Serilog;

namespace CSpider.Core.Crawler;

public class TuoiTreArticleCrawler : IArticleCrawler
{
    private readonly ITuoiTreSpider _spider;

    public TuoiTreArticleCrawler(
        ITuoiTreSpider spider)
    {
        _spider = spider;
    }

    public void CrawlArticle(DateTime fromDate, DateTime toDate)
    {
        Log.Information("Start Crawling TuoiTre articles from {fromDate} to {toDate}", fromDate, toDate);

        var stopWatch = Stopwatch.StartNew();
        try
        {
            toDate = Helper.NormalizeDateTime(toDate, true);
            var currentDate = toDate;
            while (currentDate > fromDate)
            {
                var chunkStartDate = currentDate.AddDays(-1) < fromDate ? fromDate : currentDate.AddDays(-1);
                Log.Information("Crawling TuoiTre articles from {chunkStartDate} to {currentDate}", chunkStartDate.Date, currentDate);
        
                _spider.CrawlAsync(chunkStartDate, currentDate).Wait();

                currentDate = chunkStartDate.AddDays(-1);
            }

            Log.Information("Completed full crawl TuoiTre from {fromDate} to {nextDate} in {ElapsedTime:hh\\:mm\\:ss}", fromDate, toDate, stopWatch.Elapsed);
        }
        catch (Exception e)
        {
            Log.Error($"Error crawling TuoiTre articles from {fromDate} to {toDate}: {e.Message}");
        }
    }
}