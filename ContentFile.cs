using System;
using System.Linq;
using System.Xml.Serialization;

namespace ContentTool
{
    [Serializable()]
    public class ContentFile : ContentItem
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
        private static string getProcessor(string name)
        {
            var tp = PipelineHelper.GetImporterType(System.IO.Path.GetExtension(name));
            if (tp != null)
            {
                foreach (var attr in tp.GetCustomAttributes(true).Select(x => x as engenious.Content.Pipeline.ContentImporterAttribute))
                {
                    if (attr == null)
                        continue;
                    return attr.DefaultProcessor;
                }
            }
            return "";
        }
        [XmlIgnore()]
        private string processor;
        [XmlElement(IsNullable = true)]
        [System.ComponentModel.Editor(typeof(Dialog.ProcessorEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Processor {
            get {
                return processor;
            }
            set
            {
                processor = value;
                if (string.IsNullOrWhiteSpace(processor))
                {
                    processor = getProcessor(Name);
                }
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

