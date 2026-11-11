using System;
using System.Collections.Generic;

namespace SimpleWebBrowser.DataStructures
{
    public class BookmarkTree
    {
        private BookmarkNode root;

        public BookmarkNode Root
        {
            get => root;
            set => root = value ?? new BookmarkNode("Bookmarks", null);
        }

        public BookmarkTree()
        {
            root = new BookmarkNode("Bookmarks", null);
        }

        public void AddBookmark(string name, string url)
        {
            var newNode = new BookmarkNode(name, url);
            root.AddChild(newNode);
        }

        public bool RemoveBookmark(string name)
        {
            return root.RemoveChildByName(name);
        }

        public List<BookmarkNode> GetAllBookmarks()
        {
            return new List<BookmarkNode>(root.Children);
        }

        public List<BookmarkNode> FindBookmark(string name)
        {
            return root.FindAllChildren(name);
        }

        public class BookmarkNode
        {
            public string Name { get; set; }
            public string? Url { get; set; }
            public List<BookmarkNode> Children { get; }
            public DateTime DateAdded { get; set; }
            public DateTime LastVisited { get; set; }
            public int VisitCount { get; set; }
            public string? Description { get; set; }

            public BookmarkNode()
            {
                Name = string.Empty;
                Children = new List<BookmarkNode>();
                DateAdded = DateTime.Now;
                LastVisited = DateTime.Now;
            }

            public BookmarkNode(string name, string? url)
            {
                Name = name;
                Url = url;
                Children = new List<BookmarkNode>();
                DateAdded = DateTime.Now;
                LastVisited = DateTime.Now;
            }

            public void AddChild(BookmarkNode child)
            {
                Children.Add(child);
            }

            public bool RemoveChildByName(string name)
            {
                var child = Children.Find(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                if (child == null)
                {
                    return false;
                }

                Children.Remove(child);
                return true;
            }

            public List<BookmarkNode> FindAllChildren(string name)
            {
                var matches = new List<BookmarkNode>();
                foreach (var child in Children)
                {
                    if (string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        matches.Add(child);
                    }
                }
                return matches;
            }
        }
    }
}