using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Matrix.Common.Infrastructure
{
    public class BindingCollection<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        private readonly List<T> list = new List<T>();

        public void Add(T item)
        {
            if (Equals(item, null)) return;

            list.Add(item);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T> { item }));
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (Equals(items, null)) return;

            list.AddRange(items);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(items)));
        }
        public void Remove(T item)
        {
            if (Equals(item, null)) return;

            int index = list.IndexOf(item);
            if(index<0)return;

            list.Remove(item);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T> { item }, index));

        }

        public void Clear()
        {
            list.Clear();
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, e);
        }

        public void Insert(int index, T item)
        {
            if (Equals(item, null)) return;

            list.Insert(index, item);
            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T> { item }, index));
        }

        public int Count
        {
            get { return list.Count; }
        }

        public T this[int index]
        {
            get { return list[index]; }
        }
    }
}
