using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using ModernStartMenu_MVVM.Helpers;
using ModernStartMenu_MVVM.Models;

namespace ModernStartMenu_MVVM.ViewModels
{
    public class ShellViewModel : ViewModelBase
    {
        private bool _isShellActivated;
        private ObservableCollection<StartMenuApp> _favAppsCollection;

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

        public RelayCommand ShellActivatedCommand { get; }
        public RelayCommand ShellDeactivatedCommand { get; }
        public RelayCommand AddNewFavAppCommand { get; }

        public ShellViewModel()
        {
            IsShellActivated = false;
            FavAppsCollection = new ObservableCollection<StartMenuApp>();
            ShellActivatedCommand = new RelayCommand(ShellActivated);
            ShellDeactivatedCommand = new RelayCommand(ShellDeactivated);
            AddNewFavAppCommand = new RelayCommand(AddNewFavApp);
        }

        //=====================================================
        private void ShellActivated()
        {
            try
            {
                IsShellActivated = true;
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error", MessageText = "Shell activation error."
                });
            }
        }

        private void ShellDeactivated()
        {
            try
            {
                IsShellActivated = false;
            }
            catch (Exception e)
            {
                WeakReferenceMessenger.Default.Send(new Message(null)
                {
                    Caption = "Error",
                    MessageText = "Shell deactivation error."
                });
            }
        }

        private void AddNewFavApp()
        {
            try
            {
                var filePath = WeakReferenceMessenger.Default.Send<FilePickerMessage>();
                if (filePath != String.Empty)
                {
                    FilesHelper helper = new FilesHelper();
                    var iconImage = helper.GetFileIcon(filePath, false, false);

                    StartMenuApp app = new StartMenuApp
                    {
                        AppIcon = iconImage,
                        AppName = "Some App",
                        IsFav = true,
                        Path = filePath,
                        UpdatedDate = DateTime.Now,
                    };

                    if (FavAppsCollection.Any())
                    {
                        app.TransitionDelay = FavAppsCollection.Last().TransitionDelay + 100;
                    }
                    else
                    {
                        app.TransitionDelay = 500;
                    }
                    FavAppsCollection.Add(app);
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
    } // end of class
}