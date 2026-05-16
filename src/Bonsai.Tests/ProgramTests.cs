using Shouldly;
using Bonsai;
using System;
using System.IO;
using System.Threading.Tasks;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using VerifyTests;
using Xunit;
using VerifyXunit;

namespace Bonsai.Tests
{
    [UsesVerify]
    public class ProgramTests
    {
        [Fact]
        [Expectation("Baz", "Default")]
        public async Task Should_Return_Expected_Result_For_Baz_Not_Specifying_Framework()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project);

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Solution", "Default")]
        public async Task Should_Return_Expected_Result_For_Solution_Not_Specifying_Framework()
        {
            // Given
            var fixture = new Fixture();
            var solution = Fixture.GetPath("Bonsai.Tests.Fixtures.sln");

            // When
            var (exitCode, output) = await Fixture.Run(solution);

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Baz", "netstandard2.0")]
        public async Task Should_Return_Expected_Result_For_Baz_Specifying_Framework()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--tfm", "netstandard2.0");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Baz", "netstandard2.0_Strict")]
        public async Task Should_Return_Non_Zero_Exit_Code_For_Baz_When_Running_With_Strict()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--tfm", "netstandard2.0", "--strict");

            // Then
            exitCode.ShouldBe(-1);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Baz", "Exclude_Autofac")]
        public async Task Should_Return_Expected_Result_For_Baz_When_Excluding_Library()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--exclude", "Autofac");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Baz", "Skip_Bar")]
        public async Task Should_Return_Expected_Result_For_Baz_When_Skipping_Project()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--skip", "Bar");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Baz", "Skip_Bar_NoPreRelease")]
        public async Task Should_Return_Expected_Result_For_Baz_When_Skipping_Project_And_NoReleases()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--skip", "Bar", "--no-prerelease");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Baz", "netstandard2.Strict.NoPreRelease")]
        public async Task Should_Return_Non_Zero_Exit_Code_For_Baz_When_Running_With_Strict_And_NoPreRelease()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--no-prerelease", "--strict");

            // Then
            exitCode.ShouldBe(-1);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Thud", "netstandard2.Strict.NoPreRelease")]
        public async Task Should_Return_Non_Zero_Exit_Code_For_Thud_When_Running_With_Strict_And_NoPreRelease()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Thud/Thud.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--no-prerelease", "--strict");

            // Then
            exitCode.ShouldBe(-1);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Thuuud", "netstandard2.Strict.NoPreRelease")]
        public async Task Should_Return_Non_Zero_Exit_Code_For_Thuuud_When_Running_With_Strict_And_NoPreRelease()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Thuuud/Thuuud.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--no-prerelease", "--strict");

            // Then
            exitCode.ShouldBe(-1);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Thuuud", "netstandard2.NoPreRelease")]
        public async Task Should_Return_Zero_Exit_Code_For_Thuuud_When_Running_With_NoPreRelease()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Thuuud/Thuuud.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--no-prerelease");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        public sealed class Fixture
        {
            public static string GetPath(string path)
            {
                var workingDirectory = Environment.CurrentDirectory;
                var solutionDirectory = Path.GetFullPath(Path.Combine(workingDirectory, "../../../../Bonsai.Tests.Fixtures"));
                return Path.GetFullPath(Path.Combine(solutionDirectory, path));
            }

            public static async Task<(int exitCode, string output)> Run(params string[] args)
            {
                var console = new TestConsole { EmitAnsiSequences = false };
                var exitCode = await Program.Run(args, c => c.ConfigureConsole(console));
                return (exitCode, console.Output.Trim());
            }
        }

        [Fact]
        [Expectation("FSharp", "Default")]
        public async Task Should_Return_Expected_Result_For_FSharp_Not_Specifying_Framework()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("FSharp/FSharp.fsproj");

            // When
            var (exitCode, output) = await Fixture.Run(project);

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Slnx", "Default")]
        public async Task Should_Return_Expected_Result_For_Slnx_Solution()
        {
            // Given
            var fixture = new Fixture();
            var solution = Fixture.GetPath("Bonsai.Tests.Fixtures.slnx");

            // When
            var (exitCode, output) = await Fixture.Run(solution);

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("CpmBaz", "Default")]
        public async Task Should_Produce_Cpm_Aware_Output_For_CpmBaz()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Cpm/CpmBaz/CpmBaz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project);

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Solution", "Internal_Autofac")]
        public async Task Should_Group_Results_By_Internal_When_Internal_Prefix_Specified()
        {
            // Given
            var fixture = new Fixture();
            var solution = Fixture.GetPath("Bonsai.Tests.Fixtures.sln");

            // When
            var (exitCode, output) = await Fixture.Run(solution, "--internal", "Autofac");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Solution", "Internal_Wildcard")]
        public async Task Should_Match_Internal_Packages_Using_Wildcard_Pattern()
        {
            // Given
            var fixture = new Fixture();
            var solution = Fixture.GetPath("Bonsai.Tests.Fixtures.sln");

            // When
            var (exitCode, output) = await Fixture.Run(solution, "--internal", "Newt*");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        [Expectation("Baz", "Internal_NoMatch")]
        public async Task Should_Keep_Behavior_When_Internal_Pattern_Does_Not_Match()
        {
            // Given
            var fixture = new Fixture();
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run(project, "--internal", "Acme.*");

            // Then
            exitCode.ShouldBe(0);
            await Verifier.Verify(output);
        }

        [Fact]
        public async Task Why_Should_Report_Direct_Package_Reference()
        {
            // Given
            var project = Fixture.GetPath("Foo/Foo.csproj");

            // When
            var (exitCode, output) = await Fixture.Run("why", "Newtonsoft.Json", project);

            // Then
            exitCode.ShouldBe(0);
            output.ShouldContain("Foo");
            output.ShouldContain("Newtonsoft.Json");
            output.ShouldContain("12.0.1");
            output.ShouldContain("Found 1 path(s)");
        }

        [Fact]
        public async Task Why_Should_Walk_Through_Project_References()
        {
            // Given
            var project = Fixture.GetPath("Baz/Baz.csproj");

            // When
            var (exitCode, output) = await Fixture.Run("why", "Newtonsoft.Json", project);

            // Then
            exitCode.ShouldBe(0);
            output.ShouldContain("Baz");
            output.ShouldContain("Bar");
            output.ShouldContain("Foo");
            output.ShouldContain("(project)");
            output.ShouldContain("Newtonsoft.Json");
        }

        [Fact]
        public async Task Why_Should_Walk_Through_Transitive_Packages()
        {
            // Given
            var project = Fixture.GetPath("Foo/Foo.csproj");

            // When
            var (exitCode, output) = await Fixture.Run("why", "Microsoft.NETCore.Platforms", project);

            // Then
            exitCode.ShouldBe(0);
            output.ShouldContain("NETStandard.Library");
            output.ShouldContain("Microsoft.NETCore.Platforms");
        }

        [Fact]
        public async Task Why_Should_Report_When_No_Path_Found()
        {
            // Given
            var project = Fixture.GetPath("Foo/Foo.csproj");

            // When
            var (exitCode, output) = await Fixture.Run("why", "System.Text.Json", project);

            // Then
            exitCode.ShouldBe(0);
            output.ShouldContain("No dependency paths");
            output.ShouldContain("System.Text.Json");
        }

        [Fact]
        public async Task Why_Should_Match_Package_Name_Case_Insensitively()
        {
            // Given
            var project = Fixture.GetPath("Foo/Foo.csproj");

            // When
            var (exitCode, output) = await Fixture.Run("why", "newtonsoft.json", project);

            // Then
            exitCode.ShouldBe(0);
            output.ShouldContain("Newtonsoft.Json");
            output.ShouldContain("12.0.1");
        }

        [Fact]
        public async Task Why_Should_Scan_All_Projects_In_Solution()
        {
            // Given
            var solution = Fixture.GetPath("Bonsai.Tests.Fixtures.sln");

            // When
            var (exitCode, output) = await Fixture.Run("why", "Newtonsoft.Json", solution);

            // Then
            exitCode.ShouldBe(0);
            output.ShouldContain("Foo");
            output.ShouldContain("Zap");
            output.ShouldContain("Thud");
        }
    }
}