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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#endregion

namespace SevenUpdate.Pages
{
    /// <summary>
    /// Interaction logic for InfoBar.xaml
    /// </summary>
    public partial class InfoBar : UserControl
    {
        public InfoBar()
        {
            InitializeComponent();
        }

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
    }
}