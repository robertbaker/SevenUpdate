// <copyright file="App.cs" project="SevenUpdate.Admin">Robert Baker</copyright>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3" />

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

    using Application = System.Windows.Application;
    using Timer = System.Timers.Timer;

    /// <summary>The main class of the application.</summary>
    internal static class App
    {
        /// <summary>The all users application data location.</summary>
        public static readonly string AllUserStore =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Seven Update");

        /// <summary>The location of the list of applications Seven Update can update.</summary>
        public static readonly string ApplicationsFile = Path.Combine(AllUserStore, "Apps.sul");

        /// <summary>The location of the application settings file.</summary>
        public static readonly string ConfigFile = Path.Combine(AllUserStore, "App.config");

        /// <summary>The location of the hidden updates file.</summary>
        public static readonly string HiddenFile = Path.Combine(AllUserStore, "Hidden.suh");

        /// <summary>The location of the update history file.</summary>
        static readonly string HistoryFile = Path.Combine(AllUserStore, "History.suh");

        /// <summary>The WCF service host.</summary>
        static ElevatedProcessCallback client;

        /// <summary>Gets or sets a value indicating whether the installation was executed by automatic settings.</summary>
        static bool isAutoInstall;

        /// <summary>Gets or sets a value indicating whether Seven Update UI is currently connected.</summary>
        static bool isClientConnected;

        /// <summary>The notifyIcon used only when Auto Updating.</summary>
        static NotifyIcon notifyIcon;

        /// <summary>Indicates if the program is waiting.</summary>
        static bool waiting;

        /// <summary>Defines constants for the notification type, such has SearchComplete.</summary>
        enum NotifyType
        {
            /// <summary>Indicates searching is completed.</summary>
            SearchComplete, 

            /// <summary>Indicates the downloading of updates has started.</summary>
            DownloadStarted, 

            /// <summary>Indicates download has completed.</summary>
            DownloadComplete, 

            /// <summary>Indicates that the installation of updates has begun.</summary>
            InstallStarted, 

            /// <summary>Indicates that the installation of updates has completed.</summary>
            InstallCompleted
        }

        /// <summary>Gets or sets the collection of applications to update.</summary>
        internal static Collection<Sui> Applications { get; set; }

        /// <summary>Gets a value indicating whether the program is currently installing updates.</summary>
        internal static bool IsInstalling { get; private set; }

        /// <summary>Gets Seven Updates program settings.</summary>
        static Config Settings
        {
            get
            {
                return File.Exists(ConfigFile)
                           ? Utilities.Deserialize<Config>(ConfigFile)
                           : new Config { AutoOption = AutoUpdateOption.Notify, IncludeRecommended = false };
            }
        }

        /// <summary>Adds an update to the history.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The event data.</param>
        static void AddHistory(object sender, UpdateInstalledEventArgs e)
        {
            Collection<Suh> history = File.Exists(HistoryFile)
                                          ? Utilities.Deserialize<Collection<Suh>>(HistoryFile) : new Collection<Suh>();
            history.Add(e.Update);
            Utilities.Serialize(history, HistoryFile);
        }

        /// <summary>Checks if Seven Update is running.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <see cref="System.Timers.ElapsedEventArgs" /> instance containing the event data.</param>
        static void CheckIfRunning(object sender, ElapsedEventArgs e)
        {
            Task.Factory.StartNew(
                () =>
                    {
                        if (
                            File.Exists(
                                Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "abort.lock")))
                        {
                            Download.CancelDownload();
                            Install.CancelInstall();
                            try
                            {
                                File.Delete(
                                    Path.Combine(
                                        Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "abort.lock"));
                            }
                            catch (IOException)
                            {
                            }
                        }

                        if (client == null)
                        {
                            StartWcfHost();
                        }

                        if (IsInstalling)
                        {
                            return;
                        }

                        if (Process.GetProcessesByName("SevenUpdate").Length > 0 || waiting)
                        {
                            return;
                        }

#if (!DEBUG)
                        ShutdownApp();
#endif
                    });
        }

        /// <summary>Reports that the download has completed and starts update installation if necessary.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>DownloadCompletedEventArgs</c> instance containing the event data.</param>
        static void DownloadCompleted(object sender, DownloadCompletedEventArgs e)
        {
            if ((Settings.AutoOption == AutoUpdateOption.Install && isAutoInstall) || !isAutoInstall)
            {
                if (isClientConnected)
                {
                    client.OnDownloadCompleted(sender, e);
                }

                Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.InstallStarted);
                IsInstalling = true;
                File.Delete(Path.Combine(AllUserStore, "updates.sui"));
                Task.Factory.StartNew(
                    () => Install.InstallUpdates(Applications, Path.Combine(AllUserStore, "downloads")));
            }
            else
            {
                IsInstalling = false;
                Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.DownloadComplete);
            }
        }

        /// <summary>Reports that the download progress has changed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>DownloadProgressChangedEventArgs</c> instance containing the event data.</param>
        static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            IsInstalling = true;
            if (isClientConnected)
            {
                client.OnDownloadProgressChanged(sender, e);
            }

            Application.Current.Dispatcher.BeginInvoke(
                UpdateNotifyIcon, 
                string.Format(CultureInfo.CurrentCulture, Resources.DownloadProgress, e.FilesTransferred, e.FilesTotal));
        }

        /// <summary>Runs when there is an error occurs</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>ErrorOccurredEventArgs</c> instance containing the event data.</param>
        static void ErrorOccurred(object sender, ErrorOccurredEventArgs e)
        {
            if (e.ErrorType == ErrorType.FatalNetworkError)
            {
                ShutdownApp();
            }

            if (!isClientConnected)
            {
                ShutdownApp();
                return;
            }

            client.OnErrorOccurred(sender, e);

            if (e.ErrorType == ErrorType.FatalError || e.ErrorType == ErrorType.DownloadError)
            {
                IsInstalling = false;
            }
        }

        /// <summary>Reports the installation has completed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>InstallCompletedEventArgs</c> instance containing the event data.</param>
        static void InstallCompleted(object sender, InstallCompletedEventArgs e)
        {
            IsInstalling = false;
            File.Delete(Path.Combine(AllUserStore, "updates.sui"));
            if (isClientConnected)
            {
                client.OnInstallCompleted(sender, e);
            }
            else
            {
                ShutdownApp();
            }
        }

        /// <summary>Reports when the installation progress has changed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>InstallProgressChangedEventArgs</c> instance containing the event data.</param>
        static void InstallProgressChanged(object sender, InstallProgressChangedEventArgs e)
        {
            if (isClientConnected)
            {
                client.OnInstallProgressChanged(sender, e);
            }

            Application.Current.Dispatcher.BeginInvoke(
                UpdateNotifyIcon, 
                string.Format(CultureInfo.CurrentCulture, Resources.InstallProgress, e.CurrentProgress));
        }

        /// <summary>The main execution method.</summary>
        /// <param name="args">The command line arguments.</param>
        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            using (new Mutex(true, "SevenUpdate.Admin", out createdNew))
            {
                if (createdNew)
                {
                    StartWcfHost();
                    SystemEvents.SessionEnding += PreventClose;
                    Search.SearchCompleted += SearchCompleted;
                    Search.ErrorOccurred += ErrorOccurred;
                    Download.DownloadCompleted += DownloadCompleted;
                    Download.DownloadProgressChanged += DownloadProgressChanged;
                    Utilities.ErrorOccurred += ErrorOccurred;
                    Install.InstallCompleted += InstallCompleted;
                    Install.InstallProgressChanged += InstallProgressChanged;
                    Install.UpdateInstalled += AddHistory;
                    if (!Directory.Exists(AllUserStore))
                    {
                        Directory.CreateDirectory(AllUserStore);
                    }
                }
            }

            var app = new Application();

            using (notifyIcon = new NotifyIcon())
            {
                notifyIcon.Icon = Resources.TrayIcon;
                notifyIcon.Text = Resources.CheckingForUpdates;
                notifyIcon.Visible = false;

                ProcessArgs(args);

                using (var timer = new Timer(3000))
                {
                    timer.Elapsed += CheckIfRunning;
                    timer.Start();
                    app.Run();
                }

                if (client != null)
                {
                    client.ElevatedProcessStopped();
                    client.Close();
                }

                try
                {
                    File.Delete(Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "abort.lock"));
                }
                catch (IOException e)
                {
                    ErrorOccurred(
                        null, new ErrorOccurredEventArgs(Utilities.GetExceptionAsString(e), ErrorType.FatalError));
                }

                notifyIcon.Icon = null;
            }

            SystemEvents.SessionEnding -= PreventClose;
        }

        /// <summary>Prevents the system from shutting down until the installation is safely stopped.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <see cref="Microsoft.Win32.SessionEndingEventArgs" /> instance containing the event data.</param>
        static void PreventClose(object sender, SessionEndingEventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            using (
                FileStream fs =
                    File.Create(Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "abort.lock")))
            {
                fs.WriteByte(0);
            }

            e.Cancel = true;
        }

        /// <summary>Processes the command line arguments.</summary>
        /// <param name="args">The arguments to process.</param>
        static void ProcessArgs(IList<string> args)
        {
            if (args.Count <= 0)
            {
            }
            else
            {
                if (string.Compare(args[0], "Abort", true) == 0)
                {
                    try
                    {
                        using (
                            FileStream fs =
                                File.Create(
                                    Path.Combine(
                                        Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "abort.lock")))
                        {
                            fs.WriteByte(0);
                        }
                    }
                    catch (Exception e)
                    {
                        if (
                            !(e is OperationCanceledException || e is UnauthorizedAccessException
                              || e is InvalidOperationException || e is NotSupportedException))
                        {
                            ErrorOccurred(
                                null, 
                                new ErrorOccurredEventArgs(Utilities.GetExceptionAsString(e), ErrorType.FatalError));
                            throw;
                        }

                        Utilities.ReportError(e, ErrorType.GeneralError);
                    }

                    ShutdownApp();
                }

                if (string.Compare(args[0], "Auto", true) == 0)
                {
                    if (
                        File.Exists(
                            Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "abort.lock")))
                    {
                        try
                        {
                            File.Delete(
                                Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", "abort.lock"));
                        }
                        catch (Exception e)
                        {
                            if (
                                !(e is OperationCanceledException || e is UnauthorizedAccessException
                                  || e is InvalidOperationException || e is NotSupportedException))
                            {
                                ErrorOccurred(
                                    null, 
                                    new ErrorOccurredEventArgs(Utilities.GetExceptionAsString(e), ErrorType.FatalError));
                                throw;
                            }

                            Utilities.ReportError(e, ErrorType.GeneralError);
                        }
                    }

                    isAutoInstall = true;
                    IsInstalling = true;
                    notifyIcon.BalloonTipClicked += RunSevenUpdate;
                    notifyIcon.Click += RunSevenUpdate;
                    notifyIcon.Visible = true;
                    Search.ErrorOccurred += ErrorOccurred;

                    var apps = new Collection<Sua>();
                    if (File.Exists(ApplicationsFile))
                    {
                        apps = Utilities.Deserialize<Collection<Sua>>(ApplicationsFile);
                    }

                    var publisher = new ObservableCollection<LocaleString>();
                    var ls = new LocaleString { Value = "Seven Software", Lang = "en" };
                    publisher.Add(ls);

                    var name = new ObservableCollection<LocaleString>();
                    ls = new LocaleString { Value = "Seven Update", Lang = "en" };
                    name.Add(ls);

                    var app = new Sua(name, publisher)
                        {
                            AppUrl = @"http://sevenupdate.com/", 
                            Directory = @"HKLM\Software\Microsoft\Windows\CurrentVersion\App Paths\SevenUpdate.exe", 
                            ValueName = "Path", 
                            HelpUrl = @"http://sevenupdate.com/support/", 
                            Platform = Platform.AnyCpu, 
                            IsEnabled = true, 
                            SuiUrl = @"http://apps.sevenupdate.com/SevenUpdate"
                        };

                    string channel = null;
                    try
                    {
                        channel =
                            Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Seven Update", "channel", null).ToString();
                    }
                    catch (NullReferenceException)
                    {
                    }
                    catch (AccessViolationException)
                    {
                    }

                    switch (channel)
                    {
                        case "dev":
                            app.SuiUrl += @"-dev.sui";
                            break;
                        case "beta":
                            app.SuiUrl += @"-beta.sui";
                            break;
                        default:
                            app.SuiUrl += @".sui";
                            break;
                    }

                    apps.Insert(0, app);

                    Search.SearchForUpdates(apps, Path.Combine(AllUserStore, "downloads"));
                }
                else
                {
                    ShutdownApp();
                }
            }
        }

        /// <summary>Starts Seven Update UI.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <see cref="System.EventArgs" /> instance containing the event data.</param>
        static void RunSevenUpdate(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                if (notifyIcon.Text == Resources.UpdatesFoundViewThem
                    || notifyIcon.Text == Resources.UpdatesDownloadedViewThem
                    || notifyIcon.Text == Resources.CheckingForUpdates)
                {
                    Utilities.StartProcess(Path.Combine(Utilities.AppDir, "SevenUpdate.exe"), @"Auto");
                }
                else
                {
                    Utilities.StartProcess(Path.Combine(Utilities.AppDir, "SevenUpdate.exe"), @"Reconnect");
                }
            }
            else
            {
                Utilities.StartProcess(@"schtasks.exe", "/Run /TN \"SevenUpdate\"");
            }

            if (notifyIcon.Text == Resources.UpdatesFoundViewThem
                || notifyIcon.Text == Resources.UpdatesDownloadedViewThem
                || notifyIcon.Text == Resources.CheckingForUpdates)
            {
                ShutdownApp();
            }
        }

        /// <summary>Runs when the search for updates has completed for an auto update.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>SearchCompletedEventArgs</c> instance containing the event data.</param>
        static void SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            IsInstalling = false;
            Applications = e.Applications as Collection<Sui>;
            if (Applications == null)
            {
                return;
            }

            if (Applications.Count > 0)
            {
                if (Applications[0].AppInfo.SuiUrl == @"http://apps.sevenupdate.com/SevenUpdate.sui"
                    || Applications[0].AppInfo.SuiUrl == @"http://apps.sevenupdate.com/SevenUpdate-dev.sui")
                {
                    Sui sevenUpdate = Applications[0];
                    Applications.Clear();
                    Applications.Add(sevenUpdate);
                    e.OptionalCount = 0;
                    e.ImportantCount = 1;
                }

                Utilities.Serialize(Applications, Path.Combine(AllUserStore, "updates.sui"));

                Utilities.StartProcess(
                    @"cacls.exe", "\"" + Path.Combine(AllUserStore, "updates.sui") + "\" /c /e /g Users:F");

                if (Settings.AutoOption == AutoUpdateOption.Notify)
                {
                    Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.SearchComplete);
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(UpdateNotifyIcon, NotifyType.DownloadStarted);
                    Download.DownloadUpdates(Applications, "SevenUpdate", Path.Combine(AllUserStore, "downloads"));

                    // Task.Factory.StartNew(() => Download.DownloadUpdates(Applications, "SevenUpdate",
                    // Path.Combine(AllUserStore, "downloads")));
                    IsInstalling = true;
                }
            }
            else
            {
                ShutdownApp();
            }
        }

        /// <summary>Shuts down the process and removes the icon of the notification bar.</summary>
        static void ShutdownApp()
        {
            if (client != null)
            {
                if (client.State != CommunicationState.Closed)
                {
                    try
                    {
                        client.Close();
                    }
                    catch (CommunicationObjectAbortedException)
                    {
                    }
                    catch (CommunicationObjectFaultedException)
                    {
                    }
                }
            }

            if (notifyIcon != null)
            {
                notifyIcon.Icon = null;
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null;
            }

            try
            {
                File.Delete(Path.Combine(Environment.ExpandEnvironmentVariables("%WINDIR%"), "Temp", @"abort.lock"));
            }
            catch (IOException)
            {
            }

            Environment.Exit(0);
        }

        /// <summary>Starts the WCF service.</summary>
        static void StartWcfHost()
        {
            var binding = new NetNamedPipeBinding
                {
                   Name = "sevenupdatebinding", Security = {
                                                                Mode = NetNamedPipeSecurityMode.Transport 
                                                            } 
                };
            var address = new EndpointAddress(@"net.pipe://localhost/sevenupdate/");

            try
            {
                client = new ElevatedProcessCallback(new InstanceContext(new WcfServiceCallback()), binding, address);
                client.ElevatedProcessStarted();
                isClientConnected = true;
            }
            catch (EndpointNotFoundException)
            {
                client = null;
                isClientConnected = false;
            }
            catch (FaultException)
            {
                client = null;
                isClientConnected = false;
            }
        }

        /// <summary>Updates the notify icon text.</summary>
        /// <param name="text">The string to set the <c>notifyIcon</c> text.</param>
        static void UpdateNotifyIcon(string text)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Text = text;
            }
        }

        /// <summary>Updates the <c>notifyIcon</c> state.</summary>
        /// <param name="filter">The <c>NotifyType</c> to set the <c>notifyIcon</c> to.</param>
        static void UpdateNotifyIcon(NotifyType filter)
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
                    notifyIcon.ShowBalloonTip(
                        5000, Resources.UpdatesDownloaded, Resources.UpdatesDownloadedViewThem, ToolTipIcon.Info);
                    break;
                case NotifyType.InstallStarted:
                    notifyIcon.Text = Resources.InstallingUpdates;
                    break;
                case NotifyType.SearchComplete:
                    waiting = true;
                    notifyIcon.Text = Resources.UpdatesFoundViewThem;
                    notifyIcon.ShowBalloonTip(
                        5000, Resources.UpdatesFound, Resources.UpdatesFoundViewThem, ToolTipIcon.Info);
                    break;
                case NotifyType.InstallCompleted:
                    notifyIcon.Text = Resources.InstallationCompleted;
                    notifyIcon.ShowBalloonTip(
                        5000, Resources.UpdatesInstalled, Resources.InstallationCompleted, ToolTipIcon.Info);
                    ShutdownApp();
                    break;
            }
        }
    }
}