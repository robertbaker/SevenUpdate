// <copyright file="Main.xaml.cs" project="SevenUpdate">Robert Baker</copyright>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3" />

namespace SevenUpdate.Pages
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using System.Timers;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Navigation;
    using System.Windows.Shell;

    using SevenSoftware.Windows;

    using SevenUpdate.Properties;
    using SevenUpdate.Windows;

    /// <summary>Interaction logic for Main.xaml.</summary>
    public sealed partial class Main
    {
        /// <summary>Indicates if the page was already initialized.</summary>
        static bool init;

        /// <summary>Indicates if Seven Update will only install updates.</summary>
        static bool isInstallOnly;

        /// <summary>A timer to check if seven update is trying to connect.</summary>
        Timer timer;

        /// <summary>Initializes a new instance of the <see cref="Main" /> class.</summary>
        public Main()
        {
            this.InitializeComponent();
            this.MouseLeftButtonDown -= Core.EnableDragOnGlass;
            this.MouseLeftButtonDown += Core.EnableDragOnGlass;
            AeroGlass.CompositionChanged -= this.UpdateUI;
            AeroGlass.CompositionChanged += this.UpdateUI;

            if (AeroGlass.IsGlassEnabled)
            {
                this.tbAbout.Foreground = Brushes.Black;
                Grid.SetRow(this.rectSide, 1);
                Grid.SetRowSpan(this.rectSide, 5);
                this.spBackButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.tbAbout.Foreground = Brushes.White;
                Grid.SetRow(this.rectSide, 0);
                Grid.SetRowSpan(this.rectSide, 7);
                this.spBackButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Updates the UI when the downloading of updates completes.</summary>
        /// <param name="e">The <c>SevenUpdate.DownloadCompletedEventArgs</c> instance containing the event data.</param>
        static void DownloadCompleted(DownloadCompletedEventArgs e)
        {
            Core.Instance.UpdateAction = e.ErrorOccurred ? UpdateAction.ErrorOccurred : UpdateAction.Installing;
        }

        /// <summary>Downloads updates.</summary>
        static void DownloadInstallUpdates()
        {
            for (int x = 0; x < Core.Applications.Count; x++)
            {
                for (int y = 0; y < Core.Applications[x].Updates.Count; y++)
                {
                    if (Core.Applications[x].Updates[y].Selected)
                    {
                        continue;
                    }

                    Core.Applications[x].Updates.RemoveAt(y);
                    y--;
                }

                if (Core.Applications[x].Updates.Count != 0)
                {
                    continue;
                }

                Core.Applications.RemoveAt(x);
                x--;
            }

            if (Core.Applications.Count > 0)
            {
                var sla = new LicenseAgreement();
                if (sla.LoadLicenses() == false)
                {
                    Core.Instance.UpdateAction = UpdateAction.Canceled;
                    return;
                }

                if (WcfService.Install())
                {
                    try
                    {
                        File.Delete(Path.Combine(App.AllUserStore, @"updates.sui"));
                    }
                    catch (Exception e)
                    {
                        if (!(e is UnauthorizedAccessException || e is IOException))
                        {
                            Utilities.ReportError(e, ErrorType.GeneralError);
                        }
                    }

                    Core.Instance.UpdateAction = isInstallOnly ? UpdateAction.Installing : UpdateAction.Downloading;
                    Settings.Default.LastInstall = DateTime.Now;
                }
                else
                {
                    Core.Instance.UpdateAction = UpdateAction.Canceled;
                }
            }
            else
            {
                Core.Instance.UpdateAction = UpdateAction.Canceled;
            }
        }

        /// <summary>Checks for updates after settings were changed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.EventArgs</c> instance containing the event data.</param>
        static void SettingsChanged(object sender, EventArgs e)
        {
            Core.CheckForUpdates(true);
        }

        /// <summary>Checks for updates.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void CheckForUpdates(object sender, MouseButtonEventArgs e)
        {
            Core.CheckForUpdates();
        }

        /// <summary>Check if Seven Update is still trying to connect to <c>WcfService</c>.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Timers.ElapsedEventArgs</c> instance containing the event data.</param>
        void CheckIfConnecting(object sender, ElapsedEventArgs e)
        {
            this.timer.Enabled = false;
            this.timer.Stop();
            if (Core.Instance.UpdateAction != UpdateAction.ConnectingToService)
            {
                return;
            }

            WcfService.AdminError(new Exception(Properties.Resources.CouldNotConnectService));
        }

        /// <summary>Updates the UI when the downloading of updates has completed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>SevenUpdate.DownloadCompletedEventArgs</c> instance containing the event data.</param>
        void DownloadCompleted(object sender, DownloadCompletedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(DownloadCompleted, e);
            }
            else
            {
                DownloadCompleted(e);
            }
        }

        /// <summary>Updates the UI when the download progress has changed.</summary>
        /// <param name="e">The DownloadProgress data.</param>
        void DownloadProgressChanged(DownloadProgressChangedEventArgs e)
        {
            if (Core.IsReconnect)
            {
                Core.Instance.UpdateAction = UpdateAction.Downloading;
                Core.IsReconnect = false;
            }

            if (e.BytesTotal > 0 && e.BytesTransferred > 0)
            {
                if (e.BytesTotal == e.BytesTransferred)
                {
                    return;
                }

                ulong progress = e.BytesTransferred * 100 / e.BytesTotal;
                App.TaskBar.ProgressState = TaskbarItemProgressState.Normal;
                App.TaskBar.ProgressValue = Convert.ToDouble(progress) / 100;
                this.tbStatus.Text = string.Format(
                    CultureInfo.CurrentCulture, 
                    Properties.Resources.DownloadPercentProgress, 
                    Utilities.ConvertFileSize(e.BytesTotal), 
                    progress.ToString("F0", CultureInfo.CurrentCulture));
            }
            else
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                this.tbStatus.Text = string.Format(
                    CultureInfo.CurrentCulture, Properties.Resources.DownloadProgress, e.FilesTransferred, e.FilesTotal);
            }
        }

        /// <summary>Updates the UI when the download progress has changed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>SevenUpdate.DownloadProgressChangedEventArgs</c> instance containing the event data.</param>
        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(this.DownloadProgressChanged, e);
            }
            else
            {
                this.DownloadProgressChanged(e);
            }
        }

        /// <summary>Sets the UI when an error occurs.</summary>
        /// <param name="e">The <c>SevenUpdate.ErrorOccurredEventArgs</c> instance containing the event data.</param>
        void ErrorOccurred(ErrorOccurredEventArgs e)
        {
            Core.Instance.UpdateAction = UpdateAction.ErrorOccurred;
            switch (e.ErrorType)
            {
                case ErrorType.FatalNetworkError:
                    this.tbStatus.Text = Properties.Resources.CheckConnection;
                    break;
                case ErrorType.InstallationError:
                case ErrorType.SearchError:
                case ErrorType.DownloadError:
                case ErrorType.GeneralError:
                case ErrorType.FatalError:
                    this.tbStatus.Text = e.Exception;
                    break;
            }
        }

        /// <summary>Sets the UI when an error has occurred.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>SevenUpdate.ErrorOccurredEventArgs</c> instance containing the event data.</param>
        void ErrorOccurred(object sender, ErrorOccurredEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(this.ErrorOccurred, e);
            }
            else
            {
                this.ErrorOccurred(e);
            }
        }

        /// <summary>Loads settings and UI for the page.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.RoutedEventArgs</c> instance containing the event data.</param>
        void Init(object sender, RoutedEventArgs e)
        {
            if (Utilities.RebootNeeded)
            {
                Core.Instance.UpdateAction = UpdateAction.RebootNeeded;
            }

            if (init)
            {
                return;
            }

            init = true;

            this.DataContext = Core.Instance;

            // Subscribe to events
            RestoreUpdates.RestoredHiddenUpdate += SettingsChanged;
            WcfService.SettingsChanged += SettingsChanged;
            Search.ErrorOccurred += this.ErrorOccurred;
            Search.SearchCompleted += this.SearchCompleted;
            UpdateInfo.UpdateSelectionChanged += this.UpdateSelectionChanged;
            Core.UpdateActionChanged += this.SetUI;
            WcfService.DownloadProgressChanged += this.DownloadProgressChanged;
            WcfService.DownloadDone += this.DownloadCompleted;
            WcfService.InstallProgressChanged += this.InstallProgressChanged;
            WcfService.InstallDone += this.InstallCompleted;
            WcfService.ErrorOccurred += this.ErrorOccurred;
            WcfService.ServiceError += this.ErrorOccurred;

            if (App.IsDev)
            {
                this.tbDevNote.Visibility = Visibility.Visible;
                this.Title += " - " + Properties.Resources.DevChannel;
            }

            if (App.IsBeta)
            {
                this.Title += " - " + Properties.Resources.BetaChannel;
            }

            Core.Instance.UpdateAction = UpdateAction.NoUpdates;
            if (Core.IsReconnect)
            {
                Core.Instance.UpdateAction = UpdateAction.ConnectingToService;
                this.timer = new Timer { Enabled = true, Interval = 30000 };
                this.timer.Elapsed -= this.CheckIfConnecting;
                this.timer.Elapsed += this.CheckIfConnecting;
                WcfService.Connect();
            }
            else if (File.Exists(Path.Combine(App.AllUserStore, "updates.sui")))
            {
                DateTime lastCheck = File.GetLastWriteTime(Path.Combine(App.AllUserStore, "updates.sui"));

                DateTime today = DateTime.Now;

                if (lastCheck.Month == today.Month && lastCheck.Year == today.Year)
                {
                    if (lastCheck.Day == today.Day || lastCheck.Day + 1 == today.Day || lastCheck.Day + 2 == today.Day
                        || lastCheck.Day + 3 == today.Day || lastCheck.Day + 4 == today.Day
                        || lastCheck.Day + 5 == today.Day)
                    {
                        WcfService.Disconnect();
                        if (File.Exists(Path.Combine(App.AllUserStore, "updates.sui")))
                        {
                            Task.Factory.StartNew(
                                () =>
                                Search.SetUpdatesFound(
                                    Utilities.Deserialize<Collection<Sui>>(
                                        Path.Combine(App.AllUserStore, "updates.sui"))));
                        }
                    }
                }
                else
                {
                    try
                    {
                        File.Delete(Path.Combine(App.AllUserStore, "updates.sui"));
                    }
                    catch (Exception f)
                    {
                        if (!(f is UnauthorizedAccessException || f is IOException))
                        {
                            Utilities.ReportError(f, ErrorType.GeneralError);
                            throw;
                        }
                    }

                    Core.Instance.UpdateAction = UpdateAction.CheckForUpdates;
                }
            }
            else
            {
                if (Utilities.RebootNeeded)
                {
                    Core.Instance.UpdateAction = UpdateAction.RebootNeeded;
                    return;
                }

                if (Settings.Default.LastUpdateCheck == DateTime.MinValue)
                {
                    Core.Instance.UpdateAction = UpdateAction.CheckForUpdates;
                }

                if (!Settings.Default.LastUpdateCheck.Date.Equals(DateTime.Now.Date))
                {
                    Core.Instance.UpdateAction = UpdateAction.CheckForUpdates;
                }
            }
        }

        /// <summary>Updates the UI when the installation has completed.</summary>
        /// <param name="e">The InstallCompleted data.</param>
        void InstallCompleted(InstallCompletedEventArgs e)
        {
            Settings.Default.LastInstall = DateTime.Now;
            Core.Instance.IsAdmin = false;

            // if a reboot is needed lets say it
            if (Utilities.RebootNeeded)
            {
                Core.Instance.UpdateAction = UpdateAction.RebootNeeded;
                return;
            }

            Core.Instance.UpdateAction = UpdateAction.InstallationCompleted;

            if (e.UpdatesFailed <= 0)
            {
                this.tbStatus.Text = e.UpdatesInstalled == 1
                                         ? Properties.Resources.UpdateInstalled
                                         : string.Format(
                                             CultureInfo.CurrentCulture, 
                                             Properties.Resources.UpdatesInstalled, 
                                             e.UpdatesInstalled);
                return;
            }

            Core.Instance.UpdateAction = UpdateAction.ErrorOccurred;

            if (e.UpdatesInstalled == 0)
            {
                this.tbStatus.Text = e.UpdatesFailed == 1
                                         ? Properties.Resources.UpdateFailed
                                         : string.Format(
                                             CultureInfo.CurrentCulture, 
                                             Properties.Resources.UpdatesFailed, 
                                             e.UpdatesFailed);
            }
            else
            {
                this.tbStatus.Text = string.Format(
                    CultureInfo.CurrentCulture, 
                    Properties.Resources.UpdatesInstalledFailed, 
                    e.UpdatesInstalled, 
                    e.UpdatesFailed);
            }
        }

        /// <summary>Sets the UI when the installation of updates has completed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>SevenUpdate.InstallCompletedEventArgs</c> instance containing the event data.</param>
        void InstallCompleted(object sender, InstallCompletedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(this.InstallCompleted, e);
            }
            else
            {
                this.InstallCompleted(e);
            }
        }

        /// <summary>Updates the UI when the installation progress has changed.</summary>
        /// <param name="e">The InstallProgress data.</param>
        void InstallProgressChanged(InstallProgressChangedEventArgs e)
        {
            if (Core.IsReconnect)
            {
                Core.Instance.UpdateAction = UpdateAction.Installing;
                Core.IsReconnect = false;
            }

            if (e.CurrentProgress == -1)
            {
                this.tbStatus.Text = Properties.Resources.PreparingInstall;
                App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
            }
            else
            {
                App.TaskBar.ProgressState = TaskbarItemProgressState.Normal;
                App.TaskBar.ProgressValue = e.CurrentProgress;
                this.tbStatus.Text = e.TotalUpdates > 1
                                         ? string.Format(
                                             CultureInfo.CurrentCulture, 
                                             Properties.Resources.InstallExtendedProgress, 
                                             e.UpdateName, 
                                             e.UpdatesComplete, 
                                             e.TotalUpdates, 
                                             e.CurrentProgress)
                                         : string.Format(
                                             CultureInfo.CurrentCulture, 
                                             Properties.Resources.InstallProgress, 
                                             e.UpdateName, 
                                             e.CurrentProgress);
            }
        }

        /// <summary>Sets the UI when the install progress has changed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>SevenUpdate.InstallProgressChangedEventArgs</c> instance containing the event data.</param>
        void InstallProgressChanged(object sender, InstallProgressChangedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(this.InstallProgressChanged, e);
            }
            else
            {
                this.InstallProgressChanged(e);
            }
        }

        /// <summary>Opens a browser and navigates to the Uri.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Navigation.RequestNavigateEventArgs</c> instance containing the event data.</param>
        void NavigateToGoogleCode(object sender, RequestNavigateEventArgs e)
        {
            Utilities.StartProcess(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        /// <summary>Navigates to the Options page.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void NavigateToOptions(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(new Uri(@"/SevenUpdate;component/Pages/Options.xaml", UriKind.Relative));
        }

        /// <summary>Navigates to the Restore Updates page.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void NavigateToRestoreUpdates(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(
                new Uri(@"/SevenUpdate;component/Pages/RestoreUpdates.xaml", UriKind.Relative));
        }

        /// <summary>Navigates to the Update History page.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void NavigateToUpdateHistory(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(
                new Uri(@"/SevenUpdate;component/Pages/UpdateHistory.xaml", UriKind.Relative));
        }

        /// <summary>Opens a browser and navigates to the Uri.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Navigation.RequestNavigateEventArgs</c> instance containing the event data.</param>
        void OpenErrorLog(object sender, RequestNavigateEventArgs e)
        {
            string errorLog = Path.Combine(App.UserStore, "error.log");

            if (File.Exists(errorLog))
            {
                Utilities.StartProcess(errorLog, null, false, false);
            }

            e.Handled = true;
        }

        /// <summary>Performs an action based on the <c>UpdateAction</c>.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.RoutedEventArgs</c> instance containing the event data.</param>
        void PerformAction(object sender, RoutedEventArgs e)
        {
            switch (Core.Instance.UpdateAction)
            {
                case UpdateAction.DownloadCompleted:
                case UpdateAction.UpdatesFound:
                    DownloadInstallUpdates();
                    break;
                case UpdateAction.Downloading:
                case UpdateAction.Installing:
                    if (WcfService.AbortInstall())
                    {
                        Core.Instance.UpdateAction = UpdateAction.Canceled;
                    }

                    break;

                case UpdateAction.CheckForUpdates:
                case UpdateAction.Canceled:
                case UpdateAction.ErrorOccurred:
                    Core.Instance.UpdateAction = UpdateAction.CheckingForUpdates;
                    Core.CheckForUpdates();
                    break;
                case UpdateAction.RebootNeeded:
                    Utilities.StartProcess(@"shutdown.exe", "-r -t 00");
                    break;
            }
        }

        /// <summary>Updates the UI the search for updates has completed.</summary>
        /// <param name="e">The SearchComplete data.</param>
        void SearchCompleted(SearchCompletedEventArgs e)
        {
            if (Core.Instance.UpdateAction == UpdateAction.ErrorOccurred)
            {
                return;
            }

            Core.Applications = e.Applications as Collection<Sui>;
            if (Core.Applications == null)
            {
                Core.Instance.UpdateAction = UpdateAction.NoUpdates;
                return;
            }

            if (Core.Applications.Count > 0)
            {
                if (Core.Settings.IncludeRecommended)
                {
                    e.ImportantCount += e.RecommendedCount;
                }
                else
                {
                    e.OptionalCount += e.RecommendedCount;
                }

                string suiUrl = null;
                if (App.IsDev)
                {
                    suiUrl = Core.SevenUpdateUrl + @"-dev.sui";
                }

                if (App.IsBeta)
                {
                    suiUrl = Core.SevenUpdateUrl + @"-beta.sui";
                }

                if (!App.IsDev && !App.IsBeta)
                {
                    suiUrl = Core.SevenUpdateUrl + @".sui";
                }

                if (Core.Applications[0].AppInfo.SuiUrl == suiUrl)
                {
                    Sui sevenUpdate = Core.Applications[0];
                    Core.Applications.Clear();
                    Core.Applications.Add(sevenUpdate);
                    e.OptionalCount = 0;
                    e.ImportantCount = 1;
                }

                try
                {
                    Utilities.Serialize(Core.Applications, Path.Combine(App.AllUserStore, "updates.sui"));
                    Utilities.StartProcess(
                        @"cacls.exe", "\"" + Path.Combine(App.AllUserStore, "updates.sui") + "\" /c /e /g Users:F");
                    Utilities.StartProcess(
                        @"cacls.exe", 
                        "\"" + Path.Combine(App.AllUserStore, "updates.sui") + "\" /c /e /r " + Environment.UserName);
                }
                catch (Exception ex)
                {
                    if (!(ex is UnauthorizedAccessException || ex is IOException))
                    {
                        Utilities.ReportError(ex, ErrorType.GeneralError);
                        throw;
                    }
                }

                if (e.ImportantCount > 0 || e.OptionalCount > 0)
                {
                    string uiString;

                    Core.Instance.UpdateAction = UpdateAction.UpdatesFound;

                    if (e.ImportantCount > 0 && e.OptionalCount > 0)
                    {
                        this.line.Y1 = 50;
                    }

                    if (e.ImportantCount > 0)
                    {
                        uiString = e.ImportantCount == 1
                                       ? Properties.Resources.ImportantUpdateAvailable
                                       : Properties.Resources.ImportantUpdatesAvailable;
                        this.tbViewImportantUpdates.Text = string.Format(
                            CultureInfo.CurrentCulture, uiString, e.ImportantCount);

                        this.tbViewImportantUpdates.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        this.tbViewImportantUpdates.Visibility = Visibility.Collapsed;
                    }

                    if (e.OptionalCount > 0)
                    {
                        if (e.ImportantCount == 0)
                        {
                            this.tbHeading.Text = Properties.Resources.NoImportantUpdates;
                        }

                        uiString = e.OptionalCount == 1
                                       ? Properties.Resources.OptionalUpdateAvailable
                                       : Properties.Resources.OptionalUpdatesAvailable;

                        this.tbViewOptionalUpdates.Text = string.Format(
                            CultureInfo.CurrentCulture, uiString, e.OptionalCount);

                        this.tbViewOptionalUpdates.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        this.tbViewOptionalUpdates.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                Core.Instance.UpdateAction = UpdateAction.NoUpdates;
            }
        }

        /// <summary>Sets the UI when the search for updates has completed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>SevenUpdate.SearchCompletedEventArgs</c> instance containing the event data.</param>
        void SearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(this.SearchCompleted, e);
            }
            else
            {
                this.SearchCompleted(e);
            }
        }

        /// <summary>Handles the MouseDown event of the ImportantUpdates control.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void SelectImportantUpdates(object sender, MouseButtonEventArgs e)
        {
            UpdateInfo.DisplayOptionalUpdates = false;
            MainWindow.NavService.Navigate(new Uri(@"/SevenUpdate;component/Pages/UpdateInfo.xaml", UriKind.Relative));
        }

        /// <summary>Selects optional updates and navigates to the <c>UpdateInfo</c> page.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void SelectOptionalUpdates(object sender, MouseButtonEventArgs e)
        {
            UpdateInfo.DisplayOptionalUpdates = true;
            MainWindow.NavService.Navigate(new Uri(@"/SevenUpdate;component/Pages/UpdateInfo.xaml", UriKind.Relative));
        }

        /// <summary>Sets the UI based on the <c>UpdateAction</c>.</summary>
        /// <param name="action">The action.</param>
        void SetUI(UpdateAction action)
        {
            this.btnAction.IsShieldNeeded = false;
            this.btnAction.Visibility = Visibility.Collapsed;
            this.tbHeading.Visibility = Visibility.Collapsed;
            this.tbStatus.Visibility = Visibility.Collapsed;
            this.tbSelectedUpdates.Visibility = Visibility.Collapsed;
            this.tbSelectedUpdates.FontWeight = FontWeights.Normal;
            this.tbViewOptionalUpdates.Visibility = Visibility.Collapsed;
            this.tbViewImportantUpdates.Visibility = Visibility.Collapsed;
            this.line.Visibility = Visibility.Collapsed;
            App.TaskBar.ProgressState = TaskbarItemProgressState.None;

            switch (action)
            {
                case UpdateAction.Canceled:
                    this.tbHeading.Text = Properties.Resources.UpdatesCanceled;
                    this.tbStatus.Text = Properties.Resources.CancelInstallation;
                    this.btnAction.ButtonText = Properties.Resources.TryAgain;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;
                    this.btnAction.Visibility = Visibility.Visible;

                    break;

                case UpdateAction.CheckForUpdates:
                    this.tbHeading.Text = Properties.Resources.CheckForUpdatesHeading;
                    this.tbStatus.Text = Properties.Resources.InstallLatestUpdates;
                    this.btnAction.ButtonText = Properties.Resources.CheckForUpdates;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;
                    this.btnAction.Visibility = Visibility.Visible;

                    break;

                case UpdateAction.CheckingForUpdates:
                    this.tbHeading.Text = Properties.Resources.CheckingForUpdates;
                    this.tbHeading.Visibility = Visibility.Visible;
                    this.line.Y1 = 25;

                    App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;

                    break;

                case UpdateAction.ConnectingToService:
                    this.tbHeading.Text = Properties.Resources.ConnectingToService;

                    this.tbHeading.Visibility = Visibility.Visible;
                    break;

                case UpdateAction.DownloadCompleted:
                    this.tbHeading.Text = Properties.Resources.UpdatesReadyInstalled;
                    this.btnAction.ButtonText = Properties.Resources.InstallUpdates;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbSelectedUpdates.Visibility = Visibility.Visible;
                    this.btnAction.Visibility = Visibility.Visible;
                    this.line.Visibility = Visibility.Visible;
                    this.line.Y1 = 25;
                    this.btnAction.IsShieldNeeded = !Core.Instance.IsAdmin;

                    break;

                case UpdateAction.Downloading:
                    this.tbHeading.Text = Properties.Resources.DownloadingUpdates;
                    this.tbStatus.Text = Properties.Resources.PreparingDownload;
                    this.btnAction.ButtonText = Properties.Resources.StopDownload;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;
                    this.btnAction.Visibility = Visibility.Visible;

                    this.btnAction.IsShieldNeeded = !Core.Instance.IsAdmin;

                    App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                    break;

                case UpdateAction.ErrorOccurred:
                    this.tbHeading.Text = Properties.Resources.ErrorOccurred;
                    this.tbStatus.Text = Properties.Resources.UnknownErrorOccurred;
                    this.btnAction.ButtonText = Properties.Resources.TryAgain;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;
                    this.btnAction.Visibility = Visibility.Visible;

                    App.TaskBar.ProgressState = TaskbarItemProgressState.Error;
                    break;

                case UpdateAction.InstallationCompleted:
                    this.tbHeading.Text = Properties.Resources.UpdatesInstalledTitle;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;

                    break;

                case UpdateAction.Installing:
                    this.tbHeading.Text = Properties.Resources.InstallingUpdates;
                    this.tbStatus.Text = Properties.Resources.PreparingInstall;
                    this.btnAction.ButtonText = Properties.Resources.StopInstallation;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;
                    this.btnAction.Visibility = Visibility.Visible;

                    this.btnAction.IsShieldNeeded = !Core.Instance.IsAdmin;
                    App.TaskBar.ProgressState = TaskbarItemProgressState.Indeterminate;
                    break;

                case UpdateAction.NoUpdates:
                    this.tbHeading.Text = Properties.Resources.ProgramsUpToDate;
                    this.tbStatus.Text = Properties.Resources.NoNewUpdates;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;

                    break;

                case UpdateAction.RebootNeeded:
                    this.tbHeading.Text = Properties.Resources.RebootNeeded;
                    this.tbStatus.Text = Properties.Resources.SaveAndReboot;
                    this.btnAction.ButtonText = Properties.Resources.RestartNow;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbStatus.Visibility = Visibility.Visible;
                    this.btnAction.Visibility = Visibility.Visible;
                    break;

                case UpdateAction.UpdatesFound:

                    this.tbHeading.Text = Properties.Resources.DownloadAndInstallUpdates;
                    this.tbSelectedUpdates.Text = Properties.Resources.NoUpdatesSelected;
                    this.btnAction.ButtonText = Properties.Resources.InstallUpdates;

                    this.tbHeading.Visibility = Visibility.Visible;
                    this.tbSelectedUpdates.Visibility = Visibility.Visible;
                    this.line.Visibility = Visibility.Visible;
                    this.btnAction.IsShieldNeeded = !Core.Instance.IsAdmin;
                    break;
            }
        }

        /// <summary>Sets the UI when the update action is changed.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.EventArgs</c> instance containing the event data.</param>
        void SetUI(object sender, EventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.BeginInvoke(this.SetUI, Core.Instance.UpdateAction);
            }
            else
            {
                this.SetUI(Core.Instance.UpdateAction);
            }
        }

        /// <summary>Shows the About Dialog window.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void ShowAboutDialog(object sender, MouseButtonEventArgs e)
        {
            var about = new About();
            about.ShowDialog();
        }

        /// <summary>Updates the UI when the update selection changes.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>UpdateInfo.UpdateSelectionChangedEventArgs</c> instance containing the event data.</param>
        void UpdateSelectionChanged(object sender, UpdateInfo.UpdateSelectionChangedEventArgs e)
        {
            if (e.ImportantUpdates > 0)
            {
                this.tbViewImportantUpdates.Visibility = Visibility.Visible;
                this.tbSelectedUpdates.Text = e.ImportantUpdates == 1
                                                  ? Properties.Resources.ImportantUpdateSelected
                                                  : string.Format(
                                                      CultureInfo.CurrentCulture, 
                                                      Properties.Resources.ImportantUpdatesSelected, 
                                                      e.ImportantUpdates);

                if (e.ImportantDownloadSize > 0)
                {
                    this.tbSelectedUpdates.Text += ", " + Utilities.ConvertFileSize(e.ImportantDownloadSize);
                }
            }

            if (e.OptionalUpdates > 0)
            {
                this.tbViewOptionalUpdates.Visibility = Visibility.Visible;
                if (e.ImportantUpdates == 0)
                {
                    this.tbSelectedUpdates.Text = e.OptionalUpdates == 1
                                                      ? Properties.Resources.OptionalUpdateSelected
                                                      : string.Format(
                                                          CultureInfo.CurrentCulture, 
                                                          Properties.Resources.OptionalUpdatesSelected, 
                                                          e.OptionalUpdates);
                }
                else
                {
                    if (e.OptionalUpdates == 1)
                    {
                        this.tbSelectedUpdates.Text += Environment.NewLine + Properties.Resources.OptionalUpdateSelected;
                    }
                    else
                    {
                        this.tbSelectedUpdates.Text += Environment.NewLine
                                                       +
                                                       string.Format(
                                                           CultureInfo.CurrentCulture, 
                                                           Properties.Resources.OptionalUpdatesSelected, 
                                                           e.OptionalUpdates);
                    }
                }

                if (e.OptionalDownloadSize > 0)
                {
                    this.tbSelectedUpdates.Text += ", " + Utilities.ConvertFileSize(e.OptionalDownloadSize);
                }
            }

            if ((e.ImportantDownloadSize == 0 && e.OptionalDownloadSize == 0)
                && (e.ImportantUpdates > 0 || e.OptionalUpdates > 0))
            {
                isInstallOnly = true;
                this.tbHeading.Text = Properties.Resources.InstallUpdatesForPrograms;
            }
            else
            {
                this.tbHeading.Text = Properties.Resources.DownloadAndInstallUpdates;
                isInstallOnly = false;
            }

            if (e.ImportantUpdates > 0 || e.OptionalUpdates > 0)
            {
                this.tbSelectedUpdates.FontWeight = FontWeights.Bold;
                this.btnAction.Visibility = Visibility.Visible;
            }
            else
            {
                this.tbSelectedUpdates.Text = Properties.Resources.NoUpdatesSelected;
                this.tbSelectedUpdates.FontWeight = FontWeights.Normal;
                this.btnAction.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Changes the UI depending on whether Aero Glass is enabled.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>CompositionChangedEventArgs</c> instance containing the event data.</param>
        void UpdateUI(object sender, CompositionChangedEventArgs e)
        {
            if (AeroGlass.IsGlassEnabled)
            {
                this.tbAbout.Foreground = Brushes.Black;
                Grid.SetRow(this.rectSide, 1);
                Grid.SetRowSpan(this.rectSide, 5);
                this.spBackButton.Visibility = Visibility.Visible;
            }
            else
            {
                this.tbAbout.Foreground = Brushes.White;
                Grid.SetRow(this.rectSide, 0);
                Grid.SetRowSpan(this.rectSide, 7);
                this.spBackButton.Visibility = Visibility.Collapsed;
            }
        }
    }
}