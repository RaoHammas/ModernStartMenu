using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IWshRuntimeLibrary;
using File = System.IO.File;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace ModernStartMenu
{
    /// <summary>
    /// January 5 2021 12:50 PM Rao Hammas
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            MenuItemsCollection = new ObservableCollection<MenuItem>();
            FavItemsCollection = new ObservableCollection<MenuItem>();
            InitializeAsync();
        }


        public ObservableCollection<MenuItem> MenuItemsCollection { get; set; }
        public ObservableCollection<MenuItem> FavItemsCollection { get; set; }

        //===========================

        private async void InitializeAsync()
        {
            await ReadFromJsonDatabase();
            LoadAllInstalledPrograms();
            CheckBoxGoogleSearch.IsChecked = Settings.Default.IsGoogleSearch;
            try
            {
                await SearchWebBrowser.EnsureCoreWebView2Async();
            }
            catch (Exception e)
            {
                CheckBoxGoogleSearch.IsChecked = false;
                CheckBoxGoogleSearch.IsEnabled = false;
                CheckBoxGoogleSearch.ToolTip =
                    "Google search not available because you need to install WebWiew2 runtime. Please Install this runtime and check again.";
            }
        }


        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            AnimationCheckBox.IsChecked = true;
            TextTime.Text = DateTime.Now.ToString("D");
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            AnimationCheckBox.IsChecked = false;
        }

        private void BtnAddNewMenuItemClicked(object sender, MouseButtonEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    var icon = ToImageSource(System.Drawing.Icon.ExtractAssociatedIcon(openFileDialog.FileName));

                    ReadOnlySpan<char> fileName = openFileDialog.SafeFileName;
                    int lastPeriod = fileName.LastIndexOf('.');
                    if (lastPeriod != -1)
                    {
                        fileName = fileName.Slice(0, lastPeriod).ToString();
                    }

                    MenuItem item = new MenuItem
                    {
                        Icon = icon,
                        Name = fileName.ToString(),
                        NameWithExtension = openFileDialog.SafeFileName,
                        Path = openFileDialog.FileName,
                        Type = "Manual",
                        UpdatedDate = DateTime.Now
                    };

                    if (sender.ToString() == "BtnFav")
                    {
                        item.IsFav = true;
                        FavItemsCollection.Add(item);
                    }

                    MenuItemsCollection.Add(item);
                    SaveToJsonDatabase();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private async void SaveToJsonDatabase()
        {
            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    MaxDepth = 0,
                    IgnoreNullValues = true,
                    IgnoreReadOnlyProperties = true
                };
                await using FileStream createStream = File.Create("Data.Json");
                await JsonSerializer.SerializeAsync(createStream,
                    MenuItemsCollection.Where(x => x.Type == "Manual" || x.Type == "AutoFav"),
                    options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private async Task ReadFromJsonDatabase()
        {
            try
            {
                await using FileStream openStream = File.OpenRead("Data.Json");

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    MaxDepth = 0,
                    IgnoreNullValues = true,
                    IgnoreReadOnlyProperties = true
                };

                var items = await JsonSerializer.DeserializeAsync<ObservableCollection<MenuItem>>(openStream, options);

                foreach (var item in items)
                {
                    item.Icon = ToImageSource(System.Drawing.Icon.ExtractAssociatedIcon(item.Path));
                    if (item.IsFav)
                    {
                        FavItemsCollection.Add(item);
                    }

                    MenuItemsCollection.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private static ImageSource ToImageSource(Icon icon)
        {
            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return imageSource;
        }

        private void MenuItemClicked(object sender, MouseButtonEventArgs e)
        {
            MenuItem clickedItem = new MenuItem();
            try
            {
                Border border = sender as Border;
                if (border != null)
                {
                    clickedItem = border.DataContext as MenuItem;
                    Process process = new Process();
                    if (clickedItem != null)
                    {
                        process.StartInfo.FileName = clickedItem.Path;
                        process.StartInfo.UseShellExecute = true;
                        process.Start();
                    }
                }
            }
            catch (Exception)
            {
                var result = MessageBox.Show("File not found. Do you want to remove this item from menu?",
                    "Error | File Not Found ", MessageBoxButton.YesNo, MessageBoxImage.Error);
                if (result == MessageBoxResult.Yes)
                {
                    MenuItemsCollection.Remove(clickedItem);
                    SaveToJsonDatabase();
                }
            }
        }


        //===============================================================================

        private void LoadAllInstalledPrograms()
        {
            try
            {
                string s = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
                string[] files = Directory.GetFiles(s, "*.LNK", SearchOption.AllDirectories);

                foreach (var item in files)
                {
                    try
                    {
                        var lnk = GetLnkTarget(item);
                        var path = lnk.TargetPath.Replace('\\', '-').Replace('-', '/');
                        var fullname = lnk.FullName.Split('\\').Last().Split('.')[0];
                        var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);

                        var program = new MenuItem
                        {
                            Name = fullname,
                            Path = lnk.TargetPath,
                            UpdatedDate = DateTime.Now,
                            Type = "Auto",
                            Icon = ToImageSource(icon)
                        };

                        if (MenuItemsCollection.FirstOrDefault(x => x.Name == program.Name) == null)
                        {
                            MenuItemsCollection.Add(program);
                        }
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private static IWshShortcut GetLnkTarget(string lnkPath)
        {
            if (File.Exists(lnkPath))
            {
                WshShell shell = new WshShell(); //Create a new WshShell Interface
                IWshShortcut link = (IWshShortcut) shell.CreateShortcut(lnkPath); //Link the interface to our shortcut

                return link;
            }
            else
            {
                return null;
            }
        }


        private async void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                await Task.Delay(500);
                var text = (sender as TextBox).Text.Trim().ToLower();

                if (text == string.Empty)
                {
                    MenuItemsControl.ItemsSource = MenuItemsCollection;
                    GridBrowser.Visibility = Visibility.Collapsed;
                    GridAllApps.Visibility = Visibility.Visible;
                    return;
                }

                var found = MenuItemsCollection.Where(x => x.Name.ToLower().StartsWith(text)).AsParallel();
                if (!found.Any() && CheckBoxGoogleSearch.IsChecked == true)
                {
                    StartBrowsing("https://www.google.com/search?q=" + text);
                    return;
                }

                GridBrowser.Visibility = Visibility.Collapsed;
                GridAllApps.Visibility = Visibility.Visible;
                MenuItemsControl.ItemsSource = found;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void StartBrowsing(string text)
        {
            try
            {
                GridAllApps.Visibility = Visibility.Collapsed;
                GridBrowser.Visibility = Visibility.Visible;
                SearchWebBrowser.CoreWebView2.Navigate(text);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void BtnAddFavClicked(object sender, MouseButtonEventArgs e)
        {
            if (FavItemsCollection.Count < 8)
            {
                BtnAddNewMenuItemClicked("BtnFav", null);
            }
            else
            {
                MessageBox.Show("No free slot for fav App. Please remove a fav app and then add it again",
                    "No free slot for fav app");
            }
        }

        private void AllItemsContextMenuAddToFav_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FavItemsCollection.Count < 8)
                {
                    if (sender is System.Windows.Controls.MenuItem menu)
                    {
                        var item = menu.DataContext as MenuItem;
                        if (FavItemsCollection.Contains(item))
                        {
                            return;
                        }

                        if (item != null)
                        {
                            item.IsFav = true;
                            item.Type = "AutoFav";
                            FavItemsCollection.Add(item);
                            SaveToJsonDatabase();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No free slot for fav App. Please remove a fav app and then add it again",
                        "No free slot for fav app");
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void FavItemsContextMenu_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.MenuItem menu)
                {
                    var item = menu.DataContext as MenuItem;
                    FavItemsCollection.Remove(item);

                    if (item.Type == "AutoFav")
                    {
                        MenuItemsCollection.Remove(item);
                    }

                    item.IsFav = false;
                    SaveToJsonDatabase();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void OpenWinSettings_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start("ms-settings:Display");
        }

        private void OpenMyPc_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe");
        }

        private void OpenTerminal_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start("cmd");
        }

        private void OpeMyGithub_Click(object sender, MouseButtonEventArgs e)
        {
            StartBrowsing("https://github.com/RaoHammas");
        }

        private void CheckBoxGoogleSearch_OnChecked(object sender, RoutedEventArgs e)
        {
            if (CheckBoxGoogleSearch.IsChecked == true)
            {
                Settings.Default["IsGoogleSearch"] = true;
            }
            else
            {
                Settings.Default["IsGoogleSearch"] = false;
            }

            Settings.Default.Save();
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    } // end of class
}