# <img src="/src/icon.png" height="30px"> Snitch

[![NuGet Status](https://img.shields.io/nuget/v/Snitch.svg)](https://www.nuget.org/packages/Snitch/)

A tool that help you find transitive package references that can be removed.

## Example

```
> snitch --tfm net462
```

Results in:

<!-- snippet: Solution.Default.verified.txt -->
<a id='snippet-Solution.Default.verified.txt'></a>
```txt
Analyzing...
Analyzing Snitch.Tests.Fixtures.sln
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
<sup><a href='/src/Snitch.Tests/Expectations/Solution.Default.verified.txt#L1-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-Solution.Default.verified.txt' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Installation

```
> dotnet tool install -g snitch
```

## Usage

_Examine a specific project or solution using the first built 
target framework._

```
> snitch MyProject.csproj
```

_Examine a specific project using a specific
target framework moniker._

```
> snitch MyProject.csproj --tfm net462
```

_Examine a specific project using a specific target framework moniker
and return exit code 0 only if there was no transitive package collisions.
Useful for continuous integration._

```
> snitch MyProject.csproj --tfm net462 --strict
```

_Examine a specific project using a specific target framework moniker
and make sure that the packages Foo and Bar are excluded from the result._

```
> snitch MyProject.csproj --tfm net462 --exclude Foo --exclude Bar
```

_Examine a specific project using a specific target framework moniker
and exclude the project OtherProject from analysis._

```
> snitch MyProject.csproj --tfm net462 --skip OtherProject
```

_Examine a specific project or solution to make sure there are no pre-release package references._

```
> snitch MyProject.csproj --no-prerelease
```

_Examine a specific project or solution and cross-reference every referenced
package against the [OSV.dev](https://osv.dev) vulnerability database (which
aggregates GHSA, NVD and CVE data). Severity is shown next to each removable
package, plus a dedicated "Vulnerable packages" section listing every advisory
hit. Combine with `--strict` to fail CI when any vulnerable package is found._

```
> snitch MyProject.csproj --vulnerable
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
> snitch MyProject.csproj --internal Acme --internal MyCompany.*
```

## Reverse dependency lookup

The `why` command shows every dependency path from a direct reference down to a
specific package, across the whole solution at once. It replaces having to run
`dotnet nuget why` per project when chasing a vulnerable or unwanted transitive
package.

```
> snitch why System.Text.Json
```

```
> snitch why System.Text.Json MyProject.csproj
```

```
> snitch why System.Text.Json MySolution.sln --tfm net8.0
```

Paths are displayed as a tree per project, merging shared prefixes. Project
references in the chain are marked `(project)` so you can tell them apart from
NuGet packages. Run `dotnet restore` first — the command reads each project's
`project.assets.json`.

## Building Snitch from source

```
> dotnet tool restore
> dotnet cake
```

## Icon

[Hollow](https://thenounproject.com/term/stitch/1571973/) designed by [Ben Davis](https://thenounproject.com/smashicons/) from [The Noun Project](https://thenounproject.com).
