// <copyright file="Core.cs" project="SevenUpdate">Robert Baker</copyright>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3" />

namespace SevenUpdate
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;

    using SevenSoftware.Windows.Dialogs.TaskDialog;
    using SevenSoftware.Windows.Internal;

    using SevenUpdate.Pages;
    using SevenUpdate.Properties;
    using SevenUpdate.Windows;

    /// <summary>Contains properties and methods that are essential.</summary>
    internal sealed class Core : INotifyPropertyChanged
    {
        /// <summary>Gets the main location where Seven Update files are stored.</summary>
        internal const string SevenUpdateUrl = @"http://apps.sevenupdate.com/SevenUpdate";

        /// <summary>The main page for Seven Update.</summary>
        static readonly Main MainPage = new Main();

        /// <summary>The static instance of the Core class.</summary>
        static Core instance;

        /// <summary>Indicates if the current user logged in is an admin.</summary>
        static bool isAdmin;

        /// <summary>The current action Seven Update is performing.</summary>
        static UpdateAction updateAction;

        /// <summary>Occurs when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Occurs when the user cancels their update selection.</summary>
        internal static event EventHandler UpdateActionChanged;

        /// <summary>Gets the update configuration settings.</summary>
        public static Config Settings
        {
            get
            {
                return File.Exists(App.ConfigFile)
                           ? Utilities.Deserialize<Config>(App.ConfigFile)
                           : new Config { AutoOption = AutoUpdateOption.Notify, IncludeRecommended = false };
            }
        }

        /// <summary>Gets or sets a value indicating whether the current user enabled admin access.</summary>
        /// <value><c>True</c> if this instance is admin; otherwise, <c>False</c>.</value>
        public bool IsAdmin
        {
            get { return NativeMethods.IsUserAdmin || isAdmin; }

            set
            {
                isAdmin = value;
                this.OnPropertyChanged("IsAdmin");
            }
        }

        /// <summary>Gets or sets the current action relating to updates.</summary>
        /// <value>The update action.</value>
        public UpdateAction UpdateAction
        {
            get { return updateAction; }

            set
            {
                updateAction = value;
                this.OnPropertyChanged("UpdateAction");
                if (UpdateActionChanged != null)
                {
                    UpdateActionChanged(this, new EventArgs());
                }
            }
        }

        /// <summary>Gets or sets a collection of applications to update.</summary>
        internal static Collection<Sui> Applications { get; set; }

        /// <summary>Gets the static instance of Core.</summary>
        /// <value>The instance.</value>
        internal static Core Instance
        {
            get { return instance ?? (instance = new Core()); }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether if an install is currently in progress and Seven Update was
        ///   started after an auto check.
        /// </summary>
        internal static bool IsReconnect { get; set; }

        /// <summary>Gets a collection of software that Seven Update can check for updates.</summary>
        static IEnumerable<Sua> AppsToUpdate
        {
            get
            {
                var apps = new Collection<Sua>();
                if (File.Exists(App.ApplicationsFile))
                {
                    apps = Utilities.Deserialize<Collection<Sua>>(App.ApplicationsFile);
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
                        SuiUrl = SevenUpdateUrl
                    };
                if (App.IsDev)
                {
                    app.SuiUrl += @"-dev.sui";
                }

                if (App.IsBeta)
                {
                    app.SuiUrl += @"-beta.sui";
                }

                if (!App.IsDev && !App.IsBeta)
                {
                    app.SuiUrl += @".sui";
                }

                apps.Insert(0, app);
                return apps;
            }
        }

        /// <summary>Checks for updates.</summary>
        /// <param name="auto"><c>True</c> if it's called because of an auto update check, otherwise <c>False</c>.</param>
        internal static void CheckForUpdates(bool auto)
        {
            if (auto)
            {
                if (!Install.IsInstalling && !Download.IsDownloading && !Search.IsSearching && !Utilities.RebootNeeded)
                {
                    CheckForUpdates();
                }
            }
            else
            {
                CheckForUpdates();
            }
        }

        /// <summary>Checks for updates.</summary>
        internal static void CheckForUpdates()
        {
            if (AppsToUpdate == null)
            {
                return;
            }

            try
            {
                File.Delete(Path.Combine(App.AllUserStore, "updates.sui"));
            }
            catch (IOException)
            {
            }

            if (!Install.IsInstalling && !Download.IsDownloading && !Search.IsSearching && !IsReconnect)
            {
                if (Utilities.RebootNeeded == false)
                {
                    Instance.UpdateAction = UpdateAction.CheckingForUpdates;
                    Properties.Settings.Default.LastUpdateCheck = DateTime.Now;
                    Search.SearchForUpdatesAsync(AppsToUpdate, Path.Combine(App.AllUserStore, "downloads"));
                }
                else
                {
                    Instance.UpdateAction = UpdateAction.RebootNeeded;
                    if (ShowMessage(
                        Resources.RebootComputer, 
                        TaskDialogStandardIcon.Information, 
                        TaskDialogStandardButtons.Cancel, 
                        Resources.RebootNeededFirst, 
                        null, 
                        Resources.RestartNow) != TaskDialogResult.Cancel)
                    {
                        Utilities.StartProcess(@"shutdown.exe", "-r -t 00");
                    }
                }
            }
            else
            {
                ShowMessage(null, TaskDialogStandardIcon.Information, Resources.AlreadyUpdating);
            }
        }

        /// <summary>Enables the ability to drag on glass.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The arguments generated by the event.</param>
        internal static void EnableDragOnGlass(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>Gets the total size of a single update.</summary>
        /// <param name="files">The collection of files of an update.</param>
        /// <returns>A ulong value of the size of the update.</returns>
        internal static ulong GetUpdateSize(IEnumerable<UpdateFile> files)
        {
            return files.Aggregate<UpdateFile, ulong>(0, (current, t) => current + t.FileSize);
        }

        /// <summary>Goes back to the Main page.</summary>
        internal static void NavigateToMainPage()
        {
            MainWindow.NavService.Navigate(MainPage);
        }

        /// <summary>Shows either a <c>TaskDialog</c> or a <c>MessageBox</c> if running legacy windows.</summary>
        /// <param name="instructionText">The main text to display (Blue 14pt for <c>TaskDialog</c>).</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="standardButtons">The standard buttons to use (with or without the custom default button text).</param>
        /// <param name="description">A description of the message, supplements the instruction text.</param>
        /// <param name="footerText">Text to display as a footer message.</param>
        /// <param name="defaultButtonText">Text to display on the button.</param>
        /// <param name="displayShieldOnButton">Indicates if a UAC shield is to be displayed on the defaultButton.</param>
        /// <returns>Returns the result of the message.</returns>
        internal static TaskDialogResult ShowMessage(
            string instructionText, 
            TaskDialogStandardIcon icon, 
            TaskDialogStandardButtons standardButtons, 
            string description = null, 
            string footerText = null, 
            string defaultButtonText = null, 
            bool displayShieldOnButton = false)
        {
            if (TaskDialog.IsPlatformSupported)
            {
                using (var td = new TaskDialog())
                {
                    td.Caption = Resources.SevenUpdate;
                    td.InstructionText = instructionText;
                    td.Text = description;
                    td.Icon = icon;
                    td.FooterText = footerText;
                    td.FooterIcon = TaskDialogStandardIcon.Information;
                    td.CanCancel = true;
                    td.StandardButtons = standardButtons;
                    if (defaultButtonText != null)
                    {
                        var button = new TaskDialogButton(@"btnCustom", defaultButtonText)
                            {
                               Default = true, ShowElevationIcon = displayShieldOnButton 
                            };
                        button.Click += TaskDialogCustomButtonClicked;
                        td.Controls.Add(button);
                    }

                    return Application.Current.MainWindow == null
                               ? td.Show() : td.ShowDialog(Application.Current.MainWindow);
                }
            }

            string message = instructionText;
            var msgIcon = MessageBoxImage.None;

            if (description != null)
            {
                message += Environment.NewLine + description;
            }

            if (footerText != null)
            {
                message += Environment.NewLine + footerText;
            }

            switch (icon)
            {
                case TaskDialogStandardIcon.Error:
                    msgIcon = MessageBoxImage.Error;
                    break;
                case TaskDialogStandardIcon.Information:
                    msgIcon = MessageBoxImage.Information;
                    break;
                case TaskDialogStandardIcon.Warning:
                    msgIcon = MessageBoxImage.Warning;
                    break;
            }

            MessageBoxResult result;

            if (standardButtons == TaskDialogStandardButtons.Cancel || defaultButtonText != null)
            {
                result = MessageBox.Show(message, Resources.SevenUpdate, MessageBoxButton.OKCancel, msgIcon);
            }
            else
            {
                result = MessageBox.Show(message, Resources.SevenUpdate, MessageBoxButton.OK, msgIcon);
            }

            switch (result)
            {
                case MessageBoxResult.No:
                    return TaskDialogResult.No;
                case MessageBoxResult.OK:
                    return TaskDialogResult.Ok;
                case MessageBoxResult.Yes:
                    return TaskDialogResult.Yes;
                default:
                    return TaskDialogResult.Cancel;
            }
        }

        /// <summary>Shows either a <c>TaskDialog</c> or a <c>MessageBox</c> if running legacy windows.</summary>
        /// <param name="instructionText">The main text to display (Blue 14pt for <c>TaskDialog</c>).</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="description">A description of the message, supplements the instruction text.</param>
        static void ShowMessage(string instructionText, TaskDialogStandardIcon icon, string description = null)
        {
            ShowMessage(instructionText, icon, TaskDialogStandardButtons.Ok, description);
        }

        /// <summary>Closes the dialog when a custom button is clicked.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The arguments generated by the event.</param>
        static void TaskDialogCustomButtonClicked(object sender, EventArgs e)
        {
            var customButton = (TaskDialogButton)sender;
            var taskDialog = (TaskDialog)customButton.HostingDialog;
            taskDialog.Close(TaskDialogResult.Ok);
        }

        /// <summary>When a property has changed, call the <c>OnPropertyChanged</c> Event.</summary>
        /// <param name="name">The name of the property that changed.</param>
        void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}