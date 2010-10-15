// ***********************************************************************
// <copyright file="App.cs"
//            project="SevenUpdate.Admin"
//            assembly="SevenUpdate.Admin"
//            solution="SevenUpdate"
//            company="Seven Software">
//     Copyright (c) Seven Software. All rights reserved.
// </copyright>
// <author username="sevenalive">Robert Baker</author>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3">
//  This file is part of Seven Update.
//
//    Seven Update is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    Seven Update is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with Seven Update.  If not, see http://www.gnu.org/licenses/.
// </license>
// ***********************************************************************
namespace SevenUpdate.Admin
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.ServiceModel;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows.Forms;

    using Microsoft.Win32;

    using SevenUpdate.Admin.Properties;
    using SevenUpdate.Service;

    using Application = System.Windows.Application;

    /// <summary>The main class of the application</summary>
    internal static class App
    {
        #region Constants and Fields

        /// <summary>The collection of applications to update</summary>
        private static Collection<Sui> apps;

        /// <summary>The WCF service host</summary>
        private static ServiceHost host;

        /// <summary>Indicates if the installation was executed by automatic settings</summary>
        private static bool isAutoInstall;

        /// <summary>Indicates if the program is currently installing updates</summary>
        private static bool isInstalling;

        /// <summary>The notifyIcon used only when Auto Updating</summary>
        private static NotifyIcon notifyIcon;

        /// <summary>Indicates if the program is waiting</summary>
        private static bool waiting;

        #endregion

        #region Enums

        /// <summary>Defines constants for the notification type, such has SearchComplete</summary>
        private enum NotifyType
        {
            /// <summary>Indicates searching is completed</summary>
            SearchComplete,

            /// <summary>Indicates the downloading of updates has started</summary>
            DownloadStarted,

            /// <summary>Indicates download has completed</summary>
            DownloadComplete,

            /// <summary>Indicates that the installation of updates has begun</summary>
            InstallStarted,

            /// <summary>Indicates that the installation of updates has completed</summary>
            InstallCompleted
        }

        #endregion

        #region Properties

        /// <summary>Gets or sets a value indicating whether Seven Update UI is currently connected.</summary>
        private static bool IsClientConnected
        {
            get;
            set;
        }

        /// <summary>Gets Seven Updates program settings</summary>
        private static Config Settings
        {
            get
            {
                var t = Utilities.Deserialize<Config>(Utilities.ConfigFile);
                return t ?? new Config
                    {
                        AutoOption = AutoUpdateOption.Notify,
                        IncludeRecommended = false
                    };
            }
        }

        #endregion

        #region Methods

        /// <summary>Reports that the download has completed and starts update installation if necessary</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DownloadCompletedEventArgs"/> instance containing the event data.</param>
        private static void DownloadCompleted(object sender, DownloadCompletedEventArgs e)
        {
            if ((Settings.AutoOption == AutoUpdateOption.Install && isAutoInstall) || !isAutoInstall)
            {
                if (IsClientConnected)
                {
                    WcfService.ReportProgress(e);
                }

                Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.InstallStarted);
                isInstalling = true;
                File.Delete(Utilities.AllUserStore + "updates.sui");
                Install.InstallUpdates(apps);
            }
            else
            {
                isInstalling = false;
                Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.DownloadComplete);
            }
        }

        /// <summary>Reports that the download progress has changed</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DownloadProgressChangedEventArgs"/> instance containing the event data.</param>
        private static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            isInstalling = true;
            if (IsClientConnected)
            {
                WcfService.ReportProgress(e);
            }

            Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, String.Format(CultureInfo.CurrentCulture, Resources.DownloadProgress, e.FilesTransferred, e.FilesTotal));
        }

        /// <summary>Runs when there is an error searching for updates</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ErrorOccurredEventArgs"/> instance containing the event data.</param>
        private static void ErrorOccurred(object sender, ErrorOccurredEventArgs e)
        {
            if (e.ErrorType == ErrorType.FatalNetworkError)
            {
                ShutdownApp();
            }
        }

        /// <summary>Reports an error when the host faults</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void HostFaulted(object sender, EventArgs e)
        {
            IsClientConnected = false;
            Utilities.ReportError(@"Host Fault", Utilities.AllUserStore);
            WcfService.ReportProgress(new ErrorOccurredEventArgs(Resources.UpdateServiceInterrupted, ErrorType.FatalError));

            if (host.State == CommunicationState.Opened)
            {
                host.Abort();
            }

            ShutdownApp();
        }

        /// <summary>Reports an error if an unknown message received</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.ServiceModel.UnknownMessageReceivedEventArgs"/> instance containing the event data.</param>
        private static void HostUnknownMessageReceived(object sender, UnknownMessageReceivedEventArgs e)
        {
            Utilities.ReportError(e.Message.ToString(), Utilities.AllUserStore);
        }

        /// <summary>Reports the installation has completed</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="InstallCompletedEventArgs"/> instance containing the event data.</param>
        private static void InstallCompleted(object sender, InstallCompletedEventArgs e)
        {
            isInstalling = false;
            if (IsClientConnected)
            {
                WcfService.ReportProgress(e);
            }

            File.Delete(Utilities.AllUserStore + "updates.sui");
            ShutdownApp();
        }

        /// <summary>Reports when the installation progress has changed</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="InstallProgressChangedEventArgs"/> instance containing the event data.</param>
        private static void InstallProgressChanged(object sender, InstallProgressChangedEventArgs e)
        {
            if (IsClientConnected)
            {
                WcfService.ReportProgress(e);
            }

            Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, String.Format(CultureInfo.CurrentCulture, Resources.InstallProgress, e.CurrentProgress));
        }

        /// <summary>Starts the WCF service</summary>
        private static void StartWcfHost()
        {
            host = new ServiceHost(typeof(WcfService));
            host.Faulted += HostFaulted;
            host.UnknownMessageReceived += HostUnknownMessageReceived;
            WcfService.ClientConnected += ServiceClientConnected;
            WcfService.ClientDisconnected += ServiceClientDisconnected;
            SystemEvents.SessionEnding += PreventClose;
            Search.SearchCompleted += SearchCompleted;
            Search.ErrorOccurred += ErrorOccurred;
            Download.DownloadCompleted += DownloadCompleted;
            Download.DownloadProgressChanged += DownloadProgressChanged;
            Install.InstallCompleted += InstallCompleted;
            Install.InstallProgressChanged += InstallProgressChanged;
            try
            {
                host.Open();
            }
            catch (FaultException e)
            {
                Utilities.ReportError(e, Utilities.AllUserStore);
                WcfService.ReportProgress(new ErrorOccurredEventArgs(e.Message, ErrorType.FatalError));

                SystemEvents.SessionEnding -= PreventClose;
                ShutdownApp();
            }
        }

        /// <summary>The main execution method</summary>
        /// <param name="args">The command line arguments</param>
        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
