using m3u8Video.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m3u8Video
{
    public class Logger
    {
        private static int Level = 1;
        public static Action<string> action = default;

        public static void LogDebug(string msg)
        {
            Log(msg, 1);

        }

        public static void LogRelease(string msg)
        {
            Log(msg, 2);
        }


        public static void LogError(string msg)
        {
            Log(msg, 3);
        }

        private static void Log(string msg, int level)
        {
            //debugView
            DebugView.LogDebugger(msg);
            //Debugger.Log(0, null, $"{DateTime.Now}:{msg}");
            if (Level <= level)
            {
                Console.WriteLine($"{DateTime.Now}:{msg}");
                if (action != default)
                {
                    action.BeginInvoke(msg, null, null);
                }
            }
        }

        public static void ChangeNumber()
        {
            // Common.form.changeNumber();
        }
    }
}
