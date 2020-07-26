using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ColorCat
{
    class Program
    {
        static readonly object CON_LOCK = new object();

        static void Main(string[] cmdLine)
        {
            //add own help if no cmdLine
            if(cmdLine.Length == 0)
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

            //start process
            using (Process p = new Process())
            {
                //prepare process
                p.StartInfo = adbSI;
                p.EnableRaisingEvents = true;
                p.ErrorDataReceived += WriteADBError;
                p.OutputDataReceived += WriteADBOutput;

                //start adb
                p.Start();

                //begin reading output
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();

                //wait until process exits
                p.WaitForExit();
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
