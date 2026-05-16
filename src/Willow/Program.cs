using System;
using System.Threading.Tasks;
using Spectre.Console.Cli;
using Willow.Commands;
using Willow.Utilities;

namespace Willow
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await Run(args);
        }

        public static async Task<int> Run(string[] args, Action<IConfigurator>? configator = null)
        {
            var typeRegistrar = new TypeRegistrar();
            typeRegistrar.Register(typeof(AnalyzeCommand.Settings), typeof(AnalyzeCommand.Settings));
            typeRegistrar.Register(typeof(WhyCommand.Settings), typeof(WhyCommand.Settings));

            var app = new CommandApp<AnalyzeCommand>(typeRegistrar);

            app.Configure(config =>
            {
                config.SetApplicationName("willow");
                configator?.Invoke(config);

                config.UseStrictParsing();
                config.ValidateExamples();

                config.AddExample(new[] { "Project.csproj" });
                config.AddExample(new[] { "Project.csproj", "-e", "Foo", "-e", "Bar" });
                config.AddExample(new[] { "Project.csproj", "--tfm", "net462" });
                config.AddExample(new[] { "Project.csproj", "--tfm", "net462", "--strict" });

                config.AddExample(new[] { "Solution.sln" });
                config.AddExample(new[] { "Solution.sln", "-e", "Foo", "-e", "Bar" });
                config.AddExample(new[] { "Solution.sln", "--tfm", "net462" });
                config.AddExample(new[] { "Solution.sln", "--tfm", "net462", "--strict" });

                config.AddCommand<VersionCommand>("version");
                config.AddCommand<WhyCommand>("why")
                    .WithDescription("Shows every dependency path that leads to a package.")
                    .WithExample(new[] { "why", "Newtonsoft.Json" })
                    .WithExample(new[] { "why", "System.Text.Json", "Solution.sln" })
                    .WithExample(new[] { "why", "System.Text.Json", "Project.csproj", "--tfm", "net8.0" });
            });

            return await app.RunAsync(args);
        }
    }
}