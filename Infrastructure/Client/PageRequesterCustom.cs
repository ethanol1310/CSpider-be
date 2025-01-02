using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abot2.Core;
using Abot2.Poco;
using Bogus;
using Serilog;

namespace CSpider.Infrastructure.Client
{
    public interface IPageRequester : IDisposable
    {
        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        Task<CrawledPage> MakeRequestAsync(Uri uri);

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
    }

    public class PageRequesterCustom : IPageRequester
    {
        private readonly CrawlConfiguration _config;
        private readonly IWebContentExtractor _contentExtractor;
        private readonly CookieContainer _cookieContainer = new CookieContainer();
        private HttpClientHandler _httpClientHandler;
        private HttpClient _httpClient;
        private Faker _faker;

        public PageRequesterCustom(CrawlConfiguration config, IWebContentExtractor contentExtractor, HttpClient httpClient = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _contentExtractor = contentExtractor ?? throw new ArgumentNullException(nameof(contentExtractor));

            if (_config.HttpServicePointConnectionLimit > 0)
                ServicePointManager.DefaultConnectionLimit = _config.HttpServicePointConnectionLimit;

            _httpClient = httpClient;
        }

        /// <summary>
        /// Make an http web request to the url and download its content
        /// </summary>
        public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri)
        {
            return await MakeRequestAsync(uri, (x) => new CrawlDecision { Allow = true }).ConfigureAwait(false);
        }

        /// <summary>
        /// Make an http web request to the url and download its content based on the param func decision
        /// </summary>
        public virtual async Task<CrawledPage> MakeRequestAsync(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            if (_faker == null)
            {
                _faker = new Faker();
            }

            if (_httpClient == null)
            {
                _httpClientHandler = BuildHttpClientHandler(uri);
                _httpClient = BuildHttpClient(_httpClientHandler);
            }

            var crawledPage = new CrawledPage(uri);
            HttpResponseMessage response = null;
            int retryCount = 0;
            int maxRetries = _config.MaxRetryCount;
            TimeSpan delay = TimeSpan.FromMilliseconds(_config.MinRetryDelayInMilliseconds);

            while (retryCount < maxRetries)
            {
                try
                {
                    crawledPage.RequestStarted = DateTime.Now;
                    using (var requestMessage = BuildHttpRequestMessage(uri))
                    {
                        _httpClient.DefaultRequestHeaders.Remove("User-Agent");
                        _httpClient.DefaultRequestHeaders.Add("User-Agent", _faker.Internet.UserAgent());
                        response = await _httpClient.SendAsync(requestMessage, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    var statusCode = Convert.ToInt32(response.StatusCode);
                    if (statusCode == 200)
                    {
                        break;
                    }

                    if (statusCode < 200 || statusCode > 399)
                    {
                        if (retryCount >= maxRetries)
                        {
                            throw new HttpRequestException(
                                $"Server response was unsuccessful, returned [http {statusCode}]");
                        }
                    }
                }
                catch (HttpRequestException hre)
                {
                    crawledPage.HttpRequestException = hre;
                    Log.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri, hre);
                }
                catch (TaskCanceledException ex)
                {
                    crawledPage.HttpRequestException =
                        new HttpRequestException("Request timeout occurred",
                            ex); //https://stackoverflow.com/questions/10547895/how-can-i-tell-when-httpclient-has-timed-out
                    Log.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri,
                        crawledPage.HttpRequestException);
                }
                catch (Exception e)
                {
                    crawledPage.HttpRequestException = new HttpRequestException("Unknown error occurred", e);
                    Log.Debug("Error occurred requesting url [{0}] {@Exception}", uri.AbsoluteUri,
                        crawledPage.HttpRequestException);
                }

                retryCount++;
                if (retryCount < maxRetries)
                {
                    Log.Information("Retrying request to url [{0}], attempt {1} of {2}", uri.AbsoluteUri, retryCount + 1, maxRetries);
                    await Task.Delay(delay);
                }
            }

            crawledPage.HttpRequestMessage = response?.RequestMessage;
            crawledPage.RequestCompleted = DateTime.Now;
            crawledPage.HttpResponseMessage = response;
            crawledPage.HttpClientHandler = _httpClientHandler;

            try
            {
                if (response != null)
                {
                    var shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                    if (shouldDownloadContentDecision.Allow)
                    {
                        crawledPage.DownloadContentStarted = DateTime.Now;
                        crawledPage.Content =
                            await _contentExtractor.GetContentAsync(response).ConfigureAwait(false);
                        crawledPage.DownloadContentCompleted = DateTime.Now;
                    }
                    else
                    {
                        Log.Debug("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri,
                            shouldDownloadContentDecision.Reason);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Debug("Error occurred finalizing requesting url [{0}] {@Exception}", uri.AbsoluteUri, e);
            }

            return crawledPage;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpClientHandler?.Dispose();
        }


        protected virtual HttpRequestMessage BuildHttpRequestMessage(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);

            request.Version = GetEquivalentHttpProtocolVersion();

            return request;
        }

        protected virtual HttpClient BuildHttpClient(HttpClientHandler clientHandler)
        {
            var httpClient = new HttpClient(clientHandler);

            httpClient.DefaultRequestHeaders.Add("User-Agent", _config.UserAgentString);
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

            if (_config.HttpRequestTimeoutInSeconds > 0)
                httpClient.Timeout = TimeSpan.FromSeconds(_config.HttpRequestTimeoutInSeconds);

            if (_config.IsAlwaysLogin)
            {
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(_config.LoginUser + ":" + _config.LoginPassword));
                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + credentials);
            }

            return httpClient;
        }

        protected virtual HttpClientHandler BuildHttpClientHandler(Uri rootUri)
        {
            if (rootUri == null)
                throw new ArgumentNullException(nameof(rootUri));

            var httpClientHandler = new HttpClientHandler
            {
                MaxAutomaticRedirections = _config.HttpRequestMaxAutoRedirects,
                UseDefaultCredentials = _config.UseDefaultCredentials
            };

            if (_config.IsHttpRequestAutomaticDecompressionEnabled)
                httpClientHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            if (_config.HttpRequestMaxAutoRedirects > 0)
                httpClientHandler.AllowAutoRedirect = _config.IsHttpRequestAutoRedirectsEnabled;

            if (_config.IsSendingCookiesEnabled)
            {
                httpClientHandler.CookieContainer = _cookieContainer;
                httpClientHandler.UseCookies = true;
            }

            if (!_config.IsSslCertificateValidationEnabled)
                httpClientHandler.ServerCertificateCustomValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;

            if (_config.IsAlwaysLogin && rootUri != null)
            {
                //Added to handle redirects clearing auth headers which result in 401...
                //https://stackoverflow.com/questions/13159589/how-to-handle-authenticatication-with-httpwebrequest-allowautoredirect
                var cache = new CredentialCache();
                cache.Add(new Uri($"http://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));
                cache.Add(new Uri($"https://{rootUri.Host}"), "Basic", new NetworkCredential(_config.LoginUser, _config.LoginPassword));

                httpClientHandler.Credentials = cache;
            }

            return httpClientHandler;
        }


        private Version GetEquivalentHttpProtocolVersion()
        {
            if (_config.HttpProtocolVersion == HttpProtocolVersion.Version10)
                return HttpVersion.Version10;

            return HttpVersion.Version11;
        }

    }
}