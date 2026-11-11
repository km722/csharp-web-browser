using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using SimpleWebBrowser.DataStructures;
using SimpleWebBrowser.Models;
using SimpleWebBrowser.Services;

namespace SimpleWebBrowser.Views
{
    public partial class MainForm : Form
    {
        private const int MaxHistoryEntries = 1000;

        private readonly DatabaseService databaseService;
        private readonly HttpService httpService;
        private readonly BookmarkTree bookmarkTree = new();
        private readonly CustomLinkedList<WebPage> historyItems = new();
        private readonly NavigationStack<WebPage> navigationStack = new();
        private readonly List<Bookmark> bookmarkCache = new();

        private string currentUser;
        private WebPage? currentPage;

        private MenuStrip menuStrip = null!;
        private ToolStrip toolStrip = null!;
        private ToolStripLabel userLabel = null!;
        private StatusStrip statusStrip = null!;
        private ToolStripStatusLabel statusLabel = null!;
        private TextBox addressBar = null!;
        private Button backButton = null!;
        private Button forwardButton = null!;
        private Button homeButton = null!;
        private Button reloadButton = null!;
        private Button goButton = null!;
        private Label pageTitleLabel = null!;
        private RichTextBox htmlDisplay = null!;
        private ListBox historyListBox = null!;
        private ListBox bookmarkListBox = null!;
        private ListBox urlPreviewListBox = null!;

        public MainForm(DatabaseService databaseService, HttpService httpService, string username)
        {
            this.databaseService = databaseService;
            this.httpService = httpService;
            currentUser = username;

            InitializeComponent();
            Load += async (_, __) => await InitializeStateAsync();
        }

        private async Task InitializeStateAsync()
        {
            currentUser = databaseService.EnsureUser(currentUser);
            databaseService.SwitchUser(currentUser);
            UpdateUserLabel();

            ReloadBookmarks();
            ReloadHistory();

            if (!await TryLoadMostRecentPageAsync())
            {
                await NavigateHome();
            }
        }

        private void InitializeComponent()
        {
            Text = "Simple Web Browser";
            Size = new Size(1200, 800);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;

            InitializeMenu();
            InitializeToolStrip();
            InitializeLayout();
            InitializeStatusStrip();
        }

        private void InitializeMenu()
        {
            menuStrip = new MenuStrip { Dock = DockStyle.Top };

            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Exit", null, (_, __) => Close())
            {
                ShortcutKeys = Keys.Control | Keys.Q
            });

            var navigationMenu = new ToolStripMenuItem("&Navigation");
            navigationMenu.DropDownItems.Add(new ToolStripMenuItem("&Home", null, async (_, __) => await NavigateHome())
            {
                ShortcutKeys = Keys.Alt | Keys.Home
            });
            navigationMenu.DropDownItems.Add(new ToolStripMenuItem("&Back", null, async (_, __) => await GoBack())
            {
                ShortcutKeys = Keys.Alt | Keys.Left
            });
            navigationMenu.DropDownItems.Add(new ToolStripMenuItem("&Forward", null, async (_, __) => await GoForward())
            {
                ShortcutKeys = Keys.Alt | Keys.Right
            });
            navigationMenu.DropDownItems.Add(new ToolStripMenuItem("&Reload", null, async (_, __) => await Reload())
            {
                ShortcutKeys = Keys.Control | Keys.R
            });
            navigationMenu.DropDownItems.Add(new ToolStripSeparator());
            navigationMenu.DropDownItems.Add(new ToolStripMenuItem("Set &Homepage", null, (_, __) => SetHomepage()));

            var bookmarkMenu = new ToolStripMenuItem("&Bookmarks");
            bookmarkMenu.DropDownItems.Add(new ToolStripMenuItem("&Add", null, (_, __) => AddBookmark())
            {
                ShortcutKeys = Keys.Control | Keys.D
            });
            bookmarkMenu.DropDownItems.Add(new ToolStripMenuItem("&Edit Selected", null, (_, __) => EditSelectedBookmark())
            {
                ShortcutKeys = Keys.Control | Keys.E
            });
            bookmarkMenu.DropDownItems.Add(new ToolStripMenuItem("&Delete Selected", null, (_, __) => DeleteSelectedBookmark())
            {
                ShortcutKeys = Keys.Control | Keys.Delete
            });

