// <copyright file="TaskDialogCommonButtonReturnIds.cs" project="SevenSoftware.Windows" company="Microsoft Corporation">Microsoft Corporation</copyright>
// <license href="http://code.msdn.microsoft.com/WindowsAPICodePack/Project/License.aspx" name="Microsoft Software License" />

namespace SevenSoftware.Windows.Dialogs.TaskDialog
{
    /// <summary>
    ///   Identify button *return values* - note that, unfortunately, these are different from the inbound button
    ///   values.
    /// </summary>
    internal enum TaskDialogCommonButtonReturnIds
    {
        /// <summary>The button returned OK.</summary>
        Ok = 1, 

        /// <summary>The button returned cancel.</summary>
        Cancel = 2, 

        /// <summary>The button return abort.</summary>
        Abort = 3, 

        /// <summary>The button returned retry.</summary>
        Retry = 4, 

        /// <summary>The button was ignored.</summary>
        Ignore = 5, 

        /// <summary>The button returned yes.</summary>
        Yes = 6, 

        /// <summary>The button returned no.</summary>
        No = 7, 

        /// <summary>The button returned close.</summary>
        Close = 8
    }
}