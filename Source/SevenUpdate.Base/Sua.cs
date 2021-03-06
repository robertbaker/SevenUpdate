// <copyright file="Sua.cs" project="SevenUpdate.Base">Robert Baker</copyright>
// <license href="http://www.gnu.org/licenses/gpl-3.0.txt" name="GNU General Public License 3" />

namespace SevenUpdate
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    using ProtoBuf;

    /// <summary>Seven Update Application information.</summary>
    [ProtoContract]
    [DataContract(IsReference = true)]
    [KnownType(typeof(ObservableCollection<LocaleString>))]
    public sealed class Sua : INotifyPropertyChanged
    {
        /// <summary>The <c>Uri</c> for the application's website.</summary>
        string appUrl;

        /// <summary>The directory where the application is installed.</summary>
        string directory;

        /// <summary>The help website <c>Uri</c> of the application.</summary>
        string helpUrl;

        /// <summary>Indicates whether the SUA is enabled with Seven Update (SDK does not use this value).</summary>
        bool isEnabled;

        /// <summary>Indicates the cpu platform the application can run under.</summary>
        Platform platform;

        /// <summary>The <c>Uri</c> pointing to the sui file containing the application updates.</summary>
        string suiUrl;

        /// <summary>The name of the value to the registry key that contains the application directory location.</summary>
        string valueName;

        /// <summary>Initializes a new instance of the <see cref="Sua" /> class.</summary>
        /// <param name="name">The collection of localized update names.</param>
        /// <param name="publisher">The collection of localized publisher names.</param>
        /// <param name="description">The collection of localized update descriptions.</param>
        public Sua(
            ObservableCollection<LocaleString> name, 
            ObservableCollection<LocaleString> publisher, 
            ObservableCollection<LocaleString> description)
        {
            this.Name = name;
            this.Publisher = publisher;
            this.Description = description;

            if (this.Name == null)
            {
                this.Name = new ObservableCollection<LocaleString>();
            }

            if (this.Description == null)
            {
                this.Description = new ObservableCollection<LocaleString>();
            }

            if (this.Publisher == null)
            {
                this.Publisher = new ObservableCollection<LocaleString>();
            }
        }

        /// <summary>Initializes a new instance of the <see cref="Sua" /> class.</summary>
        /// <param name="name">The collection of localized update names.</param>
        /// <param name="publisher">The collection of localized publisher names.</param>
        public Sua(ObservableCollection<LocaleString> name, ObservableCollection<LocaleString> publisher)
        {
            this.Name = name;
            this.Publisher = publisher;

            if (this.Name == null)
            {
                this.Name = new ObservableCollection<LocaleString>();
            }

            if (this.Publisher == null)
            {
                this.Publisher = new ObservableCollection<LocaleString>();
            }

            this.Description = new ObservableCollection<LocaleString>();

            this.Name.CollectionChanged -= this.NameCollectionChanged;
            this.Description.CollectionChanged -= this.DescriptionCollectionChanged;
            this.Publisher.CollectionChanged -= this.PublisherCollectionChanged;

            this.Name.CollectionChanged += this.NameCollectionChanged;
            this.Description.CollectionChanged += this.DescriptionCollectionChanged;
            this.Publisher.CollectionChanged += this.PublisherCollectionChanged;
        }

        /// <summary>Initializes a new instance of the <see cref="Sua" /> class.</summary>
        public Sua()
        {
            this.Name = new ObservableCollection<LocaleString>();
            this.Description = new ObservableCollection<LocaleString>();
            this.Publisher = new ObservableCollection<LocaleString>();
        }

        /// <summary>Occurs when a property has changed.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Gets or sets the <c>Uri</c> for the application's website.</summary>
        /// <value>The application website.</value>
        [ProtoMember(8, IsRequired = false)]
        [DataMember]
        public string AppUrl
        {
            get { return this.appUrl; }

            set
            {
                this.appUrl = value;

                // Call OnPropertyChanged whenever the property is updated
                this.OnPropertyChanged("AppUrl");
            }
        }

        /// <summary>Gets the collection of localized descriptions for the application.</summary>
        /// <value>The application description.</value>
        [ProtoMember(2)]
        [DataMember]
        public ObservableCollection<LocaleString> Description { get; private set; }

        /// <summary>Gets or sets the directory where the application is installed.</summary>
        /// <value>The install directory.</value>
        [ProtoMember(3)]
        [DataMember]
        public string Directory
        {
            get { return this.directory; }

            set
            {
                this.directory = value;

                // Call OnPropertyChanged whenever the property is updated
                this.OnPropertyChanged("Directory");
            }
        }

        /// <summary>Gets or sets the help website <c>Uri</c> of the application.</summary>
        /// <value>The help and support website for the application.</value>
        [ProtoMember(9, IsRequired = false)]
        [DataMember]
        public string HelpUrl
        {
            get { return this.helpUrl; }

            set
            {
                this.helpUrl = value;

                // Call OnPropertyChanged whenever the property is updated
                this.OnPropertyChanged("HelpUrl");
            }
        }

        /// <summary>
        ///   Gets or sets a value indicating whether the SUA is enabled with Seven Update (SDK does not use this
        ///   value).
        /// </summary>
        /// <value><c>True</c> if this instance is enabled; otherwise, <c>False</c>.</value>
        [ProtoMember(5)]
        [DataMember]
        public bool IsEnabled
        {
            get { return this.isEnabled; }

            set
            {
                this.isEnabled = value;

                // Call OnPropertyChanged whenever the property is updated
                this.OnPropertyChanged("IsEnabled");
            }
        }

        /// <summary>Gets the collection of localized application names.</summary>
        /// <value>The name of the application localized.</value>
        [ProtoMember(1)]
        [DataMember]
        public ObservableCollection<LocaleString> Name { get; private set; }

        /// <summary>Gets or sets the cpu platform the application can run under.</summary>
        [ProtoMember(11)]
        [DataMember]
        public Platform Platform
        {
            get { return this.platform; }

            set
            {
                this.platform = value;

                // Call OnPropertyChanged whenever the property is updated
                this.OnPropertyChanged("Platform");
            }
        }

        /// <summary>Gets the collection of localized publisher names.</summary>
        /// <value>The publisher.</value>
        [ProtoMember(6)]
        [DataMember]
        public ObservableCollection<LocaleString> Publisher { get; private set; }

        /// <summary>Gets or sets the <c>Uri</c> pointing to the sui file containing the application updates.</summary>
        /// <value>The url pointing to the sui file.</value>
        [ProtoMember(7)]
        [DataMember]
        public string SuiUrl
        {
            get { return this.suiUrl; }

            set
            {
                this.suiUrl = value;

                // Call OnPropertyChanged whenever the property is updated
                this.OnPropertyChanged("SuiUrl");
            }
        }

        /// <summary>Gets or sets the name of the value to the registry key that contains the application directory location.</summary>
        /// <value>The name of the value.</value>
        [ProtoMember(10, IsRequired = false)]
        [DataMember]
        public string ValueName
        {
            get { return this.valueName; }

            set
            {
                this.valueName = value;

                // Call OnPropertyChanged whenever the property is updated
                this.OnPropertyChanged("ValueName");
            }
        }

        /// <summary>Fires the OnPropertyChanged Event with the collection changes.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The event data.</param>
        void DescriptionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged("Description");
        }

        /// <summary>Fires the OnPropertyChanged Event with the collection changes.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The event data.</param>
        void NameCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged("Name");
        }

        /// <summary>When a property has changed, call the <c>OnPropertyChanged</c> Event.</summary>
        /// <param name="propertyName">The name of the property.</param>
        void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>Fires the OnPropertyChanged Event with the collection changes.</summary>
        /// <param name="sender">The object that called the event.</param>
        /// <param name="e">The event data.</param>
        void PublisherCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnPropertyChanged("Publisher");
        }
    }
}