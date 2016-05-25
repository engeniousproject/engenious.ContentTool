using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
        private List<ContentFile> toClean;
        private BuildStep currentBuild = BuildStep.Finished;

        private System.Threading.Thread buildingThread;

        public ContentBuilder(ContentProject project)
        {
            Project = project;
            toClean = new List<ContentFile>();
        }
        private static void CreateFolderIfNeeded(string filename)
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
        public ContentProject Project { get; internal set; }
        public bool IsBuilding { get; private set; }
        public bool CanClean
        {
            get
            {
                return toClean.Count > 0;
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

            //string importFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Project.File), contentFile.getPath());
            return System.IO.Path.Combine(getOutputDir(), System.IO.Path.GetDirectoryName(contentFile.getPath()), System.IO.Path.GetFileNameWithoutExtension(contentFile.Name) + DESTINATION_EXT);
        }
        private string getOutputDir()
        {
            string relOut = string.Format(Project.OutputDir.Replace("{Configuration}", "{0}"), Project.Configuration.ToString());
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Project.File), relOut);
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
            string importFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Project.File), contentFile.getPath());
            string destFile = getDestinationFile(contentFile);
            CreateFolderIfNeeded(destFile);

            var importer = contentFile.Importer;
            if (importer == null)
                return;

            object importerOutput = importer.Import(importFile, importerContext);
            if (importerOutput == null)
                return;

            engenious.Content.Pipeline.IContentProcessor processor = contentFile.Processor;
            if (processor == null)
                return;

            object processedData = processor.Process(importerOutput,importFile, processorContext);

            if (processedData == null)
                return;

            engenious.Content.Serialization.IContentTypeWriter typeWriter = engenious.Content.Serialization.SerializationManager.Instance.GetWriter(processedData.GetType());
            engenious.Content.ContentFile outputFileWriter = new engenious.Content.ContentFile(typeWriter.RuntimeReaderName);

            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream fs = new FileStream(destFile, FileMode.Create, FileAccess.Write))
            {
                formatter.Serialize(fs, outputFileWriter);
                engenious.Content.Serialization.ContentWriter writer = new engenious.Content.Serialization.ContentWriter(fs);
                writer.WriteObject(processedData, typeWriter);
            }

            toClean.Add(contentFile);
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
                IsBuilding = true;
                foreach (var item in toClean)
                {
                    ItemProgress?.BeginInvoke(this, new ItemProgressEventArgs(BuildStep.Clean, item), null, null);
                    if (System.IO.File.Exists(getDestinationFile(item)))
                        System.IO.File.Delete(getDestinationFile(item));
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

