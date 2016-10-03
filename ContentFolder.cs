using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

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

        public ContentFolder(string name, ContentFolder parent = null)
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

        public ContentItem getElement(string path)
        {
            string trailingPath=null;
            int ind = path.IndexOf("/");
            if (ind != -1)
            {
                trailingPath = path.Substring(ind+1);
                path = path.Substring(0,ind);
            }
            foreach(var c in Contents)
            {
                if (c.Name == path)
                {
                    if (c is ContentFolder && trailingPath != null)
                        return ((ContentFolder)c).getElement(trailingPath);
                    else
                        return c;
                }
            }
            return null;
        }

        [System.ComponentModel.Browsable(false)]
        public ObservableList<ContentItem> Contents{ get; set; }

        public override string ToString()
        {
            return Name;
        }


        #region implemented abstract members of ContentItem

        public override void ReadItem(XmlElement node)
        {
            switch (node.Name)
            {
                case "Name":
                    Name = node.ChildNodes.OfType<XmlText>().FirstOrDefault()?.InnerText;
                    break;
                case "Contents":
                    foreach(var child in node.ChildNodes.OfType<XmlElement>())
                    {
                        ContentItem item;
                        switch(child.Name)
                        {
                            case "ContentFolder":
                                item = new ContentFolder("",this);
                                break;
                            case "ContentFile":
                                item = new ContentFile("",this);
                                break;
                            default:
                                continue;
                        }
                        foreach(var inner in child.ChildNodes.OfType<XmlElement>())
                            item.ReadItem(inner);
                        Contents.Add(item);
                    }
                    break;
            }
        }

        public override void WriteItems(System.Xml.XmlWriter writer)
        {
            writer.WriteElementString("Name", Name);

            writer.WriteStartElement("Contents");
            {
                foreach (var item in Contents)
                {
                    writer.WriteStartElement(item.GetType().Name);
                    item.WriteItems(writer);
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        #endregion
    }
}

