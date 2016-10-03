using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentTool
{
    public class ContentItemCollection : INotifyCollectionChanged, IList<ContentItem>, INotifyPropertyChanged
    {
        ObservableList<ContentItem> contents = new ObservableList<ContentItem>();

        public ContentItemCollection()
        {
            contents.PropertyChanged += (sender, args) => PropertyChanged?.Invoke(sender, args);
            contents.CollectionChanged += (sender, args) => CollectionChanged?.Invoke(sender, args);
        }

        public ContentItem this[int index]
        {
            get { return contents[index]; }

            set { contents[index] = value; }
        }

        public int Count
        {
            get { return contents.Count; }
        }

        public bool IsReadOnly
        {
            get { return contents.IsReadOnly; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Add(ContentItem item)
        {
            if (!contents.Contains(item))
                contents.Add(item);
        }

        public void Clear()
        {
            contents.Clear();
        }

        public bool Contains(ContentItem item)
        {
            return contents.Contains(item);
        }

        public void CopyTo(ContentItem[] array, int arrayIndex)
        {
            contents.CopyTo(array, arrayIndex);
        }

        public IEnumerator<ContentItem> GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        public int IndexOf(ContentItem item)
        {
            return contents.IndexOf(item);
        }

        public void Insert(int index, ContentItem item)
        {
            if (!contents.Contains(item))
                contents.Insert(index,item);
        }

        public bool Remove(ContentItem item)
        {
            return contents.Remove(item);
        }

        public void RemoveAt(int index)
        {
            contents.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
