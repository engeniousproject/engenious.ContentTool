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
            var cpd = context.GetMSBuildItems("EngeniousContentData");
            foreach (var cp in cps.Zip(cpd, (contentReference, contentData) => (contentReference, contentData)))
            {
                context.ReportDiagnostic(Diagnostic.Create("ECP01", "ContentSourceGen", $"Trying to build content project: {cp}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;
                BuildContentProjectSources(context, cp.contentReference, cp.contentData);
            }
        }

        private static void BuildContentProjectSources(GeneratorExecutionContext context, string? cp, string? cpd)
        {
            if (cp == null || !File.Exists(cp))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create("ECP02", "ContentSourceGen", $"Load content project: {cp} with content data {cpd}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;

            
            var p = ContentProject.Load(cp);

            var cacheFile = cpd == null || !File.Exists(cpd) ? Path.Combine(Path.GetDirectoryName(p.ContentProjectPath), "obj", p.Configuration,
                p.Name + ".CreatedCode.dat") : cpd;

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