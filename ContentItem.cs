using System;
using System.ComponentModel;
using engenious.Pipeline;
using System.Collections.Generic;
using System.Xml;

namespace ContentTool
{
    [System.Xml.Serialization.XmlInclude(typeof(ContentFolder))]
    [System.Xml.Serialization.XmlInclude(typeof(ContentFile))]
    public abstract class ContentItem : System.ComponentModel.INotifyPropertyChanged,System.Collections.Specialized.INotifyCollectionChanged,ICustomTypeDescriptor
    {

        #region ICustomTypeDescriptor implementation

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this,true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this,true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public PropertyDescriptorCollection GetProperties()
        {
            return GetProperties(null);//return TypeDescriptor.GetProperties(this, true);
        }

        public virtual PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(this,attributes,true);
        }
        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion



        public ContentItem(ContentItem parent = null)
        {
            this.Parent = parent;
        }

        private string name;
        [DisplayName("(Name)")]
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
        [System.ComponentModel.Browsable(false)]
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

        public abstract void ReadItem(XmlElement node);
        public abstract void WriteItems(XmlWriter writer);
    }
}

