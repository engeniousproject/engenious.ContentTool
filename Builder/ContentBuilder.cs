using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;

namespace ContentTool.Builder
{
    public class ContentBuilder
    {

        private const string DESTINATION_EXT = ".ego";

        public delegate void ItemProgressDel(object sender, ItemProgressEventArgs e);
        public delegate void BuildStatusChangedDel(object sender, BuildStep buildStep);

        public event engenious.Content.Pipeline.BuildMessageDel BuildMessage;
        public event ItemProgressDel ItemProgress;
        public event BuildStatusChangedDel BuildStatusChanged;
        private Dictionary<string,ContentFile> builtFiles;
        private BuildStep currentBuild = BuildStep.Finished;

        private System.Threading.Thread buildingThread;
        private BuildCache cache;

        public ContentBuilder(ContentProject project)
        {
            Project = project;
            builtFiles = new Dictionary<string, ContentFile>();
        }
        internal static void CreateFolderIfNeeded(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return;
            string folder = System.IO.Path.GetDirectoryName(filename);
            if (string.IsNullOrEmpty(folder))
                return;
            if (!System.IO.Directory.Exists(folder))
            {
                CreateFolderIfNeeded(folder);
                System.IO.Directory.CreateDirectory(folder);
            }
        }
        private ContentProject project;
        public ContentProject Project {
            get{return project;}
            private set{
                project = value;
                if (project == null)
                    cache = null;
                else
                    cache = BuildCache.Load(getCacheFile());
            }
        }
        public bool IsBuilding { get; private set; }
        public bool CanClean
        {
            get
            {
                return cache.CanClean(getOutputDir());
            }
        }
        public bool CanBuild
        {
            get
            {
                return true;//TODO:
            }
        }
        public int FailedBuilds { get; private set; }

