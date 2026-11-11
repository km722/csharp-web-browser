using System;

namespace SimpleWebBrowser.Models
{
    /// <summary>
    /// Represents a bookmark with URL, name, category, and visit tracking.
    /// Implements deep copying functionality for bookmark management.
    /// </summary>
    public class Bookmark
    {
        // Properties
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }
        public DateTime LastVisited { get; set; }
        public int VisitCount { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int UserId { get; set; }

        /// <summary>
        /// Default constructor initializing timestamps.
        /// </summary>
        public Bookmark()
        {
            DateAdded = DateTime.Now;
            LastVisited = DateTime.Now;
            VisitCount = 0;
        }

        /// <summary>
        /// Full constructor for loading bookmarks from storage with all metadata.
        /// Preserves original dates and visit counts when loading from file.
        /// </summary>
        public Bookmark(string name, string url, string? category, 
                       DateTime dateAdded, DateTime lastVisited, int visitCount, 
                       string? description = null, int userId = 0)
        {
            Name = name;
            Url = url;
            Category = category;
            DateAdded = dateAdded;
            LastVisited = lastVisited;
            VisitCount = visitCount;
            Description = description;
            UserId = userId;
        }

        /// <summary>
        /// Updates visit tracking when bookmark is accessed.
        /// </summary>
        public void UpdateVisit()
        {
            LastVisited = DateTime.Now;
            VisitCount++;
        }

        /// <summary>
        /// Validates if the bookmark URL is a valid absolute URI.
        /// </summary>
        public bool IsValidUrl()
        {
            return Uri.TryCreate(Url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Returns formatted string for display in UI.
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({Url})";
        }

        /// <summary>
        /// Compares bookmarks based on URL (case-insensitive).
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not Bookmark bookmark)
                return false;

            return string.Equals(Url, bookmark.Url, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns hash code based on URL.
        /// </summary>
        public override int GetHashCode()
        {
            return Url?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Creates a deep copy of the bookmark.
        /// </summary>
        public Bookmark Clone()
        {
            return new Bookmark(Name, Url, Category, DateAdded, LastVisited, VisitCount, Description, UserId);
        }
    }
}