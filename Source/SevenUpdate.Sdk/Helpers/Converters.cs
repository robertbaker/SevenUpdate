﻿#region

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using SevenUpdate.Sdk.Properties;

#endregion

namespace SevenUpdate.Sdk
{
    /// <summary>
    ///   Converts a <see cref = "LocaleString" /> to a localized string
    /// </summary>
    [ValueConversion(typeof (LocaleString), typeof (string))]
    public sealed class StringToLocaleStringConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        ///   Converts a object into another object
        /// </summary>
        /// <returns>the converted object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var localeStrings = value as Collection<LocaleString>;

            // Loops through the collection of LocaleStrings
            return localeStrings != null ? localeStrings.Where(t => t.Lang == Base.SelectedLocale).Select(t => t.Value).FirstOrDefault() : null;
        }

        /// <summary>
        ///   Converts a converted object back into it's original form
        /// </summary>
        /// <returns>The original object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var valueString = value as string;

            ObservableCollection<LocaleString> localeStrings = null;
            switch (parameter as string)
            {
                case "App.Name":
                    localeStrings = Base.AppInfo.Name;
                    break;
                case "App.Publisher":
                    localeStrings = Base.AppInfo.Publisher;
                    break;
                case "App.Description":
                    localeStrings = Base.AppInfo.Description;
                    break;
                case "Update.Description":
                    localeStrings = Base.UpdateInfo.Description;
                    break;
                case "Update.Name":
                    localeStrings = Base.UpdateInfo.Name;
                    break;
            }


            if (localeStrings != null)
            {
                if (!String.IsNullOrWhiteSpace(valueString))
                {
                    var found = false;

                    foreach (var t in localeStrings.Where(t => t.Lang == Base.SelectedLocale))
                    {
                        t.Value = valueString;
                        found = true;
                    }
                    if (!found)
                    {
                        var ls = new LocaleString {Lang = Base.SelectedLocale, Value = valueString};
                        localeStrings.Add(ls);
                    }
                }
                else
                {
                    for (int x = 0; x < localeStrings.Count; x++)
                    {
                        if (localeStrings[x].Lang == Base.SelectedLocale)
                            localeStrings.RemoveAt(x);
                    }
                }
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(valueString))
                {
                    localeStrings = new ObservableCollection<LocaleString>();
                    var ls = new LocaleString {Lang = Base.SelectedLocale, Value = valueString};
                    localeStrings.Add(ls);
                }
            }
            return localeStrings;
        }

        #endregion
    }

    /// <summary>
    ///   Converts a Bool to a Label
    /// </summary>
    [ValueConversion(typeof (bool), typeof (string))]
    public sealed class BoolToLabelConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        ///   Converts a object into another object
        /// </summary>
        /// <returns>the converted object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool) value ? @"HKLM\Software\MyCompany\MyApp" : @"%PROGRAMFILES%\Seven Software\Seven Update";
        }

        /// <summary>
        ///   Converts a converted object back into it's original form
        /// </summary>
        /// <returns>The original object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///   Converts the string to a bool
    /// </summary>
    [ValueConversion(typeof (string), typeof (bool))]
    public sealed class StringToBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        ///   Converts a object into another object
        /// </summary>
        /// <returns>the converted object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;
            // If no value should return false
            if (parameter != null)
            {
                if (System.Convert.ToBoolean(parameter))
                    // If  no value or hash is generating return true, otherwise false
                    return stringValue != (Resources.CalculatingHash + "...") && !String.IsNullOrEmpty(stringValue);
            }

            // If  no value or hash is generating return true, otherwise false
            return stringValue == (Resources.CalculatingHash + "...") || String.IsNullOrEmpty(stringValue);
        }

        /// <summary>
        ///   Converts a converted object back into it's original form
        /// </summary>
        /// <returns>The original object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///   Converts the string to a bool
    /// </summary>
    [ValueConversion(typeof (string), typeof (Visibility))]
    public sealed class StringToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        ///   Converts a object into another object
        /// </summary>
        /// <returns>the converted object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = value as string;
            // If no value should return false
            if (parameter != null)
            {
                if (System.Convert.ToBoolean(parameter))
                {
                    // If  no value or hash is generating return true, otherwise false
                    if (stringValue != (Resources.CalculatingHash + "...") && !String.IsNullOrEmpty(stringValue))
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
            }

            // If  no value or hash is generating return true, otherwise false
            return stringValue == (Resources.CalculatingHash + "...") || String.IsNullOrEmpty(stringValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        ///   Converts a converted object back into it's original form
        /// </summary>
        /// <returns>The original object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///   Converts the Int to Visibility
    /// </summary>
    [ValueConversion(typeof (int), typeof (Visibility))]
    public sealed class IntToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        ///   Converts a object into another object
        /// </summary>
        /// <returns>the converted object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int ? (int) value : 0;

            if (parameter != null)
            {
                // If count is less then 1 and should return visible
                if (count < 1 && System.Convert.ToBoolean(parameter))
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }

            return count < 1 ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        ///   Converts a converted object back into it's original form
        /// </summary>
        /// <returns>The original object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///   Converts the Int to Visibility
    /// </summary>
    [ValueConversion(typeof (int), typeof (bool))]
    public sealed class IntToBoolConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        ///   Converts a object into another object
        /// </summary>
        /// <returns>the converted object</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value is int ? (int) value : 0;

            if (parameter != null)
            {
                // If count is less then 1 and should return visible
                return count < 1 && System.Convert.ToBoolean(parameter);
            }

            return count < 1 ? false : true;
        }

        /// <summary>
        ///   Converts a converted object back into it's original form
        /// </summary>
        /// <returns>The original object</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new NotImplementedException();
        }

        #endregion
    }

    /// <summary>
    ///   Converts the Enum to a Boolean
    /// </summary>
    [ValueConversion(typeof (Enum), typeof (bool))]
    public sealed class StringToLocaleString : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(false) ? DependencyProperty.UnsetValue : parameter;
        }

        #endregion
    }

    /// <summary>
    ///   Converts the string to a DateTime
    /// </summary>
    [ValueConversion(typeof (DateTime), typeof (string))]
    public sealed class DateConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? DateTime.Parse(value.ToString()) : DateTime.Now;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? DateTime.Now.ToShortDateString() : ((DateTime) value).ToShortDateString();
        }

        #endregion
    }
}