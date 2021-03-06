// <copyright file="Localize.cs" project="WPFLocalizeExtension">Bernhard Millauer</copyright>
// <license href="http://www.microsoft.com/en-us/openness/licenses.aspx" name="Microsoft Public License" />

namespace WPFLocalizeExtension.Engine
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Windows;

    /// <summary>Represents the culture interface for localization.</summary>
    public sealed class Localize : DependencyObject
    {
        /// <summary>Holds the default <c>ResourceDictionary</c> name.</summary>
        public const string ResourcesName = "Resources";

        /// <summary>Holds the binding flags for the reflection to find the resource files.</summary>
        const BindingFlags ResourceBindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        /// <summary>Holds the extension of the resource files.</summary>
        const string ResourceFileExtension = ".resources";

        /// <summary>Holds the name of the Resource Manager.</summary>
        const string ResourceManagerName = "ResourceManager";

        /// <summary><c>DependencyProperty</c> DesignCulture to set the Culture.Only supported at DesignTime.</summary>
        [DesignOnly(true)]
        static readonly DependencyProperty DesignCultureProperty = DependencyProperty.RegisterAttached(
            "DesignCulture", typeof(string), typeof(Localize), new PropertyMetadata(SetCultureFromDependencyProperty));

        /// <summary>Holds a SyncRoot to be thread safe.</summary>
        static readonly object SyncRoot = new object();

        /// <summary>Holds the instance of singleton.</summary>
        static Localize instance;

        /// <summary>Holds the current chosen <c>CultureInfo</c>.</summary>
        CultureInfo culture;

        /// <summary>Prevents a default instance of the Localize class from being created.</summary>
        Localize()
        {
            this.ResourceManagerList = new Dictionary<string, ResourceManager>();
        }

        /// <summary>Get raised if the LocalizeDictionary.Culture is changed.</summary>
        internal event Action OnCultureChanged;

        /// <summary>
        ///   Gets the LocalizeDictionary singleton.If the underlying instance is <c>null</c>, a instance will be
        ///   created.
        /// </summary>
        public static Localize Instance
        {
            get
            {
                // check if the underlying instance is null
                if (instance == null)
                {
                    // if it is null, lock the sync root.

                    // if another thread is accessing this too, it have to wait until the sync root is released
                    lock (SyncRoot)
                    {
                        // check again, if the underlying instance is null
                        if (instance == null)
                        {
                            // create a new instance
                            instance = new Localize();
                        }
                    }
                }

                // return the existing/new instance
                return instance;
            }
        }

        /// <summary>Gets or sets the <c>CultureInfo</c> for localization.On set, <c>OnCultureChanged</c> is raised.</summary>
        /// <exception cref="System.InvalidOperationException">
        ///   You have to set LocalizeDictionary.Culture first or wait until
        ///   System.Windows.Application.Current.MainWindow is created. Otherwise you will get an Exception.</exception>
        /// <exception cref="System.ArgumentNullException">thrown if Culture will be set to <c>null</c></exception>
        public CultureInfo Culture
        {
            get { return this.culture ?? (this.culture = DefaultCultureInfo); }

            set
            {
                // the culture info cannot contains a null reference
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                // Set the CultureInfo
                this.culture = value;

                // Raise the OnCultureChanged event
                if (this.OnCultureChanged != null)
                {
                    this.OnCultureChanged();
                }
            }
        }

        /// <summary>Gets a value indicating whether the status of the design mode.</summary>
        /// <returns><c>True</c> if in design mode, else <c>False</c>.</returns>
        public bool IsInDesignMode
        {
            get { return DesignerProperties.GetIsInDesignMode(this); }
        }

        /// <summary>Gets the used ResourceManagers with their corresponding <c>namespaces</c>.</summary>
        public Dictionary<string, ResourceManager> ResourceManagerList { get; private set; }

        /// <summary>
        ///   Gets the specific <c>CultureInfo</c> of the current culture.This can be used for format manners.If the
        ///   Culture is an invariant <c>CultureInfo</c>, SpecificCulture will also return an invariant
        ///   <c>CultureInfo</c>.
        /// </summary>
        public CultureInfo SpecificCulture
        {
            get { return CultureInfo.CreateSpecificCulture(this.Culture.ToString()); }
        }

        /// <summary>Gets the default <c>CultureInfo</c> to initialize the LocalizeDictionary. <c>CultureInfo</c>.</summary>
        static CultureInfo DefaultCultureInfo
        {
            get { return CultureInfo.InvariantCulture; }
        }

        /// <summary>Attach an WeakEventListener to the LocalizeDictionary.</summary>
        /// <param name="listener">The listener to attach.</param>
        public static void AddEventListener(IWeakEventListener listener)
        {
            // calls AddListener from the inline WeakCultureChangedEventManager
            WeakCultureChangedEventManager.AddListener(listener);
        }

        /// <summary>Returns the <c>AssemblyName</c> of the passed assembly instance.</summary>
        /// <param name="assembly">The Assembly where to get the name from.</param>
        /// <returns>The Assembly name.</returns>
        public static string GetAssemblyName(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(@"assembly");
            }

            return assembly.FullName.Split(',')[0];
        }

        /// <summary>
        ///   Getter of <c>DependencyProperty</c> Culture.Only supported at DesignTime.If its in Runtime,
        ///   LocalizeDictionary.Culture will be returned.
        /// </summary>
        /// <param name="obj">The dependency object to get the design culture from.</param>
        /// <returns>The design culture at design time or the current culture at runtime.</returns>
        [DesignOnly(true)]
        public static string GetDesignCulture(DependencyObject obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return Instance.IsInDesignMode ? (string)obj.GetValue(DesignCultureProperty) : Instance.Culture.ToString();
        }

        /// <summary>Parses a key and return the parts of it.</summary>
        /// <param name="inKey">The key to parse.</param>
        /// <param name="outAssembly">The found or default assembly.</param>
        /// <param name="outDictionary">The found or default dictionary.</param>
        /// <param name="outKey">The found or default key.</param>
        public static void ParseKey(string inKey, out string outAssembly, out string outDictionary, out string outKey)
        {
            // reset the vars to null
            outAssembly = null;
            outDictionary = null;
            outKey = null;

            // its a assembly/dict/key pair
            if (!string.IsNullOrEmpty(inKey))
            {
                string[] split = inKey.Trim().Split(":".ToCharArray(), 3);

                // assembly:dict:key
                if (split.Length == 3)
                {
                    outAssembly = !string.IsNullOrEmpty(split[0]) ? split[0] : null;
                    outDictionary = !string.IsNullOrEmpty(split[1]) ? split[1] : null;
                    outKey = split[2];
                }

                // dict:key assembly = ExecutingAssembly
                if (split.Length == 2)
                {
                    outDictionary = !string.IsNullOrEmpty(split[0]) ? split[0] : null;
                    outKey = split[1];
                }

                // key assembly = ExecutingAssembly dict = standard resource dictionary
                if (split.Length == 1)
                {
                    outKey = split[0];
                }
            }
            else
            {
                // if the passed value is null pr empty, throw an exception if in runtime
                if (!Instance.IsInDesignMode)
                {
                    throw new ArgumentNullException("inKey");
                }
            }
        }

        /// <summary>Detach an WeakEventListener to the LocalizeDictionary.</summary>
        /// <param name="listener">The listener to detach.</param>
        public static void RemoveEventListener(IWeakEventListener listener)
        {
            // calls RemoveListener from the inline WeakCultureChangedEventManager
            WeakCultureChangedEventManager.RemoveListener(listener);
        }

        /// <summary>Setter of <c>DependencyProperty</c> Culture.Only supported at DesignTime.</summary>
        /// <param name="obj">The dependency object to set the culture to.</param>
        /// <param name="value">The odds format.</param>
        [DesignOnly(true)]
        public static void SetDesignCulture(DependencyObject obj, string value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (Instance.IsInDesignMode)
            {
                obj.SetValue(DesignCultureProperty, value);
            }
        }

        /// <summary>
        ///   Returns an object from the passed dictionary with the given name.If a wrong <typeparamref name="TType" />
        ///   is passed, no exception will get thrown (return obj as <typeparam
        /// refname="TType" />).
        /// </summary>
        /// <param name="resourceAssembly">The Assembly where the Resource is located at.</param>
        /// <param name="resourceDictionary">Name of the resource directory.</param>
        /// <param name="resourceKey">The key for the resource.</param>
        /// <param name="cultureToUse">The culture to use.</param>
        /// <returns>The found object or <c>null</c> if not found or wrong <typeparamref name="TType" /> is given.</returns>
        /// <typeparam name="TType">The type of object to get.</typeparam>
        public TType GetLocalizedObject<TType>(
            string resourceAssembly, string resourceDictionary, string resourceKey, CultureInfo cultureToUse)
            where TType : class
        {
            // Validation
            if (resourceAssembly == null)
            {
                throw new ArgumentNullException("resourceAssembly");
            }

            if (resourceAssembly != null && string.IsNullOrEmpty(resourceAssembly))
            {
                throw new ArgumentException("resourceAssembly is empty", "resourceAssembly");
            }

            if (resourceDictionary == null)
            {
                throw new ArgumentNullException("resourceDictionary");
            }

            if (resourceDictionary != null && string.IsNullOrEmpty(resourceDictionary))
            {
                throw new ArgumentException("resourceDictionary is empty", "resourceDictionary");
            }

            if (string.IsNullOrEmpty(resourceKey))
            {
                if (this.IsInDesignMode)
                {
                    return null;
                }

                if (resourceKey == null)
                {
                    throw new ArgumentNullException("resourceKey");
                }

                if (string.IsNullOrEmpty(resourceKey))
                {
                    throw new ArgumentException("resourceKey is empty", "resourceKey");
                }
            }

            // declaring local ResourceManager
            ResourceManager resManager;

            // try to get the resource manager
            try
            {
                resManager = this.GetResourceManager(resourceAssembly, resourceDictionary, resourceKey);
            }
            catch
            {
                // if an error occur, throw exception, if in runtime
                if (this.IsInDesignMode)
                {
                    return null;
                }

                throw;
            }

            // gets the resource object with the chosen localization
            object retVal = resManager.GetObject(resourceKey, cultureToUse) as TType;

            // if the retVal is null, throw exception, if in runtime
            if (retVal == null && this.IsInDesignMode == false)
            {
                throw new ArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture, 
                        "No resource key with name '{0}' in dictionary '{1}' in assembly '{2}' found! ({2}.{1}.{0})", 
                        resourceKey, 
                        resourceDictionary, 
                        resourceAssembly));
            }

            // finally, return the searched object as type of the generic type
            return retVal as TType;
        }

        /// <summary>
        ///   Looks up the ResourceManagers for the searched <paramref name="resourceKey" /> in the <param
        /// refname="resourceDictionary" /> in the <paramref name="resourceAssembly" />with an Invariant Culture.
        /// </summary>
        /// <param name="resourceAssembly">The resource assembly (e.g.: <c>BaseLocalizeExtension</c>). <c>null</c> = Name of the executing assembly.</param>
        /// <param name="resourceDictionary">The dictionary to look up (e.g.: ResHelp, Resources, ...). <c>null</c> = Name of the default resource file (Resources).</param>
        /// <param name="resourceKey">The key of the searched entry (e.g.: <c>btnHelp</c>, Cancel, ...). <c>null</c> = Exception.</param>
        /// <returns><c>True</c> if the searched one is found, otherwise <c>False</c>.</returns>
        public bool ResourceKeyExists(string resourceAssembly, string resourceDictionary, string resourceKey)
        {
            return this.ResourceKeyExists(
                resourceAssembly, resourceDictionary, resourceKey, CultureInfo.InvariantCulture);
        }

        /// <summary>Callback function. Used to set the LocalizeDictionary.Culture if set in Xaml.Only supported at DesignTime.</summary>
        /// <param name="obj">The dependency object.</param>
        /// <param name="args">The event argument.</param>
        [DesignOnly(true)]
        static void SetCultureFromDependencyProperty(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (!Instance.IsInDesignMode)
            {
                return;
            }

            CultureInfo culture;

            try
            {
                culture = CultureInfo.GetCultureInfo((string)args.NewValue);
            }
            catch
            {
                if (Instance.IsInDesignMode)
                {
                    culture = DefaultCultureInfo;
                }
                else
                {
                    throw;
                }
            }

            if (culture != null)
            {
                Instance.Culture = culture;
            }
        }

        /// <summary>Looks up in the cached <c>ResourceManager</c> list for the searched <c>ResourceManager</c>.</summary>
        /// <param name="resourceAssembly">The resource assembly (e.g.: <c>BaseLocalizeExtension</c>). <c>null</c> = Name of the executing assembly.</param>
        /// <param name="resourceDictionary">The dictionary to look up (e.g.: ResHelp, Resources, ...). <c>null</c> = Name of the default resource file (Resources).</param>
        /// <param name="resourceKey">The key of the searched entry (e.g.: <c>btnHelp</c>, Cancel, ...). <c>null</c> = Exception.</param>
        /// <returns>The found <c>ResourceManager</c>.</returns>
        ResourceManager GetResourceManager(string resourceAssembly, string resourceDictionary, string resourceKey)
        {
            if (resourceAssembly == null)
            {
                resourceAssembly = GetAssemblyName(Assembly.GetExecutingAssembly());
            }

            if (resourceDictionary == null)
            {
                resourceDictionary = ResourcesName;
            }

            if (string.IsNullOrEmpty(resourceKey))
            {
                throw new ArgumentNullException("resourceKey");
            }

            Assembly assembly = null;
            ResourceManager resManager;
            string foundResource = null;
            string resManagerNameToSearch = "." + resourceDictionary + ResourceFileExtension;

            if (this.ResourceManagerList.ContainsKey(resourceAssembly + resManagerNameToSearch))
            {
                resManager = this.ResourceManagerList[resourceAssembly + resManagerNameToSearch];
            }
            else
            {
                // if the assembly cannot be loaded, throw an exception go through every assembly loaded in the app
                // domain
                foreach (var assemblyInAppDomain in
                    from assemblyInAppDomain in AppDomain.CurrentDomain.GetAssemblies()
                    where true
                    let assemblyName = new AssemblyName(assemblyInAppDomain.FullName)
                    where assemblyName.Name == resourceAssembly
                    select assemblyInAppDomain)
                {
                    // assigned the assembly
                    assembly = assemblyInAppDomain;

                    // stop the search here
                    break;
                }

                // check if the assembly is still null
                if (assembly == null)
                {
                    // assign the loaded assembly
                    assembly = Assembly.Load(new AssemblyName(resourceAssembly));
                }

                // availableResources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                string[] availableResources = assembly.GetManifestResourceNames();

                // search for the best fitting resource file. pattern: ".{NAME}.resources"
                foreach (string t in
                    availableResources.Where(
                        t =>
                        t.StartsWith(resourceAssembly + ".", StringComparison.OrdinalIgnoreCase)
                        && t.EndsWith(resManagerNameToSearch, StringComparison.OrdinalIgnoreCase)))
                {
                    // take the first occurrence and break
                    foundResource = t;
                    break;
                }

                // if no one was found, exception
                if (foundResource == null)
                {
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.CurrentCulture, 
                            "No resource key with name '{0}' in dictionary '{1}' in assembly '{2}' found! ({2}.{1}.{0})", 
                            resourceKey, 
                            resourceDictionary, 
                            resourceAssembly));
                }

                // remove ".resources" from the end
                foundResource = foundResource.Substring(0, foundResource.Length - ResourceFileExtension.Length);

                //// Resources.{foundResource}.ResourceManager.GetObject()
                //// ^^ prop-info ^^ method get

                try
                {
                    // get the property info from resManager over the type from foundResource
                    PropertyInfo propInfo = assembly.GetType(foundResource).GetProperty(
                        ResourceManagerName, ResourceBindingFlags);

                    // get the GET-method from the method info
                    MethodInfo methodInfo = propInfo.GetGetMethod(true);

                    // get the static ResourceManager property
                    object resManObject = methodInfo.Invoke(null, null);
                    // cast it to a Resource Manager for better working with
                    resManager = (ResourceManager)resManObject;
                }
                catch (Exception ex)
                {
                    // this error has to get thrown because this has to work
                    throw new InvalidOperationException("Cannot resolve the Resource Manager!", ex);
                }

                // Add the ResourceManager to the cache list
                this.ResourceManagerList.Add(resourceAssembly + resManagerNameToSearch, resManager);
            }

            // return the found ResourceManager
            return resManager;
        }

        /// <summary>
        ///   Looks up the ResourceManagers for the searched <paramref name="resourceKey" />in the <param
        /// refname="resourceDictionary" /> in the <paramref name="resourceAssembly" />with the passed culture. If the
        /// searched one does not exists with the passed culture, is will searched until the invariant culture is used.
        /// </summary>
        /// <param name="resourceAssembly">The resource assembly (e.g.: <c>BaseLocalizeExtension</c>). <c>null</c> = Name of the executing assembly.</param>
        /// <param name="resourceDictionary">The dictionary to look up (e.g.: ResHelp, Resources, ...). <c>null</c> = Name of the default resource file (Resources).</param>
        /// <param name="resourceKey">The key of the searched entry (e.g.: <c>btnHelp</c>, Cancel, ...). <c>null</c> = Exception.</param>
        /// <param name="cultureToUse">The culture to use.</param>
        /// <returns><c>True</c> if the searched one is found, otherwise <c>False</c>.</returns>
        bool ResourceKeyExists(
            string resourceAssembly, string resourceDictionary, string resourceKey, CultureInfo cultureToUse)
        {
            try
            {
                return this.GetResourceManager(resourceAssembly, resourceDictionary, resourceKey).GetObject(
                    resourceKey, cultureToUse) != null;
            }
            catch
            {
                if (this.IsInDesignMode)
                {
                    return false;
                }

                throw;
            }
        }
    }
}