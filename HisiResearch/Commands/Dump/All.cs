using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console.Cli;

namespace HisiResearch.Commands
{
    [Description("Dump all partitions from EMMC.")]
    public sealed class DumpAllCommand : Command<DumpAllCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [CommandOption("--output <OUTPUT>")]
            [Description("Dump all partitions to a specified directory.")]
            public string OutputPath { get; set; }

            [CommandOption("-U|--userparts")]
            [Description("Dump userdata and cache.")]
            public bool UserPartitions { get; set; }
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var connection = Engine.Connection.Get();
            var fastboot = new Engine.Fastboot(connection);
            var emmc = new Engine.EMMC(fastboot);
            var partitions = emmc.GetPartitionTable().ToList();

            if (!settings.UserPartitions)
            {
                partitions.RemoveAll(x => x.Name == "userdata" || x.Name == "cache"
#if DEBUG
                    || x.Name == "system" || x.Name == "vendor"
#endif
                );
            }

            Directory.CreateDirectory(settings.OutputPath);

            emmc.DumpPartitions(partitions
                .Select(x => (x, Path.Combine(settings.OutputPath, $"{x}.img")))
                .ToArray());

            return 0;
        }
    }
}
