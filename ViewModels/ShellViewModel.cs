using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.WindowsAPICodePack.COMNative.Shell;
using Microsoft.WindowsAPICodePack.Shell;
using ModernStartMenu_MVVM.Models;
using Message = ModernStartMenu_MVVM.Models.Message;

namespace ModernStartMenu_MVVM.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private bool _isShellActivated;
        private ObservableCollection<StartMenuApp> _favAppsCollection;
        private ObservableCollection<StartMenuApp> _allAppsCollection;
        public RelayCommand<object> AppClickedCommand { get; }
        public AsyncRelayCommand<object> AddAppToFavListCommand { get; }
        public RelayCommand<object> StarTheAppCommand { get; }
        public RelayCommand ShellActivatedCommand { get; }
        public RelayCommand ShellDeactivatedCommand { get; }
        public AsyncRelayCommand<object> AddNewUserAppCommand { get; }

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

        public ShellViewModel()
        {
            IsShellActivated = false;

            AddAppToFavListCommand = new AsyncRelayCommand<object>(AddAppToFavList);
            AllAppsCollection = new ObservableCollection<StartMenuApp>();
            FavAppsCollection = new ObservableCollection<StartMenuApp>();
            ShellActivatedCommand = new RelayCommand(ShellActivated);
            ShellDeactivatedCommand = new RelayCommand(ShellDeactivated);
            AddNewUserAppCommand = new AsyncRelayCommand<object>(AddNewUserApp);
            AppClickedCommand = new RelayCommand<object>(AppClicked);
            StarTheAppCommand = new RelayCommand<object>(StarTheApp);
            LoadApps();
        }

        private void LoadApps()
        {
            var allUserApps = GetAllUserApps();
            FavAppsCollection = allUserApps.AllFavAppsCollection;
            AllAppsCollection = allUserApps.allUserAppsCollection;

            foreach (var userApp in GetAllShellApps())
            {
                if (allUserApps.allUserAppsCollection.FirstOrDefault(x=>x.ParsingName == userApp.ParsingName) == null)
                {
                    AllAppsCollection.Add(userApp);
                }
            }

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

        private void SortTheCollection()
        {
            var lastStar = AllAppsCollection.Last(x => x.IsStar);
            AllAppsCollection.Remove(lastStar);
            //add again
            AllAppsCollection.Insert(0,lastStar);
        }
        private async void StarTheApp(object sender)
        {
            try
            {
                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.DisplayName == senderAppName);
                if (senderApp != null)
                {
                    senderApp.IsStar = true;
                    SortTheCollection();
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
    } // end of class
}