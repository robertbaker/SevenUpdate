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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using SevenUpdate.Controls;
using SevenUpdate.Properties;
using SevenUpdate.WCF;
using SevenUpdate.Windows;

#endregion

namespace SevenUpdate.Pages
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Page
    {
        #region Global Vars

        /// <summary>
        /// The Seven Update list location
        /// </summary>
        private static readonly Uri SUALocation = new Uri("http://ittakestime.org/su/Apps.sul");

        /// <summary>
        /// A collection of SUA's that Seven Update can update
        /// </summary>
        private ObservableCollection<SUA> userAppList;

        #endregion

        /// <summary>
        /// The constructor for the Options Page
        /// </summary>
        public Options()
        {
            InitializeComponent();
            listView.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler(Thumb_DragDelta), true);
            if (App.IsAdmin)
                btnSave.Content = App.RM.GetString("Save");
        }

        #region Methods

        /// <summary>
        /// Downloads the Seven Update Application List
        /// </summary>
        private void DownloadSUL()
        {
            try
            {
                File.Delete(Shared.UserStore + @"Apps.sul");
            }
            catch
            {
                return;
            }
            var wc = new WebClient();

            wc.DownloadFileCompleted += WcDownloadFileCompleted;

            wc.DownloadFileAsync(SUALocation, Shared.UserStore + @"Apps.sul");
        }

        /// <summary>
        /// Loads the configuration and sets the UI
        /// </summary>
        private void LoadSettings()
        {
            switch (App.Settings.AutoOption)
            {
                case AutoUpdateOption.Install:
                    cbAutoUpdateMethod.SelectedIndex = 0;
                    break;

                case AutoUpdateOption.Download:
                    cbAutoUpdateMethod.SelectedIndex = 1;
                    break;

                case AutoUpdateOption.Notify:
                    cbAutoUpdateMethod.SelectedIndex = 2;
                    break;

                case AutoUpdateOption.Never:
                    cbAutoUpdateMethod.SelectedIndex = 3;
                    break;
            }

            chkRecommendedUpdates.IsChecked = App.Settings.IncludeRecommended;
            //tbLastUpdated.Text = App.RM.GetString("LastUpdated") + " " + Settings.Default.lastListUpdate.ToShortDateString() + " " + App.RM.GetString("At") + " " +
            //                     Settings.Default.lastListUpdate.ToShortTimeString();
        }

        /// <summary>
        /// Loads the list of Seven Update applications and sets the UI
        /// </summary>
        private void LoadSUL()
        {
            ObservableCollection<SUA> officialAppList = null;
            if (File.Exists(Shared.UserStore + @"Apps.sul"))
                officialAppList = Shared.Deserialize<ObservableCollection<SUA>>(Shared.UserStore + @"Apps.sul");
            userAppList = Shared.Deserialize<ObservableCollection<SUA>>(Shared.AppsFile);

            if (officialAppList != null)
            {
                for (var x = 0; x < officialAppList.Count; x++)
                {
                    if (Directory.Exists(Shared.ConvertPath(officialAppList[x].Directory, true, officialAppList[x].Is64Bit)))
                        continue;
                    officialAppList.RemoveAt(x);
                    x--;
                }
                if (userAppList == null)
                    userAppList = officialAppList;
            }

            if (userAppList != null)
                for (var x = 0; x < userAppList.Count; x++)
                {
                    if (!Directory.Exists(Shared.ConvertPath(userAppList[x].Directory, true, userAppList[x].Is64Bit)))
                    {
                        userAppList.RemoveAt(x);
                        x--;
                        continue;
                    }
                    if (officialAppList == null)
                        continue;
                    for (var y = 0; y < officialAppList.Count; y++)
                    {
                        if (officialAppList[y].Source != userAppList[x].Source)
                            continue;
                        officialAppList[y].IsEnabled = userAppList[x].IsEnabled;
                        userAppList[x] = officialAppList[y];
                    }
                }
            if (userAppList == null && officialAppList != null)
                userAppList = officialAppList;

            Dispatcher.BeginInvoke(UpdateList);
        }

        /// <summary>
        /// Updates the list with the <see cref="userAppList"/>
        /// </summary>
        private void UpdateList()
        {
            listView.Cursor = Cursors.Arrow;
            if (userAppList != null)
            {
                listView.ItemsSource = userAppList;
                userAppList.CollectionChanged += UserAppList_CollectionChanged;
                AddSortBinding();
                tbLastUpdated.Text = App.RM.GetString("LastUpdated") + " " + Settings.Default.lastListUpdate.ToShortDateString() + " " + App.RM.GetString("At") + " " +
                                     Settings.Default.lastListUpdate.ToShortTimeString();
            }
            else
            {
                tbLastUpdated.Text = App.RM.GetString("CouldNotConnect");
            }
        }

        /// <summary>
        /// Adds the <see cref="GridViewColumn"/>'s of the <see cref="ListView"/> to be sorted
        /// </summary>
        private void AddSortBinding()
        {
            var gv = (GridView) listView.View;

            var col = gv.Columns[1];
            ListViewSorter.SetSortBindingMember(col, new Binding("ApplicationName"));

            col = gv.Columns[2];
            ListViewSorter.SetSortBindingMember(col, new Binding("Publisher"));

            col = gv.Columns[3];
            ListViewSorter.SetSortBindingMember(col, new Binding("Architecture"));

            ListViewSorter.SetCustomSorter(listView, new ListViewExtensions.SUASorter());
        }

        /// <summary>
        /// Saves the Settings
        /// </summary>
        private void SaveSettings()
        {
            var options = new Config();

            if (cbAutoUpdateMethod.SelectedIndex == 0)
                options.AutoOption = AutoUpdateOption.Install;

            if (cbAutoUpdateMethod.SelectedIndex == 1)
                options.AutoOption = AutoUpdateOption.Download;

            if (cbAutoUpdateMethod.SelectedIndex == 2)
                options.AutoOption = AutoUpdateOption.Notify;

            if (cbAutoUpdateMethod.SelectedIndex == 3)
                options.AutoOption = AutoUpdateOption.Never;

            options.IncludeRecommended = ((bool) chkRecommendedUpdates.IsChecked);


            if (cbAutoUpdateMethod.SelectedIndex == 3)
            {
                options.AutoOption = AutoUpdateOption.Never;

                Admin.SaveSettings(false, options, userAppList);
            }
            else
                Admin.SaveSettings(true, options, userAppList);
        }

        #endregion

        #region UI Events

        /// <summary>
        /// Loads the SUA list after the download has completed
        /// </summary>
        private void WcDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
                return;
            Settings.Default.lastListUpdate = DateTime.Now;

            Settings.Default.Save();

            LoadSUL();
        }

        /// <summary>
        /// When the AutoUpdate selection changes update the shield image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbAutoUpdateMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbAutoUpdateMethod.SelectedIndex)
            {
                case 0:
                    imgShield.Source = App.GreenShield;
                    break;
                case 1:
                    imgShield.Source = App.GreenShield;
                    break;
                case 2:
                    imgShield.Source = null;
                    break;
                case 3:
                    imgShield.Source = App.RedShield;
                    break;
            }
        }

        /// <summary>
        /// Loads the settings and SUA list when the page is loaded
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            listView.Cursor = Cursors.Wait;
            LoadSettings();

            if (!Settings.Default.lastListUpdate.Date.Equals(DateTime.Now.Date) || !File.Exists(Shared.AppsFile))
            {
                var thread = new Thread(DownloadSUL);
                thread.Start();
            }
            else
            {
                var thread = new Thread(LoadSUL);
                thread.Start();
            }
        }

        #region ListView Events

        /// <summary>
        /// Updates the <see cref="CollectionView"/> when the <c>userAppList</c> collection changes
        /// </summary>
        private void UserAppList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // update the view when item change is NOT caused by replacement
            if (e.Action != NotifyCollectionChangedAction.Replace)
                return;
            var dataView = CollectionViewSource.GetDefaultView(listView.ItemsSource);
            dataView.Refresh();
        }

        /// <summary>
        /// Limit the size of the <see cref="GridViewColumn"/> when it's being resized
        /// </summary>
        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            ListViewExtensions.LimitColumnSize(((Thumb) e.OriginalSource));
        }

        #endregion

        #region TextBlocks

        /// <summary>
        /// Underlines the text when mouse is over the <see cref="TextBlock"/>
        /// </summary>
        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            var textBlock = ((TextBlock) sender);
            textBlock.TextDecorations = TextDecorations.Underline;
        }

        /// <summary>
        /// Removes the Underlined text when mouse is leaves the <see cref="TextBlock"/>
        /// </summary>
        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            var textBlock = ((TextBlock) sender);
            textBlock.TextDecorations = null;
        }

        /// <summary>
        /// Downloads the Apps.sul from the server
        /// </summary>
        private void tbRefresh_MouseDown(object sender, MouseButtonEventArgs e)
        {
            listView.Cursor = Cursors.Wait;
            var thread = new Thread(DownloadSUL);
            thread.Start();
        }

        #endregion

        #region Buttons

        /// <summary>
        /// Saves the settings and goes back to the Main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            MainWindow.NavService.GoBack();
        }

        /// <summary>
        /// Goes back to the Main page without saving the settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavService.GoBack();
        }

        #endregion

        #endregion
    }
}