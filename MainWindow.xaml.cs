using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IWshRuntimeLibrary;
//using static System.Windows.Forms.Application;
using File = System.IO.File;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace ModernStartMenu
{
    /// <summary>
    /// January 7 2021 12:05 PM Rao Hammas
    /// </summary>
    public partial class MainWindow
    {
        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("user32")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        //Modifiers:
        private const uint MOD_NONE = 0x0000; //(none)

        private const uint MOD_ALT = 0x0001; //ALT

        /*private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        private const uint VK_CAPITAL = 0x14; //capsLock
        private const uint VK_ALT = 0x14; //Alt Key*/
        private const uint VK_SPACE = 0x20; //Alt Key

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

        private IntPtr _windowHandle;
        private HwndSource _source;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(HwndHook);

            RegisterHotKey(_windowHandle, HOTKEY_ID, MOD_ALT, VK_SPACE); //CTRL + CAPS_LOCK
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID:
                            int vkey = (((int) lParam >> 16) & 0xFFFF);
                            if (vkey == VK_SPACE)
                            {
                                if (this.Visibility == Visibility.Hidden)
                                {
                                    this.Visibility = Visibility.Visible;
                                    this.Focus();
                                    this.Activate();
                                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                }
                                else
                                {
                                    this.Visibility = Visibility.Hidden;
                                }
                            }

                            handled = true;
                            break;
                    }

                    break;
            }

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            base.OnClosed(e);
        }
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
                MessageBox.Show(
                    "Google search not available because you need to install 'WebWiew2 runtime'.\nPlease Install this runtime and check again.\n Press Alt+Space to activate Modern Start Menu.");
            }
        }


        private void MainWindow_OnActivated(object sender, EventArgs e)
        {
            AnimationCheckBox.IsChecked = true;
            TextTime.Text = DateTime.Now.ToString("D");
            BoxSearch.Focus();
        }

        private void MainWindow_OnDeactivated(object sender, EventArgs e)
        {
            AnimationCheckBox.IsChecked = false;
            Visibility = Visibility.Hidden;
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
                //MessageBox.Show(ex.ToString());
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
            Process process = new Process();
            process.StartInfo.FileName = "ms-settings:Home-Page";
            process.StartInfo.UseShellExecute = true;
            process.Start();
        }

        private void OpenMyPc_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start("explorer.exe");
        }

        private void OpenTerminal_Click(object sender, MouseButtonEventArgs e)
        {
            Process.Start("cmd");
        }

        private async void OpeMyGithub_Click(object sender, MouseButtonEventArgs e)
        {
            if (CheckBoxGoogleSearch.IsChecked == true)
            {
                BoxSearch.SelectedText = "https://github.com/RaoHammas";
                await Task.Delay(2000);
                StartBrowsing("https://github.com/RaoHammas");
            }
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

        private void BtnPower_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var senderBorder = sender as Border;
                if (senderBorder != null)
                {
                    senderBorder.ContextMenu.IsOpen = true;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void PowerContextMenu_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var senderMenu = (sender as System.Windows.Controls.MenuItem)?.Header.ToString();

                var psi = new ProcessStartInfo();

                switch (senderMenu)
                {
                    case "Shut Down":
                        psi = new ProcessStartInfo("shutdown", "/s /t 0");
                        psi.CreateNoWindow = false;
                        psi.UseShellExecute = false;
                        Process.Start(psi);
                        break;
                    case "Restart":
                        psi = new ProcessStartInfo("shutdown", "/r /t 0");
                        psi.CreateNoWindow = false;
                        psi.UseShellExecute = false;
                        Process.Start(psi);
                        break;
                    case "Log Off":
                        ExitWindowsEx(0, 0);
                        break;
                    case "Sleep":
                        //SetSuspendState(PowerState.Suspend, false, false);
                        break;
                    case "Hibernate":
                        //SetSuspendState(PowerState.Hibernate, false, false);
                        break;
                    default:
                        return;
                        break;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void BoxSearch_OnKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    // just execute first app if enter pressed while search box active
                    var clickedItem = MenuItemsControl.Items[0] as MenuItem;
                    Process process = new Process();
                    if (clickedItem != null)
                    {
                        process.StartInfo.FileName = clickedItem.Path;
                        process.StartInfo.UseShellExecute = true;
                        process.Start();
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }
    } // end of class
}