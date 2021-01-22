using System.Text.Json.Serialization;
using System.Windows.Media;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace ModernStartMenu_MVVM.Models
{
    public class StartMenuApp : ObservableObject
    {
        public StartMenuApp()
        {
            DisplayName = "";
            IsUserAddedApp = false;
            ParsingName = "";
            IsFav = false;
            IsStar = false;
            IconSource = null;
        }

        public string DisplayName { get; set; }
        public bool IsFav { get; set; }

        private bool _isStar;

        public bool IsStar
        {
            get => _isStar;
            set => SetProperty(ref _isStar, value);
        }

        public bool IsUserAddedApp { get; set; }
        public string ParsingName { get; set; }
        [JsonIgnore] public ImageSource IconSource { get; set; }
    }
}