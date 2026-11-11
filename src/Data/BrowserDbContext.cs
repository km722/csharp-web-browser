using Microsoft.EntityFrameworkCore;
using SimpleWebBrowser.Data.Models;

namespace SimpleWebBrowser.Data
{
    // Database context for browser data
    public class BrowserDbContext : DbContext
    {
        private readonly string dbPath;

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<BookmarkEntity> Bookmarks { get; set; }
        public DbSet<HistoryEntity> History { get; set; }
        public DbSet<SettingsEntity> Settings { get; set; }

        public BrowserDbContext(string databasePath = "data/browser.db")
        {
            dbPath = databasePath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User entity configuration
            modelBuilder.Entity<UserEntity>()
                .HasKey(u => u.Id);
            
            modelBuilder.Entity<UserEntity>()
                .HasMany(u => u.Bookmarks)
                .WithOne(b => b.User)
                .HasForeignKey(b => b.UserId);
            
            modelBuilder.Entity<UserEntity>()
                .HasMany(u => u.History)
                .WithOne(h => h.User)
                .HasForeignKey(h => h.UserId);
            
            modelBuilder.Entity<UserEntity>()
                .HasOne(u => u.Settings)
                .WithOne(s => s.User)
                .HasForeignKey<SettingsEntity>(s => s.UserId);

            // Bookmark entity configuration
            modelBuilder.Entity<BookmarkEntity>()
                .HasKey(b => b.Id);

            // History entity configuration
            modelBuilder.Entity<HistoryEntity>()
                .HasKey(h => h.Id);

            // Settings entity configuration
            modelBuilder.Entity<SettingsEntity>()
                .HasKey(s => s.Id);
        }

        // Initialize database with default user
        public void EnsureCreated()
        {
            Database.EnsureCreated();
            
            // Create default user if none exists
            if (!Users.Any())
            {
                var defaultUser = new UserEntity
                {
                    Username = "Default",
                    CreatedAt = DateTime.Now
                };
                
                defaultUser.Settings = new SettingsEntity
                {
                        HomePage = "https://www.google.com",
                    AutoLoadHomePage = true,
                    MaxHistorySize = 1000,
                    HttpTimeoutSeconds = 30
                };
                
                Users.Add(defaultUser);
                SaveChanges();
            }
        }
    }
}

