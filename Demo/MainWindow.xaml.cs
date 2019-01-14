using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using HalconDotNet;
using ViewROIWPF;
using ViewROIWPF.ROIs;

namespace Demo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private HWndCtrl ctrl;

        public MainWindow()
        {
            InitializeComponent();
           
        }

        private void hal_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                var image = new HImage("fabrik");
                ctrl.AddIconicVar(image);
            });
            //hal.DisplayAction = hwindow => hwindow.DispImage(new HImage("fabrik"));
            //hal.HalconWindow.DispImage(new HImage("fabrik"));
            //hal.InvalidateVisual();
        }

        private void hal_HMouseDown(object sender, HalconControl.HSmartMouseEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ctrl = new HWndCtrl(hal);
            //ctrl.SetWindowColor(Brushes.CadetBlue.Color);
            //ctrl.SetBackgroundText("123", new ViewROIWPF.Models.Font() { Bold = false, Italic = false, FontFamily = SystemFonts.CaptionFontFamily, Size = 50, Strikeout = false, Underline = false }, Brushes.GreenYellow.Color);
            //ctrl.ROIController.SetShowToolTip(true);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ctrl.ROIController.SetROIShape(new ROIRectangle1());
        }
    }
}
