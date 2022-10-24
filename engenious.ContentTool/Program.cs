using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using engenious.Content.CodeGenerator;
using engenious.Content.Pipeline;
using engenious.ContentTool.Builder;
using engenious.ContentTool.Forms;
using engenious.Content.Models;
using engenious.ContentTool.Presenters;

namespace engenious.ContentTool
{
    public class AssemblyContext : AssemblyLoadContext
    {
        public Assembly Load(Stream stream)
        {
            this.Resolving += ResolvingHandler;
            return this.LoadFromStream(stream);
        }

        public Assembly ResolvingHandler(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            var assembly = context.LoadFromAssemblyName(assemblyName);
            Console.WriteLine("Resolving: " + assemblyName.FullName);
            return assembly;
        }
    }

    internal static class Program
    {
        [STAThread]
        static async Task<int> Main(string[] args)
        {
            var arguments = new Arguments();
            arguments.ParseArguments(args);

            var hidden = arguments.Hidden;
            //TODO implement CommandLine

            //var mainShell = new MainShell();

            if (!hidden)
            {
                var execPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var pluginsPath = execPath ?? string.Empty;

                var factories = new List<IShellFactory>();
                if (Directory.Exists(pluginsPath))
                {
                    AssemblyLoadContext.Default.Resolving += (context, name) =>
                    {
                        return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(execPath,
                            name.Name + ".dll"));
                    };
                    foreach (var assemblyPath in Directory.EnumerateFiles(pluginsPath, "*.dll",
                        SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                            var shellFactoryAttribute = assembly.GetCustomAttribute<ShellFactoryAttribute>();
                            if (shellFactoryAttribute == null)
                                continue;
                            factories.Add(
                                (IShellFactory) Activator.CreateInstance(shellFactoryAttribute.ShellFactoryType));
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"Loading {Path.GetFileName(assemblyPath)}: " + ex.Message);
                        }
                    }
                }

                var shellFactory = factories.FirstOrDefault();

                if (shellFactory == null)
                {
                    Console.Error.WriteLine("error: Could not open UI. No UI shell found.");
                    hidden = true;
                }
                else
                {
                    ReferenceManager.References.Add(Assembly.GetExecutingAssembly());
                    ReferenceManager.References.Add(shellFactory.GetType().Assembly);


                    var promptShell = shellFactory.CreatePromptShell();

                    using (var mainShell = shellFactory.CreateMainShell())
                    {
                        using var mainShellPresenter = new MainShellPresenter(mainShell, promptShell, arguments);
                        mainShell.Run();
                    }
                    
                    return 0;
                }

            }

            if (!File.Exists(arguments.ContentProject))
            {
                arguments.PrintHelp();
                return -1;
            }
            try
            {
                var project = ContentProject.Load(arguments.ContentProject,true);

                if (arguments.ReadProjectProperty != null)
                {
                    string[] rec = arguments.ReadProjectProperty.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries);
                    object currentNode = project;
                    foreach (var r in rec)
                    {
                        var prop = currentNode.GetType().GetProperty(r);
                        if (prop == null)
                            return 3;

                        currentNode = prop.GetValue(currentNode);
                    }
                    var res = currentNode?.ToString();
                    if (res != null)
                        Console.WriteLine(res);
                    else
                        return 3;
                    
                    return 0;
                }
                
                project.OutputDirectory = arguments.OutputDirectory ?? project.OutputDirectory;
                project.Configuration = arguments.Configuration ?? project.Configuration;
                if (arguments.DependencyAnalyse)
                {
                    foreach (var (item, output) in ContentBuilder.EnumerateContentFiles(project))
                    {
                        Console.WriteLine(item.FilePath + "#" + output);
                        if (item is not ContentFile contentFile)
                            continue;
                        foreach (var d in contentFile.Dependencies)
                        {
                            Console.WriteLine(d + "#" + output);
                        }
                    }
                    return 0;
                }
                using (var builder = new ContentBuilder(project))
                {

                    builder.BuildMessage += eventArgs =>
                    {
                        if (eventArgs.MessageType != BuildMessageEventArgs.BuildMessageType.Information)
                            Console.Error.WriteLine(eventArgs.Message);
                    };

                    ContentBuilder.BuildTask buildTask;
                    switch (arguments.BuildAction)
                    {
                        case BuildAction.Clean:
                            buildTask = builder.Clean();
                            break;
                        case BuildAction.Build:
                            buildTask = builder.Build();
                            break;
                        case BuildAction.Rebuild:
                            buildTask = builder.Rebuild();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    buildTask.CompletionHandle.WaitOne();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"error: {ex.Message}");
                return -1;
            }
        }
    }
}