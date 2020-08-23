using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using engenious.Content.Pipeline;
using engenious.Content.Serialization;
using engenious.ContentTool.Models;
using engenious.Graphics;
using Microsoft.CSharp;
using Mono.Cecil;

namespace engenious.ContentTool.Builder
{
    public class ContentBuilder : IDisposable
    {
        public const string FileExtension = ".ego";

        public ContentProject Project { get; set; }

        public int FailedBuilds { get; private set; }

        public bool IsBuilding { get; private set; }

        private readonly SynchronizationContext _syncContext;

        private readonly BuildCache _cache;

        private readonly Thread _buildThread;
        private readonly ConcurrentQueue<BuildTask> _buildQueue;
        private readonly AutoResetEvent _startBuild;
        private readonly CancellationTokenSource _buildThreadCancellation;
        private readonly CancellationTokenSource _buildCancellation;

        public enum BuildTaskType
        {
            Clean,
            Build,
            Rebuild
        }
        public class BuildTask
        {
            public BuildTask(BuildTaskType buildTaskType, ContentItem buildItem = null)
            {
                BuildTaskType = buildTaskType;
                BuildItem = buildItem;
                CancellationTokenSource = new CancellationTokenSource();
            }

            public BuildTaskType BuildTaskType { get; }
            
            public ContentItem BuildItem { get; }
            
            public CancellationTokenSource CancellationTokenSource { get; }
        }

        public ContentBuilder(ContentProject project, IRenderingSurface renderingSurface = null,
            GraphicsDevice graphicsDevice = null)
        {
            Project = project;
            _syncContext = SynchronizationContext.Current;
            RenderingSurface = renderingSurface;
            GraphicsDevice = graphicsDevice;

            _cache = BuildCache.Load(Path.Combine(Path.GetDirectoryName(project.ContentProjectPath), "obj",
                project.Configuration,
                project.Name + ".dat"));
            
            
            _buildThreadCancellation = new CancellationTokenSource();
            var buildThreadToken = _buildThreadCancellation.Token;
            buildThreadToken.Register(() => _startBuild.Set());
            
            _buildQueue = new ConcurrentQueue<BuildTask>();
            
            _buildThread = new Thread((cancellationObj) =>
            {
                var cancellationToken = (CancellationToken)(cancellationObj ?? throw new InvalidCastException());
                while(!cancellationToken.IsCancellationRequested)
                {
                    _startBuild.WaitOne();
                    if (cancellationToken.IsCancellationRequested)
                        return;
                    while (_buildQueue.TryDequeue(out var buildTask))
                    {
                        IsBuilding = true;
                        switch (buildTask.BuildTaskType)
                        {
                            case BuildTaskType.Clean:
                                CleanThread(buildTask.CancellationTokenSource.Token);
                                break;
                            case BuildTaskType.Build:
                                BuildThread(buildTask.BuildItem, buildTask.CancellationTokenSource.Token);
                                break;
                            case BuildTaskType.Rebuild:
                                if (Project.HasUnsavedChanges)
                                    Project.Save();
                                CleanThread(buildTask.CancellationTokenSource.Token);
                                BuildThread(Project, buildTask.CancellationTokenSource.Token);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        IsBuilding = false;
                    }
                }
            });

            _buildThread.Start(buildThreadToken);
        }

        public void Build()
        {
            Build(Project);
        }

        private BuildTask EnqueueBuildTask(BuildTask task)
        {
            _buildQueue.Enqueue(task);

            _startBuild.Set();
            
            return task;
        }

        public BuildTask Build(ContentItem item)
        {
            return EnqueueBuildTask(new BuildTask(BuildTaskType.Build, item));
        }

        public BuildTask Clean()
        {
            return EnqueueBuildTask(new BuildTask(BuildTaskType.Clean));
        }

        public BuildTask Rebuild()
        {
            return EnqueueBuildTask(new BuildTask(BuildTaskType.Rebuild));
        }

        public void Abort()
        {
            if (IsBuilding)
                _buildThread.Abort();
        }

        internal IRenderingSurface RenderingSurface { get; }
        internal GraphicsDevice GraphicsDevice { get; }

        protected void BuildThread(ContentItem item, CancellationToken cancellationToken)
        {
            FailedBuilds = 0;
            PipelineHelper.PreBuilt(Project);

            var outputDestination = Path.Combine(Path.GetDirectoryName(Project.ContentProjectPath),
                Project.ConfiguredOutputDirectory);

            string moduleName = "engenious.CreatedContent." + Project.Name;

            string createdContentAssemblyFile = Path.Combine(Path.GetDirectoryName(Project.ContentProjectPath),
                moduleName + ".dll");
            bool rewriteAssembly = File.Exists(createdContentAssemblyFile);
            
            AssemblyDefinition assemblyDefinition = rewriteAssembly ? 
                AssemblyDefinition.ReadAssembly(createdContentAssemblyFile) : 
                AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(moduleName, new Version()), moduleName,
                    ModuleKind.Dll);
            using (var iContext = new ContentImporterContext(assemblyDefinition))
            using (var pContext = new ContentProcessorContext(_syncContext, assemblyDefinition, RenderingSurface, GraphicsDevice,
                Path.GetDirectoryName(Project.ContentProjectPath)))
            {
                //Console.WriteLine($"GL Version: {pContext.GraphicsDevice.DriverVersion.ToString()}");
                //Console.WriteLine($"GLSL Version: {pContext.GraphicsDevice.GlslVersion.ToString()}");


                iContext.BuildMessage += RaiseBuildMessage;
                pContext.BuildMessage += RaiseBuildMessage;
                InternalBuildItem(item, outputDestination, iContext, pContext, cancellationToken);
                try
                {
                    if (rewriteAssembly)
                    {
                        File.Delete(createdContentAssemblyFile);
                    }
                    assemblyDefinition.Write(createdContentAssemblyFile);
                }
                catch (Exception ex)
                {
                    pContext.RaiseBuildMessage("<Module>", ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
                }
            }


            _cache.Save();
        }

        protected void CleanThread(CancellationToken cancellationToken)
        {
            foreach (var item in _cache.Files)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (File.Exists(item.Value.OutputFilePath))
                    File.Delete(item.Value.OutputFilePath);
            }

            try
            {
                Directory.Delete(Path.Combine(Path.GetDirectoryName(Project.ContentProjectPath), "obj"), true);
            }
            catch (Exception ex) //TODO perhaps other delete, to delete all possible files?
            {
            }
        }

