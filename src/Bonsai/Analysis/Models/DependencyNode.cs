using System;

namespace Bonsai.Analysis
{
    internal sealed class DependencyNode
    {
        public string Name { get; }
        public string Version { get; }
        public string Type { get; }

        public bool IsProject => Type.Equals("project", StringComparison.OrdinalIgnoreCase);

        public DependencyNode(string name, string version, string type)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? "?";
            Type = type ?? "package";
        }
    }
}