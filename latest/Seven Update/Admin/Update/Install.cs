﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using SevenUpdate.WCF;

namespace SevenUpdate
{
    class Install
    {
        #region Global Vars

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);
        internal const int MoveOnReboot = 5;

        /// <summary>
        /// Specifies if the installation has been aborted
        /// </summary>
        internal static bool Abort { get; set; }

        /// <summary>
        /// The index of the current update that is installing
        /// </summary>
        static int curUpdate;

        static string DownloadedPath;

        static bool ErrorOccurred { get; set; }

        /// <summary>
        /// A list of history of updates
        /// </summary>
        static Collection<UpdateInformation> history;

        /// <summary>
        /// Indicates if Seven Update is being updated
        /// </summary>
        static bool IsSevenUpdate;
       
        /// <summary>
        /// Number of total updates to install
        /// </summary>
        static int totalUpdates;

        #endregion

        #region Update Installation

        /// <summary>
        /// Checks if the file is in use
        /// </summary>
        /// <param name="files">The list of files of an update</param>
        /// <param name="appDir">The application directory</param>
        /// <returns></returns>
        static bool CheckFileInUse(Collection<UpdateFile> files, string appDir, bool Is64Bit)
        {
            string fileDest;

            foreach (UpdateFile file in files)
            {
                fileDest = Shared.ConvertPath(file.Destination, appDir, Is64Bit);

                if (File.Exists(fileDest))
                {
                    try
                    {
                        FileStream fileStream = new FileStream(fileDest, FileMode.Open, FileAccess.ReadWrite);

                        fileStream.Close();
                    }
                    catch (Exception)
                    {
                        return true;
                    }
                }
                else
                {
                    TextWriter tw = new StreamWriter(Shared.userStore + "error.log");

                    tw.WriteLine(DateTime.Now.ToString() + ": " + fileDest + " not found");

                    tw.Close();

                    return false;

                }
            }

            return false;
        }

