// <copyright file="LocTextExtension.cs" project="WPFLocalizeExtension">Bernhard Millauer</copyright>
// <license href="http://www.microsoft.com/en-us/openness/licenses.aspx" name="Microsoft Public License" />

namespace WPFLocalizeExtension.Extensions
{
    using System;
    using System.Globalization;
    using System.Windows.Markup;

    using WPFLocalizeExtension.Engine;

    /// <summary><c>BaseLocalizeExtension</c> for string objects.</summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class LocTextExtension : BaseLocalizeExtension<string>
    {
        /// <summary>Holds the local format segment array.</summary>
        string[] formatSegments;

        /// <summary>Holds the local prefix value.</summary>
        string prefix;

        /// <summary>Holds the local suffix value.</summary>
        string suffix;

        /// <summary>Initializes a new instance of the <see cref="LocTextExtension" /> class.</summary>
        /// <param name="key">The resource identifier.</param>
        public LocTextExtension(string key) : base(key)
        {
            this.InitializeLocText();
        }

        /// <summary>Initializes a new instance of the <see cref="LocTextExtension" /> class.</summary>
        protected LocTextExtension()
        {
            this.InitializeLocText();
        }

        /// <summary>This enumeration is used to determine the type of the return value of <c>GetAppendText</c>.</summary>
        enum TextAppendType
        {
            /// <summary>The return value is used as prefix.</summary>
            Prefix, 

            /// <summary>The return value is used as suffix.</summary>
            Suffix
        }

        /// <summary>
        ///   Gets or sets the format segment 1.This will be used to replace format place holders from the localized
        ///   text.<c>LocTextLowerExtension</c> and <c>LocTextUpperExtension</c> will format this segment.
        /// </summary>
        /// <value>The format segment 1.</value>
        public string FormatSegment1
        {
            get { return this.formatSegments[0]; }

            set
            {
                this.formatSegments[0] = value;
                this.HandleNewValue();
            }
        }

        /// <summary>
        ///   Gets or sets the format segment 2.This will be used to replace format place holders from the localized
        ///   text.<c>LocTextUpperExtension</c> and <c>LocTextLowerExtension</c> will format this segment.
        /// </summary>
        /// <value>The format segment 2.</value>
        public string FormatSegment2
        {
            get { return this.formatSegments[1]; }

            set
            {
                this.formatSegments[1] = value;
                this.HandleNewValue();
            }
        }

        /// <summary>
        ///   Gets or sets the format segment 3.This will be used to replace format place holders from the localized
        ///   text.<c>LocTextUpperExtension</c> and <c>LocTextLowerExtension</c> will format this segment.
        /// </summary>
        /// <value>The format segment 3.</value>
        public string FormatSegment3
        {
            get { return this.formatSegments[2]; }

            set
            {
                this.formatSegments[2] = value;
                this.HandleNewValue();
            }
        }

        /// <summary>
        ///   Gets or sets the format segment 4.This will be used to replace format place holders from the localized
        ///   text.<c>LocTextUpperExtension</c> and <c>LocTextLowerExtension</c> will format this segment.
        /// </summary>
        /// <value>The format segment 4.</value>
        public string FormatSegment4
        {
            get { return this.formatSegments[3]; }

            set
            {
                this.formatSegments[3] = value;
                this.HandleNewValue();
            }
        }

        /// <summary>
        ///   Gets or sets the format segment 5.This will be used to replace format place holders from the localized
        ///   text.<c>LocTextUpperExtension</c> and <c>LocTextLowerExtension</c> will format this segment.
        /// </summary>
        /// <value>The format segment 5.</value>
        public string FormatSegment5
        {
            get { return this.formatSegments[4]; }

            set
            {
                this.formatSegments[4] = value;
                this.HandleNewValue();
            }
        }

        /// <summary>Gets or sets a prefix for the localized text.</summary>
        public string Prefix
        {
            get { return this.prefix; }

            set
            {
                this.prefix = value;

                // reset the value of the target property
                this.HandleNewValue();
            }
        }

        /// <summary>Gets or sets a suffix for the localized text.</summary>
        public string Suffix
        {
            get { return this.suffix; }

            set
            {
                this.suffix = value;

                // reset the value of the target property
                this.HandleNewValue();
            }
        }

        /// <summary>Provides the Value for the first Binding as <c>System.String</c>.</summary>
        /// <param name="serviceProvider">The <c>System.Windows.Markup.IProvideValueTarget</c> provided from the <c>MarkupExtension</c>.</param>
        /// <returns>The found item from the .resx directory or <c>null</c> if not found.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            object obj = base.ProvideValue(serviceProvider);

            if (obj == null)
            {
                return null;
            }

            if (this.IsTypeOf(obj.GetType(), typeof(BaseLocalizeExtension<>)))
            {
                return obj;
            }

            if (obj.GetType().Equals(typeof(string)))
            {
                return this.FormatOutput(obj);
            }

            throw new NotSupportedException(
                string.Format(
                    CultureInfo.CurrentCulture, 
                    "ResourceKey '{0}' returns '{1}' which is not type of System.String", 
                    this.Key, 
                    obj.GetType().FullName));
        }

