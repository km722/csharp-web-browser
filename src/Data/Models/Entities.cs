using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleWebBrowser.Data.Models
{
    // User entity for multi-user support
    public class UserEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }

        public virtual ICollection<BookmarkEntity> Bookmarks { get; set; } = new List<BookmarkEntity>();
        public virtual ICollection<HistoryEntity> History { get; set; } = new List<HistoryEntity>();
        public virtual SettingsEntity? Settings { get; set; }
    }

    public class BookmarkEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime DateAdded { get; set; }
        public DateTime LastVisited { get; set; }
        public int VisitCount { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual UserEntity? User { get; set; }
    }

    public class HistoryEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Url { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        public int StatusCode { get; set; }

        [MaxLength(100)]
        public string StatusMessage { get; set; } = string.Empty;

        public DateTime VisitTime { get; set; }
        public double LoadDurationMs { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual UserEntity? User { get; set; }
    }

    public class SettingsEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(2000)]
        public string HomePage { get; set; } = "http://hw.ac.uk";

        public bool AutoLoadHomePage { get; set; } = true;
        public int MaxHistorySize { get; set; } = 1000;
        public int HttpTimeoutSeconds { get; set; } = 30;
        public bool SaveHistoryOnExit { get; set; } = true;
        public bool SaveBookmarksOnExit { get; set; } = true;

        [MaxLength(200)]
        public string UserAgent { get; set; } = "SimpleWebBrowser/1.0";

        public int WindowWidth { get; set; } = 1024;
        public int WindowHeight { get; set; } = 768;
        public bool ShowSidebar { get; set; } = true;
        public bool ShowStatusBar { get; set; } = true;

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual UserEntity? User { get; set; }
    }
}
