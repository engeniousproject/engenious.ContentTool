using System;

namespace ContentTool
{
    [Serializable()]
    public class ContentFile:ContentItem
    {
        public ContentFile()
            : base()
        {
            
        }

        public ContentFile(string name, ContentItem parent = null)
            : base(parent)
        {
            this.Name = name;
        }

        public string Processor{ get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}