        /// <summary>
        /// Installs updates
        /// </summary>
        internal static void InstallUpdates(Collection<Application> applications)
        {
            history = Shared.DeserializeCollection<UpdateInformation>(Shared.appStore + "Update History.xml");
            File.Delete(Shared.appStore + "Update List.xml");

            bool inUse = false;
            int count = 0;
            string fileDest;

            for (int x = 0; x < applications.Count; x++)
            {
                totalUpdates += applications[x].Updates.Count;
            }

            foreach (Application app in applications)
            {
                if (Abort)
                {
                    Environment.Exit(0);
                    return;
                }

                try
                {
                    if (app.Directory == Shared.ConvertPath(@"%PROGRAMFILES%\Seven Software\Seven Update", true, true))
                    {
                        IsSevenUpdate = true;
                    }

                    for (int x = 0; x < app.Updates.Count; x++)
                    {
                        DownloadedPath = Shared.appStore + @"downloads\" + app.Name + @"\" + app.Updates[x].Title + @"\";

                        if (Abort)
                            break;

                        if (EventService.InstallProgressChanged != null)
                            EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(1, 100, x + 1), curUpdate, totalUpdates);
                        if (Program.NotifyIcon != null)
                            Program.NotifyIcon.Text = Program.RM.GetString("InstallingUpdates") + " " + Progress(1, 100, x + 1) + " " + Program.RM.GetString("Complete");


                        inUse = CheckFileInUse(app.Updates[x].Files, app.Directory, app.Is64Bit);

                        if (EventService.InstallProgressChanged != null)
                            EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(3, 100, x + 1), curUpdate, totalUpdates);
                        if (Program.NotifyIcon != null)
                            Program.NotifyIcon.Text = Program.RM.GetString("InstallingUpdates") + " " + Progress(1, 100, x + 1) + " " + Program.RM.GetString("Complete");

                        count++;

                        if (inUse)
                        {
                            AddHistory(app, app.Updates[x]);

                            if (EventService.InstallProgressChanged != null)
                                EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(10, 100, x + 1), curUpdate, totalUpdates);
                            if (Program.NotifyIcon != null)
                                Program.NotifyIcon.Text = Program.RM.GetString("InstallingUpdates") + " " + Progress(1, 100, x + 1) + " " + Program.RM.GetString("Complete");

                            UpdateOnReboot(app.Updates[x], app.Name[0].Value, app.Directory, x, app.Is64Bit);

                        }
                        else
                        {
                            #region Files

                            for (int y = 0; y < app.Updates[x].Files.Count; y++)
                            {
                                fileDest = Shared.ConvertPath(app.Updates[x].Files[y].Destination, app.Directory, app.Is64Bit);

                                Directory.CreateDirectory(Path.GetDirectoryName(fileDest));

                                switch (app.Updates[x].Files[y].Action)
                                {
                                    case FileAction.ExecuteAndDelete:
                                    case FileAction.UnregisterAndDelete:
                                    case FileAction.Delete:
                                        if (app.Updates[x].Files[y].Action == FileAction.ExecuteAndDelete)
                                        {
                                            Process proc = new Process();
                                            proc.StartInfo.FileName = DownloadedPath + Path.GetFileName(app.Updates[x].Files[y].Destination);
                                            proc.StartInfo.Arguments = app.Updates[x].Files[y].Arguments;
                                            proc.Start();
                                            proc.WaitForExit();
                                        }

                                        if (app.Updates[x].Files[y].Action == FileAction.UnregisterAndDelete)
                                        {
                                            Process.Start("regsvr32", "/u " + fileDest);
                                        }

                                        try
                                        {
                                            System.IO.File.Delete(fileDest);
                                        }
                                        catch (Exception)
                                        {
                                            MoveFileEx(fileDest, null, MoveOnReboot);
                                        }

                                        break;
                                    case FileAction.Update:
                                    case FileAction.UpdateAndExecute:
                                    case FileAction.UpdateAndRegister:
                                        try
                                        {
                                            InstallFile(app.Name[0].Value, fileDest, app.Updates[x].Title[0].Value);

                                            if (app.Updates[x].Files[y].Action == FileAction.UpdateAndExecute)
                                            {
                                                Process.Start(fileDest, app.Updates[x].Files[y].Arguments);
                                            }

                                            if (app.Updates[x].Files[y].Action == FileAction.UpdateAndRegister)
                                            {
                                                Process.Start("regsvc32", "/s " + fileDest);
                                            }
                                        }
                                        catch (FileNotFoundException e)
                                        {
                                            AddHistory(app, app.Updates[x], e.Message + " " + fileDest);

                                            ErrorOccurred = true;

                                            if (File.Exists(fileDest + ".bak"))
                                            {
                                                try
                                                {
                                                    File.Copy(fileDest + ".bak", fileDest, true);
                                                    File.Delete(fileDest + ".bak");
                                                }
                                                catch (Exception f)
                                                {
                                                    Program.ReportError(f.Message);
                                                }
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Program.ReportError(e.Message);
                                        }

                                        break;
                                }

                                if (EventService.InstallProgressChanged != null)
                                    EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(y + 1, app.Updates[x].Files.Count, x + 1) - 10, curUpdate, totalUpdates);
                            } //end foreach loop
                            #endregion

                            if (EventService.InstallProgressChanged != null)
                                EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(90, 100, x + 1), curUpdate, totalUpdates);
                            if (Program.NotifyIcon != null)
                                Program.NotifyIcon.Text = Program.RM.GetString("InstallingUpdates") + " " + Progress(1, 100, x + 1) + " " + Program.RM.GetString("Complete");

                            #region Registry

                            SetRegistryItems(app.Updates[x]);

                            #endregion

                            if (EventService.InstallProgressChanged != null)
                                EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(95, 100, x + 1), curUpdate, totalUpdates);
                            if (Program.NotifyIcon != null)
                                Program.NotifyIcon.Text = Program.RM.GetString("InstallingUpdates") + " " + Progress(1, 100, x + 1) + " " + Program.RM.GetString("Complete");

                            #region Shortcuts

                            SetShortcuts(app.Updates[x], app.Directory, app.Is64Bit);

                            #endregion

