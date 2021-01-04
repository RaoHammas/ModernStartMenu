using System;
using System.Runtime.Serialization;

namespace ModernStartMenu
{
    public class MenuItem
    {
        [IgnoreDataMember]
        public object Icon { get; set; }
        public string Name { get; set; }
        public string NameWithExtension { get; set; }
        public bool IsFav { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
