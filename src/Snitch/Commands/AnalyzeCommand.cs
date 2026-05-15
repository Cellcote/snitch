using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Snitch.Analysis;
using Snitch.Analysis.Utilities;
using Snitch.Analysis.Vulnerabilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Snitch.Commands
{
    [Description("Shows transitive package dependencies that can be removed")]
    public sealed class AnalyzeCommand : AsyncCommand<AnalyzeCommand.Settings>
    {
        private readonly IAnsiConsole _console;
        private readonly ProjectBuilder _builder;
        private readonly ProjectAnalyzer _analyzer;
        private readonly ProjectReporter _reporter;

        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "[PROJECT|SOLUTION]")]
            [Description("The project or solution you want to analyze.")]
            public string ProjectOrSolutionPath { get; set; } = string.Empty;

            [CommandOption("-t|--tfm <MONIKER>")]
            [Description("The target framework moniker to analyze.")]
            public string? TargetFramework { get; set; }

            [CommandOption("-e|--exclude <PACKAGE>")]
            [Description("One or more packages to exclude.")]
            public string[]? Exclude { get; set; }

            [CommandOption("--skip <PROJECT>")]
            [Description("One or more project references to exclude.")]
            public string[]? Skip { get; set; }

            [CommandOption("-s|--strict")]
            [Description("Returns exit code 0 only if no conflicts were found.")]
            public bool Strict { get; set; }

            [CommandOption("--no-prerelease")]
            [Description("Verifies that all package references are not pre-releases.")]
            public bool NoPreRelease { get; set; }

            [CommandOption("--vulnerable")]
            [Description("Cross-references packages with the OSV.dev vulnerability database and tags rows with severity.")]
            public bool CheckVulnerabilities { get; set; }

            [CommandOption("--internal <PATTERN>")]
            [Description("One or more prefixes/patterns that classify a package as internal (e.g. Acme or Acme.*). When set, results are grouped into internal (fixable at source) and external (must wait or override).")]
            public string[]? Internal { get; set; }
        }

        public AnalyzeCommand(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _builder = new ProjectBuilder(console);
            _analyzer = new ProjectAnalyzer();
            _reporter = new ProjectReporter(console);
        }

        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            var projectsToAnalyze = PathUtility.GetProjectPaths(settings.ProjectOrSolutionPath, out var entry);

            // Remove all projects that we want to skip.
            projectsToAnalyze.RemoveAll(p =>
            {
                var projectName = Path.GetFileNameWithoutExtension(p);
                return settings.Skip?.Contains(projectName, StringComparer.OrdinalIgnoreCase) ?? false;
            });

            // Sort projects topologically (leaves first) to maximize build cache hits.
            // This ensures that when a project is built, all its dependencies are already
            // cached, avoiding redundant MSBuild design-time builds.
            projectsToAnalyze = DependencyGraph.TopologicalSort(projectsToAnalyze);

            var targetFramework = settings.TargetFramework;
            var analyzerResults = new List<ProjectAnalyzerResult>();
            var projectCache = new HashSet<Project>(new ProjectComparer());

            _console.WriteLine();

            await _console.Status().StartAsync("Analyzing...", async ctx =>
            {
                ctx.Refresh();

                _console.MarkupLine($"Analyzing [yellow]{Path.GetFileName(entry)}[/]");

                // Reuse a single AnalyzerManager across all project builds to avoid
                // reinitializing MSBuild infrastructure for each project.
                var manager = new Buildalyzer.AnalyzerManager();

                foreach (var projectToAnalyze in projectsToAnalyze)
                {
                    // Perform a design time build of the project.
                    var buildResult = _builder.Build(
                        projectToAnalyze,
                        targetFramework,
                        settings.Skip,
                        projectCache,
                        manager);

                    // Update the cache of built projects.
                    projectCache.Add(buildResult.Project);
                    foreach (var item in buildResult.Dependencies)
                    {
                        projectCache.Add(item);
                    }

                    // Analyze the project.
                    var analyzeResult = _analyzer.Analyze(buildResult.Project);
                    if (settings.Exclude?.Length > 0)
                    {
                        // Filter packages that should be excluded.
                        analyzeResult = analyzeResult.Filter(settings.Exclude);
                    }

                    analyzerResults.Add(analyzeResult);
                }

                await Task.CompletedTask.ConfigureAwait(false);
            }).ConfigureAwait(false);

            var vulnerabilityReport = VulnerabilityReport.Empty;
            if (settings.CheckVulnerabilities)
            {
                vulnerabilityReport = await CheckVulnerabilitiesAsync(analyzerResults, projectCache).ConfigureAwait(false);
            }

            // Write the report to the console.
            var classifier = new PackageClassifier(settings.Internal);
            _reporter.WriteToConsole(analyzerResults, settings.NoPreRelease, vulnerabilityReport, classifier);

            // Return the correct exit code.
            return GetExitCode(settings, analyzerResults, vulnerabilityReport);
        }

        private async Task<VulnerabilityReport> CheckVulnerabilitiesAsync(
            List<ProjectAnalyzerResult> analyzerResults,
            HashSet<Project> projectCache)
        {
            var packages = CollectUniquePackages(analyzerResults, projectCache);
            if (packages.Count == 0)
            {
                return VulnerabilityReport.Empty;
            }

            return await _console.Status().StartAsync(
                $"Checking {packages.Count} package(s) against the OSV.dev vulnerability database...",
                async ctx =>
                {
                    ctx.Refresh();

                    try
                    {
                        using var service = new OsvVulnerabilityService();
                        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                        return await service.CheckAsync(packages, cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _console.MarkupLine($"[yellow]WARN:[/] Could not query OSV.dev for vulnerabilities: {Markup.Escape(ex.Message)}");
                        return VulnerabilityReport.Empty;
                    }
                }).ConfigureAwait(false);
        }

        private static List<(string Name, string Version)> CollectUniquePackages(
            List<ProjectAnalyzerResult> analyzerResults,
            HashSet<Project> projectCache)
        {
            var seen = new HashSet<(string Name, string Version)>(
                new PackageKeyComparer());

            void Add(string? name, string? version)
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
                {
                    return;
                }

                seen.Add((name, version));
            }

            // Packages flagged by Snitch — the rows we'd display.
            foreach (var result in analyzerResults)
            {
                foreach (var entry in result.CanBeRemoved.Concat(result.MightBeRemoved))
                {
                    Add(entry.Package.Name, entry.Package.Version?.ToString());
                    Add(entry.Original.Package.Name, entry.Original.Package.Version?.ToString());
                }
            }

            // All packages across every analyzed project so we can surface any vulnerability,
            // not just ones tied to a removable row.
            foreach (var project in projectCache)
            {
                foreach (var package in project.Packages)
                {
                    Add(package.Name, package.Version?.ToString());
                }
            }

            return seen.ToList();
        }

        private static int GetExitCode(
            Settings settings,
            List<ProjectAnalyzerResult> result,
            VulnerabilityReport vulnerabilityReport)
        {
            if (settings.Strict)
            {
                if (result.Any(r => !r.NoPackagesToRemove))
                {
                    return -1;
                }

                if (settings.NoPreRelease && result.Any(r => r.HasPreReleases))
                {
                    return -1;
                }

                if (settings.CheckVulnerabilities && !vulnerabilityReport.IsEmpty)
                {
                    return -1;
                }
            }

            return 0;
        }

        private sealed class PackageKeyComparer : IEqualityComparer<(string Name, string Version)>
        {
            public bool Equals((string Name, string Version) x, (string Name, string Version) y)
            {
                return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Version, y.Version, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string Name, string Version) obj)
            {
                var nameHash = obj.Name?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
                var versionHash = obj.Version?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
                return HashCode.Combine(nameHash, versionHash);
            }
        }
    }
}