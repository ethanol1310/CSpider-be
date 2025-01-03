using CSpider.Config;
using Microsoft.Extensions.Hosting;
using CSpider.Interface;
using Serilog;
using CSpider.Utils;
using Microsoft.Extensions.Options;

namespace CSpider.Api.Controllers;

public class CrawlerCronJob : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IEnumerable<IArticleCrawler> _crawlers;
    private readonly CrawlerCronJobConfig _config;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    public CrawlerCronJob(IEnumerable<IArticleCrawler> crawlers, IOptions<Config.Config> config)
    {
        _crawlers = crawlers;
        _config = config.Value.CrawlerCronJobConfig;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(DoWork, null,
            TimeSpan.FromSeconds(_config.InitialDelaySeconds),
            TimeSpan.FromMinutes(_config.IntervalMinutes));
        return Task.CompletedTask;
    }

    private void DoWork(object state)
    {
        if (!Monitor.TryEnter(_lock))
        {
            Log.Information("CronJob: crawling articles is running, cannot acquired lock");
            return;
        }

        try
        {
            Log.Information("CronJob: crawling articles");
            var toDate = DateTime.Now;
            var fromDate = toDate.AddDays((-1) * _config.CrawlDayRange);

            fromDate = Helper.NormalizeDateTime(fromDate, true);
            toDate = Helper.NormalizeDateTime(toDate, false);

            Parallel.ForEach(_crawlers,new ParallelOptions { MaxDegreeOfParallelism = 4 }, crawler =>
            {
                try
                {
                    crawler.CrawlArticle(fromDate, toDate);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in crawler {CrawlerType}", crawler.GetType().Name);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in CrawlerCronJob.DoWork");
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}