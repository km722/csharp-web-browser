using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleWebBrowser.DataStructures
{
    public class NavigationStack<T>
    {
        private readonly Stack<T> backStack;
        private readonly Stack<T> forwardStack;
        private T? currentItem;
        private bool hasCurrent;

        public NavigationStack()
        {
            backStack = new Stack<T>();
            forwardStack = new Stack<T>();
            currentItem = default;
            hasCurrent = false;
        }

        public void NavigateTo(T item)
        {
            if (hasCurrent)
            {
                backStack.Push(currentItem!);
            }

            currentItem = item;
            hasCurrent = true;
            forwardStack.Clear();
        }

        public void Initialize(IEnumerable<T> history)
        {
            Clear();

            if (history == null)
            {
                return;
            }

            var list = history.ToList();
            if (list.Count == 0)
            {
                return;
            }

            currentItem = list[0];
            hasCurrent = true;

            for (int i = list.Count - 1; i >= 1; i--)
            {
                backStack.Push(list[i]);
            }
        }

        public T? GoBack()
        {
            if (!CanGoBack)
            {
                return default;
            }

            forwardStack.Push(currentItem!);
            currentItem = backStack.Pop();
            return currentItem;
        }

        public T? GoForward()
        {
            if (!CanGoForward)
            {
                return default;
            }

            backStack.Push(currentItem!);
            currentItem = forwardStack.Pop();
            return currentItem;
        }

        public void JumpTo(T item)
        {
            if (!hasCurrent)
            {
                return;
            }

            if (EqualityComparer<T>.Default.Equals(currentItem, item))
            {
                return;
            }

            while (backStack.Count > 0)
            {
                var previous = GoBack();
                if (EqualityComparer<T>.Default.Equals(previous, item))
                {
                    return;
                }
            }

            while (forwardStack.Count > 0)
            {
                var next = GoForward();
                if (EqualityComparer<T>.Default.Equals(next, item))
                {
                    return;
                }
            }
        }

        public void Clear()
        {
            backStack.Clear();
            forwardStack.Clear();
            currentItem = default;
            hasCurrent = false;
        }

        public bool CanGoBack => hasCurrent && backStack.Count > 0;
        public bool CanGoForward => hasCurrent && forwardStack.Count > 0;
        public T? Current => hasCurrent ? currentItem : default;

        public List<T> GetBackHistory()
        {
            return backStack.ToList();
        }

        public List<T> GetForwardHistory()
        {
            return forwardStack.ToList();
        }
    }
}