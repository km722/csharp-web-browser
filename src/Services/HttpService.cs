using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SimpleWebBrowser.Models;


namespace SimpleWebBrowser.Services
{
    public class HttpService : IDisposable
    {
        private readonly HttpClient httpClient;
        private readonly CookieContainer cookieContainer = new();
        private static readonly Random Jitter = new();
        private bool disposed = false;

        public HttpService()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = cookieContainer,
                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate |
                    DecompressionMethods.Brotli
            };

            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            ConfigureHttpClient();
        }

        private void ConfigureHttpClient()
        {
            // Identify ourselves so some sites return friendlier content for the coursework browser
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SimpleWebBrowser", "1.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; F21SC Coursework)"));

            // Accept headers for better compatibility
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            // Language preferences
            httpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
            httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));
            httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.9));
            httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.8));
        }

        public async Task<WebPage> FetchWebPageAsync(string url)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                url = NormalizeUrl(url);

                if (!ValidateUrl(url))
                {
                    stopwatch.Stop();
                    int code = (int)HttpStatusCode.BadRequest;
                    var reason = GetStatusReason(code, "Bad Request");
                    var summary = GetSummary(code, "Invalid URL");
                    var errorContent = GenerateErrorContent(url, code, reason, summary, "URL format validation failed");
                    return new WebPage(url, ExtractDomainName(url), errorContent, code, reason)
                    {
                        LoadDuration = stopwatch.Elapsed
                    };
                }

                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var mediaType = response.Content.Headers.ContentType?.MediaType ?? string.Empty;
                string content;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Non-success status code for {url}: {(int)response.StatusCode} {response.StatusCode}");
                    int code = (int)response.StatusCode;
                    string reason = GetStatusReason(code, response.ReasonPhrase);
                    string summary = GetSummary(code, reason);
                    string detail = string.IsNullOrWhiteSpace(response.ReasonPhrase)
                        ? $"Server returned {code} {reason}."
                        : response.ReasonPhrase.Trim();
                    var errorContent = GenerateErrorContent(url, code, reason, summary, detail);
                    stopwatch.Stop();
                    return new WebPage(url, ExtractDomainName(url), errorContent, code, reason)
                    {
                        LoadDuration = stopwatch.Elapsed
                    };
                }

                if (IsTextualContent(mediaType))
                {
                    content = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    content = $"<pre>Downloaded {bytes.Length} bytes of {mediaType} content.</pre>";
                }

                stopwatch.Stop();
                var statusReason = GetStatusReason((int)response.StatusCode, response.ReasonPhrase);
                var page = new WebPage(
                    url,
                    string.Empty,
                    content,
                    (int)response.StatusCode,
                    statusReason)
                {
                    LoadDuration = stopwatch.Elapsed
                };

                page.ExtractTitle();
                return page;
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                int code = (int)HttpStatusCode.RequestTimeout;
                var reason = GetStatusReason(code, "Request Timeout");
                var summary = GetSummary(code, reason);
                var errorContent = GenerateErrorContent(url, code, reason, summary, "Connection timed out.");
                return new WebPage(url, ExtractDomainName(url), errorContent, code, reason)
                {
                    LoadDuration = stopwatch.Elapsed
                };
            }
            catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode statusCode)
            {
                stopwatch.Stop();
                int code = (int)statusCode;
                var reason = GetStatusReason(code, statusCode.ToString());
                var summary = GetSummary(code, reason);
                var errorContent = GenerateErrorContent(url, code, reason, summary, ex.Message);
                return new WebPage(url, ExtractDomainName(url), errorContent, code, reason)
                {
                    LoadDuration = stopwatch.Elapsed
                };
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                int code = (int)HttpStatusCode.ServiceUnavailable;
                var reason = GetStatusReason(code, "Service Unavailable");
                var summary = GetSummary(code, "Network or DNS error");
                var errorContent = GenerateErrorContent(url, code, reason, summary, ex.Message);
                return new WebPage(url, ExtractDomainName(url), errorContent, code, reason)
                {
                    LoadDuration = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                int code = (int)HttpStatusCode.InternalServerError;
                var reason = GetStatusReason(code, "Internal Server Error");
                var summary = GetSummary(code, "Connection Error");
                var errorContent = GenerateErrorContent(url, code, reason, summary, ex.Message);
                return new WebPage(url, ExtractDomainName(url), errorContent, code, reason)
                {
                    LoadDuration = stopwatch.Elapsed
                };
            }
        }

        private string GenerateErrorContent(string url, int statusCode, string statusReason, string summary, string errorMessage)
        {
            var domain = ExtractDomainName(url);
            var troubleshooting = BuildTroubleshootingSection(statusCode, statusReason, summary, errorMessage);

            return $@"
<html>
<head>
    <title>Error: {statusCode} {statusReason}</title>
</head>
<body>
    <h1>Error: {statusCode} {statusReason}</h1>
    <p><strong>URL:</strong> {url}</p>
    <p><strong>Host:</strong> {domain}</p>
    <p><strong>Status Code:</strong> {statusCode}</p>
    <p><strong>Status Reason:</strong> {statusReason}</p>
    <p><strong>Summary:</strong> {summary}</p>
    <p><strong>Details:</strong> {errorMessage}</p>
    {troubleshooting}
    <p>This is a raw HTML error page generated by SimpleWebBrowser.</p>
</body>
</html>";
        }

        private string BuildTroubleshootingSection(int statusCode, string statusReason, string errorType, string errorMessage)
        {
            var bullets = new List<string>();
            string summary;

            if (errorType.Contains("Timeout", StringComparison.OrdinalIgnoreCase) || statusCode == 408)
            {
                summary = "The request took too long to respond.";
                bullets.Add("Check your internet connection or VPN.");
                bullets.Add("Retry after a short pause—servers sometimes stall.");
                bullets.Add("If you're behind a proxy or firewall, confirm the site is allowed.");
            }
            else if (statusCode >= 500)
            {
                summary = "The website reported a server-side error.";
                bullets.Add("Wait a few minutes—outages are usually temporary.");
                bullets.Add("If you operate the site, inspect the server logs for this endpoint.");
                bullets.Add("Contact the site owner if the issue persists.");
            }
            else if (statusCode == 404)
            {
                summary = "The page or resource could not be found.";
                bullets.Add("Verify the URL spelling and path.");
                bullets.Add("Use the site's navigation or search to locate the content.");
                bullets.Add("Pages may have moved or been removed—try the site home page.");
            }
            else if (statusCode == 400)
            {
                summary = "The server could not understand the request.";
                bullets.Add("Ensure the URL is complete (no stray spaces or punctuation).");
                bullets.Add("Remove unusual query parameters or headers that might not be accepted.");
                bullets.Add("Retry using HTTPS if you attempted HTTP.");
            }
            else if (statusCode == 401 || statusCode == 403)
            {
                summary = "Access to this resource is restricted.";
                bullets.Add("Log in with an account that has permission to view this page.");
                bullets.Add("Some sites require VPN or multi-factor authentication—confirm any requirements.");
                bullets.Add("If you expect access, contact the site owner to grant permissions.");
            }
            else if (statusCode == 429)
            {
                summary = "You're sending too many requests.";
                bullets.Add("Wait a moment; most rate limits reset quickly.");
                bullets.Add("Avoid automated rapid-fire requests.");
                bullets.Add("Request higher limits from the site owner if necessary.");
            }
            else if (statusCode >= 400)
            {
                summary = "The server rejected the request.";
                bullets.Add("Double-check the URL and any credentials provided.");
                bullets.Add("Ensure the resource exists and you have permission to access it.");
                bullets.Add("Consult the site's documentation for required headers or parameters.");
            }
            else if (statusCode >= 300)
            {
                summary = "The server responded with a redirect or informational status.";
                if (statusCode == 306)
                {
                    bullets.Add("HTTP 306 is reserved and unused—httpbin exposes it only for testing.");
                }
                else
                {
                    bullets.Add("Follow the Location header to see where the server wants you to go.");
                }
                bullets.Add("Check whether the site requires HTTPS or a new domain.");
                bullets.Add("Unexpected redirects can indicate missing parameters or cookies.");
            }
            else
            {
                summary = "The request failed before the server responded.";
                bullets.Add("Verify your internet connection or DNS settings.");
                bullets.Add("Firewall or antivirus software may be blocking this site.");
                bullets.Add("If using a proxy/VPN, ensure it allows traffic to the domain.");
            }

            var bulletHtml = string.Join("", bullets.ConvertAll(b => $"<li>{b}</li>"));
            return $@"<div class='error-details'>
            <h3>🔍 Troubleshooting Information</h3>
            <p>{summary}</p>
            <ul>{bulletHtml}</ul>
        </div>";
        }

        private string ExtractDomainName(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return "Unknown Domain";
            }
        }

        public bool ValidateUrl(string url)
        {
           

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttp || 
                       uriResult.Scheme == Uri.UriSchemeHttps;
            }
            return false;
        }

        public string NormalizeUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            // Trim whitespace first
            url = url.Trim();

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }

            if (url.EndsWith("/"))
            {
                try 
                {
                    Uri tempUri = new Uri(url);
                    if (tempUri.AbsolutePath == "/")
                    {
                        url = url.TrimEnd('/');
                    }
                }
                catch 
                {
                    // If Uri creation fails, just leave the URL as is
                }
            }

            return url;
        }
        public Dictionary<string, string> GetResponseHeaders(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>();
            
            foreach (var header in response.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }
            
            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    headers[header.Key] = string.Join(", ", header.Value);
                }
            }
            
            return headers;
        }

        public async Task<WebPage> FetchWithRetryAsync(string url, int maxRetries = 3)
        {
            int attempt = 0;
            Exception? lastException = null;

            while (attempt < maxRetries)
            {
                try
                {
                    var result = await FetchWebPageAsync(url);

                    if (result.StatusCode >= 200 && result.StatusCode < 300)
                    {
                        return result;
                    }

                    if (result.StatusCode >= 400 && result.StatusCode < 500)
                    {
                        Console.WriteLine($"Client error {result.StatusCode}, not retrying");
                        return result;
                    }

                    if (result.StatusCode >= 300 && result.StatusCode < 400)
                    {
                        Console.WriteLine($"Redirect status {result.StatusCode}, not retrying");
                        return result;
                    }

                    Console.WriteLine($"Attempt {attempt + 1}: Server error {result.StatusCode}");
                    lastException = new HttpRequestException($"Server returned {result.StatusCode}");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                }

                attempt++;

                if (attempt < maxRetries)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 1000;
                    int jitter = Jitter.Next(200, 600);
                    Console.WriteLine($"Waiting {delayMs + jitter}ms before retry...");
                    await Task.Delay(delayMs + jitter);
                }
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            stopwatch.Stop();
            return new WebPage(url, string.Empty, string.Empty, 500, $"Failed after {maxRetries} attempts: {lastException?.Message}")
            {
                LoadDuration = stopwatch.Elapsed
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    httpClient?.Dispose();
                }
                disposed = true;
            }
        }

        private static string GetStatusReason(int statusCode, string? reasonPhrase)
        {
            if (!string.IsNullOrWhiteSpace(reasonPhrase) && !IsNumeric(reasonPhrase))
            {
                return reasonPhrase.Trim();
            }

            return statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                402 => "Payment Required",
                403 => "Forbidden",
                404 => "Not Found",
                405 => "Method Not Allowed",
                406 => "Not Acceptable",
                407 => "Proxy Authentication Required",
                408 => "Request Timeout",
                409 => "Conflict",
                410 => "Gone",
                411 => "Length Required",
                412 => "Precondition Failed",
                413 => "Payload Too Large",
                414 => "URI Too Long",
                415 => "Unsupported Media Type",
                416 => "Range Not Satisfiable",
                417 => "Expectation Failed",
                418 => "I'm a Teapot",
                429 => "Too Many Requests",
                500 => "Internal Server Error",
                501 => "Not Implemented",
                502 => "Bad Gateway",
                503 => "Service Unavailable",
                504 => "Gateway Timeout",
                505 => "HTTP Version Not Supported",
                _ => $"HTTP {statusCode}"
            };
        }

        private static bool IsNumeric(string value)
        {
            foreach (var ch in value)
            {
                if (!char.IsDigit(ch))
                {
                    return false;
                }
            }
            return value.Length > 0;
        }

        private static bool IsTextualContent(string mediaType)
        {
            if (string.IsNullOrWhiteSpace(mediaType))
            {
                return true;
            }

            return mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
                   mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
                   mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
                   mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSummary(int statusCode, string context)
        {
            if (!string.IsNullOrWhiteSpace(context) &&
                !context.StartsWith("HTTP ", StringComparison.OrdinalIgnoreCase) &&
                !IsGenericReason(context))
            {
                return context;
            }

            return statusCode switch
            {
                400 => "The server could not understand the request.",
                401 => "Authentication is required to access this resource.",
                403 => "Access to this resource is forbidden.",
                404 => "The resource you requested could not be found.",
                405 => "The HTTP method is not allowed for this resource.",
                408 => "The server timed out waiting for the request.",
                429 => "The server is receiving too many requests.",
                500 => "The server encountered an unexpected condition.",
                503 => "The server is currently unable to handle the request.",
                _ => string.IsNullOrWhiteSpace(context)
                    ? "The request failed before the server responded."
                    : context
            };
        }

        private static bool IsGenericReason(string value)
        {
            return value.Equals("Bad Request", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Internal Server Error", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Request Timeout", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Too Many Requests", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Forbidden", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Not Found", StringComparison.OrdinalIgnoreCase);
        }
    }
}
