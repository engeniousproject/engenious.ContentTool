using System;
using engenious.ContentTool.Builder;

namespace engenious.ContentTool
{
    
    public class Arguments
    {
        public string OutputDirectory{get;private set;}
        public string ContentProject{get;private set;}
        public bool Hidden{get;private set;}
        public string ReadProjectProperty { get; private set; }
        public string Configuration{ get; set; }
        public BuildAction BuildAction { get; set; }
        public Arguments()
        {
            Configuration = null;
            OutputDirectory = null;
            ReadProjectProperty = null;
            //ProjectDir = Environment.CurrentDirectory;
            BuildAction = BuildAction.Build;
        }
        private static string ParsePath(string dir)
        {
            if (dir.Length >= 2)
            {
                if (dir[0] == '"')
                    dir = dir.Substring(1);
                if (dir[dir.Length-1] == '"')
                    dir = dir.Substring(0,dir.Length-1);
                return dir;
            }
            return null;
        }
        public void ParseArguments(string[] args)
        {
            foreach(var arg in args)
            {
                if (arg.StartsWith("/hidden:"))
                {
                    if (bool.TryParse(arg.Substring("/hidden:".Length),out var hidden))
                        Hidden = hidden;
                }else if(arg.StartsWith("/outputDir:"))
                {
                    var dir = arg.Substring("/outputDir:".Length).Trim();
                    OutputDirectory = ParsePath(dir);
                }else if(arg.StartsWith("/@:"))
                {
                    var dir = arg.Substring("/@:".Length).Trim();
                    ContentProject = ParsePath(dir);
                }
                else if(arg.StartsWith("/configuration:"))
                {
                    Configuration = arg.Substring("/configuration:".Length);//TODO: back to enum?
                    //Configuration configuration;
                    //if (Enum.TryParse(arg.Substring("/configuration:".Length),out configuration))
                    //    Configuration = configuration;
                }
                else if (arg.StartsWith("/readProperty:"))
                {
                    ReadProjectProperty = arg.Substring("/readProperty:".Length);
                }
                else if (arg.StartsWith("/clean"))
                {
                    BuildAction = BuildAction.Clean;
                }
                else if (arg.StartsWith("/rebuild"))
                {
                    BuildAction = BuildAction.Rebuild;
                }
                else if (arg.StartsWith("/help"))
                {
                    PrintHelp();
                }
            }
        }

        /// <summary>
        /// Prints the usage options for this <see cref="Arguments"/> parser.
        /// </summary>
        public void PrintHelp()
        {
            Console.WriteLine("Usage: ContentTool [arguments...]");
            Console.WriteLine("Arguments:");
            Console.WriteLine("    /hidden:[True|False]           Whether the program shall be executed in background or not.(Without a UI)");
            Console.WriteLine("    /outputDir:[directory path]    The output path for the compiled results.");
            Console.WriteLine("    /@:[content project file]      The content project file to compile/open.");
            Console.WriteLine("    /configuration:[Debug|Release] The configuration to build the project with.");
            Console.WriteLine("    /readProperty:[Property Names - see ContentProject.cs in source]\tThe property to parse and read from the content file and output on stdout."); // TODO: create list of possible values
            Console.WriteLine("    /clean                         Cleans the build of the content project.");
            Console.WriteLine("    /rebuild                       Rebuilds the content project(same as clean and build in succession).");
            Console.WriteLine("    /build                         Builds the content project.");
            Console.WriteLine("    /help                          Shows this help.");
        }
    }
}

