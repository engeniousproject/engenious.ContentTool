using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace engenious.ContentTool.SourceGen
{
    public static class SourceExtensions
    {
        private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

        public static IncrementalValueProvider<string> GetMSBuildProperty(
            this IncrementalGeneratorInitializationContext context,
            string name,
            string defaultValue = "")
        {
            return context.AnalyzerConfigOptionsProvider.Select((x, _) =>
                                                         {
                                                             var res = x.GlobalOptions.TryGetValue(
                                                                 $"build_property.{name}", out var value);
                                                             return value ?? defaultValue;
                                                         });
        }

        public static IEnumerable<string> GetMSBuildItems(this GeneratorExecutionContext context, string name)
            => context
                .AdditionalFiles
                .Where(f => context.AnalyzerConfigOptions
                                .GetOptions(f)
                                .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                            && sourceItemGroup == name)
                .Select(f => f.Path);

        public static IncrementalValuesProvider<AdditionalText> GetMSBuildItems(this IncrementalGeneratorInitializationContext context,
            string name)
        {
            return context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider).Where(tuple =>
                                                             tuple.Right.GetOptions(tuple.Left).TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                                                                     && sourceItemGroup == name).Select((x, _) => x.Left);
        }
        public static IncrementalValuesProvider<AdditionalText> GetMSBuildItems(this IncrementalGeneratorInitializationContext context,
            Func<string, bool> nameMatcher)
        {
            return context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider).Where(tuple =>
                tuple.Right.GetOptions(tuple.Left).TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                && nameMatcher(sourceItemGroup)).Select((x, _) => x.Left);
        }
        public static IncrementalValuesProvider<(string match, AdditionalText text)> GetMatchedMSBuildItems(this IncrementalGeneratorInitializationContext context,
            Func<string, bool> nameMatcher)
        {
            return context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider).Select((tuple, _) =>
            {
                if (tuple.Right.GetOptions(tuple.Left)
                        .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup) &&
                    nameMatcher(sourceItemGroup))
                    return (isMatch: true, group: sourceItemGroup, tuple.Left);
                return (isMatch: false, group: "", tuple.Left);
            }).Where(x => x.isMatch).Select((x, _) => (x.group, x.Left));
        }
    }
}