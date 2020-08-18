using System;
using System.Collections.Generic;

namespace engenious.ContentTool.Viewer
{
    public interface IPropertyProvider
    {
        void SetValue<T>(string propertyName, T value)
            where T : class;

        T GetValue<T>(string propertyName)
            where T : class;

        IEnumerable<string> GetPropertyNames(Type type);
    }
}