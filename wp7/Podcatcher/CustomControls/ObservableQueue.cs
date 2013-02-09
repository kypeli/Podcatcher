/**
 * Copyright (c) 2012, 2013, Johan Paul <johan@paul.fi>
 * All rights reserved.
 * 
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */



using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Podcatcher.CustomControls
{
    public class ObservableQueue<T> : INotifyCollectionChanged, IEnumerable<T>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private Queue<T> queue = new Queue<T>();

        public void Enqueue(T item)
        {
            queue.Enqueue(item);
            if (CollectionChanged != null)
                CollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, item, (queue.Count - 1)));
        }

        public T Dequeue()
        {
            if (queue.Count == 0)
            {
                return default(T);
            }

            var item = queue.Dequeue();
            if (CollectionChanged != null)
                CollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, item, 0));
            return item;
        }

        public T Peek()
        {
            if (queue.Count == 0)
            {
                return default(T);
            }

            var item = queue.Peek();
            return item;
        }

        public int Count
        {
            get
            {
                return queue.Count;
            }
        }

        public void Clear()
        {
            queue.Clear();
            if (CollectionChanged != null)
                CollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset));
        }

        public void RemoveItem(T item)
        {
            bool found = false;
            int i = 0;
            int j = 0;
            List<T> items = new List<T>();

            foreach (T it in queue)
            {
                if (it.Equals(item))
                {
                    found = true;
                    j = i;
                    continue;
                }

                items.Insert(i, it);
                i++;
            }

            queue = new Queue<T>(items);

            if (found)
            {
                if (CollectionChanged != null)
                    CollectionChanged(this,
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove, item, j));
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }        
    }

}
