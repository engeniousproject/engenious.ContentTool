using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using ContentTool.Items;

namespace ContentTool.Dialog
{
    class ProcessorEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context == null || context.Instance == null)
                return base.GetEditStyle(context);

            return UITypeEditorEditStyle.DropDown;
        }
        private IWindowsFormsEditorService editorService;
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {

            if (context == null || context.Instance == null || !(context.Instance is ContentFile) || provider == null)
                return value;

            editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));

            ListBox lb = new ListBox();
            lb.SelectionMode = SelectionMode.One;
            lb.SelectedValueChanged += OnListBoxSelectedValueChanged;

            var file = context.Instance as ContentFile;

            string ext = System.IO.Path.GetExtension(file.Name);
            var baseType = PipelineHelper.GetImporterOutputType(ext,file.ImporterName);
            lb.Items.AddRange(PipelineHelper.GetProcessors(baseType).ToArray());
            

            editorService.DropDownControl(lb);
            if (lb.SelectedItem == null)
                return value;

            return lb.SelectedItem;
        }

        private void OnListBoxSelectedValueChanged(object sender, EventArgs e)
        {
            editorService.CloseDropDown();
        }
    }
}
