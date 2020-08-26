using System;
using System.Collections.Generic;
using System.IO;
using engenious.Pipeline;

namespace engenious.ContentTool.Builder
{
    [Serializable]
    public class BuildFile
    {
        public string InputFilePath { get; }
        public string OutputFilePath { get; }

        public DateTime InputFileModifiedTime { get; private set; }
        public DateTime OutputFileModifiedTime { get; private set; }
        
        public List<string> OutputTypes { get; }

        public List<string> Dependencies { get; }
        
        public Guid BuildId { get; private set; }
        
        public bool CreatesUserContent { get; set; }
        
        public BuildFile(Guid buildId, string inputFilePath, string outputFilePath)
        {
            InputFilePath = inputFilePath;
            OutputFilePath = outputFilePath;
            Dependencies = new List<string>();
            OutputTypes = new List<string>();
            RefreshBuildCache(buildId);
        }

        public BuildFile(Guid buildId, string inputFilePath, string outputFileDirectory, string name)
            : this(buildId, inputFilePath, Path.Combine(outputFileDirectory, name)){}

        public void RefreshBuildCache(Guid buildId)
        {
            BuildId = buildId;
            InputFileModifiedTime = new FileInfo(InputFilePath).LastWriteTimeUtc;

            if(File.Exists(OutputFilePath))
                OutputFileModifiedTime = new FileInfo(OutputFilePath).LastWriteTimeUtc;
        }

        public bool IsBuilt()
        {
            if (string.IsNullOrEmpty(OutputFilePath))
                return true;
            if (!File.Exists(OutputFilePath))
                return false;
            return true;
        }

        public bool NeedsRebuild(DateTime? parentOutputModifiedTime = null)
        {
            //RefreshModifiedTime();

            if (!IsBuilt())
                return true;
            return (!File.Exists(InputFilePath) || InputFileModifiedTime != new FileInfo(InputFilePath).LastWriteTimeUtc || (OutputFilePath != null && (!File.Exists(OutputFilePath) || OutputFileModifiedTime != new FileInfo(OutputFilePath).LastWriteTimeUtc)) || (parentOutputModifiedTime != null && parentOutputModifiedTime.Value < InputFileModifiedTime));
                
        }
    }
}
