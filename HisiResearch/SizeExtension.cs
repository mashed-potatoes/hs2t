using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HisiResearch
{
    public static class SizeExtension
    {
        public enum SizeUnits
        {
            Byte, KB, MB, GB, TB, PB, EB, ZB, YB
        }

        public static string ToSize(this uint value, SizeUnits unit)
        {
            return (value / (double)Math.Pow(1024, (uint)unit)).ToString("0.0");
        }
    }
}
