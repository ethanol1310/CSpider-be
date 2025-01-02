namespace CSpider.Interface;

public interface IArticleCrawler
{
    public void CrawlArticle(DateTime fromDate, DateTime toDate);
}