using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace Bonsai.Analysis
{
    internal sealed class PackageDependencyReporter
    {
        private readonly IAnsiConsole _console;

        public PackageDependencyReporter(IAnsiConsole console)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public void WriteToConsole(string targetPackage, IReadOnlyList<PackageDependencyResult> results)
        {
            _console.WriteLine();

            var withMissingLock = results.Where(r => r.LockFileMissing).ToList();
            foreach (var missing in withMissingLock)
            {
                _console.MarkupLine(
                    $"[yellow]WARN:[/] Skipping [aqua]{Markup.Escape(missing.Project.Name)}[/] " +
                    "(no project.assets.json — run dotnet restore).");
            }

            var withPaths = results.Where(r => r.HasPaths).ToList();
            if (withPaths.Count == 0)
            {
                _console.MarkupLine(
                    $"[yellow]No dependency paths to[/] [green]{Markup.Escape(targetPackage)}[/] [yellow]were found.[/]");
                _console.WriteLine();
                return;
            }

            foreach (var result in withPaths)
            {
                var header =
                    $"[aqua]{Markup.Escape(result.Project.Name)}[/] " +
                    $"[grey]({Markup.Escape(result.Project.TargetFramework)})[/]";

                var tree = new Tree(header);
                var root = BuildTrie(result.Paths);
                AddTrieToTree(root, tree, targetPackage);
                _console.Write(tree);
                _console.WriteLine();
            }

            var pathCount = withPaths.Sum(r => r.Paths.Count);
            _console.MarkupLine(
                $"[grey]Found[/] {pathCount} [grey]path(s) to[/] " +
                $"[green]{Markup.Escape(targetPackage)}[/] [grey]across[/] " +
                $"{withPaths.Count} [grey]project(s).[/]");
            _console.WriteLine();
        }

        private static TrieNode BuildTrie(IEnumerable<DependencyPath> paths)
        {
            var root = new TrieNode(null);
            foreach (var path in paths)
            {
                var current = root;
                foreach (var node in path.Nodes)
                {
                    if (!current.Children.TryGetValue(node.Name, out var next))
                    {
                        next = new TrieNode(node);
                        current.Children[node.Name] = next;
                    }

                    current = next;
                }
            }

            return root;
        }

        private static void AddTrieToTree(TrieNode trie, IHasTreeNodes parent, string target)
        {
            foreach (var child in trie.Children.Values)
            {
                var node = child.Node!;
                var isTarget = node.Name.Equals(target, StringComparison.OrdinalIgnoreCase);
                var typeSuffix = node.IsProject ? " [grey](project)[/]" : string.Empty;
                var label = isTarget
                    ? $"[green]{Markup.Escape(node.Name)}[/] [silver]{Markup.Escape(node.Version)}[/]"
                    : $"[yellow]{Markup.Escape(node.Name)}[/] [silver]{Markup.Escape(node.Version)}[/]{typeSuffix}";

                var treeNode = parent.AddNode(label);
                AddTrieToTree(child, treeNode, target);
            }
        }

        private sealed class TrieNode
        {
            public DependencyNode? Node { get; }
            public Dictionary<string, TrieNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);

            public TrieNode(DependencyNode? node)
            {
                Node = node;
            }
        }
    }
}