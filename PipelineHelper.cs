using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using engenious.Content.Pipeline;
using System.Linq.Expressions;
using ContentTool.Items;
using engenious.Pipeline.Pipeline.Editors;

namespace ContentTool
{
    public static class PipelineHelper
    {
        private static IList<Type> importers;
        private static List<Type> editors = new List<Type>();
        private static Dictionary<string, ContentEditorWrapper> editorsByType = new Dictionary<string, ContentEditorWrapper>();
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
            ListEditors();
        }

        private static void ListImporters()
        {
            importers = EnumerateImporters().ToList();
        }
        public static List<string> GetProcessors(Type tp)
        {
            List<string> fitting = new List<string>();
            foreach (var pair in processorsByType)
            {
                if (pair.Key.IsAssignableFrom(tp))
                {
                    fitting.Add(pair.Value);
                }
            }
            return fitting;
        }

        public static List<string> GetImporters(string extension)
        {
            List<string> fitting = new List<string>();
            foreach (var type in importers)
            {
                var attribute =
                    (ContentImporterAttribute)type.GetCustomAttributes(typeof(ContentImporterAttribute), true).First();
                if (attribute.FileExtensions != null && attribute.FileExtensions.Contains(extension))
                    fitting.Add(attribute.DisplayName);
            }
            return fitting;
        }

        public static ContentEditorWrapper GetContentEditor(string extension, Type inputType, Type outputType)
        {
            string key = extension + "$" + inputType.FullName + "$" + outputType.FullName;
            ContentEditorWrapper editorWrap = null;
            if (editorsByType.TryGetValue(key, out editorWrap))
                return editorWrap;
            Type genericType = typeof(IContentEditor<,>).MakeGenericType(inputType, outputType);

            foreach (var type in editors)
            {
                var attribute =
                   (ContentEditorAttribute)type.GetCustomAttributes(typeof(ContentEditorAttribute), true).First();
                if (attribute == null)
                    continue;
                if (attribute.SupportedFileExtensions.Contains(extension) && genericType.IsAssignableFrom(type))
                {
                    IContentEditor editor = (IContentEditor)Activator.CreateInstance(type);
                    if (editor == null)
                        continue;
                    var methodInfo = genericType.GetMethod("Open");
                    if (methodInfo == null)
                        continue;
                    var inputOArg = Expression.Parameter(typeof (object));
                    var outputOArg = Expression.Parameter(typeof (object));


                    var openMethod = Expression.Lambda<Action<object, object>>(
                        Expression.Call(Expression.Constant(editor), methodInfo,
                            Expression.Convert(inputOArg, inputType), Expression.Convert(outputOArg, outputType)),
                        inputOArg, outputOArg).Compile();

                    editorWrap = new ContentEditorWrapper(editor,openMethod);
                    foreach (var ext in attribute.SupportedFileExtensions)
                        editorsByType[ext + "$" + inputType.FullName + "$" + outputType.FullName] = editorWrap;
                    
                }
            }

            return editorWrap;
            //editorsByType[key] = 
        }

        private static void ListEditors()
        {
            editors.Clear();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IContentEditor).IsAssignableFrom(type) && !(type.IsAbstract || type.IsInterface))
                    {
                        var attribute =
                   (ContentEditorAttribute)type.GetCustomAttributes(typeof(ContentEditorAttribute), true).First();
                        if (attribute == null)
                            continue;
                        editors.Add(type);
                    }
                }
            }
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
        public static Type GetImporterOutputType(string extension, string importerName)
        {
            var tp = GetImporterType(extension, importerName);
            var field = tp.GetField("_exportType", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (field == null)
                return typeof(object);
            var lambda = Expression.Lambda<Func<Type>>(Expression.Field(null, field));
            var func = lambda.Compile();
            return func();
        }
        public static Type GetProcessorInputType(Type tp)
        {
            var field = tp.GetField("_importType", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
            if (field == null)
                return typeof(object);
            var lambda = Expression.Lambda<Func<Type>>(Expression.Field(null, field));
            var func = lambda.Compile();
            return func();
        }
        public static Type GetImporterType(string extension, string importerName)
        {
            foreach (var type in importers)
            {
                var attribute = (ContentImporterAttribute)type.GetCustomAttributes(typeof(ContentImporterAttribute), true).First();
                if (attribute.FileExtensions.Contains(extension) && (importerName == null || attribute.DisplayName == importerName))
                    return type;
            }
            return null;
        }

        public static IContentImporter CreateImporter(string extension, ref string importerName)
        {
            if (importers == null)
                DefaultInit();
            foreach (var type in importers)
            {
                var attribute = (ContentImporterAttribute)type.GetCustomAttributes(typeof(ContentImporterAttribute), true).First();
                if (attribute.FileExtensions != null && attribute.FileExtensions.Contains(extension) &&
                    (importerName == null || attribute.DisplayName == importerName))
                {
                    importerName = attribute.DisplayName;
                    return (IContentImporter)Activator.CreateInstance(type);
                }
            }
            importerName = null;
            return null;
        }
        public static IContentImporter CreateImporter(string extension, string importerName = null)
        {
            string dummy = importerName;
            return CreateImporter(extension, ref importerName);
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

        private static IEnumerable<Type> EnumerateImporters()
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

