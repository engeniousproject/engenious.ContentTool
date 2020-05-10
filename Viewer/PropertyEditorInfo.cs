using System;

namespace engenious.ContentTool.Viewer
{
    
    [AttributeUsage(AttributeTargets.Class)]
    public class PropertyEditorInfo : Attribute
    {
        public Type Type { get; }

        public PropertyEditorInfo(Type type)
        {
            Type = type;
        }
    }
}