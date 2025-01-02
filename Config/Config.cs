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
}

public class VnExpressConfig 
{
    public required string BaseUrl { get; set; }
    public required string CommentApiUrl { get; set; }
    public int MaxConcurrentCategories { get; set; } = 1;
    public int MaxConcurrentPages { get; set; } = 1;
    public int MaxConcurrentArticles { get; set; } = 1;
}

public class CrawlerCronJobConfig
{
    public int IntervalMinutes { get; set; } = 30;
    public int InitialDelaySeconds { get; set; } = 0;
    public int CrawlDayRange { get; set; } = 7;
}
