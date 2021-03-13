using System;
using System.Collections.Generic;
using System.IO;
using engenious.Content;
using engenious.Content.Serialization;
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

        public uint ContentFileVersion { get; set; }
        public uint ContentVersion { get; set; }
        
        public BuildFile(Guid buildId, string inputFilePath, string outputFilePath, ContentManagerBase contentManager)
        {
            InputFilePath = inputFilePath;
            OutputFilePath = outputFilePath;
            Dependencies = new List<string>();
            OutputTypes = new List<string>();
            ContentVersion = ContentManagerBase.ReaderVersion;
            RefreshBuildCache(buildId, contentManager);
        }

        public BuildFile(Guid buildId, string inputFilePath, string outputFileDirectory, string name, ContentManagerBase contentManager)
            : this(buildId, inputFilePath, Path.Combine(outputFileDirectory, name), contentManager){}

        public void RefreshBuildCache(Guid buildId, ContentManagerBase contentManager)
        {
            BuildId = buildId;
            ContentVersion = ContentManagerBase.ReaderVersion;
            InputFileModifiedTime = new FileInfo(InputFilePath).LastWriteTimeUtc;

            if(File.Exists(OutputFilePath))
                OutputFileModifiedTime = new FileInfo(OutputFilePath).LastWriteTimeUtc;

            ReadContentVersion(contentManager, out var version);
            ContentFileVersion = version;
        }

        private void ReadContentVersion(ContentManagerBase contentManager, out uint version)
        {
            version = ContentVersion;
            if (contentManager == null || OutputFilePath == null || !File.Exists(OutputFilePath))
                return;
            using var fs = File.OpenRead(OutputFilePath);
            var head = contentManager.ReadContentFileHead(fs, false);
            if (head != null)
                version = head.ContentVersion;
        }

        public bool IsBuilt()
        {
            if (ContentVersion != ContentManagerBase.ReaderVersion)
                return false;
            if (string.IsNullOrEmpty(OutputFilePath))
                return true;
            if (!File.Exists(OutputFilePath))
                return false;

            return true;
        }

        public bool NeedsRebuild(ContentManagerBase contentManager, DateTime? parentOutputModifiedTime = null)
        {
            //RefreshModifiedTime();
            if (!IsBuilt())
                return true;
            if (!File.Exists(InputFilePath) || InputFileModifiedTime != new FileInfo(InputFilePath).LastWriteTimeUtc ||
                (OutputFilePath != null && (!File.Exists(OutputFilePath) ||
                                            OutputFileModifiedTime != new FileInfo(OutputFilePath).LastWriteTimeUtc)) ||
                (parentOutputModifiedTime != null && parentOutputModifiedTime.Value < InputFileModifiedTime))
                return true;
            ReadContentVersion(contentManager, out var version);
            if (version != ContentFileVersion)
                return true;
            return false;
        }
    }
}
