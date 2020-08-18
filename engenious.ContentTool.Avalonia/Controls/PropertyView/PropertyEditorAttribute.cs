using System;

namespace engenious.ContentTool.Avalonia
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class PropertyEditorAttribute : Attribute
    {
        public PropertyEditorAttribute(Type type)
        {
            Type = type;
        }
        
        public Type Type { get; }
    }
}