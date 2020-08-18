using System.ComponentModel;

namespace engenious.ContentTool.Avalonia
{
    public interface IPropertyEditor : INotifyPropertyChanged
    {
        public bool IsReadOnly { get; }
        
        PropertyView Property { get; set; }
        
        object Value { get; set; }

        public object ConvertFromEditorToProperty(object editorValue);
        
        public object ConvertFromPropertyToEditor(object propertyValue);
    }
}