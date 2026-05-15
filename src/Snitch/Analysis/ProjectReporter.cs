using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Snitch.Analysis.Vulnerabilities;
using Spectre.Console;

namespace Snitch.Analysis
{
    internal class ProjectReporter
    {
        private readonly IAnsiConsole _console;

        public ProjectReporter(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public void WriteToConsole([NotNull] List<ProjectAnalyzerResult> results, bool noPreRelease)
        {
            WriteToConsole(results, noPreRelease, VulnerabilityReport.Empty, new PackageClassifier(null));
        }

        public void WriteToConsole(
            [NotNull] List<ProjectAnalyzerResult> results,
            bool noPreRelease,
            [NotNull] VulnerabilityReport vulnerabilityReport)
        {
            WriteToConsole(results, noPreRelease, vulnerabilityReport, new PackageClassifier(null));
        }

        public void WriteToConsole(
            [NotNull] List<ProjectAnalyzerResult> results,
            bool noPreRelease,
            [NotNull] VulnerabilityReport vulnerabilityReport,
            [NotNull] PackageClassifier classifier)
        {
            var showVulns = !vulnerabilityReport.IsEmpty;

            if (results.All(x => x.NoPackagesToRemove)
                && (!noPreRelease || results.All(r => !r.HasPreReleases))
                && !showVulns)
            {
                _console.WriteLine();
                _console.MarkupLine("[green]Everything looks good![/]");
                _console.WriteLine();
                return;
            }

            var report = new Grid();
            report.AddColumn();

            if (classifier.IsConfigured)
            {
                var hasInternal = HasAnythingToReport(results, p => classifier.IsInternal(p), noPreRelease);
                var hasExternal = HasAnythingToReport(results, p => !classifier.IsInternal(p), noPreRelease);

                if (hasInternal)
                {
                    report.AddRow(" [yellow u]Internal[/] [grey]— fixable at source (open a PR upstream)[/]");
                    report.AddEmptyRow();
                    AddResultsToReport(report, results, p => classifier.IsInternal(p), noPreRelease, vulnerabilityReport);
                }

                if (hasInternal && hasExternal)
                {
                    report.AddEmptyRow();
                }

                if (hasExternal)
                {
                    report.AddRow(" [yellow u]External[/] [grey]— must wait or override[/]");
                    report.AddEmptyRow();
                    AddResultsToReport(report, results, p => !classifier.IsInternal(p), noPreRelease, vulnerabilityReport);
                }
            }
            else
            {
                AddResultsToReport(report, results, _ => true, noPreRelease, vulnerabilityReport);
            }

            if (showVulns)
            {
                report.AddEmptyRow();
                report.AddRow(" [yellow]Vulnerable packages:[/]");

                var table = new Table().BorderColor(Color.Grey).Expand();
                table.AddColumns(
                    "[grey]Package[/]",
                    "[grey]Version[/]",
                    "[grey]Severity[/]",
                    "[grey]Advisory[/]",
                    "[grey]Fixed in[/]");

                var rows = vulnerabilityReport.Entries
                    .SelectMany(entry => entry.Vulnerabilities.Select(v => (entry.Package, Vulnerability: v)))
                    .OrderByDescending(r => r.Vulnerability.Severity)
                    .ThenBy(r => r.Package.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(r => r.Package.Version, StringComparer.OrdinalIgnoreCase);

                foreach (var row in rows)
                {
                    table.AddRow(
                        $"[yellow]{Markup.Escape(row.Package.Name)}[/]",
                        Markup.Escape(row.Package.Version),
                        FormatSeverity(row.Vulnerability.Severity),
                        Markup.Escape(row.Vulnerability.PrimaryAlias),
                        Markup.Escape(row.Vulnerability.FixedVersion ?? "-"));
                }

                report.AddRow(table);
            }

            _console.WriteLine();
            _console.Write(
                new Panel(report)
                    .RoundedBorder()
                    .BorderColor(Color.Grey));
        }

        private static bool HasAnythingToReport(
            List<ProjectAnalyzerResult> results,
            Func<Package, bool> predicate,
            bool noPreRelease)
        {
            foreach (var result in results)
            {
                if (result.CanBeRemoved.Any(p => predicate(p.Package)))
                {
                    return true;
                }

                if (result.MightBeRemoved.Any(p => predicate(p.Package)))
                {
                    return true;
                }
            }

            if (noPreRelease)
            {
                foreach (var result in results)
                {
                    if (result.PreReleasePackages.Any(predicate))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void AddResultsToReport(
            Grid report,
            List<ProjectAnalyzerResult> results,
            Func<Package, bool> predicate,
            bool noPreRelease,
            VulnerabilityReport vulnerabilityReport)
        {
            var showVulns = !vulnerabilityReport.IsEmpty;

            var resultsWithPackageToRemove = results
                .Select(r => new FilteredResult<PackageToRemove>(r, r.CanBeRemoved.Where(p => predicate(p.Package)).ToList()))
                .Where(x => x.Items.Count > 0)
                .ToList();

            var resultsWithPackageMayBeRemove = results
                .Select(r => new FilteredResult<PackageToRemove>(r, r.MightBeRemoved.Where(p => predicate(p.Package)).ToList()))
                .Where(x => x.Items.Count > 0)
                .ToList();

            var resultsWithPreReleases = results
                .Select(r => new FilteredResult<Package>(r, r.PreReleasePackages.Where(predicate).ToList()))
                .Where(x => x.Items.Count > 0)
                .ToList();

            if (resultsWithPackageToRemove.Count > 0)
            {
                foreach (var (_, _, last, item) in resultsWithPackageToRemove.Enumerate())
                {
                    var table = new Table().BorderColor(Color.Grey).Expand();
                    if (showVulns)
                    {
                        table.AddColumns("[grey]Package[/]", "[grey]Referenced by[/]", "[grey]Severity[/]");
                    }
                    else
                    {
                        table.AddColumns("[grey]Package[/]", "[grey]Referenced by[/]");
                    }

                    foreach (var pkg in item.Items)
                    {
                        var packageName = $"[green]{pkg.Package.Name}[/]";
                        var referencedBy = $"[aqua]{pkg.Original.Project.Name}[/]";
                        if (showVulns)
                        {
                            var severity = HighestSeverityFor(vulnerabilityReport, pkg.Package, pkg.Original.Package);
                            table.AddRow(packageName, referencedBy, FormatSeverity(severity));
                        }
                        else
                        {
                            table.AddRow(packageName, referencedBy);
                        }
                    }

                    var cpmSuffix = item.Result.IsCpmEnabled ? " [grey](CPM)[/]" : string.Empty;
                    report.AddRow($" [yellow]Packages that can be removed from[/] [aqua]{item.Result.Project}[/]{cpmSuffix}:");
                    report.AddRow(table);

                    if (item.Result.IsCpmEnabled)
                    {
                        report.AddRow($"   [grey]Remove the [/][silver]<PackageReference>[/][grey] entry from[/] [aqua]{item.Result.Project}[/][grey]; versions remain in[/] [silver]Directory.Packages.props[/][grey].[/]");
                    }

                    if (!last || (last && resultsWithPackageMayBeRemove.Count > 0))
                    {
                        report.AddEmptyRow();
                    }
                }
            }

            if (resultsWithPackageMayBeRemove.Count > 0)
            {
                foreach (var (_, _, last, item) in resultsWithPackageMayBeRemove.Enumerate())
                {
                    var table = new Table().BorderColor(Color.Grey).Expand();
                    if (showVulns)
                    {
                        table.AddColumns("[grey]Package[/]", "[grey]Version[/]", "[grey]Reason[/]", "[grey]Severity[/]");
                    }
                    else
                    {
                        table.AddColumns("[grey]Package[/]", "[grey]Version[/]", "[grey]Reason[/]");
                    }

                    foreach (var pkg in item.Items)
                    {
                        string reason;
                        if (pkg.Package.IsGreaterThan(pkg.Original.Package, out var indeterminate))
                        {
                            var name = pkg.Original.Project.Name;
                            var version = pkg.Original.Package.GetVersionString();
                            var verb = indeterminate ? "Might be updated from" : "Updated from";
                            reason = $"[grey]{verb}[/] [silver]{version}[/] [grey]in[/] [aqua]{name}[/]";
                        }
                        else
                        {
                            var name = pkg.Original.Project.Name;
                            var version = pkg.Original.Package.GetVersionString();
                            var verb = indeterminate ? "Does not match" : "Downgraded from";
                            reason = $"[grey]{verb}[/] [silver]{version}[/] [grey]in[/] [aqua]{name}[/]";
                        }

                        if (showVulns)
                        {
                            var severity = HighestSeverityFor(vulnerabilityReport, pkg.Package, pkg.Original.Package);
                            table.AddRow(
                                $"[green]{pkg.Package.Name}[/]",
                                pkg.Package.GetVersionString(),
                                reason,
                                FormatSeverity(severity));
                        }
                        else
                        {
                            table.AddRow(
                                $"[green]{pkg.Package.Name}[/]",
                                pkg.Package.GetVersionString(),
                                reason);
                        }
                    }

                    var cpmSuffix = item.Result.IsCpmEnabled ? " [grey](CPM)[/]" : string.Empty;
                    report.AddRow($" [yellow]Packages that [u]might[/] be removed from[/] [aqua]{item.Result.Project}[/]{cpmSuffix}:");
                    report.AddRow(table);

                    if (item.Result.IsCpmEnabled)
                    {
                        report.AddRow($"   [grey]Adjust versions in[/] [silver]Directory.Packages.props[/][grey] (not the csproj).[/]");
                    }

                    if (!last)
                    {
                        report.AddEmptyRow();
                    }
                }
            }

            if (noPreRelease && resultsWithPreReleases.Count > 0)
            {
                report.AddEmptyRow();
                report.AddRow(" [yellow]Projects with pre-release package references:[/]");
                var packagesByProject = resultsWithPreReleases.SelectMany(x => x.Items, (filtered, package) => new
                {
                    Project = filtered.Result.Project,
                    PackageName = package.Name,
                    Version = package.Version,
                })
                .OrderBy(o => o.Project)
                .ToList();

                var table = new Table().BorderColor(Color.Grey).Expand();
                table.AddColumns("[grey]Project[/]", "[grey]Package[/]", "[grey]Version[/]");
                foreach (var item in packagesByProject)
                {
                    table.AddRow(
                        $"[green]{item.Project}[/]",
                        $"[yellow]{item.PackageName}[/]",
                        $"{item.Version}");
                }

                report.AddRow(table);
            }
        }

        private static VulnerabilitySeverity HighestSeverityFor(
            VulnerabilityReport report,
            Package primary,
            Package fallback)
        {
            var severity = SeverityFor(report, primary);
            if (severity == VulnerabilitySeverity.Unknown)
            {
                severity = SeverityFor(report, fallback);
            }

            return severity;
        }

        private static VulnerabilitySeverity SeverityFor(VulnerabilityReport report, Package package)
        {
            var version = package.Version?.ToString();
            if (string.IsNullOrEmpty(version))
            {
                return VulnerabilitySeverity.Unknown;
            }

            return report.GetHighestSeverity(package.Name, version);
        }

        private static string FormatSeverity(VulnerabilitySeverity severity)
        {
            return severity switch
            {
                VulnerabilitySeverity.Critical => "[red]CRITICAL[/]",
                VulnerabilitySeverity.High => "[red]HIGH[/]",
                VulnerabilitySeverity.Moderate => "[yellow]MODERATE[/]",
                VulnerabilitySeverity.Low => "[silver]LOW[/]",
                _ => "[grey]-[/]",
            };
        }

        private sealed class FilteredResult<T>
        {
            public ProjectAnalyzerResult Result { get; }
            public List<T> Items { get; }

            public FilteredResult(ProjectAnalyzerResult result, List<T> items)
            {
                Result = result;
                Items = items;
            }
        }
    }
}