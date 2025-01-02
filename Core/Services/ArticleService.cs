using CSpider.Core.Interface;
using CSpider.Infrastructure.Store;
using CSpider.Interface;
using CSpider.Models;

namespace CSpider.Services;

using System;
using System.Collections.Generic;

public class ArticleService : IArticleService
{
    private readonly ArticleStore _articleStore;

    public ArticleService(ArticleStore articleStore)
    {
        _articleStore = articleStore;
    }

    public List<Article> GetArticles(DateTime fromDate, DateTime toDate, Source source)
    {
        return _articleStore.FindByPublishTimeRangeAndSource(fromDate, toDate, source);
    }
}
