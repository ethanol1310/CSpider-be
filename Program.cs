using Abot2.Core;
using CSpider.Services;
using CSpider.Core.Crawler;
using CSpider.Core.Spider;
using CSpider.Interface;
using CSpider.Config;
using CSpider.Core.Interface;
using CSpider.Core.Services;
using CSpider.Infrastructure.Client;
using CSpider.Infrastructure.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

// Configure settings
builder.Services.Configure<Config>(
    builder.Configuration.GetSection("Config"));

// Register stores
builder.Services.AddSingleton<ArticleStore>();

// Register clients
builder.Services.AddSingleton<ITuoiTreClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<Config>>().Value.TuoiTreConfig;
    return new TuoiTreClient(new WebContentExtractor(), config);
});

builder.Services.AddSingleton<IVnExpressClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<Config>>().Value.VnExpressConfig;
    return new VnExpressClient(new WebContentExtractor(), config);
});

// Register spiders
builder.Services.AddSingleton<ITuoiTreSpider, TuoiTreArticleSpider>();
builder.Services.AddSingleton<IVnExpressSpider, VnExpressArticleSpider>();

// Register crawlers
builder.Services.AddSingleton<IArticleCrawler, TuoiTreArticleCrawler>();
builder.Services.AddSingleton<IArticleCrawler, VnExpressArticleCrawler>();

// Register services
builder.Services.AddSingleton<IArticleService, ArticleService>();
builder.Services.AddHostedService<CrawlerCronJob>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        builder =>
        {
            builder
                .WithOrigins(
                    "http://localhost:5173", 
                    "http://localhost",       
                    "http://localhost:80")   
                .AllowAnyMethod()
                .AllowAnyHeader();

        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();

app.UseCors("AllowReactApp");

app.MapControllers();


app.Run();