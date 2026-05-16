using System;
using System.ComponentModel;
using Spectre.Console.Cli;

namespace Bonsai.Commands
{
    [Description("Prints the Bonsai version number")]
    public sealed class VersionCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            Console.WriteLine(typeof(VersionCommand).Assembly.GetName().Version);
            return 0;
        }
    }
}