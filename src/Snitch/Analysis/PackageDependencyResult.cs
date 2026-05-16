using System;
using System.Collections.Generic;

namespace Snitch.Analysis
{
    internal sealed class PackageDependencyResult
    {
        public Project Project { get; }
        public IReadOnlyList<DependencyPath> Paths { get; }
        public bool LockFileMissing { get; }

        public bool HasPaths => Paths.Count > 0;

        public PackageDependencyResult(Project project, IReadOnlyList<DependencyPath> paths, bool lockFileMissing)
        {
            Project = project ?? throw new ArgumentNullException(nameof(project));
            Paths = paths ?? throw new ArgumentNullException(nameof(paths));
            LockFileMissing = lockFileMissing;
        }
    }
}