using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    public class SourceGen : IIncrementalGenerator
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
            context.ReportDiagnostic(Diagnostic.Create("ECP02", "ContentSourceGen", $"Load content project: {cp} with content data ...", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;

            
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
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var cp = context.GetMSBuildItems((s) => s == "EngeniousContentReference" || s == "EngeniousContentData");

            context.RegisterSourceOutput(cp, MatchRefToData);
        }

        private class Matching
        {
            public AdditionalText? ContentReference { get; set; }
            public ContentProject? Project { get; set; }
            public AdditionalText? ContentData { get; set; }
        }

        private readonly Dictionary<string, Matching> _refDataMatch = new();
        private void MatchRefToData(SourceProductionContext context, AdditionalText text)
        {
            var cleanPath = text.Path;


            Matching matching;
            if (cleanPath.EndsWith(".ecp"))
            {
                if (!File.Exists(text.Path))
                {
                    return;
                }
                context.ReportDiagnostic(Diagnostic.Create("ECP02", "ContentSourceGen", $"Load content project: {text.Path} with content data ...", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;
                
                var p = ContentProject.Load(text.Path);

                cleanPath = Path.Combine(Path.GetDirectoryName(p.ContentProjectPath), "obj", p.Configuration,
                    p.Name + ".CreatedCode.dat");
                
                if (!_refDataMatch.TryGetValue(cleanPath, out matching))
                {
                    matching = new Matching();
                    _refDataMatch.Add(cleanPath, matching);
                }

                matching.Project = p;
                matching.ContentReference = text;
            }
            else
            {
                if (!_refDataMatch.TryGetValue(cleanPath, out matching))
                {
                    matching = new Matching();
                    _refDataMatch.Add(cleanPath, matching);
                }
                matching.ContentData = text;
            }
            

            if (matching.ContentData is not null && matching.ContentReference is not null && matching.Project is not null)
            {
                _refDataMatch.Remove(cleanPath);
                GenerateCode(context, matching.Project, matching.ContentReference, matching.ContentData);
            }
        }

        private void GenerateCode(SourceProductionContext context, ContentProject p, AdditionalText cp, AdditionalText cpd)
        {
            var cacheFile = !File.Exists(cpd.Path) ? Path.Combine(Path.GetDirectoryName(p.ContentProjectPath), "obj", p.Configuration,
                p.Name + ".CreatedCode.dat") : cpd.Path;

            var contentCode = CreatedContentCode.Load(cacheFile, Guid.Empty);
            
            context.ReportDiagnostic(Diagnostic.Create("ECP03", "ContentSourceGen", $"Loaded content code: {cacheFile} with {contentCode.FileDefinitions.Count()} file definitions.", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;

            foreach(var f in contentCode.FileDefinitions)
            {
                            
                context.ReportDiagnostic(Diagnostic.Create("ECP04", "ContentSourceGen", $"Create code for file definition: {f.Name}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true,1 ));;

                var codeBuilder = new StringCodeBuilder();
                f.WriteTo(codeBuilder);
                context.AddSource(f.Name.Replace('/', '_'), codeBuilder.ToString());
            }
        }
    }
}