using System;

namespace ModernStartMenu_MVVM.Models
{
    public class StartMenuApp
    {
        //[IgnoreDataMember]
        public object AppIcon { get; set; }
        public string AppName { get; set; }
        public bool IsFav { get; set; }
        public string Path { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
