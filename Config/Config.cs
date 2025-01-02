namespace CSpider.Config;

public class Config
{
    public required TuoiTreConfig TuoiTreConfig { get; set; }
    public required VnExpressConfig VnExpressConfig { get; set; }
    public required CrawlerCronJobConfig CrawlerCronJobConfig { get; set; }
}

public class TuoiTreConfig
{
    public required string BaseUrl { get; set; }
    public required string CommentApiUrl { get; set; }
    public int MaxConcurrentPages { get; set; } = 1;
    public int MaxConcurrentArticles { get; set; } = 1;
    public int MinDelayBetweenPagesInMilliseconds { get; set; } = 2000;
    public int MinDelayBetweenArticlesInMilliseconds { get; set; } = 1000;
    public required HttpClientConfig HttpClientConfig { get; set; }
}

public class VnExpressConfig
{
    public required string BaseUrl { get; set; }
    public required string CommentApiUrl { get; set; }
    public int MaxConcurrentCategories { get; set; } = 1;
    public int MaxConcurrentPages { get; set; } = 1;
    public int MaxConcurrentArticles { get; set; } = 1;
    public int MinDelayBetweenPagesInMilliseconds { get; set; } = 2000;
    public int MinDelayBetweenArticlesInMilliseconds { get; set; } = 1000;
    public required HttpClientConfig HttpClientConfig { get; set; }
}

public class CrawlerCronJobConfig
{
    public int IntervalMinutes { get; set; } = 30;
    public int InitialDelaySeconds { get; set; } = 0;
    public int CrawlDayRange { get; set; } = 7;
}

public class HttpClientConfig
{
    public int MaxRetry { get; set; } = 5;
    public int MinRetryDelayInMilliseconds { get; set; } = 100;
    public int HttpRequestTimeoutInSeconds { get; set; } = 30;
}