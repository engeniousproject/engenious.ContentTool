using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Linq;

namespace ContentTool
{
    [Serializable()]
    public class ContentFolder : ContentItem
    {
        public ContentFolder(ContentFolder parent = null)
            :base(parent)
        {
            this.Contents = new ContentItemCollection();
            this.Contents.CollectionChanged += Contents_CollectionChanged;
            this.Contents.PropertyChanged += Contents_PropertyChanged;
        }

        public ContentFolder(string name, ContentFolder parent = null)
            :this(parent)
        {
            this.Name = name;
        }

        void Contents_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            InvokePropertyChange(sender, e);
        }

        void Contents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            InvokeCollectionChange(sender, e);
        }

        public ContentItem GetElement(string path)
        {
            string trailingPath=null;
            int ind = path.IndexOf("/", StringComparison.Ordinal);
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
                        return ((ContentFolder)c).GetElement(trailingPath);
                    else
                        return c;
                }
            }
            return null;
        }

        [Browsable(false)]
        public ContentItemCollection Contents{ get; set; }

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

        public ContentFolder CreateTreeFolderStructure(string path)
        {
            if (string.IsNullOrEmpty(path))
                return this;
            int dInd = path.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, Path.PathSeparator, Path.VolumeSeparatorChar });
            string remainingPath = null;
            if (dInd != -1)
            {
                remainingPath = path.Substring(dInd + 1);
                path = path.Substring(0, dInd);
            }
            var newFolder = (GetElement(path) as ContentFolder) ?? new ContentFolder(path, this);

            Contents.Add(newFolder);
            if (!string.IsNullOrEmpty(remainingPath))
                return newFolder.CreateTreeFolderStructure(remainingPath);
            return newFolder;

        }

        public void AddSubFolder(string sourcePath, string destinationPath)
        {
            var fn = new DirectoryInfo(sourcePath).Name;
            ContentFolder f = GetElement(fn) as ContentFolder ?? new ContentFolder(fn,this);
            Contents.Add(f);
            if (sourcePath != destinationPath)
            {
                Directory.CreateDirectory(destinationPath);
            }

            foreach (var dir in Directory.EnumerateDirectories(sourcePath))
            {
                var bar = new DirectoryInfo(dir).Name;
                f.AddSubFolder(dir, Path.Combine(destinationPath, bar));
            }

            foreach (var file in Directory.EnumerateFiles(sourcePath))
            {
                f.AddFile( file, Path.Combine(destinationPath,new FileInfo(file).Name));
            }
        }

        public void AddFile(string sourcePath, string destinationPath)
        {
            if (sourcePath != destinationPath)
            {
                File.Copy(sourcePath, destinationPath, true); //TODO ask user
            }
            Contents.Add(new ContentFile(Path.GetFileName(destinationPath), this));
        }
    }
}

