// <copyright file="TaskDialogStandardIcon.cs" project="SevenSoftware.Windows" company="Microsoft Corporation">Microsoft Corporation</copyright>
// <license href="http://code.msdn.microsoft.com/WindowsAPICodePack/Project/License.aspx" name="Microsoft Software License" />

namespace SevenSoftware.Windows.Dialogs.TaskDialog
{
    /// <summary>Specifies the icon displayed in a task dialog.</summary>
    public enum TaskDialogStandardIcon
    {
        /// <summary>Displays no icons (default).</summary>
        None = 0, 

        /// <summary>Displays the warning icon.</summary>
        Warning = 65535, 

        /// <summary>Displays the error icon.</summary>
        Error = 65534, 

        /// <summary>Displays the Information icon.</summary>
        Information = 65533, 

        /// <summary>Displays the User Account Control shield.</summary>
        Shield = 65532, 

        /// <summary>Displays the User Account Control shield.</summary>
        ShieldBlue = ushort.MaxValue - 4, 

        /// <summary>Displays the User Account Control shield with gray background.</summary>
        ShieldGray = ushort.MaxValue - 8, 

        /// <summary>Displays a warning shield with yellow background.</summary>
        SecurityWarning = ushort.MaxValue - 5, 

        /// <summary>Displays an error shield with red background.</summary>
        SecurityError = ushort.MaxValue - 6, 

        /// <summary>Displays a success shield with green background.</summary>
        ShieldGreen = ushort.MaxValue - 7, 
    }
}