#if !DEBUG
            var timer = new System.Timers.Timer(10000);
            timer.Elapsed += ShutdownApp;
            timer.Start();
#endif
            try
            {
                using (new Mutex(true, "SevenUpdate.Admin", out createdNew))
                {
                    if (createdNew)
                    {
                        StartWcfHost();
                        if (!Directory.Exists(Utilities.AllUserStore))
                        {
                            Directory.CreateDirectory(Utilities.AllUserStore);
                        }
                    }
                }

                var app = new Application();

                using (notifyIcon = new NotifyIcon())
                {
                    notifyIcon.Icon = Resources.trayIcon;
                    notifyIcon.Text = Resources.CheckingForUpdates;
                    notifyIcon.Visible = false;

                    ProcessArgs(args);
                    app.Run();
                }
            }
            catch (Exception e)
            {
                Utilities.ReportError(e, Utilities.AllUserStore);
            }

            SystemEvents.SessionEnding -= PreventClose;
        }

        /// <summary>Processes the command line arguments</summary>
        /// <param name="args">The arguments to process</param>
        private static void ProcessArgs(IList<string> args)
        {
            if (args.Count <= 0)
            {
            }
            else
            {
                if (args[0] == "Abort")
                {
                    try
                    {
                        using (var fs = File.Create(Utilities.AllUserStore + "abort.lock"))
                        {
                            fs.WriteByte(0);
                        }
                    }
                    catch (Exception e)
                    {
                        if (!(e is OperationCanceledException || e is UnauthorizedAccessException || e is InvalidOperationException || e is NotSupportedException))
                        {
                            throw;
                        }

                        Utilities.ReportError(e, Utilities.UserStore);
                    }

                    ShutdownApp();
                }

                if (args[0] == "Auto")
                {
                    if (File.Exists(Utilities.AllUserStore + "abort.lock"))
                    {
                        try
                        {
                            File.Delete(Utilities.AllUserStore + "abort.lock");
                        }
                        catch (Exception e)
                        {
                            if (!(e is OperationCanceledException || e is UnauthorizedAccessException || e is InvalidOperationException || e is NotSupportedException))
                            {
                                throw;
                            }

                            Utilities.ReportError(e, Utilities.UserStore);
                        }
                    }

                    isAutoInstall = true;
                    isInstalling = true;
                    notifyIcon.BalloonTipClicked += RunSevenUpdate;
                    notifyIcon.Click += RunSevenUpdate;
                    notifyIcon.Visible = true;
                    Search.ErrorOccurred += ErrorOccurred;
                    Search.SearchForUpdates(Utilities.Deserialize<Collection<Sua>>(Utilities.ApplicationsFile));
                }
                else
                {
                    ShutdownApp();
                }
            }
        }

        /// <summary>Prevents the system from shutting down until the installation is safely stopped</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Win32.SessionEndingEventArgs"/> instance containing the event data.</param>
        private static void PreventClose(object sender, SessionEndingEventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            using (var fs = File.Create(Utilities.AllUserStore + "abort.lock"))
            {
                fs.WriteByte(0);
            }

            e.Cancel = true;
        }

        /// <summary>Starts Seven Update UI</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void RunSevenUpdate(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                if (notifyIcon.Text == Resources.UpdatesFoundViewThem || notifyIcon.Text == Resources.UpdatesDownloadedViewThem || notifyIcon.Text == Resources.CheckingForUpdates)
                {
                    Utilities.StartProcess(Utilities.AppDir + @"SevenUpdate.exe", @"Auto");
                }
                else
                {
                    Utilities.StartProcess(Utilities.AppDir + @"SevenUpdate.exe", @"Reconnect");
                }
            }
            else
            {
                Utilities.StartProcess(@"schtasks.exe", "/Run /TN \"SevenUpdate\"");
            }

            if (notifyIcon.Text == Resources.UpdatesFoundViewThem || notifyIcon.Text == Resources.UpdatesDownloadedViewThem || notifyIcon.Text == Resources.CheckingForUpdates)
            {
                ShutdownApp();
            }
        }

        /// <summary>Runs when the search for updates has completed for an auto update</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SearchCompletedEventArgs"/> instance containing the event data.</param>
        private static void SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            isInstalling = false;
            apps = e.Applications as Collection<Sui>;
            if (apps.Count > 0)
            {
                Utilities.Serialize(apps, Utilities.AllUserStore + "updates.sui");

                Utilities.StartProcess(@"cacls.exe", "\"" + Utilities.AllUserStore + "updates.sui\" /c /e /g Users:F");

                if (Settings.AutoOption == AutoUpdateOption.Notify)
                {
                    Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.SearchComplete);
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.DownloadStarted);
                    Task.Factory.StartNew(() => Download.DownloadUpdates(apps));
                    isInstalling = true;
                }
            }
            else
            {
                ShutdownApp();
            }
        }

        /// <summary>Occurs when Seven Update UI connects to the admin process</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The data related to the event</param>
        private static void ServiceClientConnected(object sender, EventArgs e)
        {
            IsClientConnected = true;
        }

        /// <summary>Occurs when the Seven Update UI disconnected</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The data related to the event</param>
        private static void ServiceClientDisconnected(object sender, EventArgs e)
        {
            IsClientConnected = false;

            if (isInstalling == false)
            {
                ShutdownApp();
            }
        }

        /// <summary>Shuts down the process and removes the icon of the notification bar</summary>
        private static void ShutdownApp()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            Environment.Exit(0);
        }

        /// <summary>Shutdowns the application</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs"/> instance containing the event data.</param>
        private static void ShutdownApp(object sender, ElapsedEventArgs e)
        {
            if (isInstalling)
            {
                return;
            }

            if (Process.GetProcessesByName(@"SevenUpdate").Length > 0 && waiting)
            {
                ShutdownApp();
            }

            if (Process.GetProcessesByName(@"SevenUpdate").Length < 1 && !waiting)
            {
                ShutdownApp();
            }
        }

        /// <summary>Updates the notify icon text</summary>
        /// <param name="text">The string to set the <see cref="notifyIcon"/> text</param>
        private static void UpdateNotifyIcon(string text)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Text = text;
            }
        }

        /// <summary>Updates the <see cref="notifyIcon"/> state</summary>
        /// <param name="filter">The <see cref="NotifyType"/> to set the <see cref="notifyIcon"/> to.</param>
        private static void UpdateNotifyIcon(NotifyType filter)
        {
            if (notifyIcon == null)
            {
                return;
            }

            notifyIcon.Visible = true;
            switch (filter)
            {
                case NotifyType.DownloadStarted:
                    notifyIcon.Text = Resources.DownloadingUpdates;
                    break;
                case NotifyType.DownloadComplete:
                    waiting = true;
                    notifyIcon.Text = Resources.UpdatesDownloadedViewThem;
                    notifyIcon.ShowBalloonTip(5000, Resources.UpdatesDownloaded, Resources.UpdatesDownloadedViewThem, ToolTipIcon.Info);
                    break;
                case NotifyType.InstallStarted:
                    notifyIcon.Text = Resources.InstallingUpdates;
                    break;
                case NotifyType.SearchComplete:
                    waiting = true;
                    notifyIcon.Text = Resources.UpdatesFoundViewThem;
                    notifyIcon.ShowBalloonTip(5000, Resources.UpdatesFound, Resources.UpdatesFoundViewThem, ToolTipIcon.Info);
                    break;
                case NotifyType.InstallCompleted:
                    notifyIcon.Text = Resources.InstallationCompleted;
                    notifyIcon.ShowBalloonTip(5000, Resources.UpdatesInstalled, Resources.InstallationCompleted, ToolTipIcon.Info);
                    ShutdownApp();
                    break;
            }
        }

        #endregion
    }
}