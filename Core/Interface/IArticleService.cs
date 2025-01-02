using CSpider.Models;

namespace CSpider.Core.Interface;

public interface IArticleService
{
    public List<Article> GetArticles(DateTime fromDate, DateTime toDate, Source source);
}