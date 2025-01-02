using System;
using System.IO;
using CSpider.Infrastructure.Store;
using LiteDB;

namespace CSpider.Tests.Fixtures
{
    public class ArticleStoreFixture : IDisposable
    {
        private readonly string _dbPath;
        public ArticleStore Store { get; }

        public ArticleStoreFixture()
        {
            // Create a temporary file path for the test database
            _dbPath = Path.Combine(Path.GetTempPath(), $"test_articles_{Guid.NewGuid()}.db");
            Store = new ArticleStore(_dbPath);
        }

        public void Dispose()
        {
            // Clean up the test database file
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
    }
}