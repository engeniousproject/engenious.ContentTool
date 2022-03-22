using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using engenious.Content.CodeGenerator;
using engenious.Content.Models;
using engenious.Content.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace engenious.ContentTool.SourceGen
{
    [Generator]
    public class SourceGen : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            // if (!Debugger.IsAttached)
            // {
            //     Debugger.Launch();
            // }
            //
            // SpinWait.SpinUntil(() => Debugger.IsAttached);

            Console.WriteLine("Start building");
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var cps = context.GetMSBuildItems("EngeniousContentReference");
            foreach (var cp in cps)
            {
                context.ReportDiagnostic(Diagnostic.Create("ECP01", "ContentSourceGen", $"Trying to build content project: {cp}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;
                BuildContentProjectSources(context, cp);
            }
        }

        private static void BuildContentProjectSources(GeneratorExecutionContext context, string? cp)
        {
            if (cp == null || !File.Exists(cp))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create("ECP02", "ContentSourceGen", $"Load content project: {cp}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;

            
            var p = ContentProject.Load(cp);

            var cacheFile = Path.Combine(Path.GetDirectoryName(p.ContentProjectPath), "obj", p.Configuration,
                p.Name + ".CreatedCode.dat");

            var contentCode = CreatedContentCode.Load(cacheFile, Guid.Empty);
            
            context.ReportDiagnostic(Diagnostic.Create("ECP03", "ContentSourceGen", $"Loaded content code: {cacheFile} with {contentCode.FileDefinitions.Count()} file definitions.", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;

            foreach(var f in contentCode.FileDefinitions)
            {
                            
                context.ReportDiagnostic(Diagnostic.Create("ECP04", "ContentSourceGen", $"Create code for file definition: {f.Name}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;

                var codeBuilder = new StringCodeBuilder();
                f.WriteTo(codeBuilder);
                context.AddSource(f.Name.Replace('/', '_'), codeBuilder.ToString());
            }
            //contentCode.WriteCode(Path.Combine(Path.GetDirectoryName(cacheFile), "ResultingCode"));
        }
    }
}