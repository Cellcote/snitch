using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NuGet.Frameworks;
using NuGet.ProjectModel;

namespace Snitch.Analysis
{
    internal sealed class PackageDependencyExplorer
    {
        public PackageDependencyResult Explore([NotNull] Project project, string targetPackage)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrWhiteSpace(targetPackage))
            {
                throw new ArgumentException("Target package name is required.", nameof(targetPackage));
            }

            if (string.IsNullOrEmpty(project.LockFilePath) || !File.Exists(project.LockFilePath))
            {
                return new PackageDependencyResult(project, new List<DependencyPath>(), lockFileMissing: true);
            }

            var lockFile = new LockFileFormat().Read(project.LockFilePath);
            var framework = NuGetFramework.Parse(project.TargetFramework);

            var target = lockFile.Targets
                .Where(t => string.IsNullOrEmpty(t.RuntimeIdentifier))
                .FirstOrDefault(t => t.TargetFramework.Framework.Equals(framework.Framework, StringComparison.OrdinalIgnoreCase));

            var spec = lockFile.PackageSpec?.TargetFrameworks
                .FirstOrDefault(t => t.FrameworkName.Framework.Equals(framework.Framework, StringComparison.OrdinalIgnoreCase));

            if (target == null || spec == null)
            {
                return new PackageDependencyResult(project, new List<DependencyPath>(), lockFileMissing: false);
            }

            var libraries = new Dictionary<string, LockFileTargetLibrary>(StringComparer.OrdinalIgnoreCase);
            foreach (var library in target.Libraries)
            {
                if (library.Name == null)
                {
                    continue;
                }

                libraries[library.Name] = library;
            }

            var roots = new List<string>();
            foreach (var direct in spec.Dependencies)
            {
                roots.Add(direct.Name);
            }

            var restoreFramework = lockFile.PackageSpec?.RestoreMetadata?.TargetFrameworks
                .FirstOrDefault(t => t.FrameworkName.Framework.Equals(framework.Framework, StringComparison.OrdinalIgnoreCase));
            if (restoreFramework != null)
            {
                foreach (var projectRef in restoreFramework.ProjectReferences)
                {
                    roots.Add(Path.GetFileNameWithoutExtension(projectRef.ProjectUniqueName));
                }
            }

            var paths = new List<DependencyPath>();

            foreach (var root in roots)
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var path = new List<DependencyNode>();
                Dfs(root, libraries, targetPackage, path, visited, paths);
            }

            return new PackageDependencyResult(project, paths, lockFileMissing: false);
        }

        private static void Dfs(
            string nodeName,
            Dictionary<string, LockFileTargetLibrary> libraries,
            string target,
            List<DependencyNode> path,
            HashSet<string> visited,
            List<DependencyPath> results)
        {
            if (!visited.Add(nodeName))
            {
                return;
            }

            var version = "?";
            var type = "package";
            libraries.TryGetValue(nodeName, out var lib);
            if (lib != null)
            {
                version = lib.Version?.ToString() ?? "?";
                type = lib.Type ?? "package";
            }

            path.Add(new DependencyNode(nodeName, version, type));

            if (nodeName.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new DependencyPath(new List<DependencyNode>(path)));
            }
            else if (lib != null)
            {
                foreach (var dep in lib.Dependencies)
                {
                    Dfs(dep.Id, libraries, target, path, visited, results);
                }
            }

            path.RemoveAt(path.Count - 1);
            visited.Remove(nodeName);
        }
    }
}