using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.WindowsAPICodePack.Shell;
using ModernStartMenu_MVVM.Models;
using WinCopies.Collections;
using Message = ModernStartMenu_MVVM.Models.Message;

namespace ModernStartMenu_MVVM.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private bool _isShellActivated;
        private ObservableCollection<ShellObject> _favAppsCollection;
        private ObservableCollection<ShellObject> _allAppsCollection;
        private RelayCommand<object> _appClickedCommand;
        private AsyncRelayCommand<object> _addAppToFavListCommand;
        public RelayCommand ShellActivatedCommand { get; }
        public RelayCommand ShellDeactivatedCommand { get; }
        public AsyncRelayCommand AddNewFavAppCommand { get; }

        public bool IsShellActivated
        {
            get => _isShellActivated;
            set => SetProperty(ref _isShellActivated, value);
        }

        public ObservableCollection<ShellObject> FavAppsCollection
        {
            get => _favAppsCollection;
            set => SetProperty(ref _favAppsCollection, value);
        }

        public ObservableCollection<ShellObject> AllAppsCollection
        {
            get => _allAppsCollection;
            set => SetProperty(ref _allAppsCollection, value);
        }

        public AsyncRelayCommand<object> AddAppToFavListCommand
        {
            get => _addAppToFavListCommand;
            set => SetProperty(ref _addAppToFavListCommand, value);
        }

        public RelayCommand<object> AppClickedCommand
        {
            get => _appClickedCommand;
            set => SetProperty(ref _appClickedCommand, value);
        }

        public ShellViewModel()
        {
            IsShellActivated = false;

            AddAppToFavListCommand = new AsyncRelayCommand<object>(AddAppToFavList);
            AllAppsCollection = new ObservableCollection<ShellObject>();
            FavAppsCollection = new ObservableCollection<ShellObject>();
            ShellActivatedCommand = new RelayCommand(ShellActivated);
            ShellDeactivatedCommand = new RelayCommand(ShellDeactivated);
            AddNewFavAppCommand = new AsyncRelayCommand(AddNewFavApp);
            AppClickedCommand = new RelayCommand<object>(AppClicked);

            AllAppsCollection = GetAllUserApps();

            FavAppsCollection = GetFavApps();
            foreach (var app in FavAppsCollection)
            {
                AllAppsCollection.AddIfNotContains(app);
            }
        }

        //=====================================================
        private async Task AddNewFavApp()
        {
            try
            {
                if (FavAppsCollection.Count == 8)
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
                    var app = ShellObject.FromParsingName(filePath);
                    FavAppsCollection.Add(app);
                    AllAppsCollection.Add(app);
                    await SaveFavAppAsync();
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

        private async Task AddAppToFavList(object sender)
        {
            try
            {
                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.Name == senderAppName);
                if (senderApp != null)
                {
                    FavAppsCollection.Add(senderApp);
                    await SaveFavAppAsync();
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
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.Name == senderAppName);
                if (senderApp != null)
                {
                    try
                    {
                        var path = @"shell:appsFolder\" + senderApp.ParsingName;
                        using Process process = new Process
                        {
                            StartInfo =
                            {
                                FileName = path,
                                UseShellExecute = true,
                            }
                        };
                        process.Start();
                    }
                    catch (Exception e)
                    {
                        var path = senderApp.ParsingName;
                        using Process process = new Process
                        {
                            StartInfo =
                            {
                                FileName = path,
                                UseShellExecute = true,
                            }
                        };
                        process.Start();
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

        private ObservableCollection<ShellObject> GetAllUserApps()
        {
            // GUID taken from https://docs.microsoft.com/en-us/windows/win32/shell/knownfolderid
            var folderIdAppsFolder = new Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");
            var appsFolder = KnownFolderHelper.FromKnownFolderId(folderIdAppsFolder);
            var apps = new ObservableCollection<ShellObject>();

            foreach (var app in appsFolder)
            {
                apps.Add(app);
            }

            return apps;
        }

        private void ShellActivated()
        {
            IsShellActivated = true;
        }

        private void ShellDeactivated()
        {
            IsShellActivated = false;
        }

        private async Task SaveFavAppAsync()
        {
            await using var stream = File.Create("FavApps.json");

            var apps = new ObservableCollection<FavApp>();
            foreach (var app in FavAppsCollection)
            {
                apps.Add(new FavApp
                {
                    Name = app.Name,
                    ParsingName = app.ParsingName
                });
            }

            await JsonSerializer.SerializeAsync(stream, apps);
        }

        private ObservableCollection<ShellObject> GetFavApps()
        {
            try
            {
                var reader = File.ReadAllText("FavApps.json");
                var apps = JsonSerializer.Deserialize<ObservableCollection<FavApp>>(reader);
                var appsCollection = new ObservableCollection<ShellObject>();

                foreach (var app in apps)
                {
                    try
                    {
                        var path = @"shell:appsFolder\" + app.ParsingName;
                        var shellApp = ShellObject.FromParsingName(path);
                        appsCollection.Add(shellApp);
                    }
                    catch (Exception e)
                    {
                        var shellApp = ShellObject.FromParsingName(app.ParsingName);
                        appsCollection.Add(shellApp);
                    }
                }

                return appsCollection;
            }
            catch (Exception e)
            {
                // ignored
            }

            return new ObservableCollection<ShellObject>();
        }
    } // end of class
}