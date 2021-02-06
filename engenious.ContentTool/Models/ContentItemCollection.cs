using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using engenious.ContentTool.Observer;

namespace engenious.ContentTool.Models
{
    public class ContentItemCollection : ObservableCollection<ContentItem>, INotifyPropertyValueChanged
    {
        public event NotifyPropertyValueChangedHandler PropertyValueChanged;

        protected override void ClearItems()
        {
            foreach (var i in this)
            {
                i.PropertyValueChanged -= Item_OnPropertyValueChanged;
            }
            base.ClearItems();
        }

        protected override void InsertItem(int index, ContentItem item)
        {
            base.InsertItem(index, item);
            if (item != null)
                item.PropertyValueChanged += Item_OnPropertyValueChanged;
        }

        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            base.RemoveItem(index);
            
            oldItem.PropertyValueChanged -= Item_OnPropertyValueChanged;
        }

        protected override void SetItem(int index, ContentItem item)
        {
            var oldItem = this[index];
            base.SetItem(index, item);
            oldItem.PropertyValueChanged -= Item_OnPropertyValueChanged;
            if (item != null)
                item.PropertyValueChanged += Item_OnPropertyValueChanged;
        }

        private void Item_OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            PropertyValueChanged?.Invoke(sender, e);
        }
    }
}
