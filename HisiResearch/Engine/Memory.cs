using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ** not finished ram dump

namespace HisiResearch.Engine
{
    class Memory
    {
        public struct MemNode
        {
            public uint addr;
            public uint len;
            public uint valid;
            public string name;
            public override string ToString()
            {
                return $"{name} <size {len.ToSize(SizeUnits.MB)} MB, addr: {Convert.ToString(addr, 16)}, valid: {valid}>";
            }
        }

        static void MemDump()
        {
            var memNum = Command("getvar:memory_num").Substring(4);
            var nodes = new List<MemNode>();

            for (int i = 0; i < int.Parse(memNum); i++)
            {
                var memInfo = Command($"getvar:memory_info_{i}");

                if (!memInfo.StartsWith("OKAY"))
                {
                    Console.WriteLine("mem info failed");
                    return;
                }

                memInfo = memInfo.Substring(4);

                var node = new MemNode
                {
                    addr = Convert.ToUInt32(memInfo.Substring(0, 8), 16),
                    len = Convert.ToUInt32(memInfo.Substring(8, 8), 16),
                    valid = Convert.ToUInt32(memInfo.Substring(16, 8), 16),
                    name = memInfo.Substring(24)
                };

                Console.WriteLine(node.ToString());

                nodes.Add(node);
            }

            Console.WriteLine("start dump...");

            var dir = $"dump_{Guid.NewGuid()}";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            foreach (var node in nodes)
            {
                Console.WriteLine($"uploading {node.name} to buffer");
                var data = Command(Encoding.ASCII.GetBytes($"upload_memory:" +
                    $"{Convert.ToString(node.addr, 16)}:{Convert.ToString(node.len)}"));
                Console.WriteLine("<<< {0}", Encoding.ASCII.GetString(data.Take(256).ToArray()).Replace("\0", ""));
                Console.WriteLine("exc {0}, got {1}", node.len, data.Length);
                File.WriteAllBytes(Path.Combine(dir, node.name + ".img"), data);
            }
        }
    }
}
