// <copyright file="SuiLocationRule.cs" project="SevenUpdate.Sdk">Robert Baker</copyright>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3" />

namespace SevenUpdate.Sdk.ValidationRules
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Windows.Controls;

    using SevenUpdate.Sdk.Properties;

    /// <summary>Validates a value and determines if the value is a Sui location.</summary>
    [SuppressMessage("Microsoft.StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", 
        Justification = "ValidationRule")]
    public class SuiLocationRule : ValidationRule
    {
        /// <summary>When overridden in a derived class, performs validation checks on a value.</summary>
        /// <param name="value">The value from the binding target to check.</param>
        /// <param name="cultureInfo">The culture to use in this rule.</param>
        /// <returns>A <c>T:System.Windows.Controls.ValidationResult</c> object.</returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var input = value as string;

            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, Resources.FilePathInvalid);
            }

            bool result = Uri.IsWellFormedUriString(input, UriKind.RelativeOrAbsolute);

            if (!result)
            {
                if (!Utilities.IsValidPath(input))
                {
                    return new ValidationResult(false, Resources.FilePathInvalid);
                }
            }

            string fileName = Path.GetFileName(input);

            if (fileName == null || string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult(false, Resources.FilePathInvalid);
            }

            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !input.EndsWith(@".sui", true, cultureInfo))
            {
                return new ValidationResult(false, Resources.FilePathInvalid);
            }

            return new ValidationResult(true, null);
        }
    }
}