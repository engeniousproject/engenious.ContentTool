using System;
using System.Collections.Generic;
using System.Linq;

namespace engenious.ContentTool.Viewer
{
    public class DictionaryPropertyProvider : IPropertyProvider
    {
        public DictionaryPropertyProvider()
        {
            Properties = new Dictionary<string, object>();
        }
        public Dictionary<string, object> Properties { get; }
        public void SetValue<T>(string propertyName, T value)
            where T : class
        {
            Properties[propertyName] = value;
        }

        public T GetValue<T>(string propertyName)
            where T : class
        {
            return Properties[propertyName] as T;
        }

        public IEnumerable<string> GetPropertyNames(Type type)
        {
            return Properties.Where((k, v) => v.GetType().IsAssignableFrom(type)).Select((k) => k.Key);
        }
    }
}