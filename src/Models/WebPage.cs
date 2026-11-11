using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace SimpleWebBrowser.Models
{
    /// <summary>
    /// Represents a web page with HTTP response data and content.
    /// Encapsulates URL, title, HTML content, and HTTP status information.
    /// </summary>
    public class WebPage
    {
        
        // Properties for web page data
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public DateTime LoadTime { get; set; }
        public TimeSpan LoadDuration { get; set; }
        
        /// <summary>
        /// Default constructor initializing LoadTime to current time.
        /// </summary>
        public WebPage()
        {
            LoadTime = DateTime.Now;
        }

        /// <summary>
        /// Parameterized constructor for creating a complete WebPage object.
        /// </summary>
        public WebPage(string url, string title, string content, int statusCode, string statusMessage)
        {
            Url = url;
            Title = title;
            Content = content;
            StatusCode = statusCode;
            StatusMessage = statusMessage;
            LoadTime = DateTime.Now;
        }

        /// <summary>
        /// Indicates if the HTTP response was successful (2xx status code).
        /// </summary>
        public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode < 300;
        // Return human-readable description of status code
        /// <summary>
        /// Returns human-readable description for HTTP status codes.
        /// </summary>
        public string GetStatusDescription()
        {
            return StatusCode switch
            {
                200 => "OK",
                400 => "Bad Request",
                403 => "Forbidden",
                404 => "Not Found",
                408 => "Request Timeout",
                500 => "Internal Server Error",
                _ => "Unknown Status"
            };
        }

        /// <summary>
        /// Extracts the title from HTML content using HtmlAgilityPack.
        /// Only processes if Content is not empty and Title is empty.
        /// </summary>
        public void ExtractTitle()
        {
            if (string.IsNullOrEmpty(Content) || !string.IsNullOrEmpty(Title))
                return;

            try
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(Content);
                var titleNode = htmlDoc.DocumentNode.SelectSingleNode("//title");

                if (titleNode != null)
                {
                    Title = titleNode.InnerText.Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting title: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns string representation for debugging.
        /// </summary>
        public override string ToString()
        {
            return $"[{StatusCode}] {Title} - {Url}";
        }

        /// <summary>
        /// Compares WebPage objects based on URL (case-insensitive).
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not WebPage other)
                return false;

            return string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns hash code based on URL for use in hash-based collections.
        /// </summary>
        public override int GetHashCode()
        {
            return Url?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Extracts the first 5 absolute URLs from the HTML content.
        /// Parses anchor tags and returns clickable HTTP/HTTPS links.
        /// </summary>
        public List<string> ExtractUrls()
        {
            var urls = new List<string>();
            
            if (string.IsNullOrEmpty(Content))
                return urls;
            
            try
            {
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(Content);
                
                // XPath query to find all anchor tags with href attribute
                var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a[@href]");
                
                if (linkNodes != null)
                {
                    foreach (var link in linkNodes)
                    {
                        var href = link.GetAttributeValue("href", string.Empty)?.Trim();
                        if (string.IsNullOrEmpty(href))
                        {
                            continue;
                        }

                        if (href.StartsWith("//"))
                        {
                            href = "https:" + href;
                        }

                        if (href.StartsWith("#") || href.StartsWith("javascript", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            urls.Add(href);
                            if (urls.Count >= 5)
                            {
                                break;
                            }
                            continue;
                        }

                        if (Uri.TryCreate(Url, UriKind.Absolute, out var baseUri))
                        {
                            if (Uri.TryCreate(baseUri, href, out var resolved))
                            {
                                urls.Add(resolved.ToString());
                                if (urls.Count >= 5)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting URLs: {ex.Message}");
            }
            
            return urls;
        }
    }
}
