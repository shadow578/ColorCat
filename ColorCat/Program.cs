using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ColorCat
{
    class Program
    {
        /// <summary>
        /// lock object for console writes
        /// </summary>
        static readonly object CON_LOCK = new object();

        /// <summary>
        /// adb child process
        /// </summary>
        static Process adbProc;

        static void Main(string[] cmdLine)
        {
            //save original colors first
            ConsoleColor originalFg = Console.ForegroundColor;
            ConsoleColor originalBg = Console.BackgroundColor;

            //add own help if no cmdLine
            if (cmdLine.Length == 0)
            {
                Console.WriteLine("ColorCat usage: just like normal adb ;)\n\n");
            }

            //build args string
            StringBuilder args = new StringBuilder("");
            foreach (string arg in cmdLine)
                args.Append(arg);

            //prepare adb process with output redirect
            ProcessStartInfo adbSI = new ProcessStartInfo("adb", args.ToString())
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            //register CTRL+C to exit adb process
            Console.CancelKeyPress += OnConsoleCancel;

            //prepare process
            adbProc = new Process()
            {
                StartInfo = adbSI,
                EnableRaisingEvents = true
            };
            adbProc.ErrorDataReceived += WriteADBError;
            adbProc.OutputDataReceived += WriteADBOutput;

            //start process
            adbProc.Start();

            //begin reading output
            adbProc.BeginOutputReadLine();
            adbProc.BeginErrorReadLine();

            //wait until process exits or is killed
            adbProc.WaitForExit();

            //dispose adb process
            lock (adbProc)
            {
                adbProc.Dispose();
                adbProc = null;
            }

            //reset colors
            Console.ForegroundColor = originalFg;
            Console.BackgroundColor = originalBg;
        }

        /// <summary>
        /// Called when CTRL+C is pressed
        /// </summary>
        static void OnConsoleCancel(object sender, ConsoleCancelEventArgs e)
        {
            //check adb already started
            if (adbProc == null) return;

            //cancel event killing us
            e.Cancel = true;
            Console.WriteLine("CTRL+C pressed, exiting asap...");

            //kill adb
            lock (adbProc)
            {
                adbProc.Kill();
            }
        }

        /// <summary>
        /// Write normal adb output
        /// </summary>
        static void WriteADBOutput(object sender, DataReceivedEventArgs e)
        {
            //check event and event data not null
            if (e == null || string.IsNullOrEmpty(e.Data)) return;

            //DATE     TIME       PID   TID  L    TAG  :        TEXT
            //07-26 14:35:00.003  1646  1660 E memtrack: Couldn't load memtrack module
            //Levels: V, D, I, W, E, F, S
            //CG1 = Date + Time
            //CG2 = PID
            //CG3 = TID
            //CG4 = LEVEL
            //CG5 = TAG
            //CG6 = TEXT
            const string ADB_LINE_REGEX = @"^(\d\d-\d\d \d\d:\d\d:\d\d.\d*)\s*(\d*)\s*(\d*)\s*([VDIWEFS])\s*(.*)\s*:\s*(.*)$";

            //get level with regex
            Match match = Regex.Match(e.Data, ADB_LINE_REGEX);
            if (!match.Success || !match.Groups[4].Success)
            {
                //just print the line, without coloring
                WriteLine(e.Data);
                return;
            }

            //get color for print from level in cg4
            ConsoleColor? color;
            string level = match.Groups[4].Value;
            switch (level.ToUpper())
            {
                case "V":
                    color = null;
                    break;
                case "D":
                    color = null;
                    break;
                case "I":
                    color = null;
                    break;
                case "W":
                    color = ConsoleColor.DarkYellow;//orange
                    break;
                case "E":
                    color = ConsoleColor.Red;
                    break;
                case "F":
                    color = ConsoleColor.Red;
                    break;
                case "S":
                    color = ConsoleColor.Blue;//should never happen
                    break;
                default:
                    color = null;
                    break;
            }

            WriteLine(e.Data, color);
        }

        /// <summary>
        /// Write adb errors
        /// </summary>
        static void WriteADBError(object sender, DataReceivedEventArgs e)
        {
            if (e != null && e.Data != null)
                WriteLine(e.Data, ConsoleColor.Red);
        }

        /// <summary>
        /// Write a line to console, in the given foreground color
        /// </summary>
        /// <param name="s">string to write</param>
        /// <param name="c">foreground color to use, null = default</param>
        static void WriteLine(string s, ConsoleColor? c = null)
        {
            lock (CON_LOCK)
            {
                //set foreground color
                ConsoleColor ifg = Console.ForegroundColor;
                if (c.HasValue)
                {
                    Console.ForegroundColor = c.Value;
                }

                //write line
                Console.WriteLine(s);

                //restore foreground color
                if (c.HasValue)
                {
                    Console.ForegroundColor = ifg;
                }
            }
        }
    }
}
