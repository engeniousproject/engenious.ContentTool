using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace engenious.ContentTool.Avalonia
{
    public class PropertyEditorCache
    {
        private static Dictionary<Type, Func<IPropertyEditor>> _propertyEditors;

        static PropertyEditorCache()
        {
            _propertyEditors = new Dictionary<Type, Func<IPropertyEditor>>();
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attrs = t.GetCustomAttributes<PropertyEditorAttribute>(true);

                foreach (var attr in attrs)
                {
                    if (_propertyEditors.ContainsKey(attr.Type))
                    {
                        throw new Exception($"Multiple editors for single type: {attr.Type}");
                    }

                    var factory =
                        Expression.Lambda<Func<IPropertyEditor>>(
                            Expression.New(t)).Compile();
                
                    _propertyEditors.Add(attr.Type, factory);
                }
            }
        }

        public static IPropertyEditor CreatePropertyEditor(Type type)
        {
            foreach (var (t, factory) in _propertyEditors)
            {
                if (!t.IsAssignableFrom(type))
                    continue;

                return factory();
            }

            return null;
        }
    }
}