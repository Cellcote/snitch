using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Snitch.Analysis.Utilities
{
    /// <summary>
    /// Provides topological sorting of projects based on their project references.
    /// Processing leaf projects first maximizes cache effectiveness and avoids
    /// redundant dependency analysis.
    /// </summary>
    internal static class DependencyGraph
    {
        /// <summary>
        /// Sorts project paths topologically so that leaf projects (those with no
        /// dependencies on other projects in the list) come first. Within the same
        /// dependency level, projects are sorted alphabetically for deterministic ordering.
        /// </summary>
        public static List<string> TopologicalSort(List<string> projectPaths)
        {
            if (projectPaths == null || projectPaths.Count <= 1)
            {
                return projectPaths ?? new List<string>();
            }

            // Map filenames to full paths for lookup.
            var pathByFilename = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in projectPaths)
            {
                var filename = Path.GetFileName(path);
                pathByFilename[filename] = path;
            }

            // Build dependency graph: filename -> set of dependency filenames (within our list only).
            var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in projectPaths)
            {
                var filename = Path.GetFileName(path);
                dependencies[filename] = GetProjectReferences(path, pathByFilename);
            }

            // Kahn's algorithm with alphabetical ordering within each level.
            var inDegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dependents = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var filename in dependencies.Keys)
            {
                if (!inDegree.ContainsKey(filename))
                {
                    inDegree[filename] = 0;
                }

                if (!dependents.ContainsKey(filename))
                {
                    dependents[filename] = new List<string>();
                }

                foreach (var dep in dependencies[filename])
                {
                    if (!inDegree.ContainsKey(dep))
                    {
                        inDegree[dep] = 0;
                    }

                    if (!dependents.ContainsKey(dep))
                    {
                        dependents[dep] = new List<string>();
                    }

                    dependents[dep].Add(filename);
                    inDegree[filename]++;
                }
            }

            // Use SortedSet for deterministic (alphabetical) ordering among projects at the same level.
            var ready = new SortedSet<string>(
                inDegree.Where(x => x.Value == 0).Select(x => x.Key),
                StringComparer.OrdinalIgnoreCase);

            var result = new List<string>();

            while (ready.Count > 0)
            {
                var current = ready.Min!;
                ready.Remove(current);
                result.Add(pathByFilename[current]);

                if (dependents.TryGetValue(current, out var deps))
                {
                    foreach (var dependent in deps)
                    {
                        inDegree[dependent]--;
                        if (inDegree[dependent] == 0)
                        {
                            ready.Add(dependent);
                        }
                    }
                }
            }

            // Handle circular dependencies gracefully by appending remaining projects.
            if (result.Count < projectPaths.Count)
            {
                var sorted = new HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
                foreach (var path in projectPaths)
                {
                    if (!sorted.Contains(path))
                    {
                        result.Add(path);
                    }
                }
            }

            return result;
        }

        private static HashSet<string> GetProjectReferences(
            string projectPath,
            Dictionary<string, string> knownProjects)
        {
            var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var doc = XDocument.Load(projectPath);
                foreach (var element in doc.Descendants()
                    .Where(e => e.Name.LocalName == "ProjectReference"))
                {
                    var include = element.Attribute("Include")?.Value;
                    if (include != null)
                    {
                        var refFilename = Path.GetFileName(include);
                        if (knownProjects.ContainsKey(refFilename))
                        {
                            refs.Add(refFilename);
                        }
                    }
                }
            }
            catch
            {
                // If we can't parse the project file, treat it as having no known dependencies.
                // This is a best-effort optimization; the build step will catch actual errors.
            }

            return refs;
        }
    }
}