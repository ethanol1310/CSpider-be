namespace CSpider.Core.Spider;

using AngleSharp;
using Abot2.Core;
using Abot2.Poco;
using Bogus;
using Models;
using Serilog;

public class ListArticle
{
    public List<Article> Articles { get; set; } = new List<Article>();

    public void AddArticle(Article article)
    {
        Articles.Add(article);
    }
}
