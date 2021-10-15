using System;
using System.Diagnostics;
using System.IO;
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
            context.ReportDiagnostic(Diagnostic.Create("asdf", "asdf", "Building shit", DiagnosticSeverity.Warning, DiagnosticSeverity.Info, true,1 ));;
            context.AddSource("smth", "namespace Sample{public struct smth{}}");
            var cps = context.GetMSBuildItems("EngeniousContentReference");
            foreach (var cp in cps)
            {
                context.ReportDiagnostic(Diagnostic.Create("asdf", "asdf", $"Trying to build content project: {cp}", DiagnosticSeverity.Warning, DiagnosticSeverity.Info, true,1 ));;
                BuildContentProjectSources(context, cp);
            }
        }

        private static void BuildContentProjectSources(GeneratorExecutionContext context, string? cp)
        {
            if (cp == null || File.Exists(cp))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create("asdf", "asdf", $"Shit just got real: {cp}", DiagnosticSeverity.Warning, DiagnosticSeverity.Info, true,1 ));;

            
            var p = ContentProject.Load(cp);

            var cacheFile = Path.Combine(Path.GetDirectoryName(p.ContentProjectPath), "obj", p.Configuration,
                p.Name + ".CreatedCode.dat");

            var contentCode = CreatedContentCode.Load(cacheFile, Guid.Empty);
            foreach(var f in contentCode.FileDefinitions)
            {
                var codeBuilder = new StringCodeBuilder();
                f.WriteTo(codeBuilder);
                context.AddSource(f.Name.Replace('/', '_'), codeBuilder.ToString());
            }
            //contentCode.WriteCode(Path.Combine(Path.GetDirectoryName(cacheFile), "ResultingCode"));
        }
    }
}