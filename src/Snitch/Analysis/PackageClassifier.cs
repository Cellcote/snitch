using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Snitch.Analysis
{
    internal sealed class PackageClassifier
    {
        private readonly List<Regex> _patterns;

        public bool IsConfigured => _patterns.Count > 0;

        public PackageClassifier(IEnumerable<string>? patterns)
        {
            _patterns = new List<Regex>();
            if (patterns == null)
            {
                return;
            }

            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    continue;
                }

                _patterns.Add(BuildRegex(pattern.Trim()));
            }
        }

        public bool IsInternal(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            return IsInternal(package.Name);
        }

        public bool IsInternal(string packageName)
        {
            if (string.IsNullOrEmpty(packageName) || _patterns.Count == 0)
            {
                return false;
            }

            return _patterns.Any(p => p.IsMatch(packageName));
        }

        private static Regex BuildRegex(string pattern)
        {
            if (!pattern.Contains('*') && !pattern.Contains('?'))
            {
                // No wildcards: treat as a prefix. Matches the exact name or names
                // with a "." separator after the prefix (e.g. "UiPath" matches "UiPath"
                // and "UiPath.Foo" but not "UiPathFoo").
                return new Regex(
                    "^" + Regex.Escape(pattern) + "(\\..*)?$",
                    RegexOptions.IgnoreCase);
            }

            var escaped = Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".");

            return new Regex("^" + escaped + "$", RegexOptions.IgnoreCase);
        }
    }
}