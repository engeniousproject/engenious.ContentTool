using System.Collections.Generic;
using System.Reflection;

namespace engenious.ContentTool
{
    public static class ReferenceManager
    {
        public static List<Assembly> References { get; } = new List<Assembly>();
    }
}