using System;
using System.Collections;
using System.Collections.Generic;

namespace SimpleWebBrowser.DataStructures
{
    public class CustomLinkedList<T> : IEnumerable<T>
    {
        private Node<T>? head;
        private Node<T>? tail;
        private int count;

        private class Node<TNode>
        {
            public TNode Data { get; set; }
            public Node<TNode>? Next { get; set; }
            public Node<TNode>? Previous { get; set; }

            public Node(TNode data)
            {
                Data = data;
                Next = null;
                Previous = null;
            }
        }

        public void AddFirst(T item)
        {
            // Drop new item to the front of the queue so history stay most recent first
            var newNode = new Node<T>(item);

            if (head == null)
            {
                head = tail = newNode;
            }
            else
            {
                newNode.Next = head;
                head.Previous = newNode;
                head = newNode;
            }
            count++;
        }

        public void AddLast(T item)
        {
            // Append item to tail when we trim overflow from the history list
            var newNode = new Node<T>(item);

            if (tail == null)
            {
                head = tail = newNode;
            }
            else
            {
                tail.Next = newNode;
                newNode.Previous = tail;
                tail = newNode;
            }
            count++;
        }

        public T RemoveFirst()
        {
            if (head == null)
                throw new InvalidOperationException("List is empty");

            T data = head.Data;

            if (head == tail) // Single element
            {
                head = tail = null;
            }
            else
            {
                head = head.Next;
                head!.Previous = null;
            }

            count--;
            return data;
        }

        public T RemoveLast()
        {
            if (tail == null)
                throw new InvalidOperationException("List is empty");

            T data = tail.Data;

            if (head == tail) // Single element
            {
                head = tail = null;
            }
            else
            {
                tail = tail.Previous;
                tail!.Next = null;
            }

            count--;
            return data;
        }

        public T? Find(T item)
        {
            var current = head;

            while (current != null)
            {
                if (EqualityComparer<T>.Default.Equals(current.Data, item))
                    return current.Data;
                current = current.Next;
            }

            return default(T);
        }

        public void RemoveDuplicates()
        {
            // this only nukes consecutive dupes, which matches what we want for history clutter
            if (head == null || head.Next == null)
                return;

            var current = head;

            while (current != null && current.Next != null)
            {
                if (EqualityComparer<T>.Default.Equals(current.Data, current.Next.Data))
                {
                    // Remove the duplicate
                    var duplicate = current.Next;
                    current.Next = duplicate.Next;

                    if (duplicate.Next != null)
                        duplicate.Next.Previous = current;
                    else
                        tail = current; // Update tail if removing last element

                    count--;
                }
                else
                {
                    current = current.Next;
                }
            }
        }

        public int Count => count;

        public bool IsEmpty => count == 0;

        public T First => head != null ? head.Data : throw new InvalidOperationException("List is empty");
        public T? FirstOrDefault => head != null ? head.Data : default;
        public bool Remove(T item)
        {
            var current = head;

            while (current != null)
            {
                if (EqualityComparer<T>.Default.Equals(current.Data, item))
                {
                    if (current.Previous != null)
                    {
                        current.Previous.Next = current.Next;
                    }
                    else
                    {
                        head = current.Next;
                    }

                    if (current.Next != null)
                    {
                        current.Next.Previous = current.Previous;
                    }
                    else
                    {
                        tail = current.Previous;
                    }

                    count--;
                    return true;
                }

                current = current.Next;
            }

            return false;
        }

        public T Last => tail != null ? tail.Data : throw new InvalidOperationException("List is empty");

        public void Clear()
        {
            head = null;
            tail = null;
            count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var current = head;
            while (current != null)
            {
                yield return current.Data;
                current = current.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}