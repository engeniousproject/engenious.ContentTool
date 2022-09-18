﻿using System;
using System.Collections.Generic;
using System.IO;
using engenious.Content;
using engenious.Content.Pipeline;
using NonSucking.Framework.Serialization;

namespace engenious.ContentTool.Builder
{
    [Nooson]
    public partial class BuildCache
    {
        public uint Magic
        {
            get => 0x45474f43;
            set
            {
                if (value != Magic)
                    throw new FormatException("Invalid magic header");
            }
        }
        public string CacheFilePath { get; private set; }
        
        [NoosonIgnore]
        public CreatedContentCode CreatedContentCode { get; set; }
        public Dictionary<string, BuildFile> Files { get; private init; } = new();

        internal ContentManagerBase ContentManager;

        protected BuildCache()
        {
            ContentManager = new AggregateContentManager(null!, string.Empty);
        }
        protected BuildCache(string cacheFilePath, CreatedContentCode createdContentCode)
            : this()
        {
            CacheFilePath = cacheFilePath;
            CreatedContentCode = createdContentCode;
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
                    Files[absPath].RefreshBuildCache(buildId, ContentManager);
                else
                    Files.Add(absPath, new BuildFile(buildId, absPath,null, ContentManager));

            }
        }

        public bool NeedsRebuild(string relativePath, string inputPath, DateTime? parentModifiedTime=null)
        {
            if(Files.TryGetValue(inputPath, out BuildFile buildFile))
            {
                if (buildFile.NeedsRebuild(ContentManager, parentModifiedTime))
                    return true;
                foreach (var dependency in buildFile.Dependencies)
                {
                    if (NeedsRebuild(relativePath, dependency,parentModifiedTime ?? buildFile.OutputFileModifiedTime))
                        return true;
                }

                var createdContentBuildId = CreatedContentCode.GetTypeContainerBuildId(relativePath);
                if (createdContentBuildId == null)
                    return buildFile.CreatesUserContent;
                // if (createdContentBuildId == null)
                //     return true;
                return buildFile.BuildId != createdContentBuildId;
            }
            return true;
        }

        public bool HasBuiltItems()
        {
            foreach (var file  in Files)
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
            
            var contentCodePath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, Path.GetFileNameWithoutExtension(path) + ".CreatedCode.dat");
            CreatedContentCode.Save(contentCodePath);
            
            try
            {
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
                using var bw = new BinaryWriter(fs);
                Serialize(bw);
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
        public static BuildCache Load(string cacheFilePath, Guid buildId)
        {
            var contentCodePath = Path.Combine(Path.GetDirectoryName(cacheFilePath) ?? string.Empty, Path.GetFileNameWithoutExtension(cacheFilePath) + ".CreatedCode.dat");

            var contentCode = CreatedContentCode.Load(contentCodePath, buildId);
            if (!File.Exists(cacheFilePath))
            {
                var cache = new BuildCache(cacheFilePath, contentCode);
                return cache;
            }

            try
            {
                using var fs = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read);
                using var br = new BinaryReader(fs);
                var cache = Deserialize(br);
                cache.CacheFilePath = cacheFilePath;
                cache.CreatedContentCode = contentCode;
                cache.ContentManager = new AggregateContentManager(null!, string.Empty);
                return cache;
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
                return Load(cacheFilePath, buildId);
            }
        }
    }
}
