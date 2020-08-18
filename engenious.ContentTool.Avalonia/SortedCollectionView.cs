using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace engenious.ContentTool.Avalonia
{
    public class SortedCollectionView<T> : IEnumerable<T>, INotifyPropertyChanged, INotifyCollectionChanged
    {
        private readonly IComparer<T> _comparer;
        private readonly ObservableCollection<T> _collection;
        private readonly List<int> _indices;
        
        
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public SortedCollectionView(ObservableCollection<T> collection, IComparer<T> comparer)
        {
            _collection = collection;
            _indices = new List<int>(Enumerable.Range(0, _collection.Count));
            _comparer = comparer;

            if (collection is INotifyPropertyChanged propertyChanged)
            {
                propertyChanged.PropertyChanged += PropertyChangedOnPropertyChanged;
            }

            Resort();
            
            _collection.CollectionChanged += CollectionOnCollectionChanged;
        }

        private void PropertyChangedOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void CollectionOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _indices.Clear();
            _indices.AddRange(Enumerable.Range(0, _collection.Count));
        }


        private void Resort()
        {
            _indices.Sort(Compare);
        }

        private int Compare(int a, int b)
        {
            return _comparer.Compare(_collection[a], _collection[b]);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly SortedCollectionView<T> _collectionView;
            private List<int>.Enumerator _enumerator;

            internal Enumerator(SortedCollectionView<T> collectionView)
            {
                _collectionView = collectionView;
                _enumerator = collectionView._indices.GetEnumerator();
                Current = default;
            }
            public bool MoveNext()
            {
                if (!_enumerator.MoveNext())
                    return false;

                Current = _collectionView._collection[_enumerator.Current];
                return true;
            }

            public void Reset()
            {
                ((IEnumerator<int>)_enumerator).Reset();
            }

            public T Current { get; private set; }

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}