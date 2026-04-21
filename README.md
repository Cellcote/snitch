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
│ │ Newtonsoft.Json  │ 12.0.3   │ Updated from 12.0.1 in Foo    │ │
│ │ Autofac          │ 4.9.3    │ Downgraded from 4.9.4 in Foo  │ │
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

## Building Snitch from source

```
> dotnet tool restore
> dotnet cake
```

## Icon

[Hollow](https://thenounproject.com/term/stitch/1571973/) designed by [Ben Davis](https://thenounproject.com/smashicons/) from [The Noun Project](https://thenounproject.com).