            var historyMenu = new ToolStripMenuItem("&History");
            historyMenu.DropDownItems.Add(new ToolStripMenuItem("&Search", null, (_, __) => SearchHistory())
            {
                ShortcutKeys = Keys.Control | Keys.F
            });
            historyMenu.DropDownItems.Add(new ToolStripMenuItem("Delete &Selected", null, (_, __) => DeleteSelectedHistoryEntry())
            {
                ShortcutKeys = Keys.Shift | Keys.Delete
            });
            historyMenu.DropDownItems.Add(new ToolStripMenuItem("&Clear", null, (_, __) => ClearHistory())
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete
            });

            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("&About", null, (_, __) => ShowAboutDialog())
            {
                ShortcutKeys = Keys.F1
            });

            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, navigationMenu, bookmarkMenu, historyMenu, helpMenu });
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);
        }

        private void InitializeToolStrip()
        {
            toolStrip = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                Padding = new Padding(4, 2, 4, 2)
            };

            backButton = CreateToolbarButton("←", GoBack);
            forwardButton = CreateToolbarButton("→", GoForward);
            reloadButton = CreateToolbarButton("⟳", Reload);
            homeButton = CreateToolbarButton("🏠", NavigateHome);

            addressBar = new TextBox
            {
                Width = 420,
                BorderStyle = BorderStyle.FixedSingle
            };
            addressBar.KeyDown += async (sender, args) =>
            {
                if (args.KeyCode == Keys.Enter)
                {
                    args.Handled = true;
                    args.SuppressKeyPress = true;
                    await NavigateFromAddressBarAsync();
                }
            };

            goButton = new Button
            {
                Text = "Go",
                AutoSize = true,
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            goButton.FlatAppearance.BorderSize = 0;
            goButton.Click += async (_, __) => await NavigateFromAddressBarAsync();

            var addressHost = new ToolStripControlHost(addressBar) { AutoSize = false, Width = 420 };
            var goHost = new ToolStripControlHost(goButton) { AutoSize = true };

            userLabel = new ToolStripLabel();

            toolStrip.Items.Add(CreateControlHost(backButton));
            toolStrip.Items.Add(CreateControlHost(forwardButton));
            toolStrip.Items.Add(CreateControlHost(reloadButton));
            toolStrip.Items.Add(CreateControlHost(homeButton));
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(addressHost);
            toolStrip.Items.Add(goHost);
            toolStrip.Items.Add(new ToolStripSeparator { Alignment = ToolStripItemAlignment.Right });
            toolStrip.Items.Add(userLabel);

            Controls.Add(toolStrip);
        }

        private void InitializeLayout()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

            var contentPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            pageTitleLabel = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Height = 32,
                Text = "(no page loaded)"
            };

            htmlDisplay = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10.5f),
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap = false
            };

            contentPanel.Controls.Add(htmlDisplay);
            contentPanel.Controls.Add(pageTitleLabel);

            var sidebarLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                Padding = new Padding(12)
            };
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40f));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35f));
            sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));
            sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            sidebarLayout.Controls.Add(CreateHistoryGroup(), 0, 0);
            sidebarLayout.Controls.Add(CreateBookmarksGroup(), 0, 1);
            sidebarLayout.Controls.Add(CreateUrlsGroup(), 0, 2);

            mainLayout.Controls.Add(contentPanel, 0, 0);
            mainLayout.Controls.Add(sidebarLayout, 1, 0);

            Controls.Add(mainLayout);
            mainLayout.BringToFront();
        }

        private void InitializeStatusStrip()
        {
            statusStrip = new StatusStrip { Dock = DockStyle.Bottom };
            statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            statusStrip.Items.Add(statusLabel);
            Controls.Add(statusStrip);
            statusStrip.BringToFront();
        }

        private GroupBox CreateHistoryGroup()
        {
            var group = new GroupBox { Text = "History", Dock = DockStyle.Fill };
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            historyListBox = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            historyListBox.DoubleClick += async (_, __) => await JumpToSelectedHistoryAsync();

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            var searchHistoryButton = new Button { Text = "Search", AutoSize = true };
            searchHistoryButton.Click += (_, __) => SearchHistory();
            var deleteHistoryButton = new Button { Text = "Delete", AutoSize = true };
            deleteHistoryButton.Click += (_, __) => DeleteSelectedHistoryEntry();

            buttonPanel.Controls.Add(searchHistoryButton);
            buttonPanel.Controls.Add(deleteHistoryButton);

            panel.Controls.Add(historyListBox, 0, 0);
            panel.Controls.Add(buttonPanel, 0, 1);

            group.Controls.Add(panel);
            return group;
        }

        private GroupBox CreateBookmarksGroup()
        {
            var group = new GroupBox { Text = "Bookmarks", Dock = DockStyle.Fill };
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            bookmarkListBox = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, DisplayMember = nameof(Bookmark.Name) };
            bookmarkListBox.DoubleClick += async (_, __) => await OpenSelectedBookmarkAsync();

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            var addButton = new Button { Text = "Add", AutoSize = true };
            addButton.Click += (_, __) => AddBookmark();
            var editButton = new Button { Text = "Edit", AutoSize = true };
            editButton.Click += (_, __) => EditSelectedBookmark();
            var deleteButton = new Button { Text = "Delete", AutoSize = true };
            deleteButton.Click += (_, __) => DeleteSelectedBookmark();

            buttonPanel.Controls.Add(addButton);
            buttonPanel.Controls.Add(editButton);
            buttonPanel.Controls.Add(deleteButton);

            panel.Controls.Add(bookmarkListBox, 0, 0);
            panel.Controls.Add(buttonPanel, 0, 1);

            group.Controls.Add(panel);
            return group;
        }

        private GroupBox CreateUrlsGroup()
        {
            var group = new GroupBox { Text = "Page URLs", Dock = DockStyle.Fill };
            urlPreviewListBox = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            urlPreviewListBox.DoubleClick += async (_, __) => await NavigateToSelectedUrlAsync();
            group.Controls.Add(urlPreviewListBox);
            return group;
        }

        private ToolStripControlHost CreateControlHost(Control control)
        {
            return new ToolStripControlHost(control) { AutoSize = false, Width = control.Width + 4, Margin = new Padding(2, 0, 2, 0) };
        }

        private Button CreateToolbarButton(string text, Func<Task> handler)
        {
            var button = new Button
            {
                Text = text,
                Width = 34,
                Height = 28,
                FlatStyle = FlatStyle.Flat
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += async (_, __) => await handler();
            return button;
        }

        private async Task NavigateFromAddressBarAsync()
        {
            if (string.IsNullOrWhiteSpace(addressBar.Text))
            {
                ShowError("Enter a URL to navigate to.");
                return;
            }

            await NavigateToUrl(addressBar.Text);
        }

        private async Task NavigateHome()
        {
            await NavigateToUrl(databaseService.GetHomePage());
        }

        private async Task NavigateToUrl(string url, bool recordHistory = true)
        {
            try
            {
                url = httpService.NormalizeUrl(url);
                if (!httpService.ValidateUrl(url))
                {
                    ShowError("Invalid URL format.");
                    return;
                }

                ShowStatus($"Loading {url}...");
                var page = await httpService.FetchWebPageAsync(url);
                RenderPage(page);

                if (recordHistory)
                {
                    RecordHistoryVisit(page);
                }

                currentPage = page;
            }
            catch (Exception ex)
            {
                ShowError($"Failed to load page: {ex.Message}");
            }
        }

        private void RecordHistoryVisit(WebPage page)
        {
            var latest = historyItems.FirstOrDefault;
            if (latest != null && string.Equals(latest.Url, page.Url, StringComparison.OrdinalIgnoreCase))
            {
                CopyPageState(latest, page);
                RefreshHistoryList();
                RefreshHistorySelection(latest);
                RefreshNavigationButtons();
                return;
            }

            historyItems.AddFirst(page);
            historyItems.RemoveDuplicates();

            while (historyItems.Count > MaxHistoryEntries)
            {
                historyItems.RemoveLast();
            }

            navigationStack.NavigateTo(page);
            databaseService.SaveToHistory(page);

            RefreshHistoryList();
            RefreshHistorySelection(page);
            RefreshNavigationButtons();
        }

        private async Task<bool> TryLoadMostRecentPageAsync()
        {
            var recent = navigationStack.Current;
            if (recent == null)
            {
                return false;
            }

            await DisplayExistingPageAsync(recent);
            return true;
        }

        private async Task GoBack()
        {
            var page = navigationStack.GoBack();
            if (page == null)
            {
                ShowError("No previous page available.");
                return;
            }

            await DisplayExistingPageAsync(page);
            RefreshNavigationButtons();
        }

        private async Task GoForward()
        {
            var page = navigationStack.GoForward();
            if (page == null)
            {
                ShowError("No forward page available.");
                return;
            }

            await DisplayExistingPageAsync(page);
            RefreshNavigationButtons();
        }

        private async Task Reload()
        {
            if (currentPage == null)
            {
                ShowError("No page to reload.");
                return;
            }

            await NavigateToUrl(currentPage.Url, recordHistory: false);
        }

        private async Task JumpToSelectedHistoryAsync()
        {
            if (historyListBox.SelectedItem is not WebPage page)
            {
                return;
            }

            navigationStack.JumpTo(page);
            await DisplayExistingPageAsync(page);
            RefreshNavigationButtons();
        }

        private void DeleteSelectedHistoryEntry()
        {
            if (historyListBox.SelectedItem is not WebPage page)
            {
                ShowError("Select a history entry to delete.");
                return;
            }

            if (MessageBox.Show("Remove this entry from history?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            databaseService.DeleteHistoryEntry(page.Url, page.LoadTime);
            historyItems.Remove(page);
            RefreshHistoryList();
            RefreshNavigationButtons();
        }

        private async Task OpenSelectedBookmarkAsync()
        {
            if (bookmarkListBox.SelectedItem is Bookmark bookmark)
            {
                await NavigateToUrl(bookmark.Url);
            }
        }

        private async Task NavigateToSelectedUrlAsync()
        {
            if (urlPreviewListBox.SelectedItem is string url)
            {
                await NavigateToUrl(url);
            }
        }

        private async Task DisplayExistingPageAsync(WebPage page)
        {
            if (string.IsNullOrEmpty(page.Content))
            {
                try
                {
                    var refreshed = await httpService.FetchWebPageAsync(page.Url);
                    CopyPageState(page, refreshed);
            }
            catch (Exception ex)
            {
                    ShowError($"Failed to refresh page: {ex.Message}");
                }
            }

            currentPage = page;
            RenderPage(page);
            RefreshHistorySelection(page);
        }

        private static void CopyPageState(WebPage target, WebPage source)
        {
            target.Content = source.Content;
            target.StatusCode = source.StatusCode;
            target.StatusMessage = source.StatusMessage;
            target.Title = source.Title;
            target.LoadDuration = source.LoadDuration;
            target.LoadTime = source.LoadTime;
        }

        private void RenderPage(WebPage page)
        {
            Text = $"[{page.StatusCode}] {page.Title} - Simple Web Browser";
            pageTitleLabel.Text = string.IsNullOrWhiteSpace(page.Title)
                ? $"{page.Url} ({page.StatusCode})"
                : $"{page.Title} ({page.StatusCode})";

            var displayContent = page.Content ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(page.Title))
            {
                var trimmed = displayContent.TrimStart();
                if (!trimmed.StartsWith("<title>", StringComparison.OrdinalIgnoreCase))
                {
                    displayContent = $"<title>{page.Title}</title>{Environment.NewLine}{displayContent}";
                }
            }

            htmlDisplay.Text = displayContent;
            addressBar.Text = page.Url;

            var duration = page.LoadDuration.TotalMilliseconds;
            ShowStatus($"{page.StatusCode} {page.StatusMessage} in {duration:F0} ms");

            UpdateUrlPreview(page);
        }

        private void UpdateUrlPreview(WebPage page)
        {
            var urls = page.ExtractUrls().Take(5).ToList();
            urlPreviewListBox.BeginUpdate();
            urlPreviewListBox.Items.Clear();
            foreach (var link in urls)
            {
                urlPreviewListBox.Items.Add(link);
            }
            urlPreviewListBox.EndUpdate();
        }

        private void ReloadBookmarks()
        {
            bookmarkCache.Clear();
            bookmarkTree.Root = new BookmarkTree.BookmarkNode("Bookmarks", null);

            foreach (var bookmark in databaseService.GetAllBookmarks())
            {
                bookmarkCache.Add(bookmark);
                bookmarkTree.AddBookmark(bookmark.Name, bookmark.Url);
            }

            RefreshBookmarkList();
        }

        private void ReloadHistory()
        {
            historyItems.Clear();
            navigationStack.Clear();

            var history = databaseService.GetRecentHistory(MaxHistoryEntries);
            if (history.Count == 0)
            {
                RefreshHistoryList();
                RefreshNavigationButtons();
                return;
            }

            for (int i = history.Count - 1; i >= 0; i--)
            {
                historyItems.AddFirst(history[i]);
            }

            navigationStack.Initialize(history);
            RefreshHistoryList();
            RefreshNavigationButtons();
        }

        private void RefreshBookmarkList()
        {
            bookmarkListBox.BeginUpdate();
            bookmarkListBox.Items.Clear();
            foreach (var bookmark in bookmarkCache.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase))
            {
                bookmarkListBox.Items.Add(bookmark);
            }
            bookmarkListBox.EndUpdate();
        }

        private void RefreshHistoryList()
        {
            historyListBox.BeginUpdate();
            historyListBox.Items.Clear();
            foreach (var page in historyItems.Take(50))
            {
                historyListBox.Items.Add(page);
            }
            historyListBox.EndUpdate();
        }

        private void RefreshHistorySelection(WebPage page)
        {
            for (int i = 0; i < historyListBox.Items.Count; i++)
            {
                if (historyListBox.Items[i] is WebPage candidate && string.Equals(candidate.Url, page.Url, StringComparison.OrdinalIgnoreCase))
                {
                    historyListBox.SelectedIndex = i;
                    return;
                }
            }

            historyListBox.ClearSelected();
        }

        private void RefreshNavigationButtons()
        {
            backButton.Enabled = navigationStack.CanGoBack;
            forwardButton.Enabled = navigationStack.CanGoForward;
        }

        private void AddBookmark()
        {
            var suggestedName = currentPage?.Title ?? currentPage?.Url ?? string.Empty;
            var name = Interaction.InputBox("Bookmark name:", "Add Bookmark", suggestedName).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var initialUrl = currentPage?.Url ?? addressBar.Text;
            var url = Interaction.InputBox("Bookmark URL:", "Add Bookmark", initialUrl).Trim();
            url = httpService.NormalizeUrl(url);
            if (!httpService.ValidateUrl(url))
            {
                ShowError("Please provide a valid URL.");
                return;
            }

            if (bookmarkCache.Any(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                ShowError("A bookmark with that name already exists.");
                return;
            }

            var now = DateTime.Now;
            var bookmark = new Bookmark(name, url, null, now, now, 0, null);
            databaseService.SaveBookmark(bookmark);
            bookmarkCache.Add(bookmark);
            bookmarkTree.AddBookmark(bookmark.Name, bookmark.Url);
            RefreshBookmarkList();
            ShowStatus($"Bookmark '{name}' added.");
        }

        private void EditSelectedBookmark()
        {
            if (bookmarkListBox.SelectedItem is not Bookmark selected)
            {
                ShowError("Select a bookmark to edit.");
                return;
            }

            var newName = Interaction.InputBox("Bookmark name:", "Edit Bookmark", selected.Name).Trim();
            if (string.IsNullOrWhiteSpace(newName))
            {
                return;
            }

            var newUrl = Interaction.InputBox("Bookmark URL:", "Edit Bookmark", selected.Url).Trim();
            newUrl = httpService.NormalizeUrl(newUrl);
            if (!httpService.ValidateUrl(newUrl))
            {
                ShowError("Invalid URL provided.");
                return;
            }

            if (!string.Equals(selected.Name, newName, StringComparison.OrdinalIgnoreCase) &&
                bookmarkCache.Any(b => string.Equals(b.Name, newName, StringComparison.OrdinalIgnoreCase)))
            {
                ShowError("Another bookmark already uses that name.");
                return;
            }

            var updated = new Bookmark(newName, newUrl, selected.Category, selected.DateAdded, DateTime.Now, selected.VisitCount, selected.Description);
            databaseService.UpdateBookmark(selected.Name, updated);
            ReloadBookmarks();
            ShowStatus($"Bookmark '{newName}' updated.");
        }

        private void DeleteSelectedBookmark()
        {
            if (bookmarkListBox.SelectedItem is not Bookmark selected)
            {
                ShowError("Select a bookmark to delete.");
                return;
            }

            if (MessageBox.Show($"Delete bookmark '{selected.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            databaseService.DeleteBookmark(selected.Name);
            bookmarkCache.RemoveAll(b => string.Equals(b.Name, selected.Name, StringComparison.OrdinalIgnoreCase));
            ReloadBookmarks();
            ShowStatus($"Bookmark '{selected.Name}' removed.");
        }

        private void SearchHistory()
        {
            var term = Interaction.InputBox("Search history for:", "Search History", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                return;
            }

            var results = databaseService.SearchHistory(term);
            if (results.Count == 0)
            {
                MessageBox.Show("No matching history entries found.", "Search History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new Form
            {
                Text = $"History Results - '{term}'",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(640, 360),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            foreach (var page in results)
            {
                list.Items.Add(page);
            }

            list.DoubleClick += async (_, __) =>
            {
                if (list.SelectedItem is WebPage page)
                {
                    await NavigateToUrl(page.Url);
                    dialog.Close();
                }
            };

            dialog.Controls.Add(list);
            dialog.ShowDialog(this);
        }

        private void ClearHistory()
        {
            if (MessageBox.Show("Clear browsing history for this user?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            databaseService.ClearHistory();
            historyItems.Clear();
            navigationStack.Clear();
            RefreshHistoryList();
            RefreshNavigationButtons();
            htmlDisplay.Clear();
            pageTitleLabel.Text = "(no page loaded)";
            currentPage = null;
            ShowStatus("History cleared.");
        }

        private void SetHomepage()
        {
            var suggested = currentPage?.Url ?? databaseService.GetHomePage();
            var newHome = Interaction.InputBox("Homepage URL:", "Set Homepage", suggested).Trim();
            if (string.IsNullOrWhiteSpace(newHome))
            {
                return;
            }

            newHome = httpService.NormalizeUrl(newHome);
            if (!httpService.ValidateUrl(newHome))
            {
                ShowError("The homepage must be a valid URL.");
                return;
            }

            databaseService.SetHomePage(newHome);
            ShowStatus("Homepage updated.");
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show(
                "Simple Web Browser\n\n" +
                "Coursework project demonstrating:\n" +
                "• HTTP fetching and raw HTML display\n" +
                "• Bookmark and history management with custom data structures\n" +
                "• SQLite persistence via Entity Framework\n" +
                "• Multi-user profile support",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateUserLabel()
        {
            userLabel.Text = $"User: {currentUser}";
        }

        private void ShowStatus(string message)
        {
            statusLabel.Text = message;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Simple Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }
    }
}
