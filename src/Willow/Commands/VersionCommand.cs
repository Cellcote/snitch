using System;
using System.ComponentModel;
using Spectre.Console.Cli;

namespace Willow.Commands
{
    [Description("Prints the Willow version number")]
    public sealed class VersionCommand : Command
    {
        public override int Execute(CommandContext context)
        {
            Console.WriteLine(typeof(VersionCommand).Assembly.GetName().Version);
            return 0;
        }
    }
}