namespace CSpider.Models;

public enum Source
{
    VnExpress,
    TuoiTre
}

public class Article
{
    public int Id { get; set; }
    public Source Source { get; set; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public int TotalCommentLikes { get; set; }
    public DateTime PublishTime { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class VnExpressCategory
{
    public required string Name { get; set; }
    public required int Id { get; set; }
    public required string ShareUrl { get; set; }
}