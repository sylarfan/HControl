using System.Windows;
using System.Windows.Media;

namespace ViewROIWPF.Models
{
    public struct HText
    {
        public string Message { get; set; }

        public int Row { get; set; }

        public int Column { get; set; }

        public CoorSystem CoorSystem { get; set; }

        public Color Color { get; set; }

        public bool UseBox { get; set; }

        //public int Size { get; set; }
    }
}
