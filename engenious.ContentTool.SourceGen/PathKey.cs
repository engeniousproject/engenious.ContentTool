using System;
using System.IO;

namespace engenious.ContentTool.SourceGen
{
    public readonly struct PathKey : IEquatable<PathKey>
    {
        private readonly string _path;

        private static string NormalizePath(string path)
        {
            path = path.Replace('\\', '/');
            if (path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);
            return path;
        }

        public PathKey(string path)
        {
            _path = NormalizePath(path);
        }

        public bool Equals(PathKey other)
        {
            return _path == other._path;
        }

        public override bool Equals(object? obj)
        {
            return obj is PathKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _path.GetHashCode();
        }

        public static implicit operator PathKey(string path)
        {
            return new PathKey(path);
        }
    }
}