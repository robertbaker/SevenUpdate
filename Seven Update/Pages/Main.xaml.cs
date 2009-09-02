﻿#region GNU Public License v3

// Copyright 2007, 2008 Robert Baker, aka Seven ALive.
// This file is part of Seven Update.
// 
//     Seven Update is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     Seven Update is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
// 
//    You should have received a copy of the GNU General Public License
//     along with Seven Update.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SevenUpdate.Properties;
using SevenUpdate.WCF;
using SevenUpdate.Windows;

#endregion

namespace SevenUpdate.Pages
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Page
    {
        #region Global Vars

        /// <summary>
        ///  The green side image
        /// </summary>
        private readonly BitmapImage greenSide = new BitmapImage(new Uri("/Images/GreenSide.png", UriKind.Relative));

        /// <summary>
        /// The red side image
        /// </summary>
        private readonly BitmapImage redSide = new BitmapImage(new Uri("/Images/RedSide.png", UriKind.Relative));

        /// <summary>
        /// The Seven Update 48x48 icon image
        /// </summary>
        private readonly BitmapImage suIcon = new BitmapImage(new Uri("/Images/Icon.png", UriKind.Relative));

        /// <summary>
        /// The yellow side image
        /// </summary>
        private readonly BitmapImage yellowSide = new BitmapImage(new Uri("/Images/YellowSide.png", UriKind.Relative));

        #endregion

        #region Enums

        /// <summary>
        /// The layout for the Info Panel
        /// </summary>
        private enum UILayout
        {
            /// <summary>
            /// Canceled Updates
            /// </summary>
            Canceled,

            /// <summary>
            /// Checking for updates
            /// </summary>
            CheckingForUpdates,

            /// <summary>
            /// When connecting to the admin service
            /// </summary>
            ConnectingToService,

            /// <summary>
            /// When downloading of updates has been completed
            /// </summary>
            DownloadCompleted,

            /// <summary>
            /// Downloading updates
            /// </summary>
            Downloading,

            /// <summary>
            /// An Error Occurred when downloading/installing updates
            /// </summary>
            ErrorOccurred,

            /// <summary>
            /// When installation of updates have completed
            /// </summary>
            InstallationCompleted,

            /// <summary>
            /// Installing Updates
            /// </summary>
            Installing,

            /// <summary>
            /// No updates have been found
            /// </summary>
            NoUpdates,

            /// <summary>
            /// A reboot is needed to finish installing updates
            /// </summary>
            RebootNeeded,

            /// <summary>
            /// No updates have been found
            /// </summary>
            UpdatesFound,
        }

        #endregion

        public Main()
        {
            InitializeComponent();
            LoadSettings();
            infoBar.imgAdminShield.Visibility = App.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

            if (App.IsInstallInProgress) SetUI(UILayout.ConnectingToService);
            else
            {
                if (App.IsAutoCheck) CheckForUpdates(true);
                else if (!Settings.Default.lastUpdateCheck.Contains(DateTime.Now.ToShortDateString())) CheckForUpdates();
            }
        }

        #region UI Events

        #region TextBlock

        /// <summary>
        /// Underlines the text when mouse is over the TextBlock
        /// </summary>
        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            var textBlock = ((TextBlock) sender);
            textBlock.TextDecorations = TextDecorations.Underline;
        }

        /// <summary>
        /// Removes the Underlined text when mouse is leaves the TextBlock
        /// </summary>
        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            var textBlock = ((TextBlock) sender);
            textBlock.TextDecorations = null;
        }

        private void tbChangeSettings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(new Uri(@"Pages\Options.xaml", UriKind.Relative));
        }

        private void tbCheckForUpdates_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CheckForUpdates();
            // SevenUpdate.Windows.MainWindow.ns.Navigate(new Uri(@"Pages\Update Info.xaml", UriKind.Relative));
        }

        private void tbViewUpdateHistory_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(new Uri(@"Pages\Update History.xaml", UriKind.Relative));
        }

        private void tbRestoreHiddenUpdates_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(new Uri(@"Pages\Restore Updates.xaml", UriKind.Relative));
        }

        private void tbAbout_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var about = new About();
            about.ShowDialog();

            var la = new LicenseAgreement();
            la.LoadLicenses();

            var ud = new UpdateDetails();
            ud.ShowDialog();
        }


        private static void TbViewOptionalUpdatesMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(new Uri(@"Pages\Update Info.xaml", UriKind.Relative));
        }


        private static void TbViewImportantUpdatesMouseDown(object sender, MouseButtonEventArgs e)
        {
            MainWindow.NavService.Navigate(new Uri(@"Pages\Update Info.xaml", UriKind.Relative));
        }

        #endregion

        /// <summary>
        /// Updates the UI after the user selects updates to install
        /// </summary>
        private void UpdateInfo_UpdateSelectionChangedEventHandler(object sender, UpdateInfo.UpdateSelectionChangedEventArgs e)
        {
            #region GUI Updating

            if (e.ImportantUpdates > 0)
            {
                if (e.ImportantUpdates == 1) infoBar.tbSelectedUpdates.Text = e.ImportantUpdates + " " + App.RM.GetString("ImportantUpdateSelected");
                else infoBar.tbSelectedUpdates.Text = e.ImportantUpdates + " " + App.RM.GetString("ImportantUpdatesSelected");

                if (e.ImportantDownloadSize > 0) infoBar.tbSelectedUpdates.Text += ", " + Shared.ConvertFileSize(e.ImportantDownloadSize);
            }
            if (e.OptionalUpdates > 0)
            {
                if (e.ImportantUpdates == 0)
                {
                    if (e.OptionalUpdates == 1) infoBar.tbSelectedUpdates.Text = e.OptionalUpdates + " " + App.RM.GetString("OptionalUpdateSelected");
                    else infoBar.tbSelectedUpdates.Text = e.OptionalUpdates + " " + App.RM.GetString("OptionalUpdatesSelected");
                }
                else if (e.OptionalUpdates == 1) infoBar.tbSelectedUpdates.Text += Environment.NewLine + e.OptionalUpdates + " " + App.RM.GetString("OptionalUpdateSelected");
                else infoBar.tbSelectedUpdates.Text += Environment.NewLine + e.OptionalUpdates + " " + App.RM.GetString("OptionalUpdatesSelected");

                if (e.OptionalDownloadSize > 0) infoBar.tbSelectedUpdates.Text += ", " + Shared.ConvertFileSize(e.OptionalDownloadSize);
            }
            else infoBar.tbViewOptionalUpdates.Visibility = Visibility.Collapsed;

            if (e.ImportantDownloadSize == 0 && e.OptionalDownloadSize == 0) infoBar.tbHeading.Text = App.RM.GetString("InstallUpdatesForPrograms");
            else infoBar.tbHeading.Text = App.RM.GetString("DownloadAndInstallUpdates");

            if (e.ImportantUpdates > 0 || e.OptionalUpdates > 0)
            {
                infoBar.btnAction.Visibility = Visibility.Visible;
                infoBar.tbSelectedUpdates.FontWeight = FontWeights.Bold;
            }
            else
            {
                infoBar.tbSelectedUpdates.Text = App.RM.GetString("NoUpdatesSelected");
                infoBar.tbSelectedUpdates.FontWeight = FontWeights.Normal;
                infoBar.btnAction.Visibility = Visibility.Collapsed;
            }

            #endregion
        }

        /// <summary>
        /// Checks for updates after hidden updates have been restored
        /// </summary>
        private void RestoreUpdates_RestoredHiddenUpdateEventHandler(object sender, EventArgs e)
        {
            CheckForUpdates(true);
        }

        /// <summary>
        /// Executes action based on current label. Installed, cancels, and/or searches for updates. it also can reboot the computer.
        /// </summary>
        private void BtnActionClick(object sender, RoutedEventArgs e)
        {
            if (infoBar.tbAction.Text == App.RM.GetString("InstallUpdates")) DownloadInstallUpdates();
            else if (infoBar.tbAction.Text == App.RM.GetString("StopDownload") || infoBar.tbAction.Text == App.RM.GetString("StopInstallation"))
            {
                //Cancel installation of updates
                Admin.AbortInstall();
                SetUI(UILayout.Canceled);
                return;
            }
            else if (infoBar.tbAction.Text == App.RM.GetString("TryAgain")) CheckForUpdates();
            else if (infoBar.tbAction.Text == App.RM.GetString("RestartNow")) Process.Start(Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\shutdown.exe", "-r -t 00");
        }

        #endregion

        #region Update Event Methods

        private void InstallProgressChanged(AdminCallBack.InstallProgressChangedEventArgs e)
        {
            if (e.CurrentProgress == -1) infoBar.tbStatus.Text = App.RM.GetString("PreparingInstall") + "...";
            else
            {
                infoBar.tbStatus.Text = App.RM.GetString("Installing") + " " + e.UpdateName;

                if (e.TotalUpdates > 1)
                    infoBar.tbStatus.Text += Environment.NewLine + e.UpdatesComplete + " " + App.RM.GetString("OutOf") + " " + e.TotalUpdates + ", " + e.CurrentProgress + "% " +
                                             App.RM.GetString("Complete");
                else infoBar.tbStatus.Text += ", " + e.CurrentProgress + "% " + App.RM.GetString("Complete");
            }
        }

        private void DownloadProgressChanged(AdminCallBack.DownloadProgressChangedEventArgs e)
        {
            infoBar.tbStatus.Text = App.RM.GetString("DownloadingUpdates") + " (" + Shared.ConvertFileSize(e.BytesTotal) + ", " + (e.BytesTransferred*100/e.BytesTotal).ToString("F0") + " % " +
                                    App.RM.GetString("Complete") + ")";
        }

        private void InstallDone(AdminCallBack.InstallDoneEventArgs e)
        {
            Settings.Default.lastInstall = string.Format("{0} {1} {2}", DateTime.Now.ToShortDateString(), App.RM.GetString("At"), DateTime.Now.ToShortTimeString());
            tbUpdatesInstalled.Text = App.RM.GetString("TodayAt") + " " + DateTime.Now.ToShortTimeString();
            // if a reboot is needed lets say it
            if (!Shared.RebootNeeded) SetUI(UILayout.InstallationCompleted, e.UpdatesInstalled, e.UpdatesFailed);
            else if (!Dispatcher.CheckAccess()) SetUI(UILayout.RebootNeeded);
        }

        private void DownloadDone(AdminCallBack.DownloadDoneEventArgs e)
        {
            if (e.ErrorOccurred) SetUI(UILayout.ErrorOccurred);
            else
            {
                if (App.IsAutoCheck) SetUI(UILayout.DownloadCompleted);
                else SetUI(UILayout.Installing);
            }
        }

        private void SearchDone(Search.SearchDoneEventArgs e)
        {
            if (e.Applications.Count > 0)
            {
                App.Applications = e.Applications;

                var count = new int[2];

                for (var x = 0; x < e.Applications.Count; x++)
                {
                    for (var y = 0; y < e.Applications[x].Updates.Count; y++)
                    {
                        switch (e.Applications[x].Updates[y].Importance)
                        {
                            case Importance.Important:
                                count[0]++;
                                break;
                            case Importance.Locale:
                            case Importance.Optional:

                                count[1]++;
                                break;
                            case Importance.Recommended:
                                if (App.Settings.IncludeRecommended) count[0]++;
                                else count[1]++;
                                break;
                        }
                    }
                }

                #region GUI Updating

                if (count[0] > 0 || count[1] > 0)
                {
                    SetUI(UILayout.UpdatesFound);
                    infoBar.tbSelectedUpdates.Text = App.RM.GetString("NoUpdatesSelected");

                    if (count[0] == 1) infoBar.tbViewImportantUpdates.Text = count[0] + " " + App.RM.GetString("ImportantUpdateAvaliable") + " ";
                    else infoBar.tbViewImportantUpdates.Text = count[0] + " " + App.RM.GetString("ImportantUpdatesAvaliable");

                    if (count[0] < 1) infoBar.tbViewImportantUpdates.Text = App.RM.GetString("NoImportantUpdates");

                    if (count[1] > 0)
                    {
                        if (count[1] == 1) infoBar.tbViewOptionalUpdates.Text = count[1] + " " + App.RM.GetString("OptionalUpdateAvaliable");
                        else infoBar.tbViewOptionalUpdates.Text = count[1] + " " + App.RM.GetString("OptionalUpdatesAvaliable");

                        infoBar.tbViewOptionalUpdates.Visibility = Visibility.Visible;
                    }
                    else infoBar.tbViewOptionalUpdates.Visibility = Visibility.Collapsed;
                }
                //End Code

                #endregion
            }
            else
            {
                if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(SetUI, UILayout.NoUpdates);
                else SetUI(UILayout.NoUpdates);
            }
        }

        private void Admin_ErrorOccurredEventHandler(object sender, AdminCallBack.ErrorOccurredEventArgs e)
        {
            switch (e.Type)
            {
                case ErrorType.FatalNetworkError:
                    if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(SetUI, UILayout.ErrorOccurred, App.RM.GetString("CheckConnection"));
                    else SetUI(UILayout.ErrorOccurred, App.RM.GetString("CheckConnection"));

                    break;
                case ErrorType.InstallationError:
                    break;


                case ErrorType.SearchError:
                    break;
                case ErrorType.DownloadError:
                    break;
                case ErrorType.GeneralErrorNonFatal:

                    break;
                case ErrorType.FatalError:
                    if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(SetUI, UILayout.ErrorOccurred, e.Description);
                    else SetUI(UILayout.ErrorOccurred, e.Description);


                    break;
            }
        }

        #region Invoker Events

        private void Admin_InstallProgressChangedEventHandler(object sender, AdminCallBack.InstallProgressChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(InstallProgressChanged, e);
            else InstallProgressChanged(e);
        }

        private void Admin_DownloadProgressChangedEventHandler(object sender, AdminCallBack.DownloadProgressChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(DownloadProgressChanged, e);
            else DownloadProgressChanged(e);
        }

        internal void Search_SearchDoneEventHandler(object sender, Search.SearchDoneEventArgs e)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(SearchDone, e);
            else SearchDone(e);
        }

        private void Admin_InstallDoneEventHandler(object sender, AdminCallBack.InstallDoneEventArgs e)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(InstallDone, e);
            else InstallDone(e);
        }

        private void Admin_DownloadDoneEventHandler(object sender, AdminCallBack.DownloadDoneEventArgs e)
        {
            if (!Dispatcher.CheckAccess()) Dispatcher.BeginInvoke(DownloadDone, e);
            else DownloadDone(e);
        }

        #endregion

        #endregion

        #region Update Methods

        /// <summary>
        /// Checks for updates
        /// </summary>
        /// <param name="auto">Indicates if it's an auto function, if it is it disables the warning messages</param>
        private void CheckForUpdates(bool auto)
        {
            if (auto)
            {
                if (Process.GetProcessesByName("Seven Update.Admin").Length < 1 && Shared.RebootNeeded == false) CheckForUpdates();
            }
            else CheckForUpdates();
        }

        /// <summary>
        /// Checks for updates
        /// </summary>
        private void CheckForUpdates()
        {
            if (Process.GetProcessesByName("Seven Update.Admin").Length < 1)
            {
                if (Shared.RebootNeeded == false)
                {
                    SetUI(UILayout.CheckingForUpdates);
                    Settings.Default.lastUpdateCheck = DateTime.Now.ToShortDateString() + " " + App.RM.GetString("At") + " " + DateTime.Now.ToShortTimeString();
                    Search.SearchForUpdatesAync(App.AppsToUpdate);
                }
                else
                {
                    SetUI(UILayout.RebootNeeded);
                    MessageBox.Show(App.RM.GetString("RebootNeededFirst"), App.RM.GetString("SevenUpdate"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else MessageBox.Show(App.RM.GetString("AlreadyUpdating"), App.RM.GetString("SevenUpdate"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Downloads updates
        /// </summary>
        internal void DownloadInstallUpdates()
        {
            for (var x = 0; x < App.Applications.Count; x++)
            {
                for (var y = 0; y < App.Applications[x].Updates.Count; y++)
                {
                    if (App.Applications[x].Updates[y].Selected) continue;
                    App.Applications[x].Updates.RemoveAt(y);
                    y--;
                }
                if (App.Applications[x].Updates.Count != 0) continue;
                App.Applications.RemoveAt(x);
                x--;
            }

            if (App.Applications.Count > 0)
            {
                var sla = new LicenseAgreement();
                if (sla.LoadLicenses() == false)
                {
                    SetUI(UILayout.Canceled);
                    return;
                }

                if (Admin.Install())
                {
                    Shared.Serialize(App.Applications, Shared.UserStore + "Update List.xml");
                    Admin.Connect();
                    if (infoBar.tbHeading.Text == App.RM.GetString("DownloadAndInstallUpdates")) SetUI(UILayout.Downloading);
                    else SetUI(UILayout.Installing);
                }
                else SetUI(UILayout.Canceled);
            }
            else SetUI(UILayout.Canceled);
        }

        #endregion

        #region UI Methods

        /// <summary>
        /// Loads the application settings and initializes event handlers for the pages
        /// </summary>
        private void LoadSettings()
        {
            if (Settings.Default.lastUpdateCheck.Contains(DateTime.Now.ToShortDateString())) tbRecentCheck.Text = App.RM.GetString("TodayAt") + " " + Settings.Default.lastUpdateCheck.Replace(DateTime.Now.ToShortDateString() + " " + App.RM.GetString("At") + " ", null);
            else tbRecentCheck.Text = Settings.Default.lastUpdateCheck;

            if (Settings.Default.lastInstall.Contains(DateTime.Now.ToShortDateString())) tbUpdatesInstalled.Text = App.RM.GetString("TodayAt") + " " + Settings.Default.lastInstall.Replace(DateTime.Now.ToShortDateString() + " " + App.RM.GetString("At") + " ", null);
            else tbUpdatesInstalled.Text = Settings.Default.lastInstall;

            if (Shared.RebootNeeded) SetUI(UILayout.RebootNeeded);
            else SetUI(UILayout.NoUpdates);

            #region Event Handler Declarations

            UpdateInfo.UpdateSelectionChangedEventHandler += UpdateInfo_UpdateSelectionChangedEventHandler;
            infoBar.btnAction.Click += BtnActionClick;
            infoBar.tbViewImportantUpdates.MouseDown += TbViewImportantUpdatesMouseDown;
            infoBar.tbViewOptionalUpdates.MouseDown += TbViewOptionalUpdatesMouseDown;
            Search.SearchDoneEventHandler += Search_SearchDoneEventHandler;
            AdminCallBack.DownloadProgressChangedEventHandler += Admin_DownloadProgressChangedEventHandler;
            AdminCallBack.DownloadDoneEventHandler += Admin_DownloadDoneEventHandler;
            AdminCallBack.InstallProgressChangedEventHandler += Admin_InstallProgressChangedEventHandler;
            AdminCallBack.InstallDoneEventHandler += Admin_InstallDoneEventHandler;
            AdminCallBack.ErrorOccurredEventHandler += Admin_ErrorOccurredEventHandler;
            RestoreUpdates.RestoredHiddenUpdateEventHandler += RestoreUpdates_RestoredHiddenUpdateEventHandler;

            #endregion
        }

        /// <summary>
        /// Sets the Main Page UI
        /// </summary>
        /// <param name="layout">Type of layout to set</param>
        private void SetUI(UILayout layout)
        {
            SetUI(layout, null);
        }

        /// <summary>
        /// Sets the Main Page UI
        /// </summary>
        /// <param name="layout">Type of layout to set</param>
        /// <param name="errorDescription">Description of error that occurred</param>
        private void SetUI(UILayout layout, string errorDescription)
        {
            SetUI(layout, errorDescription, 0, 0);
        }

        private void SetUI(UILayout layout, int updatesInstalled, int updatesFailed)
        {
            SetUI(layout, null, updatesInstalled, updatesFailed);
        }

        private void SetUI(UILayout layout, string errorDescription, int updatesInstalled, int updatesFailed)
        {
            switch (layout)
            {
                case UILayout.Canceled:

                    #region GUI Code

                    infoBar.btnAction.Visibility = Visibility.Visible;
                    infoBar.tbAction.Text = App.RM.GetString("TryAgain");
                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.imgShield.Source = App.RedShield;
                    infoBar.imgSide.Source = redSide;
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.line.Visibility = Visibility.Collapsed;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.tbHeading.Text = App.RM.GetString("UpdatesCanceled");
                    infoBar.tbStatus.Text = App.RM.GetString("CancelInstallation");
                    infoBar.imgAdminShield.Visibility = Visibility.Collapsed;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.CheckingForUpdates:

                    #region GUI Code

                    tbRecentCheck.Text = App.RM.GetString("TodayAt") + " " + DateTime.Now.ToShortTimeString();
                    infoBar.tbSelectedUpdates.FontWeight = FontWeights.Normal;
                    infoBar.tbViewImportantUpdates.Visibility = Visibility.Collapsed;
                    infoBar.tbViewOptionalUpdates.Visibility = Visibility.Collapsed;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.tbStatus.Visibility = Visibility.Collapsed;
                    infoBar.tbHeading.Text = App.RM.GetString("CheckingForUpdates") + "...";
                    infoBar.imgShield.Source = suIcon;
                    infoBar.imgSide.Source = null;
                    infoBar.pbProgressBar.Visibility = Visibility.Visible;
                    infoBar.tbAction.Visibility = Visibility.Collapsed;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.ConnectingToService:

                    #region GUI Code

                    infoBar.imgShield.Source = App.YellowShield;
                    infoBar.imgSide.Source = yellowSide;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.pbProgressBar.Visibility = Visibility.Visible;
                    infoBar.btnAction.Visibility = Visibility.Hidden;
                    infoBar.tbStatus.Text = App.RM.GetString("GettingInstallationStatus");
                    infoBar.tbHeading.Text = App.RM.GetString("ConnectingToService") + "...";
                    infoBar.line.Visibility = Visibility.Collapsed;
                    infoBar.tbAction.Visibility = Visibility.Collapsed;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.Downloading:

                    #region GUI Code

                    infoBar.imgShield.Source = App.YellowShield;
                    infoBar.imgSide.Source = yellowSide;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.pbProgressBar.Visibility = Visibility.Visible;
                    infoBar.btnAction.Visibility = Visibility.Visible;
                    infoBar.tbAction.Text = App.RM.GetString("StopDownload");
                    if (App.IsAdmin) infoBar.imgAdminShield.Visibility = Visibility.Visible;
                    infoBar.tbStatus.Text = App.RM.GetString("PreparingDownload");
                    infoBar.tbHeading.Text = App.RM.GetString("DownloadingUpdates") + "...";
                    infoBar.line.Visibility = Visibility.Collapsed;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.DownloadCompleted:

                    #region GUI Code

                    infoBar.tbStatus.Visibility = Visibility.Collapsed;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Visible;
                    infoBar.tbStatus.Visibility = Visibility.Collapsed;
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.btnAction.Visibility = Visibility.Hidden;
                    if (App.IsAdmin) infoBar.imgAdminShield.Visibility = Visibility.Visible;
                    infoBar.tbAction.Text = App.RM.GetString("InstallUpdates");
                    infoBar.tbHeading.Text = App.RM.GetString("UpdatesReadyInstalled");
                    infoBar.imgShield.Source = App.YellowShield;
                    infoBar.imgSide.Source = yellowSide;
                    infoBar.line.Visibility = Visibility.Visible;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.ErrorOccurred:

                    #region GUI Code

                    infoBar.tbAction.Text = App.RM.GetString("TryAgain");
                    if (App.IsAdmin) infoBar.imgAdminShield.Visibility = Visibility.Visible;
                    infoBar.btnAction.Visibility = Visibility.Visible;
                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.imgSide.Source = redSide;
                    infoBar.imgShield.Source = App.RedShield;
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.tbHeading.Text = App.RM.GetString("ErrorOccurred");

                    infoBar.line.Visibility = Visibility.Collapsed;

                    infoBar.tbStatus.Text = errorDescription ?? App.RM.GetString("UnknownErrorOccurred");

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.Installing:

                    #region GUI Code

                    infoBar.btnAction.Visibility = Visibility.Visible;
                    if (App.IsAdmin) infoBar.imgAdminShield.Visibility = Visibility.Visible;
                    infoBar.pbProgressBar.Visibility = Visibility.Visible;
                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.imgShield.Source = suIcon;
                    infoBar.tbAction.Text = App.RM.GetString("StopInstallation");
                    infoBar.tbStatus.Text = App.RM.GetString("PreparingInstall");
                    infoBar.tbHeading.Text = App.RM.GetString("InstallingUpdates") + "...";
                    infoBar.line.Visibility = Visibility.Collapsed;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = true;

                    #endregion

                    break;
                case UILayout.InstallationCompleted:

                    #region GUI Code

                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.tbHeading.Text = App.RM.GetString("UpdatesInstalled");
                    infoBar.imgShield.Source = App.GreenShield;
                    infoBar.imgSide.Source = greenSide;
                    infoBar.btnAction.Visibility = Visibility.Collapsed;
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.line.Visibility = Visibility.Collapsed;


                    infoBar.tbStatus.Text = App.RM.GetString("Succeeded") + ": " + updatesInstalled + " ";

                    if (updatesInstalled == 1) infoBar.tbStatus.Text += App.RM.GetString("Update");
                    else infoBar.tbStatus.Text += App.RM.GetString("Updates");

                    if (updatesFailed > 0)
                    {
                        infoBar.tbStatus.Text += " " + App.RM.GetString("Failed") + ": " + updatesFailed + " ";

                        if (updatesFailed == 1) infoBar.tbStatus.Text += App.RM.GetString("Update");
                        else infoBar.tbStatus.Text += App.RM.GetString("Updates");
                    }
                    if (App.IsAdmin) infoBar.imgAdminShield.Visibility = Visibility.Visible;

                    tbUpdatesInstalled.Text = App.RM.GetString("TodayAt") + " " + DateTime.Now.ToShortTimeString();

                    Settings.Default.lastInstall = DateTime.Now.ToShortDateString() + " " + App.RM.GetString("At") + " " + DateTime.Now.ToShortTimeString();
                    Settings.Default.lastInstall = DateTime.Now.ToShortDateString() + " " + App.RM.GetString("At") + " " + DateTime.Now.ToShortTimeString();

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.NoUpdates:

                    #region GUI Code

                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.tbHeading.Text = App.RM.GetString("ProgramsUpToDate");
                    infoBar.imgSide.Source = greenSide;
                    infoBar.imgShield.Source = App.GreenShield;
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.tbStatus.Text = App.RM.GetString("NoNewUpdates");
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.btnAction.Visibility = Visibility.Collapsed;
                    infoBar.line.Visibility = Visibility.Collapsed;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
                case UILayout.RebootNeeded:

                    #region GUI Code

                    infoBar.imgAdminShield.Visibility = Visibility.Collapsed;
                    infoBar.btnAction.Visibility = Visibility.Visible;
                    infoBar.tbStatus.Visibility = Visibility.Visible;
                    infoBar.imgShield.Source = App.YellowShield;
                    infoBar.imgSide.Source = yellowSide;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Collapsed;
                    infoBar.spUpdateInfo.Visibility = Visibility.Collapsed;
                    infoBar.tbAction.Text = App.RM.GetString("RestartNow");
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.tbHeading.Text = App.RM.GetString("RebootNeeded");
                    infoBar.tbStatus.Text = App.RM.GetString("SaveAndReboot");
                    infoBar.line.Visibility = Visibility.Collapsed;

                    #endregion

                    break;

                case UILayout.UpdatesFound:

                    #region GUI Code

                    infoBar.tbStatus.Visibility = Visibility.Collapsed;
                    infoBar.pbProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.tbHeading.Text = App.RM.GetString("DownloadAndInstallUpdates");
                    infoBar.imgSide.Source = yellowSide;
                    infoBar.imgShield.Source = App.YellowShield;
                    if (App.IsAdmin) infoBar.imgAdminShield.Visibility = Visibility.Visible;
                    infoBar.tbAction.Text = App.RM.GetString("InstallUpdates");
                    infoBar.spUpdateInfo.Visibility = Visibility.Visible;
                    infoBar.tbSelectedUpdates.Visibility = Visibility.Visible;
                    infoBar.btnAction.Visibility = Visibility.Collapsed;
                    infoBar.tbViewOptionalUpdates.Visibility = Visibility.Visible;
                    infoBar.tbViewImportantUpdates.Visibility = Visibility.Visible;
                    infoBar.line.Visibility = Visibility.Visible;

                    #endregion

                    #region Code

                    App.IsInstallInProgress = false;

                    #endregion

                    break;
            }
        }

        #endregion
    }
}