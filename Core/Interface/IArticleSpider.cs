using System;

namespace CSpider.Core.Interface;
using Spider;

public interface IArticleSpider
{
    public Task CrawlAsync(DateTime fromDate, DateTime toDate);
    public ListArticle ListArticle { get; }
}

public interface ITuoiTreSpider : IArticleSpider { }
public interface IVnExpressSpider : IArticleSpider { }