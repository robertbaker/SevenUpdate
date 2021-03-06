// <copyright file="LocThicknessExtension.cs" project="WPFLocalizeExtension">Bernhard Millauer</copyright>
// <license href="http://www.microsoft.com/en-us/openness/licenses.aspx" name="Microsoft Public License" />

namespace WPFLocalizeExtension.Extensions
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Markup;

    using WPFLocalizeExtension.Engine;

    /// <summary><c>BaseLocalizeExtension</c> for Thickness values.</summary>
    [MarkupExtensionReturnType(typeof(Thickness))]
    public class LocThicknessExtension : BaseLocalizeExtension<Thickness>
    {
        /// <summary>Initializes a new instance of the <see cref="LocThicknessExtension" /> class.</summary>
        public LocThicknessExtension()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="LocThicknessExtension" /> class.</summary>
        /// <param name="key">The resource identifier.</param>
        public LocThicknessExtension(string key) : base(key)
        {
        }

        /// <summary>Provides the Value for the first Binding as Thickness.</summary>
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
                    "ResourceKey '{0}' returns '{1}' which is not type of double", 
                    this.Key, 
                    obj.GetType().FullName));
        }

        /// <summary>This method is used to modify the passed object into the target format.</summary>
        /// <param name="input">The object that will be modified.</param>
        /// <returns>Returns the modified object.</returns>
        protected override object FormatOutput(object input)
        {
            MethodInfo method = typeof(ThicknessConverter).GetMethod(
                "FromString", BindingFlags.Static | BindingFlags.NonPublic);
            object[] args;

            if (Localize.Instance.IsInDesignMode && this.DesignValue != null)
            {
                args = new[] { this.DesignValue, new CultureInfo("en-US") };

                return (Thickness)method.Invoke(null, args);
            }

            args = new[] { input, new CultureInfo("en-US") };

            return (Thickness)method.Invoke(null, args);
        }

        /// <summary>
        ///   This method gets the new value for the target property and call <see cref =
        ///   "BaseLocalizeExtension{TValue}.SetNewValue" />.
        /// </summary>
        protected override void HandleNewValue()
        {
            var obj = Localize.Instance.GetLocalizedObject<object>(
                this.Assembly, this.Dictionary, this.Key, this.Culture);
            this.SetNewValue(this.FormatOutput(obj));
        }
    }
}