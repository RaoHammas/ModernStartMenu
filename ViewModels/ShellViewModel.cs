using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.WindowsAPICodePack.COMNative.Shell;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Win32Native.Shell;
using ModernStartMenu_MVVM.Models;
using OpenWeatherMapDotNet;
using static OpenWeatherMapDotNet.OpenWeather;
using Message = ModernStartMenu_MVVM.Models.Message;

namespace ModernStartMenu_MVVM.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        #region =============PROPERTIES AND COMMANDS====================================

        private bool _isShellActivated;
        private ObservableCollection<StartMenuApp> _favAppsCollection;
        private ObservableCollection<StartMenuApp> _allAppsCollection;
        private ObservableCollection<StartMenuApp> _searchCollection;

        private string _searchText;
        private bool _isSearchActive;
        private WeatherRoot _weatherDetails;
        private bool _isBrowserVisible;
        private string _browserSourceAddress;
        private AppSettings _appSettingsFile;
        private bool _isSettingsVisible;
        private bool _isQuickNotesVisible;
        private ObservableCollection<QuickNote> _quickNotesCollection;

        public RelayCommand<object> CopyQuickNoteCommand { get; set; }
        public RelayCommand<object> AppClickedCommand { get; }
        public RelayCommand IsGoogleSearchActiveCommand { get; }
        public AsyncRelayCommand<object> AddAppToFavListCommand { get; }
        public AsyncRelayCommand<object> StarTheAppCommand { get; }
        public RelayCommand ShellActivatedCommand { get; }
        public RelayCommand ShellDeactivatedCommand { get; }
        public RelayCommand SearchBoxEnterPressedCommand { get; }
        public AsyncRelayCommand ChangeThemeCommand { get; }
        public RelayCommand SaveSettingsCommand { get; }
        public RelayCommand<object> ChangeViewCommand { get; }
        public RelayCommand<object> DeleteQuickNoteCommand { get; }
        public RelayCommand<object> StartSearchCommand { get; }
        public RelayCommand OpenCloseSettingsCommand { get; }
        public AsyncRelayCommand<object> AddNewUserAppCommand { get; }
        public AsyncRelayCommand<object> RemoveStarAppCommand { get; set; }
        public AsyncRelayCommand<object> RemoveFavAppCommand { get; set; }

        public bool IsQuickNotesVisible
        {
            get => _isQuickNotesVisible;
            set => SetProperty(ref _isQuickNotesVisible, value);
        }

        public bool IsSettingsVisible
        {
            get => _isSettingsVisible;
            set => SetProperty(ref _isSettingsVisible, value);
        }

        public bool IsShellActivated
        {
            get => _isShellActivated;
            set => SetProperty(ref _isShellActivated, value);
        }

        public ObservableCollection<StartMenuApp> FavAppsCollection
        {
            get => _favAppsCollection;
            set => SetProperty(ref _favAppsCollection, value);
        }

        public ObservableCollection<StartMenuApp> AllAppsCollection
        {
            get => _allAppsCollection;
            set => SetProperty(ref _allAppsCollection, value);
        }

        public ObservableCollection<StartMenuApp> SearchCollection
        {
            get => _searchCollection;
            set => SetProperty(ref _searchCollection, value);
        }

        public bool IsSearchActive
        {
            get => _isSearchActive;
            set => SetProperty(ref _isSearchActive, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                SearchApp();
            }
        }

        public ImageSource UserImageSource { get; set; }
        public string UserDisplayName { get; set; }

        public WeatherRoot WeatherDetails
        {
            get => _weatherDetails;
            set => SetProperty(ref _weatherDetails, value);
        }

        public DateTime LastWeatherChecked { get; set; }

        public bool IsBrowserVisible
        {
            get => _isBrowserVisible;
            set => SetProperty(ref _isBrowserVisible, value);
        }

        public string BrowserSourceAddress
        {
            get => _browserSourceAddress;
            set => SetProperty(ref _browserSourceAddress, value);
        }

        public AppSettings AppSettingsFile
        {
            get => _appSettingsFile;
            set => SetProperty(ref _appSettingsFile, value);
        }

        public ObservableCollection<QuickNote> QuickNotesCollection
        {
            get => _quickNotesCollection;
            set => SetProperty(ref _quickNotesCollection, value);
        }

        #endregion

        #region =============METHODS=========================================

        public ShellViewModel()
        {
            IsShellActivated = false;
            IsSearchActive = false;
            IsBrowserVisible = false;
            IsSettingsVisible = false;
            IsQuickNotesVisible = false;
            AllAppsCollection = new ObservableCollection<StartMenuApp>();
            FavAppsCollection = new ObservableCollection<StartMenuApp>();
            SearchCollection = new ObservableCollection<StartMenuApp>();
            QuickNotesCollection = new ObservableCollection<QuickNote>();

            WeatherDetails = new WeatherRoot();
            LastWeatherChecked = DateTime.MinValue;

            AddAppToFavListCommand = new AsyncRelayCommand<object>(AddAppToFavListCommandExecute);
            ShellActivatedCommand = new RelayCommand(ShellActivatedCommandExecute);
            ShellDeactivatedCommand = new RelayCommand(ShellDeactivatedCommandExecute);
            AddNewUserAppCommand = new AsyncRelayCommand<object>(AddNewUserAppCommandExecute);
            AppClickedCommand = new RelayCommand<object>(AppClickedCommandExecute);
            ChangeViewCommand = new RelayCommand<object>(ChangeViewCommandExecute);
            StarTheAppCommand = new AsyncRelayCommand<object>(StarTheAppCommandExecute);
            RemoveStarAppCommand = new AsyncRelayCommand<object>(RemoveStarAppCommandExecute);
            RemoveFavAppCommand = new AsyncRelayCommand<object>(RemoveFavAppCommandExecute);
            SearchBoxEnterPressedCommand = new RelayCommand(SearchBoxEnterPressedCommandExecute);
            IsGoogleSearchActiveCommand = new RelayCommand(IsGoogleSearchActiveCommandExecute);
            ChangeThemeCommand = new AsyncRelayCommand(ChangeThemeCommandExecute);
            SaveSettingsCommand = new RelayCommand(SaveAllAppSettingsCommandExecute);
            OpenCloseSettingsCommand = new RelayCommand(CloseAppSettingsCommandExecute);
            DeleteQuickNoteCommand = new RelayCommand<object>(DeleteQuickNoteCommandExecute);
            CopyQuickNoteCommand = new RelayCommand<object>(CopyQuickNoteCommandExecute);
            StartSearchCommand = new RelayCommand<object>(StartSearchCommandExecute);


            GetAppSettings();
            LoadApps();
            ListenToShellAppsChanges();
        }

        private void StartSearchCommandExecute(object searchText)
        {
            IsBrowserVisible = true;
            if (searchText != null)
            {
                BrowserSourceAddress = searchText.ToString();
            }
        }

        private void CopyQuickNoteCommandExecute(object noteId)
        {
            if (noteId == null) return;
            var id = Convert.ToInt32(noteId);
            var foundNote = QuickNotesCollection.FirstOrDefault(x => x.NoteId == id);
            if (foundNote != null) Clipboard.SetText(foundNote.NoteText);
        }


        private void DeleteQuickNoteCommandExecute(object noteId)
        {
            if (noteId == null) return;
            var id = Convert.ToInt32(noteId);
            var foundNote = QuickNotesCollection.FirstOrDefault(x => x.NoteId == id);
            if (foundNote != null)
            {
                QuickNotesCollection.Remove(foundNote);
            }
        }

        private void ChangeViewCommandExecute(object senderBtnName)
        {
            if (senderBtnName == null) return;
            switch (senderBtnName)
            {
                case "BrowserView":
                    IsBrowserVisible = true;
                    IsQuickNotesVisible = false;
                    break;
                case "QuickNotesView":
                    IsBrowserVisible = false;
                    IsQuickNotesVisible = true;
                    break;
                default:
                    IsSettingsVisible = false;
                    IsBrowserVisible = false;
                    IsQuickNotesVisible = false;
                    break;
            }
        }

        private void SaveAllAppSettingsCommandExecute()
        {
            SaveAppSettings();
            WeatherDetails = GetWeatherDetails();

            WeakReferenceMessenger.Default.Send(new Message(null)
            {
                Caption = "Success",
                MessageText = "Settings Saved Successfully !"
            });
        }

        private void LoadApps()
        {
            FavAppsCollection.Clear();
            AllAppsCollection.Clear();

            GetAndSetUser();
            var allUserApps = GetAllUserApps();
            FavAppsCollection = allUserApps.AllFavAppsCollection;
            AllAppsCollection = allUserApps.allUserAppsCollection;

            foreach (var userApp in GetAllShellApps())
            {
                if (allUserApps.allUserAppsCollection.FirstOrDefault(x => x.ParsingName == userApp.ParsingName) == null)
                {
                    AllAppsCollection.Add(userApp);
                }
            }
        }

        //=====================================================
        private void CloseAppSettingsCommandExecute()
        {
            IsSettingsVisible = !IsSettingsVisible;
        }

        private void IsGoogleSearchActiveCommandExecute()
        {
            AppSettingsFile.IsGoogleSearchActive = !AppSettingsFile.IsGoogleSearchActive;
            SaveAppSettings();
        }

        private Task ChangeThemeCommandExecute()
        {
            AppSettingsFile.ThemeCode = AppSettingsFile.ThemeCode == "Dark" ? "Light" : "Dark";

            ActivateTheme();
            SaveAppSettings();
            return Task.CompletedTask;
        }

        private void ActivateTheme()
        {
            try
            {
                var styleDictionary = Application.Current.Resources.MergedDictionaries[0];
                var lightSource = new Uri("Resources/Styles/LightTheme.xaml", UriKind.RelativeOrAbsolute);
                var darkSource = new Uri("Resources/Styles/DarkTheme.xaml", UriKind.RelativeOrAbsolute);

                if (AppSettingsFile.ThemeCode == "Dark")
                {
                    styleDictionary.Source = darkSource;
                    AppSettingsFile.ThemeCode = "Dark";
                }
                else
                {
                    styleDictionary.Source = lightSource;
                    AppSettingsFile.ThemeCode = "Light";
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private void ShellActivatedCommandExecute()
        {
            IsShellActivated = true;
            var diff = DateTime.Now - LastWeatherChecked;
            if (diff.TotalHours >= 1)
            {
                WeatherDetails = GetWeatherDetails();
                LastWeatherChecked = DateTime.Now;
            }
        }

        private void ShellDeactivatedCommandExecute()
        {
            IsShellActivated = false;
        }

        private void SearchBoxEnterPressedCommandExecute()
        {
            try
            {
                //execute first app if enter btn pressed in search box
                StartMenuApp firstApp;
                if (_isSearchActive)
                {
                    if (SearchCollection.Count > 0)
                    {
                        firstApp = SearchCollection[0];
                        AppClickedCommand.Execute(firstApp.DisplayName);
                    }
                }
                else
                {
                    if (AllAppsCollection.Count > 0)
                    {
                        firstApp = AllAppsCollection[0];
                        AppClickedCommand.Execute(firstApp.DisplayName);
                    }
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private void SearchApp()
        {
            try
            {
                if (!string.IsNullOrEmpty(SearchText))
                {
                    IsSearchActive = true;
                    var results =
                        AllAppsCollection.Where(x => x.DisplayName.ToLower().Contains(SearchText.Trim().ToLower()));
                    SearchCollection = new ObservableCollection<StartMenuApp>(results);

                    if (SearchCollection.Count < 1 && AppSettingsFile.IsGoogleSearchActive)
                    {
                        IsBrowserVisible = true;
                        BrowserSourceAddress = "https://www.google.com/search?q=" + SearchText;
                    }
                }
                else
                {
                    IsSearchActive = false;
                    SearchCollection.Clear();
                    IsBrowserVisible = false;
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }


        private async Task RemoveFavAppCommandExecute(object sender)
        {
            try
            {
                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.DisplayName == senderAppName);
                if (senderApp != null)
                {
                    senderApp.IsFav = false;
                    FavAppsCollection.Remove(senderApp);
                    await SaveUserAppsToJsonAsync();
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }


        private async Task RemoveStarAppCommandExecute(object sender)
        {
            try
            {
                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.DisplayName == senderAppName);
                if (senderApp != null)
                {
                    senderApp.IsStar = false;

                    AllAppsCollection.Remove(senderApp);
                    AllAppsCollection.Add(senderApp);

                    await SaveUserAppsToJsonAsync();
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private async Task StarTheAppCommandExecute(object sender)
        {
            try
            {
                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.DisplayName == senderAppName);
                if (senderApp != null)
                {
                    senderApp.IsStar = true;

                    AllAppsCollection.Remove(senderApp);
                    AllAppsCollection.Insert(0, senderApp);

                    await SaveUserAppsToJsonAsync();
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private async Task AddAppToFavListCommandExecute(object sender)
        {
            try
            {
                if (FavAppsCollection.Count == 8)
                {
                    WeakReferenceMessenger.Default.Send(new Message(null)
                    {
                        Caption = "Error",
                        MessageText = "You can't add more Favorite apps. No slot available !"
                    });

                    return;
                }

                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.DisplayName == senderAppName);
                if (senderApp != null)
                {
                    if (FavAppsCollection.FirstOrDefault(x => x.ParsingName == senderApp.ParsingName) != null)
                    {
                        WeakReferenceMessenger.Default.Send(new Message(null)
                        {
                            Caption = "Error",
                            MessageText = "App already exists !"
                        });

                        return;
                    }

                    senderApp.IsFav = true;
                    FavAppsCollection.Add(senderApp);
                    await SaveUserAppsToJsonAsync();
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private void AppClickedCommandExecute(object sender)
        {
            try
            {
                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.DisplayName == senderAppName);
                if (senderApp != null)
                {
                    //var path = @"shell:appsFolder\" + senderApp.ParsingName;
                    using Process process = new Process
                    {
                        StartInfo =
                        {
                            FileName = senderApp.ParsingName,
                            UseShellExecute = true,
                        }
                    };
                    process.Start();
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private async Task AddNewUserAppCommandExecute(object isFav)
        {
            try
            {
                if (IsQuickNotesVisible)
                {
                    QuickNotesCollection.Insert(0, new QuickNote
                    {
                        NoteId = QuickNotesCollection.Count + 1, NoteText = "", UpdatedDate = DateTime.Now
                    });
                    return;
                }

                if (Convert.ToBoolean(isFav) && FavAppsCollection.Count == 8)
                {
                    WeakReferenceMessenger.Default.Send(new Message(null)
                    {
                        Caption = "Error",
                        MessageText = "You can't add more Favorite app. No slot available !"
                    });

                    return;
                }

                var filePath = WeakReferenceMessenger.Default.Send<FilePickerMessage>();
                if (filePath != String.Empty)
                {
                    if (AllAppsCollection.FirstOrDefault(x => x.ParsingName == filePath) != null)
                    {
                        WeakReferenceMessenger.Default.Send(new Message(null)
                        {
                            Caption = "Error",
                            MessageText = "App already exists !"
                        });

                        return;
                    }

                    var app = ShellObject.FromParsingName(filePath);

                    var newApp = new StartMenuApp
                    {
                        ParsingName = app.ParsingName,
                        IconSource = app.Thumbnail.MediumBitmapSource,
                        IsFav = Convert.ToBoolean(isFav),
                        IsUserAddedApp = true,
                        DisplayName = app.GetDisplayName(DisplayNameType.Default)
                    };

                    AllAppsCollection.Add(newApp);
                    if (Convert.ToBoolean(isFav))
                    {
                        FavAppsCollection.Add(newApp);
                    }

                    await SaveUserAppsToJsonAsync();
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = "An Error Occurred while adding app."
                });
            }
        }

        private async Task SaveUserAppsToJsonAsync()
        {
            await using var stream = File.Create("UserApps.json");
            await JsonSerializer.SerializeAsync(stream,
                AllAppsCollection.Where(x => x.IsUserAddedApp || x.IsFav || x.IsStar));
        }

        private (ObservableCollection<StartMenuApp> allUserAppsCollection, ObservableCollection<StartMenuApp>
            AllFavAppsCollection) GetAllUserApps()
        {
            try
            {
                var apps = GetAllUserAppsFromJson();

                var allFavAppsCollection = new ObservableCollection<StartMenuApp>();
                var allUserAppsCollection = new ObservableCollection<StartMenuApp>();

                foreach (var app in apps)
                {
                    try
                    {
                        var shellApp = ShellObject.FromParsingName(app.ParsingName);
                        app.IconSource = shellApp.Thumbnail.MediumBitmapSource;

                        if (app.IsFav)
                        {
                            allFavAppsCollection.Add(app);
                        }

                        allUserAppsCollection.Add(app);

                        shellApp.Dispose();
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }

                return (allUserAppsCollection, allFavAppsCollection);
            }
            catch (Exception e)
            {
                // ignored
            }

            return (null, null);
        }

        private ObservableCollection<StartMenuApp> GetAllShellApps()
        {
            // GUID taken from https://docs.microsoft.com/en-us/windows/win32/shell/knownfolderid
            var folderIdAppsFolder = new Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");
            var appsFolder = KnownFolderHelper.FromKnownFolderId(folderIdAppsFolder);
            var apps = new ObservableCollection<StartMenuApp>();
            foreach (var app in appsFolder)
            {
                var menuApp = new StartMenuApp
                {
                    DisplayName = app.GetDisplayName(DisplayNameType.Default),
                    IsFav = false,
                    IsUserAddedApp = false,
                    ParsingName = @"shell:appsFolder\" + app.ParsingName,
                    IconSource = app.Thumbnail.MediumBitmapSource
                };

                apps.Add(menuApp);
            }

            return apps;
        }


        private void GetAndSetUser()
        {
            try
            {
                // GUID taken from https://docs.microsoft.com/en-us/windows/win32/shell/knownfolderid
                var folderIdAccountPicsFolder = new Guid("{008ca0b1-55b4-4c56-b8a8-4de4b299d3be}");
                var picsFolder = KnownFolderHelper.FromKnownFolderId(folderIdAccountPicsFolder);
                if (picsFolder != null)
                {
                    UserImageSource = picsFolder.First().Thumbnail.MediumBitmapSource;
                    UserDisplayName = Environment.UserName;
                }
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private ObservableCollection<StartMenuApp> GetAllUserAppsFromJson()
        {
            try
            {
                var reader = File.ReadAllText("UserApps.json");
                var apps = JsonSerializer.Deserialize<ObservableCollection<StartMenuApp>>(reader);

                return apps;
            }
            catch (Exception e)
            {
                return new ObservableCollection<StartMenuApp>();
            }
        }

        private WeatherRoot GetWeatherDetails()
        {
            try
            {
                TempUnit unit = AppSettingsFile.TempUnit switch
                {
                    "Fahrenheit" => TempUnit.Fahrenheit,
                    "Kelvin" => TempUnit.Kelvin,
                    _ => TempUnit.Celsius
                };

                AppSettingsFile.ZipCode ??= "66000";
                AppSettingsFile.CountryCode ??= "pk";
                if (AppSettingsFile.ApiKey == null)
                {
                    return null;
                }

                WeatherRoot weather = Task.Run(async () =>
                    await OpenWeatherDotNet.GetWeatherByZipAndCountryCodeAsync(AppSettingsFile.ApiKey.Trim(),
                        AppSettingsFile.ZipCode, AppSettingsFile.CountryCode.ToLower(), unit)).Result;
                if (weather.Weather != null)
                {
                    //weather.DisplayIcon = @"http://openweathermap.org/img/wn/11d@4x.png";
                    weather.DisplayIcon = $@"http://openweathermap.org/img/wn/{weather.Weather?[0]?.Icon}@4x.png";
                }

                return weather;
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }

            return null;
        }

        private async void SaveAppSettings()
        {
            try
            {
                await using var stream = File.Create("AppSettings.json");
                await JsonSerializer.SerializeAsync(stream, AppSettingsFile);
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = e.Message
                });
            }
        }

        private void GetAppSettings()
        {
            try
            {
                var reader = File.ReadAllText("AppSettings.json");
                AppSettingsFile = JsonSerializer.Deserialize<AppSettings>(reader);
                ActivateTheme();
            }
            catch (Exception e)
            {
                AppSettingsFile = new AppSettings
                {
                    ThemeCode = "Light",
                    IsGoogleSearchActive = true
                };
            }
        }

        private void ListenToShellAppsChanges()
        {
            // GUID taken from https://docs.microsoft.com/en-us/windows/win32/shell/knownfolderid
            var folderIdAppsFolder = new Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");
            var appsFolder = KnownFolderHelper.FromKnownFolderId(folderIdAppsFolder);

            ShellObjectWatcher objectWatcher = new ShellObjectWatcher((ShellObject) appsFolder, false);
            objectWatcher.AllEvents += ObjectWatcherOnAllEvents;
            objectWatcher.Start();
        }

        private void ObjectWatcherOnAllEvents(object sender, ShellObjectNotificationEventArgs e)
        {
            if (e.ChangeType == ShellObjectChangeTypes.ItemCreate
                || e.ChangeType == ShellObjectChangeTypes.ItemDelete)
            {
                LoadApps();
            }
        }

        #endregion
    } // end of class
}