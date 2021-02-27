using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Web.WebView2.Core;
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
        #region =====HWND REGION

        [DllImport("user32")]
        public static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

        [DllImport("user32")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;
        private const uint MOD_NONE = 0x0000; //(none)

        private const uint MOD_ALT = 0x0001; //ALT

        /*private const uint MOD_CONTROL = 0x0002; //CTRL
        private const uint MOD_SHIFT = 0x0004; //SHIFT
        private const uint MOD_WIN = 0x0008; //WINDOWS
        private const uint VK_CAPITAL = 0x14; //capsLock
        private const uint VK_ALT = 0x14; //Alt Key*/
        private const uint VK_SPACE = 0x20; //Alt Key
        private IntPtr _windowHandle;
        private HwndSource _source;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);

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
                                if (Visibility == Visibility.Hidden)
                                {
                                    Visibility = Visibility.Visible;
                                    Focus();
                                    Activate();
                                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                }
                                else
                                {
                                    Visibility = Visibility.Hidden;
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

        #endregion

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

            SearchWebBrowser.CoreWebView2InitializationCompleted +=
                SearchWebBrowser_OnCoreWebView2InitializationCompleted;

            SearchWebBrowser.EnsureCoreWebView2Async();
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
                    ctx.RemoveStarAppCommand.Execute(((MenuItem) sender).Tag.ToString());
                }
                else if (menu.Name == "StarApp")
                {
                    ctx.StarTheAppCommand.Execute(((MenuItem) sender).Tag.ToString());
                }
            }
        }

        private void SearchWebBrowser_OnCoreWebView2InitializationCompleted(object sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                CheckBoxGoogleSearch.IsEnabled = false;
                BorderGoogleSearch.ToolTip =
                    "Google search not available because you need to install WebWiew2 runtime. Please Install this runtime and check again.";
                MessageBox.Show(
                    "Google search not available because you need to install 'WebWiew2 runtime'.\nPlease Install this runtime and check again.\n Press Alt+Space to activate Modern Start Menu.");
            }
            else
            {
                SearchWebBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                SearchWebBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            }
        }
    } // end of class
}