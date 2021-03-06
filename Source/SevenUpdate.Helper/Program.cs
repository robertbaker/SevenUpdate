// <copyright file="Program.cs" project="SevenUpdate.Helper">Robert Baker</copyright>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3" />

namespace SevenUpdate.Helper
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Timers;
    using System.Windows.Forms;

    using Timer = System.Timers.Timer;

    /// <summary>The main class.</summary>
    internal static class Program
    {
        /// <summary>Moves a file on reboot.</summary>
        const int MoveOnReboot = 5;

        /// <summary>The current directory the application resides in.</summary>
        static readonly string AppDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        /// <summary>Stops a running process.</summary>
        /// <param name="name">The name of the process to kill.</param>
        static void KillProcess(string name)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(name);
                if (processes.Length > 0)
                {
                    foreach (var t in processes)
                    {
                        t.Kill();
                    }
                }
            }
            catch (Exception e)
            {
                if (
                    !(e is OperationCanceledException || e is UnauthorizedAccessException
                      || e is InvalidOperationException || e is NotSupportedException || e is Win32Exception))
                {
                    throw;
                }
            }
        }

        /// <summary>The main entry point for the application.</summary>
        /// <param name="args">The arguments passed to the program at startup.</param>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                KillProcess("SevenUpdate");
                KillProcess("SevenUpdate.Admin");

                Thread.Sleep(1000);

                try
                {
                    File.Delete(Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "reboot.lock"));
                }
                catch (Exception e)
                {
                    if (!(e is UnauthorizedAccessException || e is IOException))
                    {
                        throw;
                    }

                    NativeMethods.MoveFileExW(
                        Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "reboot.lock"), 
                        null, 
                        MoveOnReboot);
                }

                string[] files = Directory.GetFiles(AppDir, "*.bak");

                foreach (string t in files)
                {
                    try
                    {
                        File.Delete(t);
                    }
                    catch (Exception e)
                    {
                        if (!(e is UnauthorizedAccessException || e is IOException))
                        {
                            throw;
                        }

                        NativeMethods.MoveFileExW(t, null, MoveOnReboot);
                    }
                }

                if (Environment.OSVersion.Version.Major < 6)
                {
                    StartProcess(Path.Combine(AppDir, "SevenUpdate.exe"), "Auto");
                }
                else
                {
                    StartProcess(@"schtasks.exe", "/Run /TN \"SevenUpdate\"");
                }

                Environment.Exit(0);
            }
            else
            {
                if (Environment.OSVersion.Version.Major < 6)
                {
                    using (var timer = new Timer())
                    {
                        timer.Elapsed += RunSevenUpdate;
                        timer.Interval = 7200000;
                        timer.Enabled = true;
                        Application.Run();
                    }
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }

        /// <summary>Run Seven Update and auto check for updates.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Timers.ElapsedEventArgs</c> instance containing the event data.</param>
        static void RunSevenUpdate(object sender, ElapsedEventArgs e)
        {
            Process.Start(Path.Combine(AppDir, "SevenUpdate.Admin.exe"), "Auto");
        }

        /// <summary>Starts a process on the system.</summary>
        /// <param name="fileName">The file to execute.</param>
        /// <param name="arguments">The arguments to execute with the file.</param>
        /// <param name="wait">If set to <c>True</c> the calling thread will be blocked until process has exited.</param>
        /// <param name="hidden">If set to <c>True</c> the process will execute with no UI.</param>
        static void StartProcess(string fileName, string arguments, bool wait = false, bool hidden = true)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                if (arguments != null)
                {
                    process.StartInfo.Arguments = arguments;
                }

                if (hidden)
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                try
                {
                    process.Start();
                    if (wait)
                    {
                        process.WaitForExit();
                    }

                    return;
                }
                catch (Exception e)
                {
                    if (
                        !(e is OperationCanceledException || e is UnauthorizedAccessException
                          || e is InvalidOperationException || e is NotSupportedException || e is Win32Exception))
                    {
                        throw;
                    }
                }
            }

            return;
        }
    }
}