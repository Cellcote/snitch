using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Snitch.Analysis
{
    internal static class CentralPackageManagementReader
    {
        private const string DirectoryPackagesPropsFileName = "Directory.Packages.props";

        public static CentralPackageManagementInfo? Read(string? centralPackagesFilePath, string projectDirectory)
        {
            var path = centralPackagesFilePath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                path = FindDirectoryPackagesProps(projectDirectory);
            }

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            var globalPackageReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var centralPackageVersions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var document = XDocument.Load(path);
                if (document.Root == null)
                {
                    return new CentralPackageManagementInfo(path);
                }

                foreach (var element in document.Root.Descendants())
                {
                    var name = element.Name.LocalName;
                    if (string.Equals(name, "GlobalPackageReference", StringComparison.OrdinalIgnoreCase))
                    {
                        var id = element.Attribute("Include")?.Value;
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            globalPackageReferences.Add(id);
                        }
                    }
                    else if (string.Equals(name, "PackageVersion", StringComparison.OrdinalIgnoreCase))
                    {
                        var id = element.Attribute("Include")?.Value;
                        var version = element.Attribute("Version")?.Value;
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(version))
                        {
                            centralPackageVersions[id] = version;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If we can't parse the file, fall back to an empty CPM info so we
                // at least track that CPM is in use.
                return new CentralPackageManagementInfo(path);
            }

            return new CentralPackageManagementInfo(path, globalPackageReferences, centralPackageVersions);
        }

        private static string? FindDirectoryPackagesProps(string projectDirectory)
        {
            if (string.IsNullOrWhiteSpace(projectDirectory))
            {
                return null;
            }

            var current = new DirectoryInfo(projectDirectory);
            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, DirectoryPackagesPropsFileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            return null;
        }
    }
}