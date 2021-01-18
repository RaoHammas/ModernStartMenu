using System;
using System.Text.Json.Serialization;
using System.Windows.Media;
using Microsoft.WindowsAPICodePack.Shell;

namespace ModernStartMenu_MVVM.Models
{
    public class StartMenuApp
    {
        [JsonIgnore]
        public ImageSource AppIcon { get; set; }
        public string AppName { get; set; }
        public bool IsFav { get; set; }
        public string Path { get; set; }
        public DateTime UpdatedDate { get; set; }

        [JsonIgnore] 
        public ShellObject Shell { get; set; }




    }
}
