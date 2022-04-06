using LibUsbDotNet;
using LibUsbDotNet.Main;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HisiResearch.Engine
{
    class Fastboot
    {
        private readonly UsbDevice device;
        private const int BUFFER_SIZE = 2 * 1024 * 1024;
        static int Timeout { get; set; } = 1000;

        public Fastboot(UsbDevice device)
        {
            this.device = device;
        }

        public byte[] Command(byte[] command)
        {
            var writeEndpoint = device.OpenEndpointWriter(WriteEndpointID.Ep01);
            var readEndpoint = device.OpenEndpointReader(ReadEndpointID.Ep01);

            writeEndpoint.Write(command, Timeout, out int wrAct);

            if (wrAct != command.Length)
            {
                throw new Exception($"Failed to write command! Transfered: {wrAct} of {command.Length} bytes");
            }

            var response = new MemoryStream();
            var buffer = new byte[2048];

            while (true)
            {
                readEndpoint.Read(buffer, Timeout, out int rdAct);

                response.Write(buffer, 0, rdAct);

                if (rdAct >= 4)
                {
                    var header = Encoding.ASCII.GetString(buffer.Take(4).ToArray());

                    if (header == "FAIL" || header == "OKAY")
                    {
                        break;
                    }
                }

                if (rdAct == 0)
                {
                    break;
                }
            }

            return response.GetBuffer();
        }

        public void ReadBytes(Stream stream, long n, ProgressTask task)
        {
            task ??= new ProgressTask(0, "dummy", 1);

            var readEndpoint = device.OpenEndpointReader(ReadEndpointID.Ep01);
            var buffer = new byte[BUFFER_SIZE];
            long ctr = 0;
            int rdAct = 0;
            task.MaxValue = n;

            while (Math.Min(rdAct, n - ctr) >= 0)
            {
                readEndpoint.Read(buffer, Timeout, out rdAct);
                stream.Write(buffer, 0, (int)Math.Min(rdAct, n - ctr));

                task.Value = ctr;

                if (rdAct <= 0)
                {
                    break;
                }

                ctr += rdAct;
            }

            task.Value = n;
        }

        public string Command(string command) => Encoding.ASCII.GetString(Command(Encoding.ASCII.GetBytes(command)))
            .Replace("\0", string.Empty);
    }
}
