using System;
using System.Drawing;
using System.Windows.Forms;
using SimpleWebBrowser.Services;

namespace SimpleWebBrowser.Views
{
    // User selection dialog for multi-user support
    public class UserSelectionForm : Form
    {
        private ListBox userListBox = null!;
        private Button selectButton = null!;
        private Button newUserButton = null!;
        private readonly DatabaseService databaseService;

        public string? SelectedUsername { get; private set; }

        public UserSelectionForm(DatabaseService dbService)
        {
            databaseService = dbService;
            InitializeComponents();
            LoadUsers();
        }

        private void InitializeComponents()
        {
            this.Text = "Select User - Simple Web Browser";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Title label
            var titleLabel = new Label
            {
                Text = "Select a user or create a new one:",
                Location = new Point(20, 20),
                Size = new Size(350, 30),
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            // User list
            userListBox = new ListBox
            {
                Location = new Point(20, 60),
                Size = new Size(350, 150)
            };
            userListBox.DoubleClick += (s, e) => SelectUser();
            userListBox.MouseDown += (s, e) =>
            {
                int index = userListBox.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    userListBox.SelectedIndex = index;
                }
            };

            // Select button
            selectButton = new Button
            {
                Text = "Select User",
                Location = new Point(20, 220),
                Size = new Size(120, 30)
            };
            selectButton.Click += (s, e) => SelectUser();

            // New user button
            newUserButton = new Button
            {
                Text = "New User",
                Location = new Point(150, 220),
                Size = new Size(120, 30)
            };
            newUserButton.Click += (s, e) => CreateNewUser();

            // Add controls
            this.Controls.Add(titleLabel);
            this.Controls.Add(userListBox);
            this.Controls.Add(selectButton);
            this.Controls.Add(newUserButton);
        }

        private void LoadUsers()
        {
            userListBox.Items.Clear();
            var usernames = databaseService.GetAllUsernames();
            foreach (var username in usernames)
            {
                userListBox.Items.Add(username);
            }

            if (userListBox.Items.Count > 0)
            {
                userListBox.SelectedIndex = 0;
            }
        }

        private void SelectUser()
        {
            if (userListBox.SelectedItem is string username)
            {
                SelectedUsername = username;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a user", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CreateNewUser()
        {
            string username = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new username:",
                "Create User",
                ""
            );

            if (!string.IsNullOrWhiteSpace(username))
            {
                if (databaseService.CreateUser(username))
                {
                    SelectedUsername = username;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"User '{username}' already exists!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}

