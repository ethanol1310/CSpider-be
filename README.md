# CSpider

### Prerequisites

- .NET 9.0 SDK
- Docker

### Installation

1. Restore the dependencies:
    ```sh
   # Run this - I met some problem on MacOS when building this project
   rm -rf */obj */bin 
   dotnet restore
    ```

2. Build the project:
    ```sh
    dotnet build
    ```

### Running Tests

To run the tests, use the following command:
```sh
dotnet test
```

### Docker

To build and run the Docker container:

1. Build the Docker image:
    ```sh
    docker build -t cspider .
    ```

2. Run the Docker container:
    ```sh
    docker run -p 8080:8080 cspider
    ```

### Usage

You can use the provided `CSpider.http` file to test the API endpoints. For example, to fetch articles:

```http
GET {{CSpider_HostAddress}}/articles?from_date=2024-12-30&to_date=2025-01-02&source=tuoitre&limit=20
Accept: application/json
```

## Configuration

- `BaseUrl`: The base URL of the website.
- `CommentApiUrl`: The URL for the comment API.
- `HttpClientConfig`: Configuration for the HTTP client, including retry settings.
- `CrawlerConfig`: Configuration for the crawler policies.


# Project Structure

- Mermaid Diagram - Supported by Claude
```mermaid
flowchart TB
    subgraph Frontend["Frontend (React)"]
        UI["UI Components"]
        APIService["API Service"]
    end

    subgraph Backend["Backend (C#)"]
        API["API Controller"]
        Crawler["Article Crawlers"]
        Spider["Spider Services"]
        Store["Article Store"]
        
        subgraph CronJob["Crawler CronJob"]
            Scheduler["Scheduler"]
        end
        
        subgraph Clients["HTTP Clients"]
            VEC["VnExpress Client"]
            TTC["TuoiTre Client"]
        end
    end

    subgraph Storage["Storage"]
        DB[(LiteDB)]
    end

    subgraph External["External Services"]
        VE["VnExpress API"]
        TT["TuoiTre API"]
    end

    UI --> APIService
    APIService --> API
    API --> Store
    Store --> DB
    
    External --> DB
    Scheduler --> Crawler
    Crawler --> Spider
    Spider --> Clients
    VEC --> VE
    TTC --> TT
```

# Tree Folder

```mermaid
├── Api
│   ├── Controllers
│   │   ├── ArticlesController.cs
│   │   ├── CrawlerCronJob.cs
│   │   └── HealthController.cs
│   └── DTO
│       ├── ApiResponse.cs
│       └── ArticlesDTO.cs
├── CSpider.Tests
│   ├── CSpider.Tests.csproj
│   ├── Core
│   │   └── Crawler
│   │       ├── TuoiTreArticleCrawlerTests.cs
│   │       └── VnexpressArticleCrawlerTests.cs
│   ├── Fixtures
│   │   └── ArticleStoreFixture.cs
│   ├── Infrastructure
│   │   └── Client
│   │       ├── TuoiTreClientTests.cs
│   │       └── VnExpressClientTests.cs
│   ├── Seed
│   │   └── Seed.cs
│   └── Store
│       └── ArticleStoreTests.cs
├── CSpider.csproj
├── CSpider.http
├── CSpider.sln
├── CSpider.sln.DotSettings.user
├── Config
│   └── Config.cs
├── Core
│   ├── Crawler
│   │   ├── TuoiTreArticleCrawler.cs
│   │   └── VnExpressArticleCrawler.cs
│   ├── Interface
│   │   ├── IArticleCrawler.cs
│   │   ├── IArticleService.cs
│   │   └── IArticleSpider.cs
│   ├── Models
│   │   └── Article.cs
│   ├── Services
│   │   └── ArticleService.cs
│   └── Spider
│       ├── Common.cs
│       ├── TuoiTreArticleSpider.cs
│       └── VnExpressArticleSpider.cs
├── Dockerfile
├── Infrastructure
│   ├── Client
│   │   ├── PageRequesterCustom.cs
│   │   ├── TuoiTreClient.cs
│   │   └── VnExpressClient.cs
│   └── Store
│       └── ArticleStore.cs
├── Makefile
├── Program.cs
├── Properties
│   └── launchSettings.json
├── README.md
├── Utils
│   └── Helper.cs
├── appsettings.Development.json
├── appsettings.json
└── data
    └── articles.db

```
# Design

- Mermaid Diagram - Supported by Claude
```mermaid
flowchart TB
    subgraph UserInterface["User Interface"]
        CLI["Command Line Interface"]
        Config["Input Arguments
        - News Sources
        - Date Range
        - Top N Articles"]
    end

    subgraph CoreSystem["Crawl System"]
        subgraph Manager["Crawler Manager"]
            CrawlPolicies["Crawl Policies
           - HTTP Cache
           - Fake User Agent
           - Concurrent Requests"]
        end
        
        subgraph CrawlerLayer["Crawler Layer"]
            direction BT
            VECrawler["VnExpress Crawler"]
            TTCrawler["TuoiTre Crawler"]
        end
        
        subgraph SpiderLayer["Spider Layer"]
            subgraph NewsSpiders["News Spiders"]
                direction RL
                VESpider["VnExpress Spider"]
                TTSpider["TuoiTre Spider"]
            end 
            subgraph Parsers["Content Parsers"]
                direction RL
                ArticleParser["Article Parser"]
                CommentParser["Comment Parser"]
            end
        end
    end

    subgraph OutputSystem["Output System"]
        FileExport
        ConsoleDisplay
    end

    subgraph Model["Model"]
        subgraph RankingEngine["Arregate"]
            direction RL
            subgraph Strategies["Ranking Methods"]
                LikeRanking["Total Comment Like Ranking"]
            end
        end
        subgraph DataLayer["Data"]
                direction RL
                Article["Article
                - Title
                - URL
                - Comments
                - Total Comment Likes"]
        end
    end 

    %% Flow connections
    CLI --> Manager
    Manager --> CrawlerLayer
    CrawlerLayer --> SpiderLayer
    NewsSpiders --> Parsers
    SpiderLayer --> Model
    
    DataLayer --> RankingEngine

    RankingEngine --> OutputSystem
    UserInterface --> OutputSystem

    %% Styling
    classDef system fill:#f9f,stroke:#333,stroke-width:2px
    classDef component fill:#bfb,stroke:#333,stroke-width:2px
    classDef interface fill:#fbb,stroke:#333,stroke-width:2px
    
    class CoreSystem,Model system
    class RankingEngine,CrawlerLayer,SpiderLayer,DataLayer component
    class UserInterface,OutputSystem interface
```
