using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;

namespace Willow.Analysis
{
    internal sealed class ProjectAnalyzer
    {
        // Cache of "transitive packages exposed by this project" keyed by Project.
        // Lives for the lifetime of the analyzer so it is shared across every
        // root project in a solution sweep.
        private readonly Dictionary<Project, Dictionary<string, ProjectPackage>> _accumulatedCache
            = new Dictionary<Project, Dictionary<string, ProjectPackage>>(new ProjectComparer());

        public ProjectAnalyzerResult Analyze(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var result = new List<PackageToRemove>();

            if (project.ProjectReferences.Count > 0)
            {
                // Merge the accumulated package sets exposed by each child reference.
                // First reference wins on collisions (matches the original semantics).
                var accumulated = new Dictionary<string, ProjectPackage>(StringComparer.OrdinalIgnoreCase);
                foreach (var child in project.ProjectReferences)
                {
                    foreach (var kvp in GetAccumulated(child))
                    {
                        if (!accumulated.ContainsKey(kvp.Key))
                        {
                            accumulated[kvp.Key] = kvp.Value;
                        }
                    }
                }

                // Any package declared on the root that also comes in transitively
                // is a candidate for removal.
                var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var package in project.Packages)
                {
                    // GlobalPackageReference items live in Directory.Packages.props and are
                    // injected into every project — they can't be removed from a csproj.
                    if (package.IsGlobalPackageReference)
                    {
                        continue;
                    }

                    if (accumulated.TryGetValue(package.Name, out var found))
                    {
                        if (added.Add(package.Name))
                        {
                            result.Add(new PackageToRemove(project, package, found));
                        }
                    }
                }
            }

            if (project.LockFilePath != null)
            {
                // Now prune stuff that we're not interested in removing
                // such as private package references and analyzers.
                result = PruneResults(project, result);
            }

            return new ProjectAnalyzerResult(project, result);
        }

        // Returns the set of packages this project "exposes" to its parents -
        // every transitively-reachable package plus the project's own non-private
        // package references. Memoized so each project in the DAG is walked once.
        private Dictionary<string, ProjectPackage> GetAccumulated(Project project)
        {
            if (_accumulatedCache.TryGetValue(project, out var cached))
            {
                return cached;
            }

            var accumulated = new Dictionary<string, ProjectPackage>(StringComparer.OrdinalIgnoreCase);

            foreach (var child in project.ProjectReferences)
            {
                foreach (var kvp in GetAccumulated(child))
                {
                    if (!accumulated.ContainsKey(kvp.Key))
                    {
                        accumulated[kvp.Key] = kvp.Value;
                    }
                }
            }

            foreach (var package in project.Packages)
            {
                if (package.IsGlobalPackageReference)
                {
                    continue;
                }

                if (package.PrivateAssets != null && package.PrivateAssets.Contains("compile"))
                {
                    continue;
                }

                if (!accumulated.ContainsKey(package.Name))
                {
                    accumulated[package.Name] = new ProjectPackage(project, package);
                }
            }

            _accumulatedCache[project] = accumulated;
            return accumulated;
        }

        private static List<PackageToRemove> PruneResults(Project project, List<PackageToRemove> packages)
        {
            // Read the lockfile.
            var lockfile = new LockFileFormat().Read(project.LockFilePath);

            // Find the expected target.
            var framework = NuGetFramework.Parse(project.TargetFramework);
            var target = lockfile.PackageSpec.TargetFrameworks.FirstOrDefault(
                x => x.FrameworkName.Framework.Equals(framework.Framework, StringComparison.OrdinalIgnoreCase));

            // Could we not find the target?
            if (target == null)
            {
                throw new InvalidOperationException("Could not determine target framework");
            }

            var result = new List<PackageToRemove>();
            foreach (var package in packages)
            {
                // Try to find the dependency.
                var dependency = target.Dependencies.FirstOrDefault(
                    x => x.Name.Equals(package.Package.Name, StringComparison.OrdinalIgnoreCase));

                if (dependency != null)
                {
                    // Auto referenced or private package?
                    if (dependency.AutoReferenced ||
                        dependency.SuppressParent == LibraryIncludeFlags.All)
                    {
                        continue;
                    }
                }

                result.Add(package);
            }

            return result;
        }
    }
}