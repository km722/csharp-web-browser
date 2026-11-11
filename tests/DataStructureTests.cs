using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleWebBrowser.DataStructures;
using SimpleWebBrowser.Models;

namespace SimpleWebBrowser.Tests
{
    //  unit tests for my custom data structures
    // Testing is crucial to ensure  implementations work correctly
    
    [TestClass]
    public class DataStructureTests
    {
        //  Test CustomLinkedList implementation
        [TestMethod]
        public void TestCustomLinkedList_AddFirst()
        {
            var list = new CustomLinkedList<string>();
            
            // Test adding elements to beginning
            list.AddFirst("First");
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("First", list.First);
            
            list.AddFirst("New First");
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("New First", list.First);
            Assert.AreEqual("First", list.Last);
        }

        [TestMethod]
        public void TestCustomLinkedList_AddLast()
        {
            var list = new CustomLinkedList<string>();
            
            // Test adding elements to end
            list.AddLast("First");
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("First", list.Last);
            
            list.AddLast("Last");
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("First", list.First);
            Assert.AreEqual("Last", list.Last);
        }

        [TestMethod]
        public void TestCustomLinkedList_RemoveDuplicates()
        {
            // Test case: A → B → B → C → B → B → D should become A → B → C → B → D
            var list = new CustomLinkedList<string>();
            
            list.AddLast("A");
            list.AddLast("B");
            list.AddLast("B");
            list.AddLast("C");
            list.AddLast("B");
            list.AddLast("B");
            list.AddLast("D");
            
            Assert.AreEqual(7, list.Count);
            
            list.RemoveDuplicates();
            
            Assert.AreEqual(5, list.Count);  // A → B → C → B → D
            
            // Verify sequence
            var items = new System.Collections.Generic.List<string>();
            foreach (var item in list)
            {
                items.Add(item);
            }
            
            Assert.AreEqual("A", items[0]);
            Assert.AreEqual("B", items[1]);
            Assert.AreEqual("C", items[2]);
            Assert.AreEqual("B", items[3]);
            Assert.AreEqual("D", items[4]);
        }

        [TestMethod]
        public void TestCustomLinkedList_Enumeration()
        {
            var list = new CustomLinkedList<string>();
            list.AddLast("A");
            list.AddLast("B");
            list.AddLast("C");
            
            var items = new System.Collections.Generic.List<string>();
            foreach (var item in list)
            {
                items.Add(item);
            }
            
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("A", items[0]);
            Assert.AreEqual("B", items[1]);
            Assert.AreEqual("C", items[2]);
        }

        // Test NavigationStack implementation
        [TestMethod]
        public void TestNavigationStack_NavigateTo()
        {
            var stack = new NavigationStack<WebPage>();
            var page1 = CreateTestWebPage("http://page1.com", "Page 1");
            var page2 = CreateTestWebPage("http://page2.com", "Page 2");
            
            stack.NavigateTo(page1);
            Assert.AreEqual(page1, stack.Current);
            Assert.IsFalse(stack.CanGoBack);
            
            stack.NavigateTo(page2);
            Assert.AreEqual(page2, stack.Current);
            Assert.IsTrue(stack.CanGoBack);
            Assert.IsFalse(stack.CanGoForward);
        }

        [TestMethod]
        public void TestNavigationStack_BackForward()
        {
            var stack = new NavigationStack<WebPage>();
            var page1 = CreateTestWebPage("http://page1.com", "Page 1");
            var page2 = CreateTestWebPage("http://page2.com", "Page 2");
            var page3 = CreateTestWebPage("http://page3.com", "Page 3");
            
            // Create sequence: Page1 → Page2 → Page3
            stack.NavigateTo(page1);
            stack.NavigateTo(page2);
            stack.NavigateTo(page3);
            Assert.AreEqual(page3, stack.Current);
            
            // Go back twice
            stack.GoBack();
            Assert.AreEqual(page2, stack.Current);
            stack.GoBack();
            Assert.AreEqual(page1, stack.Current);
            
            // Go forward once
            stack.GoForward();
            Assert.AreEqual(page2, stack.Current);
        }

        [TestMethod]
        public void TestNavigationStack_CanNavigate()
        {
            var stack = new NavigationStack<WebPage>();
            var page1 = CreateTestWebPage("http://page1.com", "Page 1");
            var page2 = CreateTestWebPage("http://page2.com", "Page 2");
            
            // Initially no navigation
            Assert.IsFalse(stack.CanGoBack);
            Assert.IsFalse(stack.CanGoForward);
            
            // After first page
            stack.NavigateTo(page1);
            Assert.IsFalse(stack.CanGoBack);
            Assert.IsFalse(stack.CanGoForward);
            
            // After second page
            stack.NavigateTo(page2);
            Assert.IsTrue(stack.CanGoBack);
            Assert.IsFalse(stack.CanGoForward);
            
            // After going back
            stack.GoBack();
            Assert.IsFalse(stack.CanGoBack);
            Assert.IsTrue(stack.CanGoForward);
        }

        // Day 4 Task 8: Test BookmarkTree implementation
        [TestMethod]
        public void TestBookmarkTree_AddBookmark()
        {
            var tree = new BookmarkTree();
            
            tree.AddBookmark("Google", "http://google.com");
            tree.AddBookmark("GitHub", "http://github.com");
            
            var allBookmarks = tree.GetAllBookmarks();
            Assert.AreEqual(2, allBookmarks.Count);
            
            var found = tree.FindBookmark("Google");
            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("Google", found[0].Name);
        }

        [TestMethod]
        public void TestBookmarkTree_AddFolder()
        {
            var tree = new BookmarkTree();
            
            tree.AddBookmark("Google", "http://google.com");
            
            var allBookmarks = tree.GetAllBookmarks();
            Assert.AreEqual(1, allBookmarks.Count);  // Only bookmark, not folder
        }

        [TestMethod]
        public void TestBookmarkTree_FindBookmark()
        {
            var tree = new BookmarkTree();
            
            tree.AddBookmark("Google", "http://google.com");
            tree.AddBookmark("GitHub", "http://github.com");
            
            var found = tree.FindBookmark("Google");
            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("http://google.com", found[0].Url);
            
            var notFound = tree.FindBookmark("NonExistent");
            Assert.AreEqual(0, notFound.Count);
        }

        [TestMethod]
        public void TestBookmarkTree_RemoveBookmark()
        {
            var tree = new BookmarkTree();
            
            tree.AddBookmark("Google", "http://google.com");
            tree.AddBookmark("GitHub", "http://github.com");
            
            Assert.AreEqual(2, tree.GetAllBookmarks().Count);
            
            tree.RemoveBookmark("Google");
            Assert.AreEqual(1, tree.GetAllBookmarks().Count);
            
            var found = tree.FindBookmark("Google");
            Assert.AreEqual(0, found.Count);
        }

        [TestMethod]
        public void TestBookmarkTree_GetAllBookmarks()
        {
            var tree = new BookmarkTree();
            
            tree.AddBookmark("Google", "http://google.com");
            tree.AddBookmark("GitHub", "http://github.com");
            
            var allBookmarks = tree.GetAllBookmarks();
            Assert.AreEqual(2, allBookmarks.Count);  // Only bookmarks, not folders
        }

        // Helper methods for creating test data
        private WebPage CreateTestWebPage(string url, string title)
        {
            return new WebPage(url, title, "<html><body>Test content</body></html>", 200, "OK");
        }

        private Bookmark CreateTestBookmark(string name, string url)
        {
            return new Bookmark(name, url, null, DateTime.Now, DateTime.Now, 0, null);
        }
    }
}
