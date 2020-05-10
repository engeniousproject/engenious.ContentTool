using System;

namespace engenious.ContentTool.Viewer
{
    public interface IPropertyEditor
    {
        event EventHandler PropertyChanged;
    }
    public interface IPropertyEditor<T> : IPropertyEditor
    {
        T Property { get; set; }
        
        
    }
}