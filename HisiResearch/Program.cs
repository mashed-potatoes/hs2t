using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System.Globalization;
using Spectre.Console;
using Spectre.Console.Cli;
using HisiResearch.Commands;

namespace HisiResearch
{
    class Program
    {   
        static int Main(string[] args)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");

            var rule = new Rule("[red]HiSiResearch[/] (c) mashed-potatoes, 2021")
            {
                Alignment = Justify.Left
            };
            AnsiConsole.Render(rule);

            var app = new CommandApp();
            app.Configure(config =>
            {
                config.SetApplicationName("hs2t");
                config.ValidateExamples();
                config.AddExample(new[] { "dump", "specified", "oeminfo" });
                config.AddExample(new[] { "dump", "secure", "--output", "./secure_dump" });
                config.AddExample(new[] { "dump", "all", "--userparts" });

                config.AddBranch("dump", dump =>
                {
                    dump.AddCommand<DumpSpecifiedCommand>("specified");
                    dump.AddCommand<DumpSecureCommand>("secure");
                    dump.AddCommand<DumpAllCommand>("all");
                });

                config.AddBranch("list", list =>
                {
                    list.AddCommand<ListPartitionsCommand>("partitions");
                });
            });

            return app.Run(args);
        }
    }
}
