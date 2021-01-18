using System;
using System.Windows;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Win32;
using ModernStartMenu_MVVM.Models;

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
    } // end of class
}