using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace System.Collections.Generic
{
    
    public class ObservableList<T> :  INotifyCollectionChanged, IList<T>, INotifyPropertyChanged
    {
        private List<T> list;

        public ObservableList()
        {
            list = new List<T>();
        }

        public ObservableList(int capacity)
        {
            list = new List<T>(capacity);
        }

        public ObservableList(IEnumerable<T> collection)
        {
            list = new List<T>(collection);
        }



        private void RemoveChangedEvents(T item)
        {
            RemoveCollectionChanged(item as INotifyCollectionChanged);
            RemovePropertyChanged(item as INotifyPropertyChanged);
        }

        private void AddChangedEvents(T item)
        {
            AddCollectionChanged(item as INotifyCollectionChanged);
            AddPropertyChanged(item as INotifyPropertyChanged);
        }

        private void RemovePropertyChanged(INotifyPropertyChanged item)
        {
            if (item == null)
                return;

            item.PropertyChanged -= Item_PropertyChanged;
        }

        private void AddPropertyChanged(INotifyPropertyChanged item)
        {
            if (item == null)
                return;

            item.PropertyChanged += Item_PropertyChanged;
        }

        private void AddCollectionChanged(INotifyCollectionChanged item)
        {
            if (item == null)
                return;

            item.CollectionChanged += Item_CollectionChanged;
        }

        private void RemoveCollectionChanged(INotifyCollectionChanged item)
        {
            if (item == null)
                return;

            item.CollectionChanged -= Item_CollectionChanged;
        }

        void Item_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(sender, e);
        }

        void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(sender, e);
        }

        #region IList implementation

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }



        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            AddChangedEvents(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void RemoveAt(int index)
        {
            T old = list[index];
            list.RemoveAt(index);
            AddChangedEvents(old);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, old));
        }

        public T this [int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                T old = list[index];
                list[index] = value;
                RemoveChangedEvents(old);
                AddChangedEvents(value);
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, old));
            }
        }

        #endregion

        #region ICollection implementation

        public void Add(T item)
        {
            list.Add(item);
            AddChangedEvents(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Clear()
        {
            list.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            bool removed = list.Remove(item);
            AddChangedEvents(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return removed;
        }

        public int Count
        {
            get
            {
                return list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region IEnumerable implementation

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion



        #region INotifyCollectionChanged implementation

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}

