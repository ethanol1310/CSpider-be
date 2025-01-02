using CSpider.Models;

namespace CSpider.Infrastructure.Store;

using LiteDB;

public class ArticleStore
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Article> _articles;

    public ArticleStore(string fileName = "articles.db")
    {
        _database = new LiteDatabase($"Filename={fileName};Connection=shared");
        _articles = _database.GetCollection<Article>("articles");
        _articles.EnsureIndex(x => new { x.Url, x.Source }, true);
        _articles.EnsureIndex(x => x.Source);
        _articles.EnsureIndex(x => x.PublishTime);
    }

    public void Upsert(Article article)
    {
        var existing = _articles.FindOne(x => x.Url == article.Url && x.Source == article.Source);
        if (existing != null)
        {
            article.Id = existing.Id;
            _articles.Update(article);
        }
        else
        {
            _articles.Insert(article);
        }
    }

    public void UpsertBatch(IEnumerable<Article> articles)
    {
        foreach (var batch in articles.Chunk(1000))
        {
            foreach (var article in batch)
            {
                Upsert(article);
            }
        }
    }

    public List<Article> FindByPublishTimeRangeAndSource(DateTime fromDate, DateTime toDate, Source source)
    {
        return _articles.Find(a =>
            a.PublishTime >= fromDate &&
            a.PublishTime <= toDate &&
            a.Source == source
        ).ToList();
    }

    public List<Article> FindByPublishTimeRange(DateTime fromDate, DateTime toDate)
    {
        return _articles.Find(a => a.PublishTime >= fromDate && a.PublishTime <= toDate).ToList();
    }

    public List<Article> FindByUrl(string url)
    {
        return _articles.Find(a => a.Url == url).ToList();
    }
}