using System;
using System.ComponentModel;

namespace engenious.ContentTool.Avalonia
{
    public class ComplexPropertyEditor<T> : PropertyEditorBase
    {
        public ComplexPropertyEditor()
        {
            
        }
        
        public override PropertyViewBase CreatePropertyView(ComplexPropertyView parent, string name)
        {
            return new ComplexPropertyView(parent, name, typeof(T));
        }
    }
}