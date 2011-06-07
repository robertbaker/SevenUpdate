// ***********************************************************************
// <copyright file="RestoreUpdates.xaml.cs" project="SevenUpdate" assembly="SevenUpdate" solution="SevenUpdate" company="Seven Software">
//     Copyright (c) Seven Software. All rights reserved.
// </copyright>
// <author username="sevenalive">Robert Baker</author>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3">
//  This file is part of Seven Update.
//    Seven Update is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//    Seven Update is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//    You should have received a copy of the GNU General Public License
//    along with Seven Update.  If not, see http://www.gnu.org/licenses/.
// </license>
// <summary>
//   Interaction logic for RestoreUpdates.xaml
// .</summary>
// ***********************************************************************

namespace SevenUpdate.Pages
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Input;
    using System.Windows.Media;

    using Windows;

    /// <summary>
    ///   Interaction logic for Update_History.xaml.
    /// </summary>
    public sealed partial class RestoreUpdates
    {
        #region Constants and Fields

        /// <summary>
        ///   Gets or sets a collection of SUH items.
        /// </summary>
        private ObservableCollection<Suh> hiddenUpdates;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///   Initializes a new instance of the RestoreUpdates class.
        /// </summary>
        public RestoreUpdates()
        {
            this.InitializeComponent();

            this.lvHiddenUpdates.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(this.RestrictColumn), true);
            this.btnRestore.IsShieldNeeded = !Core.Instance.IsAdmin;

            this.MouseLeftButtonDown -= Core.EnableDragOnGlass;
            this.MouseLeftButtonDown += Core.EnableDragOnGlass;
            AeroGlass.CompositionChanged -= this.UpdateUI;
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

        #endregion

        #region Events

        /// <summary>
        ///   Occurs when one or more hidden updates have been restored.
        /// </summary>
        public static event EventHandler<EventArgs> RestoredHiddenUpdate;

        #endregion

        #region Methods

        /// <summary>
        ///   Gets the hidden updates and loads them in the <see cref="ListView" />.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void GetHiddenUpdates(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(App.HiddenFile))
            {
                return;
            }

            this.hiddenUpdates = Utilities.Deserialize<ObservableCollection<Suh>>(App.HiddenFile);
            if (this.hiddenUpdates == null)
            {
                return;
            }

            this.lvHiddenUpdates.ItemsSource = this.hiddenUpdates;
        }

        /// <summary>
        ///   Goes back to the Main page.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void NavigateToMainPage(object sender, RoutedEventArgs e)
        {
            Core.NavigateToMainPage();
        }

        /// <summary>
        ///   Un hides one or more updates and navigates to the Main page.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void RestoreUpdate(object sender, RoutedEventArgs e)
        {
            for (var x = 0; x < this.hiddenUpdates.Count; x++)
            {
                if (this.hiddenUpdates[x].Status != UpdateStatus.Visible)
                {
                    continue;
                }

                this.hiddenUpdates.RemoveAt(x);
                x--;
            }

            if (WcfService.HideUpdates(this.hiddenUpdates))
            {
                if (RestoredHiddenUpdate != null)
                {
                    RestoredHiddenUpdate(this, new EventArgs());
                }
            }

            Core.NavigateToMainPage();
        }

        /// <summary>
        ///   Limit the size of the <see cref="GridViewColumn" /> when it's being resized.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="System.Windows.Controls.Primitives.DragDeltaEventArgs" /> instance containing the event data.
        /// </param>
        private void RestrictColumn(object sender, DragDeltaEventArgs e)
        {
            ListViewExtensions.LimitColumnSize((Thumb)e.OriginalSource);
        }

        /// <summary>
        ///   Shows the selected update details.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="System.Windows.Input.MouseButtonEventArgs" /> instance containing the event data.
        /// </param>
        private void ShowDetails(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2 || this.lvHiddenUpdates.SelectedIndex == -1)
            {
                return;
            }

            var details = new UpdateDetails();
            details.ShowDialog(this.hiddenUpdates[this.lvHiddenUpdates.SelectedIndex]);
        }

        /// <summary>
        ///   Shows the selected update details.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void ShowDetailsDialog(object sender, RoutedEventArgs e)
        {
            var details = new UpdateDetails();
            details.ShowDialog(this.hiddenUpdates[this.lvHiddenUpdates.SelectedIndex]);
        }

        /// <summary>
        ///   Changes the UI depending on whether Aero Glass is enabled.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="CompositionChangedEventArgs" /> instance containing the event data.
        /// </param>
        private void UpdateUI(object sender, CompositionChangedEventArgs e)
        {
            if (e.IsGlassEnabled)
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

        /// <summary>
        ///   Updates the UI when an update check box is clicked.
        /// </summary>
        /// <param name="sender">
        ///   The object that called the event.
        /// </param>
        /// <param name="e">
        ///   The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.
        /// </param>
        private void UpdateUIOnUpdateSelection(object sender, RoutedEventArgs e)
        {
            var checkedCount = this.hiddenUpdates.Count(t => t.Status == UpdateStatus.Visible);

            this.tbSelectedUpdates.Text = string.Format(
                CultureInfo.CurrentCulture, Properties.Resources.TotalSelectedUpdates, checkedCount);
            this.btnRestore.IsEnabled = checkedCount > 0;
        }

        #endregion
    }
}