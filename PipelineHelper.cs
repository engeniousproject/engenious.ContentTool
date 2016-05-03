using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using engenious.Content.Pipeline;
using System.Linq.Expressions;

namespace ContentTool
{
    public static class PipelineHelper
    {
        private static IList<Type> importers;
        private static Dictionary<string, Type> processors = new Dictionary<string, Type>();

        private static List<KeyValuePair<Type, string>> processorsByType = new List<KeyValuePair<Type, string>>();

        private static List<Assembly> assemblies = new List<Assembly>();
        public static void DefaultInit()
        {
            assemblies.Clear();
            assemblies.Add(Assembly.GetExecutingAssembly());
            assemblies.Add(typeof(IContentImporter).Assembly);
            ListImporters();
            ListProcessors();
        }
        public static void PreBuilt(ContentProject currentProject)
        {
            if (currentProject == null)
                return;
            assemblies.Clear();
            assemblies.Add(Assembly.GetExecutingAssembly());
            assemblies.Add(typeof(IContentImporter).Assembly);
            if (currentProject.References == null)
                currentProject.References = new List<string>();
            foreach (string reference in currentProject.References)
            {
                try
                {
                    if (System.IO.File.Exists(reference))
                        assemblies.Add(Assembly.LoadFile(reference));
                }
                catch
                {
                }
            }
            ListImporters();
            ListProcessors();
        }

        private static void ListImporters()
        {
            importers = enumerateImporters().ToList();
        }
        public static List<string> GetProcessors(Type tp)
        {
            List<string> fitting = new List<string>();
            foreach(var pair in processorsByType)
            {
                if (pair.Key.IsAssignableFrom(tp))
                {
                    fitting.Add(pair.Value);
                }
            }
            return fitting;
        }
        private static void ListProcessors()
        {
            processors.Clear();
            processorsByType.Clear();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {

                    if (typeof(IContentProcessor).IsAssignableFrom(type) && !(type.IsAbstract || type.IsInterface))
                    {
                        if (!processors.ContainsKey(type.Name))
                            processors.Add(type.Name, type);

                        Type baseType = GetProcessorInputType(type);
                        processorsByType.Add(new KeyValuePair<Type, string>(baseType, type.Name));
                    }
                }
            }
        }
        public static Type GetImporterOutputType(string extension)
        {
            var tp = GetImporterType(extension);
            var prop = tp.GetProperty("ExportType", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (prop == null)
                return typeof(object);
            var method = prop.GetGetMethod();
            var call = Expression.Call(method);
            var lambda = Expression.Lambda<Func<Type>>(call);
            var func = lambda.Compile();
            return func();
        }
        public static Type GetProcessorInputType(Type tp)
        {
            var prop = tp.GetProperty("ImportType", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (prop == null)
                return typeof(object);
            var method = prop.GetGetMethod();
            var call = Expression.Call(method);
            var lambda = Expression.Lambda<Func<Type>>(call);
            var func = lambda.Compile();
            return func();
        }
        public static Type GetImporterType(string extension)
        {
            foreach (var type in importers)
            {
                var attribute = (ContentImporterAttribute)type.GetCustomAttributes(typeof(ContentImporterAttribute), true).First();
                if (attribute.FileExtensions.Contains(extension))
                    return type;
            }
            return null;
        }
        public static IContentImporter CreateImporter(string extension)
        {
            foreach (var type in importers)
            {
                var attribute = (ContentImporterAttribute)type.GetCustomAttributes(typeof(ContentImporterAttribute), true).First();
                if (attribute.FileExtensions.Contains(extension))
                    return (IContentImporter)Activator.CreateInstance(type);
            }
            return null;
        }

        public static IContentProcessor CreateProcessor(Type importerType, string processorName)
        {
            Type type = null;
            if (!string.IsNullOrEmpty(processorName) && processors.TryGetValue(processorName, out type))
                return (IContentProcessor)Activator.CreateInstance(type);

            var attribute = (ContentImporterAttribute)importerType.GetCustomAttributes(typeof(ContentImporterAttribute), true).First();
            if (processors.TryGetValue(attribute.DefaultProcessor, out type))
                return (IContentProcessor)Activator.CreateInstance(type);

            return null;
        }


        private static TValue GetAttributeValue<TAttribute, TValue>(
            this Type type,
            Func<TAttribute, TValue> valueSelector)
            where TAttribute : Attribute
        {
            var att = type.GetCustomAttributes(
                          typeof(TAttribute), true
                      ).FirstOrDefault() as TAttribute;
            if (att != null)
            {
                return valueSelector(att);
            }
            return default(TValue);
        }

        private static IEnumerable<Type> enumerateImporters()
        {
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IContentImporter).IsAssignableFrom(type))
                    {
                        if (type.IsValueType || type.IsInterface || type.IsAbstract || type.ContainsGenericParameters || type.GetConstructor(
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                null, Type.EmptyTypes, null) == null)
                            continue;

                        var importerAttribute = (ContentImporterAttribute)System.Attribute.GetCustomAttributes(type, typeof(ContentImporterAttribute)).FirstOrDefault();
                        if (importerAttribute != null)
                        {
                            yield return type;
                        }
                    }
                }
            }

            yield break;
        }
    }
}

