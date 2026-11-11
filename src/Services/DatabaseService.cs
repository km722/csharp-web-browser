using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SimpleWebBrowser.Data;
using SimpleWebBrowser.Data.Models;
using SimpleWebBrowser.Models;

namespace SimpleWebBrowser.Services
{
    public class DatabaseService : IDisposable
    {
        private readonly BrowserDbContext dbContext;
        private UserEntity? currentUser;

        public DatabaseService(string? databasePath = null)
        {
            if (string.IsNullOrEmpty(databasePath))
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appData, "SimpleWebBrowser");
                Directory.CreateDirectory(appFolder);
                databasePath = Path.Combine(appFolder, "browser.db");
            }

            dbContext = new BrowserDbContext(databasePath);
            dbContext.EnsureCreated();

            currentUser = dbContext.Users
                .Include(u => u.Settings)
                .Include(u => u.Bookmarks)
                .Include(u => u.History)
                .FirstOrDefault();
        }

        public List<string> GetAllUsernames()
        {
            return dbContext.Users
                .AsNoTracking()
                .Select(u => u.Username)
                .OrderBy(u => u)
                .ToList();
        }

        public bool SwitchUser(string username)
        {
            var user = dbContext.Users
                .Include(u => u.Settings)
                .Include(u => u.Bookmarks)
                .Include(u => u.History)
                .FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                return false;
            }

            currentUser = user;
            currentUser.LastLogin = DateTime.Now;
            dbContext.SaveChanges();
            return true;
        }

        public bool CreateUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || dbContext.Users.Any(u => u.Username == username))
            {
                return false;
            }

            var newUser = new UserEntity
            {
                Username = username,
                CreatedAt = DateTime.Now,
                LastLogin = DateTime.Now,
                Settings = new SettingsEntity
                {
                    HomePage = "https://www.google.com",
                    AutoLoadHomePage = true,
                    MaxHistorySize = 1000,
                    HttpTimeoutSeconds = 30
                }
            };

            dbContext.Users.Add(newUser);
            dbContext.SaveChanges();

            currentUser = newUser;
            return true;
        }

        public string EnsureUser(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                username = "Default";
            }

            if (!dbContext.Users.Any(u => u.Username == username))
            {
                CreateUser(username);
            }

            SwitchUser(username);
            return username;
        }

        public string GetCurrentUsername() => currentUser?.Username ?? "Default";

        public void SaveBookmark(Bookmark bookmark)
        {
            if (currentUser == null)
            {
                return;
            }

            var entity = new BookmarkEntity
            {
                Name = bookmark.Name,
                Url = bookmark.Url,
                Category = bookmark.Category,
                Description = bookmark.Description,
                DateAdded = bookmark.DateAdded,
                LastVisited = bookmark.LastVisited,
                VisitCount = bookmark.VisitCount,
                UserId = currentUser.Id
            };

            dbContext.Bookmarks.Add(entity);
            dbContext.SaveChanges();
        }

        public void UpdateBookmark(string originalName, Bookmark updated)
        {
            if (currentUser == null)
            {
                return;
            }

            var entity = dbContext.Bookmarks
                .FirstOrDefault(b => b.UserId == currentUser.Id && b.Name == originalName);

            if (entity == null)
            {
                return;
            }

            entity.Name = updated.Name;
            entity.Url = updated.Url;
            entity.Category = updated.Category;
            entity.Description = updated.Description;
            entity.LastVisited = updated.LastVisited;
            entity.VisitCount = updated.VisitCount;

            dbContext.SaveChanges();
        }

        public List<Bookmark> GetAllBookmarks()
        {
            if (currentUser == null)
            {
                return new List<Bookmark>();
            }

            return dbContext.Bookmarks
                .Where(b => b.UserId == currentUser.Id)
                .OrderByDescending(b => b.DateAdded)
                .Select(b => new Bookmark(
                    b.Name,
                    b.Url,
                    b.Category,
                    b.DateAdded,
                    b.LastVisited,
                    b.VisitCount,
                    b.Description,
                    b.UserId))
                .ToList();
        }

        public void DeleteBookmark(string name)
        {
            if (currentUser == null)
            {
                return;
            }

            var entity = dbContext.Bookmarks
                .FirstOrDefault(b => b.UserId == currentUser.Id && b.Name == name);

            if (entity == null)
            {
                return;
            }

            dbContext.Bookmarks.Remove(entity);
            dbContext.SaveChanges();
        }

        public List<Bookmark> FindBookmarksByCategory(string category)
        {
            if (currentUser == null)
            {
                return new List<Bookmark>();
            }

            return dbContext.Bookmarks
                .Where(b => b.UserId == currentUser.Id && b.Category == category)
                .OrderBy(b => b.Name)
                .Select(b => new Bookmark(
                    b.Name,
                    b.Url,
                    b.Category,
                    b.DateAdded,
                    b.LastVisited,
                    b.VisitCount,
                    b.Description,
                    b.UserId))
                .ToList();
        }

        public void SaveToHistory(WebPage page)
        {
            if (currentUser == null)
            {
                return;
            }

            var entity = new HistoryEntity
            {
                Url = page.Url,
                Title = page.Title,
                StatusCode = page.StatusCode,
                StatusMessage = page.StatusMessage,
                VisitTime = page.LoadTime,
                LoadDurationMs = page.LoadDuration.TotalMilliseconds,
                UserId = currentUser.Id
            };

            dbContext.History.Add(entity);
            dbContext.SaveChanges();
        }

        public List<WebPage> GetRecentHistory(int count)
        {
            if (currentUser == null)
            {
                return new List<WebPage>();
            }

            var entries = dbContext.History
                .Where(h => h.UserId == currentUser.Id)
                .OrderByDescending(h => h.VisitTime)
                .Take(count)
                .ToList();

            return entries.Select(MapHistoryEntity).ToList();
        }

        public void ClearHistory()
        {
            if (currentUser == null)
            {
                return;
            }

            var userHistory = dbContext.History
                .Where(h => h.UserId == currentUser.Id)
                .ToList();

            if (userHistory.Count == 0)
            {
                return;
            }

            dbContext.History.RemoveRange(userHistory);
            dbContext.SaveChanges();
        }

        public List<WebPage> SearchHistory(string searchTerm)
        {
            if (currentUser == null || string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<WebPage>();
            }

            var matches = dbContext.History
                .Where(h => h.UserId == currentUser.Id &&
                            (EF.Functions.Like(h.Title, $"%{searchTerm}%") ||
                             EF.Functions.Like(h.Url, $"%{searchTerm}%")))
                .OrderByDescending(h => h.VisitTime)
                .ToList();

            return matches.Select(MapHistoryEntity).ToList();
        }

        public List<WebPage> GetMostVisitedPages(int count)
        {
            if (currentUser == null)
            {
                return new List<WebPage>();
            }

            return dbContext.History
                .Where(h => h.UserId == currentUser.Id)
                .GroupBy(h => h.Url)
                .Select(g => new
                {
                    Url = g.Key,
                    Count = g.Count(),
                    Latest = g.OrderByDescending(x => x.VisitTime).First()
                })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .AsEnumerable()
                .Select(x => MapHistoryEntity(x.Latest))
                .ToList();
        }

        public void DeleteHistoryEntry(string url, DateTime visitTime)
        {
            if (currentUser == null)
            {
                return;
            }

            var entity = dbContext.History
                .FirstOrDefault(h => h.UserId == currentUser.Id &&
                                     h.Url == url &&
                                     h.VisitTime == visitTime);

            if (entity == null)
            {
                return;
            }

            dbContext.History.Remove(entity);
            dbContext.SaveChanges();
        }

        public string GetHomePage()
        {
            return currentUser?.Settings?.HomePage ?? "https://hw.ac.uk";
        }

        public void SetHomePage(string url)
        {
            if (currentUser?.Settings == null)
            {
                return;
            }

            currentUser.Settings.HomePage = url;
            dbContext.SaveChanges();
        }

        public void UpdateSettings(Action<SettingsEntity> updateAction)
        {
            if (currentUser?.Settings == null)
            {
                return;
            }

            updateAction(currentUser.Settings);
            dbContext.SaveChanges();
        }

        public void Dispose()
        {
            dbContext.Dispose();
        }

        public UserEntity? CurrentUser => currentUser;
        public bool HasCurrentUser => currentUser != null;

        private static WebPage MapHistoryEntity(HistoryEntity entity)
        {
            var page = new WebPage(
                entity.Url,
                entity.Title,
                string.Empty,
                entity.StatusCode,
                entity.StatusMessage)
            {
                LoadTime = entity.VisitTime,
                LoadDuration = TimeSpan.FromMilliseconds(entity.LoadDurationMs)
            };

            return page;
        }
    }
}

