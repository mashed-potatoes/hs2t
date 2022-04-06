using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HisiResearch.Engine
{
    class EMMC
    {
        private static readonly Regex secureRegex = new Regex(@"^(ptable|vrl.*|modemnv.*|nvme|oeminfo|fastboot\d?|xloader(|_\w))$");
        private readonly Fastboot fastboot;
        private PartitionInfo[] partitionTable = null;
        private const int MAX_TASKS = 25;

        public EMMC(Fastboot fastboot)
        {
            this.fastboot = fastboot;
        }

        public struct PartitionInfo
        {
            public string Name { get; }
            public long Address { get; }
            public long Size { get; }

            public PartitionInfo(string name, long address, long size)
            {
                Name = name;
                Address = address;
                Size = size;
            }

            public PartitionInfo(string name, params long[] para)
            {
                Name = name;
                if (para.Length != 2)
                {
                    throw new Exception("Invalid params.");
                }
                Address = para[0];
                Size = para[1];
            }
            public PartitionInfo(string name, string para)
            {
                Name = name;
                var res = para.Split(':')
                    .Select(x => Convert.ToInt64(x, 16))
                    .ToArray();
                Address = res[0];
                Size = res[1];
            }
        }

        public PartitionInfo GetPartitionInfo(string name, bool preserveAB = true)
        {
            var partParams = fastboot.Command($"getvar:emmc:{name}");

            if (!partParams.StartsWith("OKAY"))
            {
                if (preserveAB && (name.EndsWith("_a") || name.EndsWith("_b")))
                {
                    return GetPartitionInfo(name.Substring(0, name.Length - 2));
                }

                throw new FastbootException($"Failed to get `{name}` partition params.");
            }

            return new PartitionInfo(name, partParams.Substring(4));
        }

        public void UploadEMMC(PartitionInfo info)
        {
            var modeSwitch = fastboot.Command($"upload_emmc:{info.Address:X16}:{info.Size:X16}");

            if (!modeSwitch.StartsWith("OKAY"))
            {
                throw new FastbootException($"Failed to initialize upload stream.");
            }
        }

        public static bool IsSecurePartition(string name) => secureRegex.IsMatch(name);

        public void DumpPartitions((PartitionInfo Partition, string Path)[] pairs)
        {
            var many = pairs.Length > MAX_TASKS;
            AnsiConsole.Progress()
                .HideCompleted(many)
                .AutoClear(many)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new TransferSpeedColumn(),
                }).Start(ctx =>
                {
                    var partitions = pairs.Select(x => x.Partition).ToArray();
                    var paths = pairs.Select(x => x.Path).ToArray();
                    var tasks = partitions.Take(many ? Math.Min(MAX_TASKS, partitions.Length) : partitions.Length)
                        .Select(p => ctx.AddTask(p.Name))
                        .ToList();
                    var x = 0;

                    foreach (var part in partitions)
                    {
                        UploadEMMC(part);
                        using var stream = new FileStream(paths[x], FileMode.Create, FileAccess.Write);
                        fastboot.ReadBytes(stream, part.Size, tasks[x]);

                        if (x < partitions.Length - MAX_TASKS && many)
                        {
                            tasks.Add(ctx.AddTask(partitions[x + MAX_TASKS].Name));
                        }

                        x++;
                    }
                });

        }

        public PartitionInfo[] FindXloaderPartitions()
        {
            var parts = new List<PartitionInfo>();

            foreach (var part in new[] { "xloader", "fastboot1", "xloader_a", "xloader_b" })
            {
                var info = fastboot.Command($"getvar:emmc:{part}");

                if (info.StartsWith("OKAY"))
                {
                    parts.Add(new PartitionInfo(part, info.Substring(4)));
                }
            }

            return parts.ToArray();
        }

        public PartitionInfo[] GetPartitionTable()
        {
            if (partitionTable != null)
            {
                return partitionTable;
            }

            var ptableInfo = GetPartitionInfo("ptable");
            var parts = new List<PartitionInfo> { ptableInfo };
            var buffer = new byte[128];

            parts.AddRange(FindXloaderPartitions());

            using (var stream = new MemoryStream())
            {
                UploadEMMC(ptableInfo);
                fastboot.ReadBytes(stream, ptableInfo.Size, null);
                Console.WriteLine($"got ptab stream {stream.Length:X}");

                File.WriteAllBytes("gtab.g", stream.GetBuffer());

                stream.Seek(0x200, SeekOrigin.Begin);
                stream.Read(buffer, 0, 8);

                if (Encoding.ASCII.GetString(buffer.Take(8).ToArray()) != "EFI PART")
                {
                    throw new FormatException("Invalid ptable header!");
                }

                stream.Seek(0x400, SeekOrigin.Begin);

                while (true)
                {
                    stream.Read(buffer, 0, 128);

                    if (buffer[0] == 0 && buffer[1] == 0)
                    {
                        break;
                    }

                    var firstLBA = BitConverter.ToInt64(buffer.Skip(32).Take(8).ToArray(), 0) * 512;
                    var lastLBA = BitConverter.ToInt64(buffer.Skip(40).Take(8).ToArray(), 0) * 512;
                    var name = Encoding.ASCII.GetString(buffer.Skip(56).Where(x => x != '\0').ToArray());

                    parts.Add(new PartitionInfo(name, firstLBA, lastLBA - firstLBA + 1));
                }
            }

            return partitionTable = parts.ToArray();
        }

        public PartitionInfo GetPartitionInfoFromPtable(string name) => GetPartitionTable().First(x => x.Name == name);

        public static string GetPartitionColor(string name)
        {
            if (name.EndsWith("_a") || name.EndsWith("_b"))
            {
                return GetPartitionColor(name.Substring(0, name.Length - 2));
            }

            if (name.Contains("recovery") || name.Contains("kernel") || name.StartsWith("eng_")
                || name == "ramdisk" || name == "boot" || name == "vbmeta")
            {
                return "purple3";
            }

            if (name.Contains("test") || name.Contains("bench") || name.Contains("reserved"))
            {
                return "yellow";
            }

            if (name.Contains("modem"))
            {
                return "magenta2";
            }

            if (IsSecurePartition(name))
            {
                return "red";
            }

            switch (name)
            {
                case "userdata":
                case "cache":
                    return "silver";
                case "dts":
                case "dto":
                case "dfx":
                case "odm":
                    return "chartreuse1";
                case "super":
                case "system":
                case "vendor":
                case "cust":
                case "version":
                case "product":
                    return "dodgerblue2";
                default:
                    return "white";
            }
        }

        public void RenderPartitionTable()
        {
            var partitions = GetPartitionTable();
            var table = new Table();

            table.AddColumn("Partition name");
            table.AddColumn(new TableColumn("Secure").Centered());
            table.AddColumns("Address", "Size");

            foreach (var part in partitions)
            {

                table.AddRow($"[b][{GetPartitionColor(part.Name)}]{part}[/][/]",
                    IsSecurePartition(part.Name) ? "[b][red]+[/][/]" : "-",
                    "0x" + part.Address.ToString("X9"),
                    ((uint)part.Size).ToSize(SizeExtension.SizeUnits.MB) + " MB");
            }

            AnsiConsole.Render(table);
        }

        public class FastbootException : Exception
        {
            public FastbootException()
            {
            }

            public FastbootException(string message) : base(message)
            {
            }

            public FastbootException(string message, Exception inner) : base(message, inner)
            {
            }
        }
    }
}
