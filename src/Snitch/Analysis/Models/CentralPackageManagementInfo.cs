using System;
using System.Collections.Generic;

namespace Snitch.Analysis
{
    internal sealed class CentralPackageManagementInfo
    {
        public string CentralPackagesFilePath { get; }
        public HashSet<string> GlobalPackageReferences { get; }
        public Dictionary<string, string> CentralPackageVersions { get; }

        public CentralPackageManagementInfo(
            string centralPackagesFilePath,
            HashSet<string>? globalPackageReferences = null,
            Dictionary<string, string>? centralPackageVersions = null)
        {
            CentralPackagesFilePath = centralPackagesFilePath ?? throw new ArgumentNullException(nameof(centralPackagesFilePath));
            GlobalPackageReferences = globalPackageReferences ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CentralPackageVersions = centralPackageVersions ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool IsGlobalPackageReference(string packageName)
        {
            return packageName != null && GlobalPackageReferences.Contains(packageName);
        }
    }
}