        private string getDestinationFile(ContentFile contentFile)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(contentFile.getPath()), System.IO.Path.GetFileNameWithoutExtension(contentFile.Name) + DESTINATION_EXT);
        }
        private string getDestinationFileAbsolute(ContentFile contentFile)
        {

            //string importFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Project.File), contentFile.getPath());
            return System.IO.Path.Combine(getOutputDir(),getDestinationFile(contentFile));
        }
        private string getOutputDir()
        {
            string relOut = string.Format(Project.OutputDir.Replace("{Configuration}", "{0}"), Project.Configuration.ToString());
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Project.File), relOut);
        }
        private string getObjectDir()
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Project.File), "obj");
        }
        private string getCacheFile()
        {
            return Path.Combine(getObjectDir(),"obj",Path.GetFileNameWithoutExtension(Project.File)+".dat");
        }
        public void Rebuild()
        {
            if (Project == null)
                return;
            var thread = new System.Threading.Thread(new System.Threading.ThreadStart(delegate ()
            {
                Clean();
                buildingThread.Join();
                Build();
            }));
            thread.Start();
        }
        private void RaiseBuildMessage(object sender,engenious.Content.Pipeline.BuildMessageEventArgs e)
        {
            if (e.MessageType == engenious.Content.Pipeline.BuildMessageEventArgs.BuildMessageType.Error)
                FailedBuilds++;
            BuildMessage?.Invoke(sender, e);
        }
        private void buildFile(ContentFile contentFile,engenious.Content.Pipeline.ContentImporterContext importerContext,engenious.Content.Pipeline.ContentProcessorContext processorContext)
        {
            string importDir = System.IO.Path.GetDirectoryName(Project.File);
            string importFile = System.IO.Path.Combine(importDir, contentFile.getPath());
            string destFile = getDestinationFileAbsolute(contentFile);
            string outputPath = getOutputDir();
            CreateFolderIfNeeded(destFile);
            if (!cache.NeedsRebuild(importDir,outputPath,contentFile.getPath())){
                //importerContext.RaiseBuildMessage(contentFile.getPath()," skipped(cached)",engenious.Content.Pipeline.BuildMessageEventArgs.BuildMessageType.Information);
                //ItemProgress?.BeginInvoke(this, new ItemProgressEventArgs(BuildStep.Build, contentFile), null, null);
                return;
            }
            BuildInfo cacheInfo = new BuildInfo(importDir,contentFile.getPath(),getDestinationFile(contentFile));
            var importer = contentFile.Importer;
            if (importer == null)
                return;
            
            object importerOutput = importer.Import(importFile, importerContext);
            if (importerOutput == null)
                return;
            
            cacheInfo.Dependencies.AddRange(importerContext.Dependencies);
            cache.AddDependencies(importDir,importerContext.Dependencies);

            engenious.Content.Pipeline.IContentProcessor processor = contentFile.Processor;
            if (processor == null)
                return;

            object processedData = processor.Process(importerOutput,importFile, processorContext);

            if (processedData == null)
                return;
            cacheInfo.Dependencies.AddRange(processorContext.Dependencies);
            cache.AddDependencies(importDir,processorContext.Dependencies);

            engenious.Content.Serialization.IContentTypeWriter typeWriter = engenious.Content.Serialization.SerializationManager.Instance.GetWriter(processedData.GetType());
            engenious.Content.ContentFile outputFileWriter = new engenious.Content.ContentFile(typeWriter.RuntimeReaderName);

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(destFile, FileMode.Create, FileAccess.Write))
            {
                formatter.Serialize(fs, outputFileWriter);
                engenious.Content.Serialization.ContentWriter writer = new engenious.Content.Serialization.ContentWriter(fs);
                writer.WriteObject(processedData, typeWriter);
            }
            cacheInfo.BuildDone(outputPath);
            cache.AddBuildInfo(cacheInfo);
            builtFiles[contentFile.getPath()]=contentFile;
            ItemProgress?.BeginInvoke(this, new ItemProgressEventArgs(BuildStep.Build, contentFile), null, null);
        }
        private void buildDir(ContentFolder folder,engenious.Content.Pipeline.ContentImporterContext importerContext,engenious.Content.Pipeline.ContentProcessorContext processorContext)
        {
            foreach (var item in folder.Contents)
            {
                if (item is ContentFile)
                {
                    buildFile(item as ContentFile,importerContext,processorContext);
                }
                else if (item is ContentFolder)
                {
                    buildDir(item as ContentFolder,importerContext,processorContext);
                }
            }
        }
        public void Build()
        {
            if (Project == null)
                return;
            currentBuild = BuildStep.Build;
            BuildStatusChanged?.BeginInvoke(this, BuildStep.Build, null, null);

            buildingThread = new System.Threading.Thread(new System.Threading.ThreadStart(delegate ()
                {
                    FailedBuilds = 0;

                    IsBuilding = true;
                    string outputDir = getOutputDir();
                    CreateFolderIfNeeded(outputDir);
                    PipelineHelper.PreBuilt(Project);
                    using (engenious.Content.Pipeline.ContentImporterContext importerContext = new engenious.Content.Pipeline.ContentImporterContext())
                    using (engenious.Content.Pipeline.ContentProcessorContext processorContext = new engenious.Content.Pipeline.ContentProcessorContext())
                    {
                        importerContext.BuildMessage += RaiseBuildMessage;
                        processorContext.BuildMessage += RaiseBuildMessage;

                        buildDir (Project,importerContext,processorContext);
                    }
                    //System.Threading.Thread.Sleep(8000);
                    cache.Save(getCacheFile());
                    IsBuilding = false;

                    BuildStatusChanged?.BeginInvoke(this, BuildStep.Build | BuildStep.Finished, null, null);
                }));
            buildingThread.Start();
        }
        public void Clean()
        {
            if (Project == null)
                return;
            
            currentBuild = BuildStep.Clean;
            BuildStatusChanged?.BeginInvoke(this, BuildStep.Clean, null, null);
            buildingThread = new System.Threading.Thread(new System.Threading.ThreadStart(delegate ()
            {
                string outputDir = getOutputDir();
                IsBuilding = true;
                foreach (var cachedItem in cache.Files.Where(x=>x.Value.IsBuilt(outputDir)))
                {
                    var item = Project.getElement(cachedItem.Value.InputFile) as ContentFile;
                    if (item != null){
                        ItemProgress?.BeginInvoke(this, new ItemProgressEventArgs(BuildStep.Clean,item), null, null);
                        if (System.IO.File.Exists(getDestinationFileAbsolute(item)))
                            System.IO.File.Delete(getDestinationFileAbsolute(item));
                    }
                }
                IsBuilding = false;
                BuildStatusChanged?.Invoke(this, BuildStep.Clean | BuildStep.Finished);
            }));
            buildingThread.Start();

        }
        public void Abort()
        {
            if (!IsBuilding)
                return;
            buildingThread.Abort();//TODO: better solution?
            IsBuilding = false;
            BuildStatusChanged?.Invoke(this, currentBuild | BuildStep.Abort);
        }

        public void Join()
        {
            buildingThread.Join();
        }
    }
}

