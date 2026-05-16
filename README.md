# <img src="/src/icon.png" height="40px"> Willow

[![NuGet Status](https://img.shields.io/nuget/v/Willow.svg)](https://www.nuget.org/packages/Willow/)

**Willow prunes your .NET dependency tree.**

A willow tree is shaped by patient pruning — Willow does the same for your
.NET project's package graph. Point it at a `.csproj`, `.sln`, or `.slnx` and
it tells you which `PackageReference` lines you're dragging around for no
reason — packages already pulled in transitively by a project reference,
duplicate version bumps, accidental downgrades, stray pre-release pins, and
known-vulnerable packages flagged by [OSV.dev](https://osv.dev).

> Willow is a fork of [Snitch](https://github.com/spectresystems/snitch) by
> [Patrik Svensson](https://github.com/patriksvensson) and Spectre Systems AB.
> All credit for the original design and implementation goes to the Snitch
> authors — Willow exists to continue publishing the tool with ongoing
> maintenance and a few opinionated additions aimed at dependency triage and
> vulnerability response.

## What it does

- **Find removable transitive references.** Lists `PackageReference` entries
  that are already supplied by a referenced project, so you can delete them
  from the consumer's `.csproj`.
- **Flag risky overrides.** Highlights packages where one project pins a
  different version than another in the same graph — upgrades, downgrades, and
  ranges that don't match.
- **Catch stray pre-releases.** `--no-prerelease` fails the run if any
  reference points at a non-stable version, which is handy for CI.
- **Cross-reference against OSV.dev.** `--vulnerable` queries the
  [OSV.dev](https://osv.dev) vulnerability database (aggregating GHSA, NVD,
  and CVE data) and shows severity beside every referenced package, plus a
  dedicated *Vulnerable packages* section.
- **Classify internal vs external.** `--internal <PATTERN>` groups results
  into packages you own (fix at source) versus packages you don't (wait or
  override locally) — handy when triaging a CVE sweep.
- **Trace transitive paths.** `willow why <package>` walks every project's
  `project.assets.json` and shows every dependency path from a direct
  reference down to the package across the whole solution.
- **Speak modern .NET.** Works with classic `.sln` files, the new
  [`.slnx`](https://learn.microsoft.com/en-us/visualstudio/ide/solutions-folders-and-projects#slnx-solution-file)
  XML solution format, and
  [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
  (`Directory.Packages.props`).
- **Strict mode for CI.** `--strict` returns a non-zero exit code when
  anything actionable is found.

## Example

```
> willow --tfm net462
```

Results in:

<!-- snippet: Solution.Default.verified.txt -->
<a id='snippet-Solution.Default.verified.txt'></a>
```txt
Analyzing...
Analyzing Willow.Tests.Fixtures.sln
Analyzing Foo...
Analyzing Bar...
Analyzing Baz...
Analyzing Quux...
Analyzing Quuux...
Analyzing Qux...
Analyzing Thud...
Analyzing Thuuud...
Analyzing Zap...

╭─────────────────────────────────────────────────────────────────╮
│  Packages that can be removed from Bar:                         │
│ ┌──────────────────────┬──────────────────────────────────────┐ │
│ │ Package              │ Referenced by                        │ │
│ ├──────────────────────┼──────────────────────────────────────┤ │
│ │ Autofac              │ Foo                                  │ │
│ └──────────────────────┴──────────────────────────────────────┘ │
│                                                                 │
│  Packages that can be removed from Baz:                         │
│ ┌──────────────────────┬──────────────────────────────────────┐ │
│ │ Package              │ Referenced by                        │ │
│ ├──────────────────────┼──────────────────────────────────────┤ │
│ │ Autofac              │ Foo                                  │ │
│ └──────────────────────┴──────────────────────────────────────┘ │
│                                                                 │
│  Packages that might be removed from Qux:                       │
│ ┌───────────┬───────────┬─────────────────────────────────────┐ │
│ │ Package   │ Version   │ Reason                              │ │
│ ├───────────┼───────────┼─────────────────────────────────────┤ │
│ │ Autofac   │ 4.9.3     │ Downgraded from 4.9.4 in Foo        │ │
│ └───────────┴───────────┴─────────────────────────────────────┘ │
│                                                                 │
│  Packages that might be removed from Thuuud:                    │
│ ┌─────────────────┬──────────────┬────────────────────────────┐ │
│ │ Package         │ Version      │ Reason                     │ │
│ ├─────────────────┼──────────────┼────────────────────────────┤ │
│ │ Newtonsoft.Json │ 13.0.2-beta2 │ Updated from 12.0.1 in Foo │ │
│ └─────────────────┴──────────────┴────────────────────────────┘ │
│                                                                 │
│  Packages that might be removed from Zap:                       │
│ ┌──────────────────┬──────────┬───────────────────────────────┐ │
│ │ Package          │ Version  │ Reason                        │ │
│ ├──────────────────┼──────────┼───────────────────────────────┤ │
│ │ Autofac          │ 4.9.3    │ Downgraded from 4.9.4 in Foo  │ │
│ │ Newtonsoft.Json  │ 12.0.3   │ Updated from 12.0.1 in Foo    │ │
│ └──────────────────┴──────────┴───────────────────────────────┘ │
╰─────────────────────────────────────────────────────────────────╯
```
<sup><a href='/src/Willow.Tests/Expectations/Solution.Default.verified.txt#L1-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-Solution.Default.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`Packages that can be removed` are safe deletions — same package, same version,
already pulled in by a referenced project. `Packages that might be removed`
need a human: the version doesn't line up, so deleting the local reference
would change what your project resolves to.

## Installation

```
> dotnet tool install -g willow
```

## Usage

_Examine a specific project or solution using the first built
target framework._

```
> willow MyProject.csproj
```

_Examine a specific project using a specific
target framework moniker._

```
> willow MyProject.csproj --tfm net462
```

_Examine a specific project using a specific target framework moniker
and return exit code 0 only if there were no transitive package collisions.
Useful for continuous integration._

```
> willow MyProject.csproj --tfm net462 --strict
```

_Examine a specific project using a specific target framework moniker
and exclude the packages Foo and Bar from the result._

```
> willow MyProject.csproj --tfm net462 --exclude Foo --exclude Bar
```

_Examine a specific project using a specific target framework moniker
and exclude the project OtherProject from analysis._

```
> willow MyProject.csproj --tfm net462 --skip OtherProject
```

_Examine a specific project or solution to make sure there are no pre-release package references._

```
> willow MyProject.csproj --no-prerelease
```

_Examine a specific project or solution and cross-reference every referenced
package against the [OSV.dev](https://osv.dev) vulnerability database (which
aggregates GHSA, NVD and CVE data). Severity is shown next to each removable
package, plus a dedicated "Vulnerable packages" section listing every advisory
hit. Combine with `--strict` to fail CI when any vulnerable package is found._

```
> willow MyProject.csproj --vulnerable
```

_Group results by internal vs external packages so you can triage who-fixes-what
during e.g. a CVE sweep. Internal packages are those you control and can fix at
source (open a PR upstream); everything else is external (must wait for an upstream
release or be overridden). Patterns are matched case-insensitively. A pattern
without wildcards is treated as a prefix matching either an exact name or names
with a "." separator after the prefix (so `Acme` matches `Acme` and
`Acme.Foo`); patterns with `*` / `?` are treated as glob patterns matched against
the full package name._

```
> willow MyProject.csproj --internal Acme --internal MyCompany.*
```

## Reverse dependency lookup

The `why` command shows every dependency path from a direct reference down to a
specific package, across the whole solution at once. It replaces having to run
`dotnet nuget why` per project when chasing a vulnerable or unwanted transitive
package.

```
> willow why System.Text.Json
```

```
> willow why System.Text.Json MyProject.csproj
```

```
> willow why System.Text.Json MySolution.sln --tfm net8.0
```

Paths are displayed as a tree per project, merging shared prefixes. Project
references in the chain are marked `(project)` so you can tell them apart from
NuGet packages. Run `dotnet restore` first — the command reads each project's
`project.assets.json`.

## Building Willow from source

```
> dotnet tool restore
> dotnet cake
```

## Credits

Willow is a fork of [Snitch](https://github.com/spectresystems/snitch) by
[Patrik Svensson](https://github.com/patriksvensson) and Spectre Systems AB.
The original work is licensed under MIT — see [`LICENSE`](LICENSE) for the
combined copyright notice.

## Logo

The Willow mark is a willow tree with its characteristic drooping branches,
beside a small pair of pruning shears and a cleanly-cut branch on the
ground — a literal nod to what the tool does to your dependency tree.
Generated with [Nano Banana](https://deepmind.google/models/gemini/image/)
(Gemini 2.5 Flash Image).
