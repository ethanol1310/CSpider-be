using System;
using CSpider.Interface;

namespace CSpider.Core.Spider;

using Models;
using Serilog;
using System.Diagnostics;
using Abot2.Core;
using Abot2.Poco;
using AngleSharp;
using Newtonsoft.Json;
using Serilog;
using System.Diagnostics;
using Bogus;
using Infrastructure.Client;
using Interface;
using AngleSharp.Dom;
using Config;
using Microsoft.Extensions.Options;

public class TuoiTreArticleSpider : ITuoiTreSpider
{
    private readonly string _baseUrl;
    private readonly SemaphoreSlim _semaphoreConcurrentPages;
    private readonly int _maxConcurrentArticles;
    private readonly int _minDelayBetweenPagesInMilliseconds;
    private readonly int _minDelayBetweenArticlesInMilliseconds;
    private readonly ITuoiTreClient _tuoiTreClient;

    public ListArticle ListArticle { get; }

    public TuoiTreArticleSpider(
        ITuoiTreClient tuoiTreClient,
        IOptions<Config> config)
    {
        var cfg = config.Value.TuoiTreConfig;
        _baseUrl = cfg.BaseUrl;
        _tuoiTreClient = tuoiTreClient;
        _semaphoreConcurrentPages = new SemaphoreSlim(cfg.MaxConcurrentPages);
        _maxConcurrentArticles = cfg.MaxConcurrentArticles;
        _minDelayBetweenPagesInMilliseconds = cfg.MinDelayBetweenPagesInMilliseconds;
        _minDelayBetweenArticlesInMilliseconds = cfg.MinDelayBetweenArticlesInMilliseconds;
        ListArticle = new ListArticle();
    }


    public async Task CrawlAsync(DateTime fromDate, DateTime toDate)
    {
        var stopwatch = Stopwatch.StartNew();
        var currentDate = fromDate;

        while (currentDate <= toDate)
        {
            try
            {
                var dateStr = currentDate.ToString("dd-MM-yyyy");
                await ProcessArticlesByDateAsync(dateStr);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error processing date {currentDate:dd-MM-yyyy}");
            }

            currentDate = currentDate.AddDays(1);
        }

        stopwatch.Stop();
        Log.Information(
            "TuoiTre crawling completed: Processed {ArticleCount} articles in {ElapsedTime:hh\\:mm\\:ss}",
            ListArticle.Articles.Count,
            stopwatch.Elapsed);
    }

    private async Task ProcessArticlesByDateAsync(string dateStr)
    {
        var pageNumber = 1;
        var pageProcessingTasks = new List<Task>();

        while (true)
        {
            await _semaphoreConcurrentPages.WaitAsync();
            try
            {
                var (articles, hasNextPage) = await _tuoiTreClient.GetArticlesByDateAsync(dateStr, pageNumber);

                if (articles != null && articles.Length > 0)
                {
                    pageProcessingTasks.Add(ProcessArticlesInParallelAsync(articles));
                }

                if (!hasNextPage) break;
                pageNumber++;
            }
            finally
            {
                _semaphoreConcurrentPages.Release();
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_minDelayBetweenPagesInMilliseconds));
        }

        await Task.WhenAll(pageProcessingTasks);
    }

    private async Task ProcessArticlesInParallelAsync(IHtmlCollection<IElement> articles)
    {
        await Parallel.ForEachAsync(
            articles,
            new ParallelOptions { MaxDegreeOfParallelism = _maxConcurrentArticles },
            async (article, ct) =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_minDelayBetweenArticlesInMilliseconds), ct);

                var linkElement = article.QuerySelector("a");
                var articleUrl = linkElement?.GetAttribute("href");
                var title = linkElement?.GetAttribute("title");

                if (string.IsNullOrEmpty(articleUrl) || string.IsNullOrEmpty(title)) return;

                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var fullUrl = $"{_baseUrl}{articleUrl}";
                    await ProcessArticle(fullUrl, title);
                    Log.Debug($"Processed article '{title}' in {stopwatch.Elapsed.TotalMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing article {title}");
                }
            });
    }

    private async Task ProcessArticle(string url, string title)
    {
        try
        {
            var articleDetails = await _tuoiTreClient.GetArticleAsync(url, title);
            if (!articleDetails.HasValue)
            {
                return;
            }

            var (objectId, objectType, publishedTime) = articleDetails.Value;
            var totalLikes = await ProcessComments(objectId, objectType, url, title);
            AddArticle(new Article
            {
                Title = title,
                Url = url,
                TotalCommentLikes = totalLikes,
                PublishTime = publishedTime,
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "Error processing article {Url}: {Title}", url, title);
        }
    }

    private async Task<int> ProcessComments(string objectId, string objectType, string url, string title,
        int page = 1, int totalCommentLikes = 0)
    {
        try
        {
            var currentPageLikes = await _tuoiTreClient.GetCommentsAsync(objectId, objectType, url, page);

            if (currentPageLikes > 0)
            {
                return await ProcessComments(objectId, objectType, url, title, page + 1,
                    totalCommentLikes + currentPageLikes);
            }

            return totalCommentLikes + currentPageLikes;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error processing comments for article {Url}, page {Page}", url, page);
            return totalCommentLikes;
        }
    }

    private void AddArticle(Article article)
    {
        if (ListArticle.Articles.Count != 0 && ListArticle.Articles.Count % 100 == 0)
        {
            Log.Information("TuoiTre: Added {Count} articles", ListArticle.Articles.Count);
        }
        Log.Debug($"Added article: {article.TotalCommentLikes} - {article.Title} - {article.Url}");
        ListArticle.AddArticle(article);
    }
}