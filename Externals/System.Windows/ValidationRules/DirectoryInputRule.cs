// ***********************************************************************
// <copyright file="DirectoryInputRule.cs"
//            project="System.Windows"
//            assembly="System.Windows"
//            solution="SevenUpdate"
//            company="Seven Software">
//     Copyright (c) Seven Software. All rights reserved.
// </copyright>
// <author username="sevenalive">Robert Baker</author>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3">
//  This file is part of Seven Update.
//
//    Seven Update is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    Seven Update is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with Seven Update.  If not, see http://www.gnu.org/licenses/.
// </license>
// ***********************************************************************
namespace System.Windows.ValidationRules
{
    using System.Globalization;
    using System.IO;
    using System.Windows.Controls;
    using System.Windows.Properties;

    /// <summary>Validates if the input is a directory</summary>
    public class DirectoryInputRule : ValidationRule
    {
        #region Properties

        /// <summary>Gets or sets a value indicating whether this instance is required.</summary>
        /// <value><see langword = "true" /> if this instance is required; otherwise, <see langword = "false" />.</value>
        public bool IsRequired { private get; set; }

        #endregion

        #region Public Methods

        /// <summary>When overridden in a derived class, performs validation checks on a value.</summary>
        /// <param name="value">The value from the binding target to check.</param>
        /// <param name="cultureInfo">The culture to use in this rule.</param>
        /// <returns>A <see cref="T:System.Windows.Controls.ValidationResult"/> object.</returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var input = value as string;
            if (input == null)
            {
                return this.IsRequired ? new ValidationResult(false, Resources.FilePathInvalid) : new ValidationResult(true, null);
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return this.IsRequired ? new ValidationResult(false, Resources.FilePathInvalid) : new ValidationResult(true, null);
            }

            var directoryName = Path.GetDirectoryName(input);

            if (string.IsNullOrEmpty(directoryName))
            {
                return this.IsRequired ? new ValidationResult(false, Resources.FilePathInvalid) : new ValidationResult(true, null);
            }

            return directoryName.IndexOfAny(Path.GetInvalidPathChars()) >= 0 ? new ValidationResult(false, Resources.FilePathInvalid) : new ValidationResult(true, null);
        }

        #endregion
    }
}