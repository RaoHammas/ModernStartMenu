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
using ModernStartMenu_MVVM.Helpers;
using ModernStartMenu_MVVM.Models;

namespace ModernStartMenu_MVVM.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private bool _isShellActivated;
        private ObservableCollection<StartMenuApp> _favAppsCollection;
        private ObservableCollection<ShellObject> _allAppsCollection;
        private RelayCommand<object> _appClickedCommand;

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

        public ObservableCollection<ShellObject> AllAppsCollection
        {
            get => _allAppsCollection;
            set => SetProperty(ref _allAppsCollection, value);
        }

        public RelayCommand ShellActivatedCommand { get; }
        public RelayCommand ShellDeactivatedCommand { get; }
        public AsyncRelayCommand AddNewFavAppCommand { get; }

        public RelayCommand<object> AppClickedCommand
        {
            get => _appClickedCommand;
            set => SetProperty(ref _appClickedCommand, value);
        }

        public ShellViewModel()
        {
            IsShellActivated = false;
            AllAppsCollection = new ObservableCollection<ShellObject>();
            FavAppsCollection = new ObservableCollection<StartMenuApp>();
            ShellActivatedCommand = new RelayCommand(ShellActivated);
            ShellDeactivatedCommand = new RelayCommand(ShellDeactivated);
            AddNewFavAppCommand = new AsyncRelayCommand(AddNewFavApp);
            AppClickedCommand = new RelayCommand<object>(AppClicked);

            AllAppsCollection = GetAllUserApps();
            //FavAppsCollection = ReadFavAppsFromFile().Result;
        }



        //=====================================================
        private void AppClicked(object sender)
        {
            try
            {
                
                var senderAppName = sender as string;
                var senderApp = AllAppsCollection.FirstOrDefault(x => x.Name == senderAppName);
                if (senderApp != null)
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
                    var helper = new FilesHelper();
                    var appInfo = helper.GetAppInfo(filePath, false, false);

                    if (appInfo != null)
                    {
                        StartMenuApp app = new StartMenuApp
                        {
                            AppIcon = appInfo.Value.imageSource,
                            AppName = appInfo.Value.displayName,
                            IsFav = true,
                            Path = filePath,
                            UpdatedDate = DateTime.Now,
                        };


                        FavAppsCollection.Add(app);
                        var result = await WriteFavAppToFileAsync();
                    }
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

        private async Task<bool> WriteFavAppToFileAsync()
        {
            await using var stream = File.Create("FavApps.json");
            await JsonSerializer.SerializeAsync(stream, FavAppsCollection);
            return true;
        }

        private async Task<ObservableCollection<StartMenuApp>> ReadFavAppsFromFile()
        {
            try
            {
                await using var stream = File.OpenRead("FavApps.json");
                var apps = await JsonSerializer.DeserializeAsync<ObservableCollection<StartMenuApp>>(stream);

                FilesHelper helper = new FilesHelper();
                foreach (var app in apps)
                {
                    try
                    {
                        var appInfo = helper.GetAppInfo(app.Path, false, false);
                        if (appInfo != null)
                        {
                            app.AppIcon = appInfo.Value.imageSource;
                            app.AppName = appInfo.Value.displayName;
                        }
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }

                return apps;
            }
            catch (Exception e)
            {
                // ignored
            }

            return new ObservableCollection<StartMenuApp>();
        }
    } // end of class
}