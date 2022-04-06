using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HisiResearch.Commands
{
    [Description("Dump Secure partitions from EMMC.")]
    public sealed class DumpSecureCommand : Command<DumpSecureCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("--output <OUTPUT>")]
            [Description("Dump the secure partitions to a specified directory.")]
            public string OutputPath { get; set; }
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var connection = Engine.Connection.Get();
            var fastboot = new Engine.Fastboot(connection);
            var emmc = new Engine.EMMC(fastboot);
            var partitions = emmc.GetPartitionTable()
                .Where(x => Engine.EMMC.IsSecurePartition(x.Name))
                .ToArray();

            Directory.CreateDirectory(settings.OutputPath);

            emmc.DumpPartitions(partitions.Select(x => (x, Path.Combine(settings.OutputPath, $"{x}.img"))).ToArray());

            return 0;
        }
    }
}
