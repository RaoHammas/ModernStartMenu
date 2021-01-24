using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Win32;
using ModernStartMenu_MVVM.Models;
using ModernStartMenu_MVVM.ViewModels;

namespace ModernStartMenu_MVVM.Views
{
    /// <summary>
    /// Interaction logic for ShellView.xaml
    /// </summary>
    public partial class ShellView : Window
    {
        public ShellView()
        {
            WeakReferenceMessenger.Default.Register<Message>(this, (r, m) => { ShowMessageDialog(m); });

            WeakReferenceMessenger.Default.Register<FilePickerMessage>(this, (r, m) =>
            {
                var path = ShowFilePicker();
                m.Reply(path);
            });

            InitializeComponent();
            BoxSearch.Focus();
        }

        private void ShowMessageDialog(Message message)
        {
            MessageBox.Show(this, message.MessageText, message.Caption);
        }

        private string ShowFilePicker()
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    return openFileDialog.FileName;
                }
                else
                {
                    MessageBox.Show("No App selected !");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return String.Empty;
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuItem;
            if (DataContext is ShellViewModel ctx)
            {
                if (menu.Name == "FavApp")
                {
                    ctx.AddAppToFavListCommand.Execute(((MenuItem) sender).Tag.ToString());
                }             
                else if (menu.Name == "RemoveFav")
                {
                    ctx.RemoveFavAppCommand.Execute(((MenuItem) sender).Tag.ToString());
                }
                else if (menu.Name == "RemoveStar")
                {
                    ctx.RemoveStarAppCommand.Execute(((MenuItem)sender).Tag.ToString());
                }
                else if(menu.Name == "StarApp")
                {
                    ctx.StarTheAppCommand.Execute(((MenuItem) sender).Tag.ToString());
                }
            }
        }
    } // end of class
}