using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace m3u8Video.Tools
{
    public class DebugView
    {
        //只有debug模式下
        public static void LogDebug(string msg)
        {
            Debug.WriteLine($"{DateTime.Now}:{msg}");
        }

        //release模式下也有
        public static void LogDebugger(string msg)
        {
            Debugger.Log(0, null, $"{DateTime.Now}:{msg}::DebugView");
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);
        public static void Log(string msg)
        {
            OutputDebugString($"{DateTime.Now}:{msg}");
        }


    }
}