        /// <summary>This method returns the finished formatted text.</summary>
        /// <param name="input">If the passed string not <c>null</c>, it will be used, otherwise a fresh localized text will be loaded.</param>
        /// <returns>Returns the finished formatted text in format [PREFIX]LocalizedText[SUFFIX].</returns>
        protected override object FormatOutput(object input)
        {
            if (Localize.Instance.IsInDesignMode && this.DesignValue != null)
            {
                input = this.DesignValue;
            }
            else
            {
                // load a fresh localized text, if the passed string is null
                input = input
                        ??
                        Localize.Instance.GetLocalizedObject<object>(
                            this.Assembly, this.Dictionary, this.Key, this.Culture);
            }

            // get the main text as string xor string.empty
            string textMain = input as string ?? string.Empty;

            try
            {
                // add some format segments, in case that the main text contains format place holders like {0}
                textMain = string.Format(
                    Localize.Instance.SpecificCulture, 
                    textMain, 
                    this.formatSegments[0] ?? string.Empty, 
                    this.formatSegments[1] ?? string.Empty, 
                    this.formatSegments[2] ?? string.Empty, 
                    this.formatSegments[3] ?? string.Empty, 
                    this.formatSegments[4] ?? string.Empty);
            }
            catch (FormatException)
            {
                // if a format exception was thrown, change the text to an error string
                textMain = "TextFormatError: Max 5 Format PlaceHolders! {0} to {4}";
            }

            // get the prefix
            string textPrefix = this.GetAppendText(TextAppendType.Prefix);

            // get the suffix
            string textSuffix = this.GetAppendText(TextAppendType.Suffix);

            // format the text with prefix and suffix to [PREFIX]LocalizedText[SUFFIX]
            input = this.FormatText(textPrefix + textMain + textSuffix);

            // return the finished formatted text
            return input;
        }

        /// <summary>
        ///   This method formats the localized text.If the passed target text is <c>null</c>, string.empty will be
        ///   returned.
        /// </summary>
        /// <param name="target">The text to format.</param>
        /// <returns>Returns the formated text or string.empty, if the target text was <c>null</c>.</returns>
        protected virtual string FormatText(string target)
        {
            return target ?? string.Empty;
        }

        /// <summary>
        ///   This method gets the new value for the target property and call <see cref =
        ///   "BaseLocalizeExtension{TValue}.SetNewValue" />.
        /// </summary>
        protected override void HandleNewValue()
        {
            this.SetNewValue(this.FormatOutput(null));
        }

        /// <summary>
        ///   Returns the prefix or suffix text, depending on the supplied <c>TextAppendType</c>.If the prefix or suffix
        ///   is <c>null</c>, it will be returned a string.empty.
        /// </summary>
        /// <param name="at">The <c>TextAppendType</c> defines the format of the return value.</param>
        /// <returns>Returns the formated prefix or suffix.</returns>
        string GetAppendText(TextAppendType at)
        {
            // define a return value
            string retVal = string.Empty;

            // check if it should be a prefix, the format will be [PREFIX], or check if it should be a suffix, the
            // format will be [SUFFIX]
            if (at == TextAppendType.Prefix && !string.IsNullOrEmpty(this.prefix))
            {
                retVal = this.prefix;
            }
            else if (at == TextAppendType.Suffix && !string.IsNullOrEmpty(this.suffix))
            {
                retVal = this.suffix;
            }

            // return the formated prefix or suffix
            return retVal;
        }

        /// <summary>Initalizes the localized text</summary>
        void InitializeLocText()
        {
            this.formatSegments = new string[5];
            this.formatSegments.Initialize();

            // removed this call, because of the fact, if the LocTextExtension is defined with "LocTextExtension
            // Key=abc" and not with "LocTextExtension abc". the value will be set at call ProvideValue, AFTER the Key
            // Property is set.

            ////SetNewValue(FormatOutput(null));
        }
    }
}