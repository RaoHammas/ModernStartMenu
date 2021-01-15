using System.Windows;
using ModernStartMenu_MVVM.Views;

namespace ModernStartMenu_MVVM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            
            if (App.Current.MainWindow is null)
            {
                App.Current.MainWindow = new ShellView();
                App.Current.MainWindow.Activate();
                App.Current.MainWindow.Show();
            }
        }
    }
}
