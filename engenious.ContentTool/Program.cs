using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using engenious.Content.Pipeline;
using engenious.ContentTool.Builder;
using engenious.ContentTool.Forms;
using engenious.ContentTool.Models;
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
            //Console.WriteLine(@"D:\Projects\engenious\Sample\Content\simple.glsl(13) : error C2143: syntax error : missing';' before '}'");


            var arguments = new Arguments();
            arguments.ParseArguments(args);

            var hidden = arguments.Hidden;
            //TODO implement CommandLine

            //var mainShell = new MainShell();

            if (!hidden)
            {
                var execPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                var pluginsPath = Path.Combine(execPath ?? string.Empty, "Plugins");

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
                            var assembly = Assembly.LoadFile(assemblyPath);
                            var shellFactoryAttribute = assembly.GetCustomAttribute<ShellFactoryAttribute>();
                            if (shellFactoryAttribute == null)
                                continue;
                            factories.Add(
                                (IShellFactory) Activator.CreateInstance(shellFactoryAttribute.ShellFactoryType));
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine(ex.Message);
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
                        var mainShellPresenter = new MainShellPresenter(mainShell, promptShell, arguments);
                        mainShell.Run();
                    }
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
                
                var builder = new ContentBuilder(project);

                builder.BuildMessage += eventArgs =>
                {
                    if (eventArgs.MessageType != BuildMessageEventArgs.BuildMessageType.Information)
                        Console.Error.WriteLine(eventArgs.Message);
                };

                switch (arguments.BuildAction)
                {
                    case BuildAction.Clean:
                        builder.Clean();
                        break;
                    case BuildAction.Build:
                        builder.Build();
                        break;
                    case BuildAction.Rebuild:
                        builder.Rebuild();
                        break;
                }
                
                builder.Join();
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        }
    }
}