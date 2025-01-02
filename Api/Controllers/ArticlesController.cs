using CSpider.Interface;
using Microsoft.AspNetCore.Mvc;
using CSpider.Utils;
using CSpider.Api.DTO;
using CSpider.Core.Interface;

namespace CSpider.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleService _articleService;
    private readonly IEnumerable<IArticleCrawler> _crawlers;

    public ArticlesController(
        IArticleService articleService,
        IEnumerable<IArticleCrawler> crawlers)
    {
        _articleService = articleService;
        _crawlers = crawlers;
    }

    [HttpGet]
    public IActionResult GetArticles([FromQuery] GetArticlesRequest request)
    {
        var start = request.FromDate ?? DateTime.Now.AddDays(-7);
        var end = request.ToDate ?? DateTime.Now;
        int top = request.Limit ?? 10;

        start = Helper.NormalizeDateTime(start, true);
        end = Helper.NormalizeDateTime(end, false);

        var articles = _articleService.GetArticles(start, end, request.Source)
            .OrderByDescending(a => a.TotalCommentLikes)
            .Take(top)
            .Select(a => new ArticleDto
            {
                Title = a.Title,
                Url = a.Url,
                TotalCommentLikes = a.TotalCommentLikes,
                PublishTime = a.PublishTime,
                Source = a.Source.ToString()
            })
            .ToList();

        return Ok(new GetArticlesResponse
        {
            Articles = articles,
            Total = articles.Count,
        });
    }
}