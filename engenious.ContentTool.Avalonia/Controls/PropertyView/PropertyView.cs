using System.Reflection;

namespace engenious.ContentTool.Avalonia
{
    public class PropertyView : PropertyViewBase
    {
        public IPropertyEditor PropertyEditor { get; }

        public PropertyView(ComplexPropertyView parent, string name, IPropertyEditor propertyEditor)
            : base(parent, name, parent.Type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.PropertyType)
        {
            PropertyEditor = propertyEditor;
        }
        
    }
}