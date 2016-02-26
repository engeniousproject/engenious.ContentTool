using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using engenious.Content.Pipeline;

namespace ContentTool
{
    public static class PipelineHelper
    {


        private static IList<Type> importers;
        private static Dictionary<string,Type> processors = new Dictionary<string,Type>();
        private static List<Assembly> assemblies = new List<Assembly>();

        public static void PreBuilt(ContentProject currentProject)
        {
            assemblies.Clear();
            assemblies.Add(Assembly.GetExecutingAssembly());
            assemblies.Add(typeof(engenious.Content.Pipeline.IContentImporter).Assembly);
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
            ListImporters(currentProject);
            ListProcessors(currentProject);
        }

        private static void ListImporters(ContentProject currentProject)
        {
            importers = enumerateImporters(currentProject).ToList();
        }

        private static void ListProcessors(ContentProject currentProject)
        {
            processors.Clear();
           
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {

                    if (typeof(engenious.Content.Pipeline.IContentProcessor).IsAssignableFrom(type))
                    {
                        if (!processors.ContainsKey(type.Name))
                            processors.Add(type.Name, type);
                        /*IList<Attribute> attributes = type.GetCustomAttributes(typeof(engenious.Content.Pipeline.ContentImporterAttribute)).ToList();
                        if (attributes.Count > 0)
                        {
                            yield return type;
                        }*/
                    }
                }
            }
        }

        public static IContentImporter CreateImporter(string extension)
        {
            foreach (var type in importers)
            {
                var attribute = (engenious.Content.Pipeline.ContentImporterAttribute)type.GetCustomAttributes(typeof(engenious.Content.Pipeline.ContentImporterAttribute), true).First();
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

            var attribute = (engenious.Content.Pipeline.ContentImporterAttribute)importerType.GetCustomAttributes(typeof(engenious.Content.Pipeline.ContentImporterAttribute), true).First();
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

        private static IEnumerable<Type> enumerateImporters(ContentProject currentProject)
        {
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(engenious.Content.Pipeline.IContentImporter).IsAssignableFrom(type))
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

