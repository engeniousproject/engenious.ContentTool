using System;
using System.Collections.Generic;

namespace ContentTool
{
    [Serializable()]
    public class ContentFolder : ContentItem
    {
        public ContentFolder()
        {
            this.Contents = new ObservableList<ContentItem>();
            this.Contents.CollectionChanged += Contents_CollectionChanged;
            this.Contents.PropertyChanged += Contents_PropertyChanged;
        }

        public ContentFolder(string name, ContentItem parent = null)
            : base(parent)
        {
            this.Name = name;
            this.Contents = new ObservableList<ContentItem>();
            this.Contents.CollectionChanged += Contents_CollectionChanged;
            this.Contents.PropertyChanged += Contents_PropertyChanged;
        }

        void Contents_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            InvokePropertyChange(sender, e);
        }

        void Contents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InvokeCollectionChange(sender, e);
        }

        [System.ComponentModel.Browsable(false)]
        public ObservableList<ContentItem> Contents{ get; set; }

        public override string ToString()
        {
            return Name;
        }


    }
}

