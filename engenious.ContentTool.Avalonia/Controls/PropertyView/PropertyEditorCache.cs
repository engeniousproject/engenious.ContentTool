using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

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

        class PropertyDescriptorContext : ITypeDescriptorContext
        {
            public PropertyDescriptorContext(object value)
            {
                Instance = value;
                Container = null;
                PropertyDescriptor = null;
            }
            public object GetService(Type serviceType)
            {
                return null;
            }

            public void OnComponentChanged()
            {
            }

            public bool OnComponentChanging()
            {
                return true;
            }

            public IContainer Container { get; private set; }
            public object Instance { get; }
            public PropertyDescriptor PropertyDescriptor { get; private set; }
        }

        public static IPropertyEditor CreatePropertyEditor([CanBeNull] TypeConverterAttribute propertyTypeConverter, Type type, object parentValue)
        {
            if (propertyTypeConverter != null)
            {
                var tct = Type.GetType(propertyTypeConverter.ConverterTypeName);
                if (tct != null)
                {
                    var tc = (TypeConverter)Activator.CreateInstance(tct);
                    if (tc != null)
                    {
                        var context = new PropertyDescriptorContext(parentValue);
                        if (tc.GetStandardValuesSupported(context))
                        {
                            var standardValues = tc.GetStandardValues(context);
                            if (standardValues != null && standardValues.Count > 0)
                            {
                                var editor = new ChoicePropertyEditor();
                                foreach (var standardValue in standardValues)
                                {
                                    editor.Choices.Add(standardValue);
                                }

                                return editor;
                            }
                        }
                    }
                }
            }
            
            if (_propertyEditors.TryGetValue(type, out var fac))
            {
                return fac();
            }
            
            
            foreach (var (t, factory) in _propertyEditors)
            {
                if (!t.IsAssignableFrom(type))
                    continue;

                return factory();
            }

            var typeConverters = type.GetCustomAttributes<TypeConverterAttribute>(true);
            if (typeConverters.Count() == 1)
            {
                if (_propertyEditors.ContainsKey(type))
                {
                    throw new Exception($"Multiple editors for single type: {type}");
                }

                var factory =
                    Expression.Lambda<Func<IPropertyEditor>>(
                        Expression.New(typeof(ComplexPropertyEditor<>).MakeGenericType(type))).Compile();
                
                _propertyEditors.Add(type, factory);
            }

            return null;
        }
    }
}