                            if (EventService.InstallProgressChanged != null)
                                EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(98, 100, x + 1), curUpdate, totalUpdates);
                            if (Program.NotifyIcon != null)
                                Program.NotifyIcon.Text = Program.RM.GetString("InstallingUpdates") + " " + Progress(1, 100, x + 1) + " " + Program.RM.GetString("Complete");

                            AddHistory(app, app.Updates[x]);

                            if (EventService.InstallProgressChanged != null)
                                EventService.InstallProgressChanged(app.Updates[x].Title[0].Value, Progress(100, 100, x + 1), curUpdate, totalUpdates);
                            if (Program.NotifyIcon != null)
                                Program.NotifyIcon.Text = Program.RM.GetString("InstallingUpdates") + " " + Progress(1, 100, x + 1) + " " + Program.RM.GetString("Complete");

                            curUpdate++;

                            app.Updates.RemoveAt(x);

                        }
                    }
                }
                catch (Exception f)
                {
                    Program.ReportError(f.Message);
                }
            }

            if (Shared.RebootNeeded)
            {
                MoveFileEx(Shared.appStore + "reboot.lock", null, MoveOnReboot);

                if (Directory.Exists(Shared.appStore + "downloads"))
                {
                    MoveFileEx(Shared.appStore + "downloads", null, MoveOnReboot);
                }
            }
            else
            {
                // Delete the downloads directory if no errors were found and no reboot is needed
                if (!ErrorOccurred && !IsSevenUpdate)
                {
                    if (Directory.Exists(Shared.appStore + "downloads"))
                    {
                        try
                        {
                            Directory.Delete(DownloadedPath, true);
                            Directory.Delete(Shared.appStore + "downloads", true);
                        }
                        catch (Exception) { }
                    }
                }
            }

            Shared.SerializeCollection<UpdateInformation>(history, Shared.appStore + "Update History.xml");

            if (EventService.InstallDone != null)
            {
                EventService.InstallDone(ErrorOccurred);
            }
            Environment.Exit(0);
        }

        /// <summary>
        /// Installs a file
        /// </summary>
        /// <param name="destination">The destination location</param>
        /// <param name="updateTitle">The title of an update</param>
        /// <param name="appName">The name of the application</param>
        static void InstallFile(string appName, string destination, string updateTitle)
        {
            string file = DownloadedPath + Path.GetFileName(destination);

            if (File.Exists(file))
            {
                Program.SetFileSecurity(file);

                try
                {
                    if (File.Exists(destination))
                    {
                        File.Copy(destination, destination + ".bak", true);
                        File.Delete(destination);
                    }
                    File.Move(file, destination);
                    if (File.Exists(destination + ".bak"))
                        File.Delete(destination + ".bak");

                }
                catch (Exception e)
                {
                    Program.ReportError(e.Message);
                    MoveFileEx(file, destination, MoveOnReboot);
                }
            }
            else
                throw new FileNotFoundException();
        }

        /// <summary>
        /// The current progress of the installation
        /// </summary>
        /// <param name="curProgress">Current progress of the installation</param>
        /// <param name="outOf">The total number of updates</param>
        /// <param name="curUpdate">The current index of the update</param>
        /// <returns>Returns the progress percentage of the update</returns>
        static int Progress(int curProgress, int outOf, int curUpdate)
        {
            try
            {
                int percent = (curProgress * 100) / outOf;

                int updProgress = percent / curUpdate;

                return updProgress / totalUpdates;
            }
            catch (Exception) { return -1; }
        }

        /// <summary>
        /// Sets the registry items of an update
        /// </summary>
        /// <param name="update">The update to use to set the registry items</param>
        /// <param name="appDir">The application directory</param>
        static void SetRegistryItems(Update update)
        {
            RegistryKey key;

            if (update.RegistryItems != null)
                foreach (RegistryItem reg in update.RegistryItems)
                {
                    switch (reg.Hive)
                    {
                        case RegistryHive.ClassesRoot:
                            key = Registry.ClassesRoot; break;
                        case RegistryHive.CurrentUser: key = Registry.CurrentUser; break;
                        case RegistryHive.LocalMachine: key = Registry.LocalMachine; break;
                        case RegistryHive.Users: key = Registry.Users; break;
                        default: key = Registry.CurrentUser; break;
                    }
                    try
                    {
                        switch (reg.Action)
                        {
                            case RegistryAction.Add:
                                Registry.SetValue(reg.Key, reg.KeyValue, reg.Data, reg.ValueKind);
                                break;
                            case RegistryAction.DeleteKey:
                                key.OpenSubKey(reg.Key, true).DeleteSubKeyTree(reg.Key);
                                break;
                            case RegistryAction.DeleteValue:
                                key.OpenSubKey(reg.Key, true).DeleteValue(reg.KeyValue, false);
                                break;
                        }
                    }
                    catch (Exception) { }
                }
        }

        /// <summary>
        /// Installs the shortcuts of an update
        /// </summary>
        /// <param name="update">The update to use to install shortcuts</param>
        /// <param name="appDir">The application directory</param>
        static void SetShortcuts(Update update, string appDir, bool Is64Bit)
        {
            if (update.Shortcuts != null)
            {
                Interop.IWshRuntimeLibrary.WshShellClass WshShell = new Interop.IWshRuntimeLibrary.WshShellClass();

                Interop.IWshRuntimeLibrary.IWshShortcut shortcut;

                // Choose the path for the shortcut
                foreach (Shortcut link in update.Shortcuts)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Shared.ConvertPath(link.Location, true, Is64Bit)));
                    File.Delete(Shared.ConvertPath(link.Location, true, Is64Bit));
                    shortcut = (Interop.IWshRuntimeLibrary.IWshShortcut)WshShell.CreateShortcut(Shared.ConvertPath(link.Location, true, Is64Bit));
                    // Where the shortcut should point to
                    shortcut.TargetPath = Shared.ConvertPath(link.Target, appDir, Is64Bit);
                    // Description for the shortcut
                    shortcut.Description = link.Description[0].Value;
                    // Location for the shortcut's icon
                    shortcut.IconLocation = Shared.ConvertPath(link.Icon, appDir, Is64Bit); ;
                    // The arguments to be used for the shortcut
                    shortcut.Arguments = link.Arguments;
                    // The working directory to be used for the shortcut
                    shortcut.WorkingDirectory = Shared.ConvertPath(appDir, true, Is64Bit);
                    // Create the shortcut at the given path
                    shortcut.Save();
                }
            }

        }

        /// <summary>
        /// Sets the update to install on reboot
        /// </summary>
        /// <param name="update">The update to install on reboot</param>
        /// <param name="appName">The name of the application</param>
        /// <param name="curUpdate">The index of the current update</param>
        static void UpdateOnReboot(Update update, string appName, string appDir, int curUpdate, bool Is64Bit)
        {
            if (EventService.InstallProgressChanged != null)
                EventService.InstallProgressChanged(update.Title[0].Value, Progress(20, 100, curUpdate + 1), curUpdate, totalUpdates);

            #region Registry

            SetRegistryItems(update);

            #endregion

            if (EventService.InstallProgressChanged != null)
                EventService.InstallProgressChanged(update.Title[0].Value, Progress(50, 100, curUpdate + 1), curUpdate, totalUpdates);

            #region Shortcuts

            SetShortcuts(update, appName, Is64Bit);

            #endregion

            if (EventService.InstallProgressChanged != null)
                EventService.InstallProgressChanged(update.Title[0].Value, Progress(75, 100, curUpdate + 1), curUpdate, totalUpdates);

            if (IsSevenUpdate)
            {
                for (int x = 0; x < update.Files.Count; x++)
                {
                    if (update.Files[x].Action == FileAction.UnregisterAndDelete || update.Files[x].Action == FileAction.Delete)
                    {
                        try
                        {
                            File.Delete(Shared.ConvertPath(update.Files[x].Destination, appDir, Is64Bit));

                        }
                        catch (Exception)
                        {
                            MoveFileEx(Shared.ConvertPath(update.Files[x].Destination, appDir, Is64Bit), null, MoveOnReboot);
                        }
                    }
                }
                Process proc = new Process();

                proc.StartInfo.FileName = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\SU Helper.exe";

                if (!Program.IsAdmin())
                    proc.StartInfo.Verb = "runas";

                proc.StartInfo.UseShellExecute = true;

                proc.StartInfo.Arguments = "\"" + update.Title + "\"";

                proc.StartInfo.CreateNoWindow = true;

                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                proc.Start();
            }
            else
            {
                string fileDest;

                for (int x = 0; x < update.Files.Count; x++)
                {
                    fileDest = Shared.ConvertPath(update.Files[x].Destination, appDir, Is64Bit);

                    Program.SetFileSecurity(DownloadedPath + Path.GetFileName(fileDest));

                    MoveFileEx(DownloadedPath + Path.GetFileName(fileDest),

                    fileDest,MoveOnReboot);
                }

                if (!File.Exists(Shared.appStore + "reboot.lock"))
                {
                    File.Create(Shared.appStore + "reboot.lock").WriteByte(0);

                    Program.SetFileSecurity(Shared.appStore + "reboot.lock");
                }
            }
            if (EventService.InstallProgressChanged != null)
                EventService.InstallProgressChanged(update.Title[0].Value, Progress(100, 100, curUpdate + 1), curUpdate, totalUpdates);
        }

        #endregion

        #region Update History

        /// <summary>
        /// Adds an update to the update history
        /// </summary>
        /// <param name="app">The application info</param>
        /// <param name="info">The update to add</param>
        /// <param name="error">The error message when trying to install updates</param>
        static void AddHistory(Application app, Update info, string error)
        {

            UpdateInformation hist = new UpdateInformation();

            hist.ApplicationName = app.Name;

            hist.HelpUrl = app.HelpUrl;

            hist.Publisher = app.Publisher;

            hist.PublisherUrl = app.PublisherUrl;

            if (error == null)
            {
                hist.Status = UpdateStatus.Successful;

                hist.Description = info.Description;
            }
            else
            {
                hist.Status = UpdateStatus.Failed;
                LocaleString ls = new LocaleString();
                ls.Value = error;
                ls.lang = "en";
                Collection<LocaleString> desc = new Collection<LocaleString>();
                desc.Add(ls);
                hist.Description = desc;
            }

            hist.InfoUrl = info.InfoUrl;

            hist.InstallDate = DateTime.Now.ToShortDateString();

            hist.ReleaseDate = info.ReleaseDate;

            hist.Importance = info.Importance;

            hist.UpdateTitle = info.Title;

            history.Add(hist);

        }

        /// <summary>
        /// Adds an update to the update history
        /// </summary>
        /// <param name="app">The application info</param>
        /// <param name="info">The update to add</param>
        static void AddHistory(Application app, Update info)
        {
            AddHistory(app, info, null);
        }

        #endregion
    }
}
