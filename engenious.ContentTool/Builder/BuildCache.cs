using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using engenious.Content.Pipeline;

namespace engenious.ContentTool.Builder
{
    [Serializable]
    public class BuildCache
    {
        public string CacheFilePath { get; private set; }
        
        [field: NonSerialized]
        public AssemblyCreatedContent AssemblyCreatedContent { get; set; }
        public Dictionary<string, BuildFile> Files { get; } = new Dictionary<string, BuildFile>();

        protected BuildCache(string cacheFilePath, AssemblyCreatedContent assemblyCreatedContent)
        {
            CacheFilePath = cacheFilePath;
            AssemblyCreatedContent = assemblyCreatedContent;
            //Files
        }

        public void AddFile(string path, BuildFile file)
        {
            Files[path] = file;
        }

        public BuildFile GetFile(string path)
        {
            return Files.TryGetValue(path, out var ret) ? ret : null;
        }
        

        public void AddDependencies(Guid buildId, string importDir, IEnumerable<string> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                var absPath = Path.Combine(importDir, dependency);
                if (Files.ContainsKey(absPath))
                    Files[absPath].RefreshBuildCache(buildId);
                else
                    Files.Add(absPath, new BuildFile(buildId, absPath,null));

            }
        }

        public bool NeedsRebuild(string relativePath, string inputPath, DateTime? parentModifiedTime=null)
        {
            if(Files.TryGetValue(inputPath, out BuildFile buildFile))
            {
                if (buildFile.NeedsRebuild(parentModifiedTime))
                    return true;
                foreach (var dependency in buildFile.Dependencies)
                {
                    if (NeedsRebuild(relativePath, dependency,parentModifiedTime ?? buildFile.OutputFileModifiedTime))
                        return true;
                }

                if (!AssemblyCreatedContent.MostRecentBuildFileBuildIdMapping.TryGetValue(relativePath,
                    out var createdContentBuildId))
                    return buildFile.CreatesUserContent;
                if (createdContentBuildId == null)
                    return true;
                return buildFile.BuildId != createdContentBuildId;
            }
            return true;
        }

        public bool HasBuiltItems()
        {
            foreach (var file in Files)
            {
                if (file.Value.IsBuilt())
                    return true;
            }

            return false;
        }

        public void Clean()
        {
            
        }

        public void Clear()
        {
            Files.Clear();
        }

        /// <summary>
        /// Saves the cache to the original location
        /// </summary>
        public void Save()
        {
            Save(CacheFilePath);
        }

        /// <summary>
        /// Saves the cache to the specified location
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, this);
                }
                CacheFilePath = path;
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Loads the cache from the specified location or returns a new one if the file could not be found
        /// </summary>
        /// <param name="cacheFilePath">Location of the cache file</param>
        /// <returns>The build cache</returns>
        public static BuildCache Load(string cacheFilePath, AssemblyCreatedContent assemblyCreatedContent)
        {
            if (!File.Exists(cacheFilePath))
            {
                var cache = new BuildCache(cacheFilePath, assemblyCreatedContent);

                return cache;
            }

            try
            {
                using (var fs = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read))
                {
                    var formatter = new BinaryFormatter();
                    var cache = (BuildCache) formatter.Deserialize(fs);
                    cache.AssemblyCreatedContent = assemblyCreatedContent;
                    return cache;
                }
            }
            catch
            {
                try
                {
                    File.Delete(cacheFilePath);
                }
                catch
                {
                    // ignored
                }
                return Load(cacheFilePath, assemblyCreatedContent);
            }
        }
    }
}
