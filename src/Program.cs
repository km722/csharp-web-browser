using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SimpleWebBrowser.Services;
using SimpleWebBrowser.Views;

namespace SimpleWebBrowser
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using var databaseService = new DatabaseService();
                using var httpService = new HttpService();

                string activeUser = SelectOrCreateUser(databaseService);

                using var mainForm = new MainForm(databaseService, httpService, activeUser);
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"A critical error occurred: {ex.Message}\n\nThe application will now exit.",
                    "Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string SelectOrCreateUser(DatabaseService databaseService)
        {
            var existingUsers = databaseService.GetAllUsernames();
            if (existingUsers.Count == 0)
            {
                return databaseService.EnsureUser("Default");
            }

            using var selectionForm = new UserSelectionForm(databaseService);
            var dialogResult = selectionForm.ShowDialog();

            if (dialogResult == DialogResult.OK && !string.IsNullOrWhiteSpace(selectionForm.SelectedUsername))
            {
                return databaseService.EnsureUser(selectionForm.SelectedUsername);
            }

            return databaseService.EnsureUser(existingUsers.First());
        }
    }
}
