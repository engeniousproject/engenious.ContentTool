using System;

namespace ContentTool
{
    [System.Xml.Serialization.XmlInclude(typeof(ContentFolder))]
    [System.Xml.Serialization.XmlInclude(typeof(ContentFile))]
    public abstract class ContentItem : System.ComponentModel.INotifyPropertyChanged,System.Collections.Specialized.INotifyCollectionChanged
    {
        
        public ContentItem(ContentItem parent = null)
        {
            this.Parent = parent;
        }

        private string name;

        public virtual string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Name"));
                }
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public virtual ContentItem Parent{ get; internal set; }

        public string getPath()
        {
            if (this is ContentProject || this.Parent == null)
                return "";
            if (this.Parent is ContentProject)
                return this.Name;
            return System.IO.Path.Combine(this.Parent.getPath(), this.Name);
        }

        #region INotifyPropertyChanged implementation

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region INotifyCollectionChanged implementation

        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        protected void InvokePropertyChange(object sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(sender, args);
        }

        protected void InvokeCollectionChange(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(sender, args);
        }
    }
}

