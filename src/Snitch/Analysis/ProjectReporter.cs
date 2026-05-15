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
            WriteToConsole(results, noPreRelease, VulnerabilityReport.Empty);
        }

        public void WriteToConsole(
            [NotNull] List<ProjectAnalyzerResult> results,
            bool noPreRelease,
            [NotNull] VulnerabilityReport vulnerabilityReport)
        {
            var resultsWithPackageToRemove = results.Where(r => r.CanBeRemoved.Count > 0).ToList();
            var resultsWithPackageMayBeRemove = results.Where(r => r.MightBeRemoved.Count > 0).ToList();
            var resultsWithPreReleases = results.Where(r => r.PreReleasePackages.Count > 0).ToList();
            var showVulns = !vulnerabilityReport.IsEmpty;

            if (results.All(x => x.NoPackagesToRemove)
                && (!noPreRelease || resultsWithPreReleases.Count == 0)
                && !showVulns)
            {
                // Output the result.
                _console.WriteLine();
                _console.MarkupLine("[green]Everything looks good![/]");
                _console.WriteLine();
                return;
            }

            var report = new Grid();
            report.AddColumn();

            if (resultsWithPackageToRemove.Count > 0)
            {
                foreach (var (_, _, last, result) in resultsWithPackageToRemove.Enumerate())
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

                    foreach (var item in result.CanBeRemoved)
                    {
                        var packageName = $"[green]{item.Package.Name}[/]";
                        var referencedBy = $"[aqua]{item.Original.Project.Name}[/]";
                        if (showVulns)
                        {
                            var severity = HighestSeverityFor(vulnerabilityReport, item.Package, item.Original.Package);
                            table.AddRow(packageName, referencedBy, FormatSeverity(severity));
                        }
                        else
                        {
                            table.AddRow(packageName, referencedBy);
                        }
                    }

                    report.AddRow($" [yellow]Packages that can be removed from[/] [aqua]{result.Project}[/]:");
                    report.AddRow(table);

                    if (!last || (last && resultsWithPackageMayBeRemove.Count > 0))
                    {
                        report.AddEmptyRow();
                    }
                }
            }

            if (resultsWithPackageMayBeRemove.Count > 0)
            {
                foreach (var (_, _, last, result) in resultsWithPackageMayBeRemove.Enumerate())
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

                    foreach (var item in result.MightBeRemoved)
                    {
                        string reason;
                        if (item.Package.IsGreaterThan(item.Original.Package, out var indeterminate))
                        {
                            var name = item.Original.Project.Name;
                            var version = item.Original.Package.GetVersionString();
                            var verb = indeterminate ? "Might be updated from" : "Updated from";
                            reason = $"[grey]{verb}[/] [silver]{version}[/] [grey]in[/] [aqua]{name}[/]";
                        }
                        else
                        {
                            var name = item.Original.Project.Name;
                            var version = item.Original.Package.GetVersionString();
                            var verb = indeterminate ? "Does not match" : "Downgraded from";
                            reason = $"[grey]{verb}[/] [silver]{version}[/] [grey]in[/] [aqua]{name}[/]";
                        }

                        if (showVulns)
                        {
                            var severity = HighestSeverityFor(vulnerabilityReport, item.Package, item.Original.Package);
                            table.AddRow(
                                $"[green]{item.Package.Name}[/]",
                                item.Package.GetVersionString(),
                                reason,
                                FormatSeverity(severity));
                        }
                        else
                        {
                            table.AddRow(
                                $"[green]{item.Package.Name}[/]",
                                item.Package.GetVersionString(),
                                reason);
                        }
                    }

                    report.AddRow($" [yellow]Packages that [u]might[/] be removed from[/] [aqua]{result.Project}[/]:");
                    report.AddRow(table);

                    if (!last)
                    {
                        report.AddEmptyRow();
                    }
                }
            }

            if (noPreRelease && resultsWithPreReleases.Count > 0)
            {
                report.AddEmptyRow();
                report.AddRow($" [yellow]Projects with pre-release package references:[/]");
                var packagesByProject = resultsWithPreReleases.SelectMany(x => x.PreReleasePackages, (project, package) => new
                                                              {
                                                                  Project = project.Project,
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
    }
}