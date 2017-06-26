﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using IniParser;
using IniParser.Model;
using WPF.JoshSmith.ServiceProviders.UI;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace FMM2
{
    public class Mod : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string Version { get; set; } = "";
        public string Desc { get; set; } = "";
        public string LongDesc { get; set; } = "";
        public string Warnings { get; set; } = "";
        public string LongWarnings { get; set; } = "";
        public BitmapImage Icon { get; set; } = new BitmapImage();
        public string Url { get; set; } = "";
        public BitmapImage Image { get; set; } = new BitmapImage();
        public string ImageFull { get; set; } = "";
        public string Location { get; set; } = "";
        public string RevisionDate { get; set; } = "";
        public string EDVersion { get; set; } = "";
        public string Credits { get; set; } = "";
        public string Required { get; set; } = "";
        
        private bool _checked;
        public bool IsChecked {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;
                NotifyPropertyChanged("IsChecked");
            }
        }

        public Mod()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

    public class Server : INotifyPropertyChanged
    {
        // Initializing these to a value so that no errors related to null objects appear in ListView3 updating
        // this is a problem because some values are not parsed in the server info json if they are null, so they remain null.
        public string name { get; set; } = "";
        public int port { get; set; } = 0;
        public string hostPlayer { get; set; } = "";
        public bool isDedicated { get; set; } = false;
        public int sprintEnabled { get; set; } = 0;
        public bool sprintEnabledBool
        {
            get
            {
                if (sprintEnabled == 1)
                    return true;
                else
                    return false;
            }
        }
        public int sprintUnlimitedEnabled { get; set; } = 0;
        public int assassinationEnabled { get; set; } = 0;
        public bool assassinationEnabledBool
        {
            get
            {
                if (assassinationEnabled == 1)
                    return true;
                else
                    return false;
            }
        }
        public bool VoIP { get; set; } = false;
        public bool teams { get; set; } = false;
        private string _map;
        public string map
        {
            get
            {
                if (_map == "" || _map == "None" || _map == "none")
                {
                    return char.ToUpper(mapFile[0]) + mapFile.Substring(1);
                }
                else
                {
                    return _map;
                }
            }
            set
            {
                _map = value;
            }
        }
        public string mapFile { get; set; } = "";
        private string _variant;
        public string variant
        {
            get
            {
                if (_variant == "" || _variant == "None" || _variant == "none")
                {
                    return variantType;
                }
                else
                {
                    return _variant;
                }
            }
            set
            {
                _variant = value;
            }
        }
        private string _variantType;
        public string variantType
        {
            get
            {
                if (_variantType == "" || _variantType == "None" || _variantType == "none")
                {
                    return "Slayer";
                }
                else
                {
                    return _variantType;
                }
            }
            set
            {
                _variantType = value;
            }
        }
        public string status { get; set; } = "";
        public int numPlayers { get; set; } = 0;
        public int maxPlayers { get; set; } = 0;
        public List<Player> players { get; set; } = new List<Player> { };
        public List<string> mods { get; set; } = new List<string> { };
        public string gameVersion { get; set; } = "";
        public string eldewritoVersion { get; set; } = "";
        public string ipAddress { get; set; } = "";
        // intializing this to "99999" so that if pinging a server fails, it gets set to "99999"
        // so that sorting the list by the "ping" column sorts failed pings to the bottom.
        public string ping { get; set; } = "99999";
        public string passworded { get; set; } = "";

        private bool _checked;
        public bool IsChecked
        {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;
                NotifyPropertyChanged("IsChecked");
            }
        }

        public Server()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
    public class Result
    {
        public int code { get; set; }
        public string msg { get; set; }
        public List<string> servers { get; set; }
    }
    public class MasterServer
    {
        public int listVersion { get; set; }
        public Result result { get; set; }
    }

    public class Player
    {
        // Initializing these to a value so that no errors related to null objects appear in ListView3 updating
        // this is a problem because some values are not parsed in the server info json if they are null, so they remain null.
        public string name { get; set; } = "";
        public int score { get; set; } = 0;
        public int kills { get; set; } = 0;
        public int assists { get; set; } = 0;
        public int deaths { get; set; } = 0;
        public int team { get; set; } = 0;

        public bool isAlive { get; set; } = false;
        public string uid { get; set; } = "";
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // global settings
        bool installListOrder = true;
        bool offlineMode = false;
        bool developerMode = false;
        // end


        public static Random Rnd = new Random();
        ObservableCollection<Mod> mMods { get; set; }
        ObservableCollection<Mod> dMods { get; set; }
        ObservableCollection<Server> servers { get; set; }
        BackgroundWorker workerDownloadInis = new BackgroundWorker();
        BackgroundWorker workerDownloadMods = new BackgroundWorker();
        BackgroundWorker workerFindServers = new BackgroundWorker();

        string repository = "https://github.com/Clef-0/FMM-Mods/trunk/";

        public MainWindow()
        {
            InitializeComponent();
            mMods = new ObservableCollection<Mod>();
            dMods = new ObservableCollection<Mod>();
            servers = new ObservableCollection<Server>();
            myModsList.ItemsSource = mMods;
            downloadableModsList.ItemsSource = dMods;
            serverBrowserList.ItemsSource = servers;
            this.DataContext = this;

            mMods.CollectionChanged += new NotifyCollectionChangedEventHandler(tabsUpdateStatus);
            dMods.CollectionChanged += new NotifyCollectionChangedEventHandler(tabsUpdateStatus);
            servers.CollectionChanged += new NotifyCollectionChangedEventHandler(tabsUpdateStatus);

            // add handlers for workers
            workerDownloadInis.DoWork += new DoWorkEventHandler(populateInstallerDLList);
            workerDownloadMods.DoWork += new DoWorkEventHandler(dlModWorker);
            workerFindServers.DoWork += new DoWorkEventHandler(serverBrowserWorker_DoWork);
            workerFindServers.RunWorkerCompleted += new RunWorkerCompletedEventHandler(serverBrowserWorker_RunWorkerCompleted);

            try
            {
                using (new FileStream("FMM.ini", FileMode.CreateNew)) { }
            }
            catch { }   
            loadFMMSettings();
            if (readFMMIni("FirstTime") != "false")
            {
                writeFMMIni("FirstTime", "false");
            }
            else
            {
                myModsAlert.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            new ListViewDragDropManager<Mod>(myModsList);

            downloadableModsAlert.Visibility = Visibility.Collapsed;

            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "mods"));
            lookDirectory(Path.Combine(Environment.CurrentDirectory, "mods")); // populates local mod list
            workerDownloadInis.RunWorkerAsync(); //populate dl mod list
            workerFindServers.RunWorkerAsync();

            // hide infobar
            // {
            infobarScroll.Visibility = Visibility.Collapsed;
            
            infobarDLScroll.Visibility = Visibility.Collapsed;
            // }
        }

        private void myModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (myModsList.SelectedItems.Count > 0)
            {
                infobarScroll.Visibility = Visibility.Visible;

                Mod item = (Mod)myModsList.SelectedItems[0];

                infobarName.Text = item.Name;
                infobarVersion.Text = item.Version;
                infobarAuthor.Text = item.Author;
                if (item.Credits != null && item.Credits != "")
                {
                    infobarCredits.Visibility = Visibility.Visible;
                    infobarCredits.Text = item.Credits;
                }
                else
                {
                    infobarCredits.Visibility = Visibility.Collapsed;
                }
                if (item.RevisionDate != null && item.RevisionDate != "")
                {
                    infobarRevisionDate.Visibility = Visibility.Visible;
                    infobarRevisionDate.Text = "Last revision: " + item.RevisionDate;
                }
                else
                {
                    infobarRevisionDate.Visibility = Visibility.Collapsed;
                }
                if (item.EDVersion != null && item.EDVersion != "")
                {
                    infobarEDVersion.Visibility = Visibility.Visible;
                    infobarEDVersion.Text = "ElDewrito version: " + item.EDVersion;
                }
                else
                {
                    infobarEDVersion.Visibility = Visibility.Collapsed;
                }
                if (item.LongDesc != null && item.LongDesc != "")
                {
                    infobarDescription.Visibility = Visibility.Visible;
                    infobarDescription.Text = item.LongDesc;
                }
                else if (item.Desc != null && item.Desc != "")
                {
                    infobarDescription.Visibility = Visibility.Visible;
                    infobarDescription.Text = item.Desc;
                }
                else
                {
                    infobarDescription.Visibility = Visibility.Collapsed;
                }

                if (item.LongWarnings != null && item.LongWarnings != "")
                {
                    infobarWarnings.Visibility = Visibility.Visible;
                    infobarWarnings.Text = item.LongWarnings;
                }
                else if (item.Warnings != null && item.Warnings != "")
                {
                    infobarWarnings.Visibility = Visibility.Visible;
                    infobarWarnings.Text = item.Warnings;
                }
                else
                {
                    infobarWarnings.Visibility = Visibility.Collapsed;
                }

                if (item.Icon != null)
                {
                    infobarIcon.Visibility = Visibility.Visible;
                    infobarIcon.Source = item.Icon;
                }
                else
                {
                    infobarIcon.Visibility = Visibility.Collapsed;
                }

                if (item.Image != null)
                {
                    infobarImage.Visibility = Visibility.Visible;
                    infobarImage.Source = item.Image;
                }
                else
                {
                    infobarImage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void downloadableModsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (downloadableModsList.SelectedItems.Count > 0)
            {
                infobarDLScroll.Visibility = Visibility.Visible;

                Mod item = (Mod)downloadableModsList.SelectedItems[0];
                infobarDLName.Text = item.Name;
                infobarDLVersion.Text = item.Version;
                infobarDLAuthor.Text = item.Author;
                if (item.Credits != null && item.Credits != "")
                {
                    infobarDLCredits.Visibility = Visibility.Visible;
                    infobarDLCredits.Text = item.Credits;
                }
                else
                {
                    infobarDLCredits.Visibility = Visibility.Collapsed;
                }
                if (item.RevisionDate != null && item.RevisionDate != "")
                {
                    infobarDLRevisionDate.Visibility = Visibility.Visible;
                    infobarDLRevisionDate.Text = "Last revision: " + item.RevisionDate;
                }
                else
                {
                    infobarDLRevisionDate.Visibility = Visibility.Collapsed;
                }
                if (item.EDVersion != null && item.EDVersion != "")
                {
                    infobarDLEDVersion.Visibility = Visibility.Visible;
                    infobarDLEDVersion.Text = "ElDewrito version: " + item.EDVersion;
                }
                else
                {
                    infobarDLEDVersion.Visibility = Visibility.Collapsed;
                }
                if (item.LongDesc != null && item.LongDesc != "")
                {
                    infobarDLDescription.Visibility = Visibility.Visible;
                    infobarDLDescription.Text = item.LongDesc;
                }
                else if (item.Desc != null && item.Desc != "")
                {
                    infobarDLDescription.Visibility = Visibility.Visible;
                    infobarDLDescription.Text = item.Desc;
                }
                else
                {
                    infobarDLDescription.Visibility = Visibility.Collapsed;
                }

                if (item.LongWarnings != null && item.LongWarnings != "")
                {
                    infobarDLWarnings.Visibility = Visibility.Visible;
                    infobarDLWarnings.Text = item.LongWarnings;
                }
                else if (item.Warnings != null && item.Warnings != "")
                {
                    infobarDLWarnings.Visibility = Visibility.Visible;
                    infobarDLWarnings.Text = item.Warnings;
                }
                else
                {
                    infobarDLWarnings.Visibility = Visibility.Collapsed;
                }

                if (item.Icon != null)
                {
                    infobarDLIcon.Visibility = Visibility.Visible;
                    infobarDLIcon.Source = item.Icon;
                }
                else
                {
                    infobarDLIcon.Visibility = Visibility.Collapsed;
                }

                if (item.Image != null)
                {
                    infobarDLImage.Visibility = Visibility.Visible;
                    infobarDLImage.Source = item.Image;
                }
                else
                {
                    infobarDLImage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void lookDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
                lookFile(fileName);

            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                lookDirectory(subdirectory);
        }

        private void lookFile(string path)
        {
            if (Path.GetExtension(path) == ".ini")
            {
                if (File.Exists(path.Replace(".ini", ".fm")))
                {
                    var parser = new FileIniDataParser();
                    IniData data = parser.ReadFile(path);
                    Mod newMod = new Mod();
                    newMod.Name = data["FMMInfo"]["Name"];
                    newMod.Author = data["FMMInfo"]["Author"];
                    newMod.Version = data["FMMInfo"]["Version"];
                    newMod.Desc = data["FMMInfo"]["Desc"];
                    newMod.LongDesc = data["FMMInfo"]["LongDesc"];
                    newMod.Warnings = data["FMMInfo"]["Warnings"];
                    newMod.LongWarnings = data["FMMInfo"]["LongWarnings"];
                    newMod.Url = data["FMMInfo"]["Url"];
                    newMod.ImageFull = data["FMMInfo"]["ImageFull"];
                    newMod.EDVersion = data["FMMInfo"]["EDVersion"];
                    newMod.RevisionDate = data["FMMInfo"]["RevisionDate"];
                    newMod.Credits = data["FMMInfo"]["Credits"];
                    newMod.Required = data["FMMInfo"]["Required"];
                    string iconUriString = data["FMMInfo"]["Icon"];
                    Uri iconUri = null;
                    Uri.TryCreate(iconUriString, UriKind.Absolute, out iconUri);
                    if (iconUri != null)
                    {
                        newMod.Icon = new BitmapImage(iconUri);
                    }
                    else
                    {
                        newMod.Icon = null;
                    }
                    string imageUriString = data["FMMInfo"]["ImageThumb"];
                    Uri imageUri = null;
                    Uri.TryCreate(imageUriString, UriKind.Absolute, out imageUri);
                    if (imageUri != null)
                    {
                        try
                        {
                            newMod.Image = new BitmapImage(imageUri);
                        }
                        catch
                        {
                            newMod.Image = null;
                        }
                    }
                    else
                    {
                        newMod.Image = null;
                    }
                    newMod.Location = path.Replace(".ini", ".fm");

                    mMods.Add(newMod);
                }
            }
        }

        private void infobarUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = myModsList.SelectedIndex;

            if (selectedIndex > 0)
            {
                Mod itemToMoveUp = mMods[selectedIndex];
                mMods.RemoveAt(selectedIndex);
                mMods.Insert(selectedIndex - 1, itemToMoveUp);
                myModsList.SelectedIndex = selectedIndex - 1;
            }
        }

        private void infobarDn_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = myModsList.SelectedIndex;
            if (selectedIndex + 1 < mMods.Count)
            {
                Mod itemToMoveDown = mMods[selectedIndex];
                mMods.RemoveAt(selectedIndex);
                mMods.Insert(selectedIndex + 1, itemToMoveDown);
                myModsList.SelectedIndex = selectedIndex + 1;
            }
        }

        private void infobarDel_Click(object sender, RoutedEventArgs e)
        {
            string sMessageBoxText = "Are you sure you want to delete " + ((Mod)myModsList.SelectedItem).Name + "?" + Environment.NewLine + "This cannot be undone.";
            string sCaption = "Foundation Mod Manager";
            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage icnMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, icnMessageBox);

            switch (rsltMessageBox)
            {
                case MessageBoxResult.Yes:
                    string localLocation = Path.GetDirectoryName(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "mods", "tagmods", ((Mod)myModsList.SelectedItem).Location.Replace("/", "\\")));

                    try
                    {
                        deleteDirectory(localLocation);
                    }
                    catch
                    {
                        // mod folder doesn't exist or user not elevated. oh well.
                    }

                    mMods.Clear();
                    lookDirectory(Path.Combine(Environment.CurrentDirectory, "mods"));
                    break;
            }
        }

        private void myModsAlertHide_Click(object sender, RoutedEventArgs e)
        {
            myModsAlert.Visibility = Visibility.Collapsed;
        }

        private void downloadableModsAlertHide_Click(object sender, RoutedEventArgs e)
        {
            downloadableModsAlert.Visibility = Visibility.Collapsed;
        }

        private void serverBrowserAlertHide_Click(object sender, RoutedEventArgs e)
        {
            serverBrowserAlert.Visibility = Visibility.Collapsed;
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (mainTabs.SelectedIndex == 0)
            {
                if (modsTabs.SelectedIndex == 0)
                {
                    mMods.Clear();
                    lookDirectory(Path.Combine(Environment.CurrentDirectory, "mods"));
                }
                else if (modsTabs.SelectedIndex == 1)
                {
                    if (!workerDownloadInis.IsBusy)
                    {
                        dMods.Clear();
                        workerDownloadInis.RunWorkerAsync(); //populate dl mod list

                        infobarDLScroll.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private void myModsList_HeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (true)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        string header = headerClicked.Column.Header as string;

                        myModsSort(header, direction);

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
        }
        private void myModsSort(string sortBy, ListSortDirection direction)
        {
            if (sortBy != null)
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(myModsList.ItemsSource);

                List<Mod> checkedMods = new List<Mod>();

                foreach (Mod item in dataView)
                {
                    if (item.IsChecked == true)
                    {
                        checkedMods.Add(item);
                    }
                }

                dataView.SortDescriptions.Clear();
                SortDescription sd = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();

                ObservableCollection<Mod> newMM = new ObservableCollection<Mod>();
                foreach (Mod item in dataView)
                {
                    newMM.Add(item);
                }

                mMods.Clear();

                foreach (Mod item in newMM)
                {
                    mMods.Add(item);
                }

                checkedMods.Reverse();
                foreach (Mod item in checkedMods)
                {
                    Mod itemToMoveUp = mMods[mMods.IndexOf(item)];
                    mMods.RemoveAt(mMods.IndexOf(item));
                    mMods.Insert(0, itemToMoveUp);
                }
                dataView.SortDescriptions.Clear();
            }
            else
            {
                bool uncheck = true;
                foreach (Mod mod in mMods)
                {
                    if (mod.IsChecked == false)
                    {
                        uncheck = false;
                    }
                }

                foreach (Mod mod in mMods)
                {
                    if (uncheck)
                        mod.IsChecked = false;
                    else
                        mod.IsChecked = true;
                }
            }
        }

        private void downloadableModsList_HeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (true)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        string header = headerClicked.Column.Header as string;

                        downloadableModsListSort(header, direction);

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
        }
        private void downloadableModsListSort(string sortBy, ListSortDirection direction)
        {
            if (sortBy != null)
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(downloadableModsList.ItemsSource);

                List<Mod> checkedMods = new List<Mod>();

                foreach (Mod item in dataView)
                {
                    if (item.IsChecked == true)
                    {
                        checkedMods.Add(item);
                    }
                }

                dataView.SortDescriptions.Clear();
                SortDescription sd = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();
                
                ObservableCollection<Mod> newDM = new ObservableCollection<Mod>();
                foreach (Mod item in dataView)
                {
                    newDM.Add(item);
                }

                dMods.Clear();

                foreach (Mod item in newDM)
                {
                    dMods.Add(item);
                }

                checkedMods.Reverse();
                foreach (Mod item in checkedMods)
                {
                    Mod itemToMoveUp = dMods[dMods.IndexOf(item)];
                    dMods.RemoveAt(dMods.IndexOf(item));
                    dMods.Insert(0, itemToMoveUp);
                }
                dataView.SortDescriptions.Clear();
            }
            else
            {
                bool uncheck = true;
                foreach (Mod mod in dMods)
                {
                    if (mod.IsChecked == false)
                    {
                        uncheck = false;
                    }
                }

                foreach (Mod mod in dMods)
                {
                    if (uncheck)
                        mod.IsChecked = false;
                    else
                        mod.IsChecked = true;
                }
            }
        }

        private void serverBrowserList_HeaderClicked(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (true)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        string header = headerClicked.Column.Header as string;

                        serverBrowserListSort(header, direction);

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
        }
        private void serverBrowserListSort(string sortBy, ListSortDirection direction)
        {
            if (sortBy != null)
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(serverBrowserList.ItemsSource);
                
                dataView.SortDescriptions.Clear();
                SortDescription sd = new SortDescription(sortBy, direction);
                dataView.SortDescriptions.Add(sd);
                dataView.Refresh();

                ObservableCollection<Server> newS = new ObservableCollection<Server>();
                foreach (Server item in dataView)
                {
                    newS.Add(item);
                }

                servers.Clear();
                
                foreach (Server item in newS)
                {
                    servers.Add(item);
                }

                dataView.SortDescriptions.Clear();
            }
        }

        private void tabsUpdateStatus(object sender, SelectionChangedEventArgs e)
        {
            if (mainTabs.SelectedIndex == 0 && modsTabs.SelectedIndex == 0)
            {
                int itemCount = mMods.Count;
                if (itemCount == 1)
                {
                    statusNumber.Content = "1 " + "mod available";
                }
                else if (itemCount != 1)
                {
                    statusNumber.Content = itemCount + " mods available";
                }
            }
            if (mainTabs.SelectedIndex == 0 && modsTabs.SelectedIndex == 1)
            {
                int itemCount = dMods.Count;
                if (itemCount == 1)
                {
                    statusNumber.Content = "1 " + "mod available";
                }
                else if (itemCount != 1)
                {
                    statusNumber.Content = itemCount + " mods available";
                }
            }
            if (mainTabs.SelectedIndex == 4)
            {
                int itemCount = servers.Count;
                if (itemCount == 1)
                {
                    statusNumber.Content = "1 " + "server available";
                }
                else if (itemCount != 1)
                {
                    statusNumber.Content = itemCount + " servers available";
                }
            }
        }
        private void tabsUpdateStatus(object sender, NotifyCollectionChangedEventArgs e)
        {
            tabsUpdateStatus(sender, (SelectionChangedEventArgs)null);
        }
    }
}