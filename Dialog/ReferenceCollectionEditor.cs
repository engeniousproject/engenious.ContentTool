using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Windows.Forms.Design;
using ContentTool.Items;

namespace ContentTool.Dialog
{
    class ReferenceCollectionEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            if (context == null || context.Instance == null)
                return base.GetEditStyle(context);

            return UITypeEditorEditStyle.Modal;
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService editorService;

            if (context == null || context.Instance == null||!(context.Instance is ContentProject) || provider == null)
                return value;

            editorService = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            
            frmEditReferences CollectionEditor = new frmEditReferences();

            CollectionEditor.RootDir = System.IO.Path.GetDirectoryName((context.Instance as ContentProject).File);
            CollectionEditor.References = value as List<string>;
            if (editorService.ShowDialog(CollectionEditor) == System.Windows.Forms.DialogResult.OK)
                return CollectionEditor.References;

            return value;
            //return base.EditValue(context, provider, value);
        }
    }
}
