using System;
using engenious.Content.Serialization;
using engenious.Content;
using engenious.Graphics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using OpenTK;
using OpenTK.Graphics;
using ContentTool.Builder;

namespace ContentTool
{
    class MainClass
    {
        public static void Main(string[] args)
        {

            Arguments arguments = new Arguments();
            arguments.ParseArguments(args);

            if (arguments.Hidden)
            {
                ContentBuilder builder = new ContentBuilder(ContentProject.Load(arguments.ContentProject));
                if(arguments.Configuration.HasValue)
                    builder.Project.Configuration = arguments.Configuration.Value;
                if (arguments.OutputDirectory != null)
                    builder.Project.OutputDir = arguments.OutputDirectory;

                builder.BuildStatusChanged += (sender, buildStep) => {
                    string message = (buildStep & (BuildStep.Build|BuildStep.Clean)).ToString() + " ";
                    bool error=false;
                    if ((buildStep & Builder.BuildStep.Abort) == Builder.BuildStep.Abort)
                    {
                        message += "aborted!";
                        error = true;
                    }else if ((buildStep & Builder.BuildStep.Finished) == Builder.BuildStep.Finished)
                    {
                        message +="finished!";
                    }
                    if (error)
                        Console.Error.WriteLine(message);
                    else
                        Console.WriteLine(message);
                };
                builder.ItemProgress += (sender, e) => {
                    string message = e.Item + " " +(e.BuildStep & (BuildStep.Build|BuildStep.Clean)).ToString().ToLower() + "ing ";

                    bool error=false;
                    if ((e.BuildStep & Builder.BuildStep.Abort) == Builder.BuildStep.Abort)
                    {
                        message += "failed!";
                        error = true;
                    }else if ((e.BuildStep & Builder.BuildStep.Finished) == Builder.BuildStep.Finished)
                    {
                        message +="finished!";
                    }
                    if (error)
                        Console.Error.WriteLine(message);
                    else
                        Console.WriteLine(message);
                };

                builder.Build();

                builder.Join();
            }
            else{
                using (frmMain mainForm = new frmMain()){
                    
                    System.Windows.Forms.Application.Run(mainForm);
                }
            }
        }
    }
}
