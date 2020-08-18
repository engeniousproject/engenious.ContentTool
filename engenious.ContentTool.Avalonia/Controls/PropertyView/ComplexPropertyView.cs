using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using DynamicData.Binding;

namespace engenious.ContentTool.Avalonia
{
    public class ComplexPropertyView : PropertyViewBase, IObservableCollection<PropertyViewBase>
    {
        private readonly ObservableCollection<PropertyViewBase> _properties;

        public ComplexPropertyView(ComplexPropertyView parent, string name, Type type) : base(parent, name, type)
        {
            _properties = new ObservableCollection<PropertyViewBase>();
        }

        public void BuildTree(int maxDepth)
        {
            if (maxDepth == 0)
                return;
            maxDepth--;
            foreach (var prop in Type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.GetIndexParameters().Length != 0)
                    continue;

                if (!(prop.GetCustomAttribute<BrowsableAttribute>()?.Browsable ?? true))
                    continue;
                
                var propEditor = PropertyEditorCache.CreatePropertyEditor(prop.PropertyType);
                if (propEditor != null)
                {
                    var view = new PropertyView(this, prop.Name, propEditor);
                    propEditor.Property = view;
                    view.BuildTree();
                    _properties.Add(view);
                }
                else if(maxDepth > 1)
                {
                    var view = new ComplexPropertyView(this, prop.Name, prop.PropertyType);
                    view.BuildTree(maxDepth);
                    _properties.Add(view);
                }
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _properties.CollectionChanged += value;
            remove => _properties.CollectionChanged -= value;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => ((INotifyPropertyChanged) _properties).PropertyChanged += value;
            remove => ((INotifyPropertyChanged) _properties).PropertyChanged -= value;
        }

        public IEnumerator<PropertyViewBase> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _properties).GetEnumerator();
        }

        public void Add(PropertyViewBase item)
        {
            _properties.Add(item);
        }

        public void Clear()
        {
            _properties.Clear();
        }

        public bool Contains(PropertyViewBase item)
        {
            return _properties.Contains(item);
        }

        public void CopyTo(PropertyViewBase[] array, int arrayIndex)
        {
            _properties.CopyTo(array, arrayIndex);
        }

        public bool Remove(PropertyViewBase item)
        {
            return _properties.Remove(item);
        }

        public int Count => _properties.Count;

        public bool IsReadOnly => ((ICollection<PropertyViewBase>) _properties).IsReadOnly;

        public int IndexOf(PropertyViewBase item)
        {
            return _properties.IndexOf(item);
        }

        public void Insert(int index, PropertyViewBase item)
        {
            _properties.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _properties.RemoveAt(index);
        }

        public PropertyViewBase this[int index]
        {
            get => _properties[index];
            set => _properties[index] = value;
        }

        public IDisposable SuspendNotifications()
        {
            return null;
            //return _properties.;
        }

        public IDisposable SuspendCount()
        {
            return null;
            //return _observableCollectionImplementation.SuspendCount();
        }

        public void Move(int oldIndex, int newIndex)
        {
            //_observableCollectionImplementation.Move(oldIndex, newIndex);
        }

        public void Load(IEnumerable<PropertyViewBase> items)
        {
            //_observableCollectionImplementation.Load(items);
        }
    }
}