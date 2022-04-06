using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HisiResearch.Commands
{
    [Description("Prints partition table.")]
    public sealed class ListPartitionsCommand : Command<ListPartitionsCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            var connection = Engine.Connection.Get();
            var fastboot = new Engine.Fastboot(connection);
            var emmc = new Engine.EMMC(fastboot);

            emmc.RenderPartitionTable();

            return 0;
        }
    }
}
