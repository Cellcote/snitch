using System;
using System.Collections.Generic;

namespace Bonsai.Analysis
{
    internal sealed class DependencyPath
    {
        public IReadOnlyList<DependencyNode> Nodes { get; }

        public DependencyPath(IReadOnlyList<DependencyNode> nodes)
        {
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }
    }
}