        private void InternalBuildItem(ContentItem item, string outputDestination,
            ContentImporterContext importerContext, ContentProcessorContext processorContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            
            if (!(item is ContentProject))
                outputDestination = Path.Combine(outputDestination, item.Name);

            var folder = item as ContentFolder;
            if (folder != null)
            {
                foreach (var child in folder.Content)
                {
                    InternalBuildItem(child, outputDestination, importerContext, processorContext, cancellationToken);
                }
            }
            else
            {
                if (_cache.NeedsRebuild(item.FilePath))
                {
                    InternalBuildFile(item as ContentFile,
                        Path.Combine(Path.GetDirectoryName(outputDestination),
                            Path.GetFileNameWithoutExtension(item.Name) + FileExtension), importerContext,
                        processorContext, cancellationToken);
                    RaiseBuildMessage(this,
                        new BuildMessageEventArgs(item.RelativePath, item.RelativePath + " built",
                            BuildMessageEventArgs.BuildMessageType.Information));
                }
                else
                {
                    RaiseBuildMessage(this,
                        new BuildMessageEventArgs(item.RelativePath, item.RelativePath + " skipped",
                            BuildMessageEventArgs.BuildMessageType.Information));
                }

                importerContext.Dependencies.Clear();
                processorContext.Dependencies.Clear();
            }
        }

        protected void RaiseBuildMessage(object sender, BuildMessageEventArgs e) => BuildMessage?.Invoke(e);

        public object InternalBuildFile(ContentFile item, string destination, ContentImporterContext importerContext,
            ContentProcessorContext processorContext, CancellationToken cancellationToken)
        {
            return BuildFile(item, destination, importerContext, processorContext, cancellationToken, _cache);
        }

        /// <summary>
        /// Builds a ContentFile and writes it to the destinatin
        /// </summary>
        /// <param name="item">ContentFile to build</param>
        /// <param name="destination">Location to save to</param>
        /// <param name="importerContext"></param>
        /// <param name="processorContext"></param>
        /// <returns></returns>
        public static object BuildFile(ContentFile item, string destination, ContentImporterContext importerContext,
            ContentProcessorContext processorContext, CancellationToken cancellationToken, BuildCache cache = null)
        {
            if (!File.Exists(item.FilePath)) return null;
            if (cancellationToken.IsCancellationRequested)
                return null;

            var dirName = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(dirName);

            var buildFile = new BuildFile(item.FilePath, destination);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var importer = item.Importer;
            var importedFile = importer?.Import(item.FilePath, importerContext);
            if (importedFile == null) return null;
            if (cancellationToken.IsCancellationRequested)
                return null;
            buildFile.Dependencies.AddRange(importerContext.Dependencies);
            cache?.AddDependencies(Path.GetDirectoryName(item.FilePath), importerContext.Dependencies);

            var processor = item.Processor;

            var processedFile = processor?.Process(importedFile, item.FilePath, processorContext);
            if (processedFile == null) return null;
            if (cancellationToken.IsCancellationRequested)
                return null;
            buildFile.Dependencies.AddRange(processorContext.Dependencies);
            cache?.AddDependencies(Path.GetDirectoryName(item.FilePath), processorContext.Dependencies);

            var typeWriter = SerializationManager.Instance.GetWriter(processedFile.GetType());
            var outputContentFileWriter = new engenious.Content.ContentFile(typeWriter.RuntimeReaderName);

            using (var fs = new FileStream(destination, FileMode.Create, FileAccess.Write))
            {
                fs.Write(
                    BitConverter.GetBytes(
                        engenious.Helper.BitHelper.BitConverterToBigEndian(engenious.Content.ContentFile.MAGIC)), 0,
                    sizeof(uint));

                const byte writerVersion = 1;
                fs.WriteByte(writerVersion);

                var fileTypeBuffer = System.Text.Encoding.UTF8.GetBytes(outputContentFileWriter.FileType);
                fs.Write(
                    BitConverter.GetBytes(
                        engenious.Helper.BitHelper.BitConverterToLittleEndian((uint) fileTypeBuffer.Length)), 0,
                    sizeof(uint));

                fs.Write(fileTypeBuffer, 0, fileTypeBuffer.Length);

                var writer = new ContentWriter(fs);
                writer.WriteObject(processedFile, typeWriter);
            }

            cache?.AddFile(item.FilePath, buildFile);
            buildFile.RefreshModifiedTime();

            return processedFile;
        }

        public event BuildMessageHandler BuildMessage;
        public event BuildStatusChangedHandler BuildStatusChanged;

        public delegate void BuildMessageHandler(BuildMessageEventArgs args);

        public delegate void BuildStatusChangedHandler(BuildStatus status);

        [Flags]
        public enum BuildStatus
        {
            Clean = 1,
            Build = 2,
            Abort = 4,
            Finished = 8,
            Built = 16
        }

        public void Dispose()
        {
            _buildThreadCancellation.Cancel();
            _startBuild.Set();
            _buildThread.Join();
        }
    }
}