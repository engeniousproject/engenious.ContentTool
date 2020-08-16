using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using engenious.ContentTool.Models;

namespace engenious.ContentTool.Controls
{
    internal class ImporterNameDropDown : StringConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return (context?.Instance is ContentFile);
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {     
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            if (!(context?.Instance is ContentFile))
                return null;
            var file = (ContentFile) context.Instance;

            var ext = Path.GetExtension(file.Name);
            
            return new StandardValuesCollection(PipelineHelper.GetImporters(ext));
            
        }
    }
}
