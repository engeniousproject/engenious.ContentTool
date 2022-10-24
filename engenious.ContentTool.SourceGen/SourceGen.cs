using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        private static void BuildContentProjectSources(GeneratorExecutionContext context, string? cp, string? cpd)
        {
            if (cp == null || !File.Exists(cp))
            {
                return;
            }
            context.ReportDiagnostic(Diagnostic.Create("ECP02", "ContentSourceGen", $"Load content project: {cp} with content data ...", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true, 1)); ;


            var p = ContentProject.Load(cp);

            var cacheFile = cpd == null || !File.Exists(cpd) ? Path.Combine(Path.GetDirectoryName(p.ContentProjectPath), "obj", p.Configuration,
                p.Name + ".CreatedCode.dat") : cpd;

            var contentCode = CreatedContentCode.Load(cacheFile, Guid.Empty);

            context.ReportDiagnostic(Diagnostic.Create("ECP03", "ContentSourceGen", $"Loaded content code: {cacheFile} with {contentCode.FileDefinitions.Count()} file definitions.", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true, 1)); ;

            foreach (var f in contentCode.FileDefinitions)
            {

                context.ReportDiagnostic(Diagnostic.Create("ECP04", "ContentSourceGen", $"Create code for file definition: {f.Name}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true, 1)); ;

                var codeBuilder = new StringCodeBuilder();
                f.WriteTo(codeBuilder);
                context.AddSource(f.Name.Replace('/', '_'), codeBuilder.ToString());
            }
        }




        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var properties = context.GetMSBuildProperty("EngeniousContentBuilderExe")
                .Combine(context.GetMSBuildProperty("EngeniousBuildParameters"))
                .Combine(context.GetMSBuildProperty("EngeniousDotnetExe"))
                .Combine(context.GetMSBuildProperty("EngeniousContentConfiguration", "UNKNOWN"));
            var cp = context.GetMatchedMSBuildItems(
                (s) => s is "EngeniousContentReference" or "EngeniousContentInput" or "EngeniousContentOutput")
                .Combine(properties).Collect();

            context.RegisterSourceOutput(cp, MatchRefToData);
        }


        private Dictionary<PathKey, AdditionalText> _additionalTexts = new();

        private void MatchRefToData(SourceProductionContext context, ImmutableArray<((string match, AdditionalText text) file, (((string contentBuilder, string header) content, string dotnet) exe, string configuration) properties)> inputs)
        {
            try
            {
                if (!CollectionFilesByPath(_additionalTexts, inputs))
                {
                    return;
                }

                if (!BuildContent(context, inputs, _additionalTexts))
                    return;


                CollectCodeToGenerate(context, inputs);
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create("ECP09", "ContentSourceGen",
                    $"{e.Message}\n{e.StackTrace}",
                    DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0));
            }
        }

        private void CollectCodeToGenerate(SourceProductionContext context, ImmutableArray<((string match, AdditionalText text) file, (((string contentBuilder, string header) content, string dotnet) exe, string configuration) properties)> inputs)
        {
            foreach (var input in inputs)
            {
                try
                {
                    var ((match, text), properties) = input;

                    switch (match)
                    {
                        case "EngeniousContentReference":
                            if (!File.Exists(text.Path))
                            {
                                continue;
                            }

                            context.ReportDiagnostic(Diagnostic.Create("ECP02", "ContentSourceGen",
                                $"Load content project: {text.Path} with content data ...", DiagnosticSeverity.Info,
                                DiagnosticSeverity.Info, true, 1));
                            ;

                            var p = ContentProject.Load(text.Path);

                            p.Configuration = properties.configuration;

                            GenerateCode(context, p, text);
                            break;

                        case "EngeniousContentInput" or "EngeniousContentOutput":
                            continue;
                    }
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(Diagnostic.Create("ECP07", "ContentSourceGen",
                        $"SourceGen failed: {e.Message}\n{e.StackTrace}",
                        DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0));
                }
            }
        }

        private static bool CollectionFilesByPath(Dictionary<PathKey, AdditionalText> additionalTexts, ImmutableArray<((string match, AdditionalText text) file, (((string contentBuilder, string header) content, string dotnet) exe, string configuration) properties)> inputs)
        {
            bool hasChanges = false;
            foreach (var input in inputs)
            {
                var path = (PathKey)input.file.text.Path;
                var text = input.file.text;
                if (additionalTexts.TryGetValue(path, out var previousText))
                {
                    //if (text != previousText)
                    //{
                    //    var textHash = text.GetText()?.GetChecksum();
                    //    var previousTextHash = previousText.GetText()?.GetChecksum();
                    //    hasChanges = !(textHash == null && previousTextHash == null || 
                    //                textHash != null && previousTextHash != null && Enumerable.SequenceEqual<byte>(textHash, previousTextHash));
                    //}
                    additionalTexts[path] = text;
                }
                else
                {
                    additionalTexts.Add(path, text);
                    hasChanges = true;
                }
            }

            return true;
        }

        private static bool TryParseError(string line, string errorType, out string? file, out (int, int)? location,
            out string? message)
        {
            string errorWithFile = $": {errorType}: ";
            string errorWithoutFile = $"{errorType}: ";
            var errorPos = line.IndexOf(errorWithFile, StringComparison.Ordinal);
            if (errorPos != -1)
            {
                location = null;
                file = line.Substring(0, errorPos);
                if (file.EndsWith(")"))
                {
                    var locPos = file.LastIndexOf('(');
                    if (locPos != -1)
                    {
                        var locSubstr = file.Substring(locPos + 1, file.Length - locPos - 2);
                        var commaPos = locSubstr.IndexOf(',');
                        int row = -1, column = 0;
                        if (commaPos != -1)
                        {
                            if (int.TryParse(locSubstr.Substring(0, commaPos), out var parsed))
                                row = parsed;
                            if (int.TryParse(locSubstr.Substring(commaPos + 1), out parsed))
                                column = parsed;
                        }
                        else
                        {
                            if (int.TryParse(locSubstr, out var parsed))
                                row = parsed;
                        }

                        if (row != -1 && column != -1)
                        {
                            location = (row, column);
                            file = file.Substring(0, locPos);
                        }
                    }
                }

                message = line.Substring(errorPos + errorWithFile.Length);
                return true;
            }
            else if (line.StartsWith(errorWithoutFile))
            {
                file = null;
                message = line.Substring(errorWithoutFile.Length);
                location = null;
                return true;
            }

            file = null;
            message = null;
            location = null;

            return false;
        }

        private static Location? CreateLocationForFile(AdditionalText file, (int row, int column)? location)
        {
            var text = file.GetText();
            if (text is null)
                return null;

            var (row, column) = location ?? (1, 1);

            if (row > 0 && row <= text.Lines.Count)
            {
                var line = text.Lines[row - 1];
                var linePos = new LinePosition(row - 1, column - 1);
                return Location.Create(file.Path, line.Span, new LinePositionSpan(linePos, linePos));
            }

            return null;
        }

        private static bool BuildContent(SourceProductionContext context, ImmutableArray<((string match, AdditionalText text) file, (((string contentBuilder, string header) content, string dotnet) exe, string configuration) properties)> inputs, Dictionary<PathKey, AdditionalText> additionalTexts)
        {
            foreach (var input in inputs)
            {
                try
                {
                    var ((match, text), (exe, _)) = input;
                    var cleanPath = text.Path;
                    switch (match)
                    {
                        case "EngeniousContentReference":
                            if (!File.Exists(text.Path))
                            {
                                continue;
                            }

                            if (!File.Exists(exe.content.contentBuilder))
                            {
                                context.ReportDiagnostic(Diagnostic.Create("ECP05", "ContentSourceGen",
                                    $"Could not find ContentTool: Be sure to reference the engenious.ContentTool nuget package!",
                                    DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0));
                                ;
                                return false;
                            }


                            var pS = new ProcessStartInfo(exe.dotnet,
                                $"\"{exe.content.contentBuilder}\" /@:\"{input.file.text.Path}\" {exe.content.header}");
                            pS.UseShellExecute = false;
                            pS.CreateNoWindow = true;
                            pS.RedirectStandardError = pS.RedirectStandardOutput = true;

                            var p = Process.Start(pS);

                            var stdout = p.StandardOutput.ReadToEnd();
                            string? line;

                            while ((line = p.StandardError.ReadLine()) != null)
                            {
                                if (TryParseError(line, "error", out var file, out var location, out var message))
                                {
                                    if (file is not null && additionalTexts.TryGetValue(file, out var additionalText))
                                    {
                                        var parsedLocation = CreateLocationForFile(additionalText, location);
                                        context.ReportDiagnostic(Diagnostic.Create("ECP06", "ContentSourceGen",
                                            $"{message}", DiagnosticSeverity.Error,
                                            DiagnosticSeverity.Error, true, 0, location: parsedLocation));
                                    }
                                    else
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create("ECP06", "ContentSourceGen",
                                            $"{message}", DiagnosticSeverity.Error
                                            , DiagnosticSeverity.Error, true, 0));
                                    }
                                }
                            }

                            p.WaitForExit(-1);

                            break;
                    }
                }
                catch (Exception e)
                {
                    context.ReportDiagnostic(Diagnostic.Create("ECP06", "ContentSourceGen",
                        $"Source generation failed: {e.Message}\n{e.StackTrace}", DiagnosticSeverity.Error
                        , DiagnosticSeverity.Error, true, 0));
                }
            }

            return true;
        }

        private void GenerateCode(SourceProductionContext context, ContentProject p, AdditionalText cp)
        {
            
            var cacheFile = Path.Combine(Path.GetDirectoryName(p.ContentProjectPath), "obj", p.Configuration,
                                p.Name + ".CreatedCode.dat");

            if (!File.Exists(cacheFile))
                return;

            var contentCode = CreatedContentCode.Load(cacheFile, Guid.Empty);

            context.ReportDiagnostic(Diagnostic.Create("ECP03", "ContentSourceGen", $"Loaded content code: {cacheFile} with {contentCode.FileDefinitions.Count()} file definitions.", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true, 1)); ;

            foreach (var f in contentCode.FileDefinitions)
            {

                context.ReportDiagnostic(Diagnostic.Create("ECP04", "ContentSourceGen", $"Create code for file definition: {f.Name}", DiagnosticSeverity.Info, DiagnosticSeverity.Info, true, 1)); ;

                var codeBuilder = new StringCodeBuilder();
                f.WriteTo(codeBuilder);
                context.AddSource(f.Name.Replace('/', '_'), codeBuilder.ToString());
            }
        }
    }
}