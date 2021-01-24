using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.WindowsAPICodePack.COMNative.Shell;
using Microsoft.WindowsAPICodePack.Shell;
using ModernStartMenu_MVVM.Models;
using OpenWeatherMapDotNet;
using static OpenWeatherMapDotNet.OpenWeather;
using Message = ModernStartMenu_MVVM.Models.Message;

namespace ModernStartMenu_MVVM.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private bool _isShellActivated;
        private ObservableCollection<StartMenuApp> _favAppsCollection;
        private ObservableCollection<StartMenuApp> _allAppsCollection;
        private string _searchText;
        private bool _isSearchActive;
        private ObservableCollection<StartMenuApp> _searchCollection;
        private WeatherRoot _weatherDetails;
        public RelayCommand<object> AppClickedCommand { get; }
        public AsyncRelayCommand<object> AddAppToFavListCommand { get; }
        public AsyncRelayCommand<object> StarTheAppCommand { get; }
        public RelayCommand ShellActivatedCommand { get; }
        public RelayCommand ShellDeactivatedCommand { get; }
        public RelayCommand SearchBoxEnterPressedCommand { get; }
        public AsyncRelayCommand<object> AddNewUserAppCommand { get; }
        public AsyncRelayCommand<object> RemoveStarAppCommand { get; set; }
        public AsyncRelayCommand<object> RemoveFavAppCommand { get; set; }

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
                //Task.Delay(new TimeSpan(0,0,0,10)).ContinueWith(task => task.IsCompletedSuccessfully);
                SetProperty(ref _searchText, value);
                SearchApp();
            }
        }

        public ImageSource UserImageSource { get; set; }
        public string UserDisplayName { get; set; }

        public WeatherRoot WeatherDetails
        {
            get => _weatherDetails;
            set => SetProperty(ref _weatherDetails,value);
        }

        public ICollectionView CollectionView { get; set; }

        public ShellViewModel()
        {
            IsShellActivated = false;
            IsSearchActive = false;
            SearchCollection = new ObservableCollection<StartMenuApp>();
            AddAppToFavListCommand = new AsyncRelayCommand<object>(AddAppToFavList);
            AllAppsCollection = new ObservableCollection<StartMenuApp>();
            FavAppsCollection = new ObservableCollection<StartMenuApp>();
            ShellActivatedCommand = new RelayCommand(ShellActivated);
            ShellDeactivatedCommand = new RelayCommand(ShellDeactivated);
            AddNewUserAppCommand = new AsyncRelayCommand<object>(AddNewUserApp);
            AppClickedCommand = new RelayCommand<object>(AppClicked);
            StarTheAppCommand = new AsyncRelayCommand<object>(StarTheApp);
            RemoveStarAppCommand = new AsyncRelayCommand<object>(RemoveStarApp);
            RemoveFavAppCommand = new AsyncRelayCommand<object>(RemoveFavApp);
            SearchBoxEnterPressedCommand = new RelayCommand(SearchBoxEnterPressed);
            WeatherDetails = new WeatherRoot();
            LoadApps();
        }

        private void LoadApps()
        {
            GetAndSetUser();
            WeatherDetails =  GetWeatherDetails();
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

            //CollectionView = CollectionViewSource.GetDefaultView(AllAppsCollection);
        }

        //=====================================================
        private void ShellActivated()
        {
            IsShellActivated = true;
        }

        private void ShellDeactivated()
        {
            IsShellActivated = false;
        }

        private void SearchBoxEnterPressed()
        {
            try
            {
                // execute first app 

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
                /*CollectionView.Filter  = delegate(object o)
                {
                    var app = (StartMenuApp) o;
                    return app != null && app.DisplayName.ToLower().Contains(SearchText.Trim().ToLower());
                };*/

                if (!string.IsNullOrEmpty(SearchText))
                {
                    IsSearchActive = true;
                    var results =
                        AllAppsCollection.Where(x => x.DisplayName.ToLower().Contains(SearchText.Trim().ToLower()));
                    SearchCollection = new ObservableCollection<StartMenuApp>(results);
                }
                else
                {
                    IsSearchActive = false;
                    SearchCollection.Clear();
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


        private async Task RemoveFavApp(object sender)
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


        private async Task RemoveStarApp(object sender)
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

        private async Task StarTheApp(object sender)
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

        private async Task AddAppToFavList(object sender)
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

        private void AppClicked(object sender)
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

        private async Task AddNewUserApp(object isFav)
        {
            try
            {
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
                WeatherRoot weather = Task.Run(async ()=> await OpenWeatherDotNet.GetWeatherByZipAndCountryCodeAsync("f801526de5966ad181a597464bc900ac","66000","pk",TempUnit.Celsius)).Result;

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
    } // end of class
}