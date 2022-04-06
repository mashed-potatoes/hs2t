using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HisiResearch.Commands
{
    [Description("Dump specified partitions from EMMC.")]
    public sealed class DumpSpecifiedCommand : Command<DumpSpecifiedCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandArgument(0, "<PARTITIONS>")]
            [Description("Dump specified partition.")]
            public string[] Partitions { get; set; }

            [CommandOption("--output <OUTPUT>")]
            [Description("Dump the specified partitions to a specified directory or file.")]
            public string OutputPath { get; set; }
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var connection = Engine.Connection.Get();
            var fastboot = new Engine.Fastboot(connection);
            var emmc = new Engine.EMMC(fastboot);

            if (settings.Partitions.Length == 1)
            {
                emmc.DumpPartitions(new[] {
                    (emmc.GetPartitionInfoFromPtable(settings.Partitions[0]), settings.OutputPath)
                });

                return 0;
            }

            Directory.CreateDirectory(settings.OutputPath);

            emmc.DumpPartitions(settings.Partitions
                .Select(x => (emmc.GetPartitionInfoFromPtable(x), Path.Combine(settings.OutputPath, $"{x}.img")))
                .ToArray());

            return 0;
        }
    }
}
