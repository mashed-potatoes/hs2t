using LibUsbDotNet;
using LibUsbDotNet.Main;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HisiResearch.Engine
{
    class Connection
    {
        private const int USB_VID = 0x18D1;
        private const int USB_PID = 0xD00D;

        public static UsbDevice Get()
        {
            var finder = new UsbDeviceFinder(USB_VID, USB_PID);
            UsbDevice device = UsbDevice.OpenUsbDevice(finder);

            if (device is null)
            {
                AnsiConsole.Status()
                    .Start("Waiting for any device...", ctx =>
                    {
                        while (device is null)
                        {
                            Thread.Sleep(200);
                            device = UsbDevice.OpenUsbDevice(finder);
                        }
                    });
            }

            var wDev = device as IUsbDevice;

            if (wDev is IUsbDevice)
            {
                wDev.SetConfiguration(1);
                wDev.ClaimInterface(0);
            }

            return device;
        }
    }
}
