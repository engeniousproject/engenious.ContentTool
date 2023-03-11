using System;

namespace engenious.ContentTool
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class ShellFactoryAttribute : Attribute
    {
        public ShellFactoryAttribute(Type shellFactoryType)
        {
            if (shellFactoryType.IsAssignableFrom(typeof(IShellFactory)))
                throw new TypeLoadException($"Type passed to '{nameof(shellFactoryType)}' needs to implement the '{nameof(IShellFactory)} interface.'");
            ShellFactoryType = shellFactoryType;
        }

        public Type ShellFactoryType { get; }
    }
}