using System;
using System.Reflection;
using System.Runtime.Loader;

namespace engenious.ContentTool;

public class PluginLoadContext : AssemblyLoadContext, IDisposable
{
    private AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        Default.Resolving += DefaultOnResolving;
    }

    private Assembly DefaultOnResolving(AssemblyLoadContext context, AssemblyName name)
    {
        return LoadFromAssemblyName(name);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return Default.LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Default.Resolving -= DefaultOnResolving;
    }
}