using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using engenious.Content;
using engenious.Content.Pipeline;
using engenious.Content.Serialization;
using engenious.Content.Models;
using engenious.Graphics;
using Microsoft.CSharp;
using Mono.Cecil;
using ContentFile = engenious.Content.Models.ContentFile;

namespace engenious.ContentTool.Builder
{
    public class ContentBuilder : IDisposable
    {
        public const string FileExtension = ".ego";

        public ContentProject Project { get; set; }

        public int FailedBuilds { get; private set; }

        public bool IsBuilding { get; private set; }

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
                CompletionHandle = new ManualResetEvent(false);
            }

            public ManualResetEvent CompletionHandle { get; }

            public BuildTaskType BuildTaskType { get; }

            public ContentItem BuildItem { get; }

            public CancellationTokenSource CancellationTokenSource { get; }
        }
        

        public ContentBuilder(ContentProject project)
        {
            Project = project;
            Game = new ContentBuilderGame();
            Game.GraphicsDevice.RemoveFromUiThread();


            _buildThreadCancellation = new CancellationTokenSource();
            _startBuild = new AutoResetEvent(false);
            var buildThreadToken = _buildThreadCancellation.Token;
            buildThreadToken.Register(() => _startBuild.Set());


            _buildQueue = new ConcurrentQueue<BuildTask>();

            _buildThread = new Thread((cancellationObj) =>
            {
                var cancellationToken = (CancellationToken)(cancellationObj ?? throw new InvalidCastException());
                while (!cancellationToken.IsCancellationRequested)
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
                                BuildStatusChanged?.Invoke(BuildStatus.Clean);
                                CleanThread(buildTask.CancellationTokenSource.Token);
                                break;
                            case BuildTaskType.Build:
                                BuildStatusChanged?.Invoke(BuildStatus.Build);
                                BuildThread(buildTask.BuildItem, buildTask.CancellationTokenSource.Token);
                                
                                if (!buildTask.CancellationTokenSource.IsCancellationRequested)
                                {
                                    BuildStatusChanged?.Invoke(BuildStatus.Built);
                                }
                                break;
                            case BuildTaskType.Rebuild:
                                if (Project.HasUnsavedChanges)
                                    Project.Save();
                                BuildStatusChanged?.Invoke(BuildStatus.Clean);
                                CleanThread(buildTask.CancellationTokenSource.Token);
                                if (!buildTask.CancellationTokenSource.IsCancellationRequested)
                                {
                                    BuildStatusChanged?.Invoke(BuildStatus.Build);
                                    BuildThread(Project, buildTask.CancellationTokenSource.Token);
                                    if (!buildTask.CancellationTokenSource.IsCancellationRequested)
                                        BuildStatusChanged?.Invoke(BuildStatus.Built);
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (buildTask.CancellationTokenSource.IsCancellationRequested)
                        {
                            BuildStatusChanged?.Invoke(BuildStatus.Abort);
                        }
                        else
                        {
                            BuildStatusChanged?.Invoke(BuildStatus.Finished);
                        }
                        buildTask.CompletionHandle.Set();
                        IsBuilding = false;
                    }
                }
            });
            _buildThread.IsBackground = true;
            _buildThread.Start(buildThreadToken);
        }

        public BuildTask Build()
        {
            return Build(Project);
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
        
        private class ContentBuilderGame : Game
        {
            public ContentBuilderGame()
                : base(new GameSettings(){Offscreen = true})
            {
                
            }
        }

        internal IGame Game { get; }

        private static string GetFullOutputDirectory(ContentProject project)
        {
            return Path.Combine(GetProjectDirectory(project),
                project.ConfiguredOutputDirectory);
        }
        private static string GetCacheFilePath(ContentProject project)
        {
            return Path.Combine(GetProjectDirectory(project), "obj",
                project.Configuration, project.Name + ".dat");
        }

        private static string GetIntermediateOutputDirectory(ContentProject project)
        {
            return Path.Combine(GetProjectDirectory(project), "obj");
        }

        private static string GetProjectDirectory(ContentProject project)
        {
            return Path.GetDirectoryName(project.ContentProjectPath);
        }

        protected void BuildThread(ContentItem item, CancellationToken cancellationToken)
        {
            FailedBuilds = 0;
            PipelineHelper.PreBuilt(Project);

            var outputDestination = GetFullOutputDirectory(Project);

            Guid buildId = Guid.NewGuid();


            var cache = BuildCache.Load(GetCacheFilePath(Project), buildId);

            var createdContentCode = cache.CreatedContentCode;

            Game.GraphicsDevice.SwitchUiThread();
            using (var iContext = new ContentImporterContext(buildId, createdContentCode, item.Project.FilePath))
            using (var pContext = new ContentProcessorContext(Game.GraphicsDevice.UiThread.SynchronizationContext, Game, buildId, createdContentCode,
                       GetProjectDirectory(Project), item.Project.FilePath))
            {

                iContext.BuildMessage += RaiseBuildMessage;
                pContext.BuildMessage += RaiseBuildMessage;
                InternalBuildItem(item, outputDestination, iContext, pContext, cancellationToken, cache);

                Game.GraphicsDevice.Context.MakeNoneCurrent();
            }


            cache.Save();
            
            
            RaiseBuildMessage(this, new BuildMessageEventArgs(string.Empty, "Building done.", BuildMessageEventArgs.BuildMessageType.Information));
        }

        protected void CleanThread(CancellationToken cancellationToken)
        {
            Guid buildId = Guid.NewGuid();
            var cache = BuildCache.Load(GetCacheFilePath(Project), buildId);
            foreach (var item in cache.Files)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (File.Exists(item.Value.OutputFilePath))
                    File.Delete(item.Value.OutputFilePath);
            }

            cache.CreatedContentCode.Clean();

            try
            {
                Directory.Delete(GetIntermediateOutputDirectory(Project), true);
            }
            catch (Exception ex) //TODO perhaps other delete, to delete all possible files?
            {
            }
            
            RaiseBuildMessage(this, new BuildMessageEventArgs(string.Empty, "Cleaning done.", BuildMessageEventArgs.BuildMessageType.Information));
        }

        private static string GetOutputDestination(string outputDestination, ContentItem item)
        {
            if (item is ContentFolder)
            {
                return item is ContentProject ? outputDestination : Path.Combine(outputDestination, item.Name);
            }

            return Path.Combine(outputDestination,
                Path.GetFileNameWithoutExtension(item.Name) + FileExtension);
        }


        public static IEnumerable<(ContentItem item, string outputPath)> EnumerateContentFiles(ContentProject project)
        {
            var outputPath = GetFullOutputDirectory(project);
            return EnumerateContentFiles(outputPath, project);
        }

        public static IEnumerable<(ContentItem item, string outputPath)> EnumerateContentFiles(string outputDestination, ContentItem item)
        {
            var outputPath = GetOutputDestination(outputDestination, item);
            if (item is ContentFolder folder)
            {
                foreach (var child in folder.Content)
                {
                    foreach (var i in EnumerateContentFiles(outputPath, child))
                    {
                        yield return i;
                    }
                }
                yield break;
            }

            yield return (item, outputPath);
        }

        private void InternalBuildItem(ContentItem buildItem, string outputDestination,
            ContentImporterContext importerContext, ContentProcessorContext processorContext, CancellationToken cancellationToken, BuildCache cache)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            foreach(var (item, outputPath) in EnumerateContentFiles(outputDestination, buildItem))
            {
                if (cache.NeedsRebuild(importerContext.GetRelativePathToContentDirectory(item.FilePath), item.FilePath))
                {
                    var res = InternalBuildFile(item as ContentFile,
                        outputPath, importerContext,
                        processorContext, cancellationToken, cache);
                    if (res == null)
                    {
                        RaiseBuildMessage(this,
                            new BuildMessageEventArgs(item.RelativePath, item.RelativePath + " build failed",
                                BuildMessageEventArgs.BuildMessageType.Error));
                    }
                    else
                    {
                        RaiseBuildMessage(this,
                                new BuildMessageEventArgs(item.RelativePath, item.RelativePath + " built",
                                    BuildMessageEventArgs.BuildMessageType.Success));
                    }
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
            ContentProcessorContext processorContext, CancellationToken cancellationToken, BuildCache cache)
        {
            return BuildFile(item, destination, importerContext, processorContext, cancellationToken, cache);
        }

        /// <summary>
        /// Builds a ContentFile and writes it to the destinatin
        /// </summary>
        /// <param name="item">ContentFile to build</param>
        /// <param name="destination">Location to save to</param>
        /// <param name="cancellationToken"></param>
        /// <param name="importerContext"></param>
        /// <param name="processorContext"></param>
        /// <param name="cache"></param>
        /// <returns></returns>
        public static object BuildFile(ContentFile item, string destination, ContentImporterContext importerContext,
            ContentProcessorContext processorContext, CancellationToken cancellationToken, BuildCache cache = null)
        {
            if (!File.Exists(item.FilePath)) return null;
            if (cancellationToken.IsCancellationRequested)
                return null;

            var dirName = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(dirName);

            var buildFile = new BuildFile(processorContext.BuildId, item.FilePath, destination, cache?.ContentManager);
            if (cancellationToken.IsCancellationRequested)
                return null;
            var importer = item.Importer;

            object dependencyImport;
            if (item.DependencyImport is null)
            {
                item.Dependencies.Clear();
                dependencyImport = importer?.DependencyImportBase(item.FilePath, importerContext, item.Dependencies);
            }
            else
            {
                dependencyImport = item.DependencyImport;
            }

            var importedFile = importer?.Import(item.FilePath, importerContext, dependencyImport);
            if (importedFile == null) return null;
            if (cancellationToken.IsCancellationRequested)
                return null;
            buildFile.Dependencies.AddRange(item.Dependencies);
            buildFile.Dependencies.AddRange(importerContext.Dependencies);
            cache?.AddDependencies(importerContext.BuildId, Path.GetDirectoryName(item.FilePath), importerContext.Dependencies);

            var processor = item.Processor;

            var processedFile = processor?.Process(importedFile, item.FilePath, processorContext);
            if (processedFile == null) return null;
            if (cancellationToken.IsCancellationRequested)
                return null;
            buildFile.Dependencies.AddRange(processorContext.Dependencies);
            cache?.AddDependencies(processorContext.BuildId, Path.GetDirectoryName(item.FilePath), processorContext.Dependencies);

            var typeWriter = SerializationManager.Instance.GetWriter(processedFile.GetType());
            var outputContentFileWriter = new engenious.Content.ContentFile(typeWriter.RuntimeReaderName, typeWriter.ContentVersion);

            using (var fs = new FileStream(destination, FileMode.Create, FileAccess.Write))
            {
                fs.Write(
                    BitConverter.GetBytes(
                        engenious.Helper.BitHelper.BitConverterToBigEndian(engenious.Content.ContentFile.Magic)), 0,
                    sizeof(uint));

                const byte writerVersion = 2;
                fs.WriteByte(writerVersion);

                var fileTypeBuffer = System.Text.Encoding.UTF8.GetBytes(outputContentFileWriter.FileType);
                fs.Write(
                    BitConverter.GetBytes(
                        engenious.Helper.BitHelper.BitConverterToLittleEndian((uint)fileTypeBuffer.Length)), 0,
                    sizeof(uint));

                fs.Write(fileTypeBuffer, 0, fileTypeBuffer.Length);
                
                fs.Write(BitConverter.GetBytes(engenious.Helper.BitHelper.BitConverterToLittleEndian(outputContentFileWriter.ContentVersion)));

                var writer = new ContentWriter(fs);
                writer.WriteObject(processedFile, typeWriter);
            }

            cache?.AddFile(item.FilePath, buildFile);
            buildFile.RefreshBuildCache(processorContext.BuildId, cache?.ContentManager);
            buildFile.CreatesUserContent = processorContext.CreatedContentCode.CreatesUserContent(processorContext.GetRelativePathToContentDirectory(item.FilePath));

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
            Game.Dispose();
            _buildThreadCancellation.Cancel();
            _startBuild.Set();
            _buildThread.Join();
        }
    }
}