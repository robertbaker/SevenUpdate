// <copyright file="UpdateShortcuts.xaml.cs" project="SevenUpdate.Sdk">Robert Baker</copyright>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3" />

namespace SevenUpdate.Sdk.Pages
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    using SevenSoftware.Windows;
    using SevenSoftware.Windows.Controls;
    using SevenSoftware.Windows.Dialogs.TaskDialog;
    using SevenSoftware.Windows.ValidationRules;

    using SevenUpdate.Sdk.Windows;

    /// <summary>Interaction logic for UpdateShortcuts.xaml.</summary>
    public sealed partial class UpdateShortcuts
    {
        /// <summary>Initializes a new instance of the <see cref="UpdateShortcuts" /> class.</summary>
        public UpdateShortcuts()
        {
            this.InitializeComponent();

            this.listBox.ItemsSource = Core.UpdateInfo.Shortcuts;

            this.MouseLeftButtonDown -= Core.EnableDragOnGlass;
            AeroGlass.CompositionChanged -= this.UpdateUI;

            this.MouseLeftButtonDown += Core.EnableDragOnGlass;
            AeroGlass.CompositionChanged += this.UpdateUI;
            if (AeroGlass.IsGlassEnabled)
            {
                this.tbTitle.Foreground = Brushes.Black;
                this.line.Visibility = Visibility.Collapsed;
                this.rectangle.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.tbTitle.Foreground = new SolidColorBrush(Color.FromRgb(0, 51, 153));
                this.line.Visibility = Visibility.Visible;
                this.rectangle.Visibility = Visibility.Visible;
            }
        }

        /// <summary>Fires the OnPropertyChanged Event with the collection changes.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The event data.</param>
        void ChangeDescription(object sender, RoutedEventArgs e)
        {
            var textBox = (InfoTextBox)sender;
            Core.UpdateLocaleStrings(textBox.Text, Core.UpdateInfo.Shortcuts[Core.SelectedShortcut].Description);
        }

        /// <summary>Fires the OnPropertyChanged Event with the collection changes.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The event data.</param>
        void ChangeName(object sender, RoutedEventArgs e)
        {
            var textBox = (InfoTextBox)sender;

            if (Utilities.Locale == "en" && string.IsNullOrWhiteSpace(textBox.Text))
            {
                return;
            }

            Core.UpdateLocaleStrings(textBox.Text, Core.UpdateInfo.Shortcuts[Core.SelectedShortcut].Name);
        }

        /// <summary>Clears the UI of errors.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>DependencyPropertyChangedEventArgs</c> instance containing the event data.</param>
        void ClearError(object sender, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (InfoTextBox)sender;

            if (Convert.ToBoolean(e.NewValue))
            {
                return;
            }

            textBox.HasError = false;
            textBox.ToolTip = false;
        }

        /// <summary>Converts a path to system variables.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.KeyboardFocusChangedEventArgs</c> instance containing the event data.</param>
        void ConvertPath(object sender, KeyboardFocusChangedEventArgs e)
        {
            var source = e.Source as InfoTextBox;
            if (source == null)
            {
                return;
            }

            string fileLocation = Utilities.ConvertPath(source.Text, true, Core.AppInfo.Platform);
            string installDirectory = Utilities.IsRegistryKey(Core.AppInfo.Directory)
                                          ? Utilities.GetRegistryValue(
                                              Core.AppInfo.Directory, Core.AppInfo.ValueName, Core.AppInfo.Platform)
                                          : Core.AppInfo.Directory;

            installDirectory = Utilities.ConvertPath(installDirectory, true, Core.AppInfo.Platform);

            string installUrl = fileLocation.Replace(installDirectory, @"%INSTALLDIR%\", true);
            installUrl = installUrl.Replace(@"\\", @"\");

            source.Text = Utilities.ConvertPath(installUrl, false, Core.AppInfo.Platform);
        }

        /// <summary>Deletes the selected UpdateShortcut from the collection.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.KeyEventArgs</c> instance containing the event data.</param>
        void DeleteShortcut(object sender, KeyEventArgs e)
        {
            int index = this.listBox.SelectedIndex;
            if (index < 0)
            {
                return;
            }

            if (e.Key != Key.Delete)
            {
                return;
            }

            Core.UpdateInfo.Shortcuts.RemoveAt(index);
            this.listBox.SelectedIndex = index - 1;

            if (this.listBox.SelectedIndex < 0 && this.listBox.Items.Count > 0)
            {
                this.listBox.SelectedIndex = 0;
            }
        }

        /// <summary>Determines whether this instance has errors.</summary>
        /// <returns><c>True</c> if this instance has errors; otherwise, <c>False</c>.</returns>
        bool HasErrors()
        {
            if (Core.UpdateInfo.Shortcuts.Count == 0)
            {
                return false;
            }

            return this.tbxName.HasError || this.tbxSaveLocation.HasError || this.tbxTarget.HasError;
        }

        /// <summary>Opens a dialog to browse for the shortcut to import.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.RoutedEventArgs</c> instance containing the event data.</param>
        void ImportShortcut(object sender, RoutedEventArgs e)
        {
            string[] file = Core.OpenFileDialog(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), null, false, "lnk", true);

            if (file == null)
            {
                return;
            }

            Shortcut importedShortcut = Shortcut.GetShortcutData(file[0]);

            string path = Utilities.ConvertPath(
                Path.GetDirectoryName(importedShortcut.Location), false, Core.AppInfo.Platform);
            path = path.Replace(Core.AppInfo.Directory, "%INSTALLDIR%");

            string icon = Utilities.ConvertPath(importedShortcut.Icon, false, Core.AppInfo.Platform);
            icon = icon.Replace(Core.AppInfo.Directory, "%INSTALLDIR%");
            var shortcut = new Shortcut
                {
                    Arguments = importedShortcut.Arguments, 
                    Icon = icon, 
                    Location = path, 
                    Action = ShortcutAction.Update, 
                    Target = Utilities.ConvertPath(importedShortcut.Target, false, Core.AppInfo.Platform), 
                };

            shortcut.Name.Add(new LocaleString(Path.GetFileNameWithoutExtension(file[0]), Utilities.Locale));

            Core.UpdateInfo.Shortcuts.Add(shortcut);
            this.listBox.SelectedIndex = Core.UpdateInfo.Shortcuts.Count - 1;
        }

        /// <summary>Load the <c>LocaleString</c>'s into the UI.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Controls.SelectionChangedEventArgs</c> instance containing the event data.</param>
        void LoadLocaleStrings(object sender, SelectionChangedEventArgs e)
        {
            if (this.tbxDescription == null || this.cbxLocale.SelectedIndex < 0)
            {
                return;
            }

            Utilities.Locale = ((ComboBoxItem)this.cbxLocale.SelectedItem).Tag.ToString();

            bool found = false;
            ObservableCollection<LocaleString> shortcutDescriptions =
                Core.UpdateInfo.Shortcuts[this.listBox.SelectedIndex].Description
                ?? new ObservableCollection<LocaleString>();

            // Load Values
            foreach (var t in shortcutDescriptions.Where(t => t.Lang == Utilities.Locale))
            {
                this.tbxDescription.Text = t.Value;
                found = true;
            }

            if (!found)
            {
                this.tbxDescription.Text = null;
            }

            found = false;
            ObservableCollection<LocaleString> shortcutNames =
                Core.UpdateInfo.Shortcuts[this.listBox.SelectedIndex].Name ?? new ObservableCollection<LocaleString>();

            // Load Values
            foreach (var t in shortcutNames.Where(t => t.Lang == Utilities.Locale))
            {
                this.tbxName.Text = t.Value;
                found = true;
            }

            if (!found)
            {
                this.tbxName.Text = null;
            }
        }

        /// <summary>Opens a dialog to browse for the shortcut icon.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void LocateIcon(object sender, MouseButtonEventArgs e)
        {
            string installDirectory = Utilities.IsRegistryKey(Core.AppInfo.Directory)
                                          ? Utilities.GetRegistryValue(
                                              Core.AppInfo.Directory, Core.AppInfo.ValueName, Core.AppInfo.Platform)
                                          : Core.AppInfo.Directory;

            installDirectory = Utilities.ConvertPath(installDirectory, true, Core.AppInfo.Platform);

            string[] shortcut = Core.OpenFileDialog(installDirectory);

            if (shortcut == null)
            {
                return;
            }

            string fileUrl = shortcut[0].Replace(installDirectory, @"%INSTALLDIR%\", true);
            fileUrl = fileUrl.Replace(@"\\", @"\");
            Core.UpdateInfo.Shortcuts[this.listBox.SelectedIndex].Icon = fileUrl;
        }

        /// <summary>Opens a dialog to browse for the shortcut location.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void LocateShortcutLocation(object sender, MouseButtonEventArgs e)
        {
            string installDirectory = Utilities.IsRegistryKey(Core.AppInfo.Directory)
                                          ? Utilities.GetRegistryValue(
                                              Core.AppInfo.Directory, Core.AppInfo.ValueName, Core.AppInfo.Platform)
                                          : Core.AppInfo.Directory;

            installDirectory = Utilities.ConvertPath(installDirectory, true, Core.AppInfo.Platform);
            string[] shortcut = Core.OpenFileDialog(installDirectory, null, false, "lnk", true);

            if (shortcut == null)
            {
                return;
            }

            string saveLoc = Path.GetDirectoryName(shortcut[0]);

            string fileUrl = saveLoc.Replace(installDirectory, @"%INSTALLDIR%\", true);
            fileUrl = fileUrl.Replace(@"\\", @"\");
            Core.UpdateInfo.Shortcuts[this.listBox.SelectedIndex].Location = fileUrl;
        }

        /// <summary>Opens a dialog to browse for the shortcut target.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Input.MouseButtonEventArgs</c> instance containing the event data.</param>
        void LocateShortcutTarget(object sender, MouseButtonEventArgs e)
        {
            string installDirectory = Utilities.IsRegistryKey(Core.AppInfo.Directory)
                                          ? Utilities.GetRegistryValue(
                                              Core.AppInfo.Directory, Core.AppInfo.ValueName, Core.AppInfo.Platform)
                                          : Core.AppInfo.Directory;

            installDirectory = Utilities.ConvertPath(installDirectory, true, Core.AppInfo.Platform);
            string[] files = Core.OpenFileDialog(installDirectory);
            string fileUrl = files[0].Replace(installDirectory, @"%INSTALLDIR%\", true);
            fileUrl = fileUrl.Replace(@"\\", @"\");
            Core.UpdateInfo.Shortcuts[this.listBox.SelectedIndex].Target = fileUrl;
        }

        /// <summary>Navigates to the next page if no errors exist.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.RoutedEventArgs</c> instance containing the event data.</param>
        void MoveOn(object sender, RoutedEventArgs e)
        {
            if (!this.HasErrors())
            {
                MainWindow.NavService.Navigate(Core.UpdateReviewPage);
            }
            else
            {
                Core.ShowMessage(Properties.Resources.CorrectErrors, TaskDialogStandardIcon.Error);
            }
        }

        /// <summary>Navigates to the main page.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.RoutedEventArgs</c> instance containing the event data.</param>
        void NavigateToMainPage(object sender, RoutedEventArgs e)
        {
            MainWindow.NavService.Navigate(Core.MainPage);
        }

        /// <summary>Removes all Shortcuts from the collection.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.RoutedEventArgs</c> instance containing the event data.</param>
        void RemoveAllShortcuts(object sender, RoutedEventArgs e)
        {
            Core.UpdateInfo.Shortcuts.RemoveAt(this.listBox.SelectedIndex);
        }

        /// <summary>Removes the selected Shortcuts from the collection.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.RoutedEventArgs</c> instance containing the event data.</param>
        void RemoveShortcut(object sender, RoutedEventArgs e)
        {
            Core.UpdateInfo.Shortcuts.Clear();
        }

        /// <summary>Sets the selected shortcut.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>System.Windows.Controls.SelectionChangedEventArgs</c> instance containing the event data.</param>
        void SetSelectedShortcut(object sender, SelectionChangedEventArgs e)
        {
            Core.SelectedShortcut = this.listBox.SelectedIndex;
        }

        /// <summary>Updates the UI based on whether Aero Glass is enabled.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>CompositionChangedEventArgs</c> instance containing the event data.</param>
        void UpdateUI(object sender, CompositionChangedEventArgs e)
        {
            if (e.IsGlassEnabled)
            {
                this.tbTitle.Foreground = Brushes.Black;
                this.line.Visibility = Visibility.Visible;
                this.rectangle.Visibility = Visibility.Visible;
            }
            else
            {
                this.tbTitle.Foreground = new SolidColorBrush(Color.FromRgb(0, 51, 153));
                this.line.Visibility = Visibility.Collapsed;
                this.rectangle.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Validates the input to make sure it's a valid directory.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>TextChangedEventArgs</c> instance containing the event data.</param>
        void ValidateDirectoryPath(object sender, TextChangedEventArgs e)
        {
            var textBox = (InfoTextBox)sender;

            if (textBox == null)
            {
                return;
            }

            textBox.HasError =
                !new DirectoryInputRule { IsRequired = true }.Validate(
                    Utilities.ExpandInstallLocation(
                        textBox.Text, Core.AppInfo.Directory, Core.AppInfo.Platform, Core.AppInfo.ValueName), 
                    null).IsValid;

            textBox.ToolTip = textBox.HasError ? Properties.Resources.FilePathInvalid : null;
        }

        /// <summary>Validates the input to make sure it's a valid file.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The <c>TextChangedEventArgs</c> instance containing the event data.</param>
        void ValidateFileName(object sender, TextChangedEventArgs e)
        {
            var textBox = (InfoTextBox)sender;

            if (textBox == null)
            {
                return;
            }

            if (textBox.Name == "tbxName")
            {
                textBox.HasError = string.IsNullOrWhiteSpace(textBox.Text)
                                   || textBox.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;
            }
            else
            {
                textBox.HasError =
                    !new FileNameInputRule { IsRequired = true }.Validate(
                        Utilities.ExpandInstallLocation(
                            textBox.Text, Core.AppInfo.Directory, Core.AppInfo.Platform, Core.AppInfo.ValueName), 
                        null).IsValid;
            }

            textBox.ToolTip = textBox.HasError ? Properties.Resources.FilePathInvalid : null;
        }
    }
}