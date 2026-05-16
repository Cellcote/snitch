using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Bonsai.Analysis;
using Bonsai.Analysis.Utilities;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Bonsai.Commands
{
    [Description("Shows every dependency path that leads to a package, across the whole solution.")]
    public sealed class WhyCommand : Command<WhyCommand.Settings>
    {
        private readonly IAnsiConsole _console;
        private readonly ProjectBuilder _builder;
        private readonly PackageDependencyExplorer _explorer;
        private readonly PackageDependencyReporter _reporter;

        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "<PACKAGE>")]
            [Description("The package whose dependency paths should be displayed.")]
            public string Package { get; set; } = string.Empty;

            [CommandArgument(1, "[PROJECT|SOLUTION]")]
            [Description("The project or solution to analyze.")]
            public string ProjectOrSolutionPath { get; set; } = string.Empty;

            [CommandOption("-t|--tfm <MONIKER>")]
            [Description("The target framework moniker to analyze.")]
            public string? TargetFramework { get; set; }

            [CommandOption("--skip <PROJECT>")]
            [Description("One or more project references to exclude.")]
            public string[]? Skip { get; set; }
        }

        public WhyCommand(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _builder = new ProjectBuilder(console);
            _explorer = new PackageDependencyExplorer();
            _reporter = new PackageDependencyReporter(console);
        }

        public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Package))
            {
                _console.MarkupLine("[red]A package name is required.[/]");
                return -1;
            }

            var projectsToAnalyze = PathUtility.GetProjectPaths(settings.ProjectOrSolutionPath, out var entry);

            projectsToAnalyze.RemoveAll(p =>
            {
                var name = Path.GetFileNameWithoutExtension(p);
                return settings.Skip?.Contains(name, StringComparer.OrdinalIgnoreCase) ?? false;
            });

            var projectCache = new HashSet<Project>(new ProjectComparer());
            var results = new List<PackageDependencyResult>();

            _console.WriteLine();

            return _console.Status().Start("Analyzing...", ctx =>
            {
                ctx.Refresh();
                _console.MarkupLine($"Analyzing [yellow]{Path.GetFileName(entry)}[/]");

                foreach (var projectToAnalyze in projectsToAnalyze)
                {
                    var buildResult = _builder.Build(
                        projectToAnalyze,
                        settings.TargetFramework,
                        settings.Skip,
                        projectCache);

                    projectCache.Add(buildResult.Project);
                    foreach (var dep in buildResult.Dependencies)
                    {
                        projectCache.Add(dep);
                    }

                    results.Add(_explorer.Explore(buildResult.Project, settings.Package));
                }

                _reporter.WriteToConsole(settings.Package, results);
                return 0;
            });
        }
    }
}