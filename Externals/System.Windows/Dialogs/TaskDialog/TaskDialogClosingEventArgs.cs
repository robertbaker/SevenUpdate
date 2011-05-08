﻿// ***********************************************************************
// <copyright file="TaskDialogClosingEventArgs.cs" project="System.Windows" assembly="System.Windows" solution="SevenUpdate" company="Microsoft Corporation">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <license href="http://code.msdn.microsoft.com/WindowsAPICodePack/Project/License.aspx">Microsoft Software License</license>
// ***********************************************************************

namespace System.Windows.Dialogs
{
    using System.ComponentModel;

    /// <summary>Data associated with <see cref="TaskDialog.Closing" /> event.</summary>
    public class TaskDialogClosingEventArgs : CancelEventArgs
    {
        #region Properties

        /// <summary>Gets or sets the text of the custom button that was clicked.</summary>
        public string CustomButton { get; set; }

        /// <summary>Gets or sets the standard button that was clicked.</summary>
        public TaskDialogResults TaskDialogResult { get; set; }

        #endregion
    }
}