using System.Windows.Media;

namespace ViewROIWPF.Models
{
    public sealed class Font
    {
        public FontFamily FontFamily { get; set; }
        public bool Italic { get; set; } 
        public bool Underline { get; set; }
        public bool Strikeout { get; set; }
        public bool Bold { get; set; }
        public int Size { get; set; }
    }
}
