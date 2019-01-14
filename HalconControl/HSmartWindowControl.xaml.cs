using HalconDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HalconControl
{
    /// <summary>
    /// HSmartWindowControl.xaml 的交互逻辑
    /// </summary>
    public partial class HSmartWindowControl : UserControl, INotifyPropertyChanged, IComponentConnector
    {
        private const int HZoomFactorMin = 1;
        private const int HZoomFactorMax = 100;
        private System.Drawing.Size _prevsize = new System.Drawing.Size();
        private double _dpiX;
        private double _dpiY;
        private bool _left_button_down;
        private Point _last_position;
#if H17
        private HObject _dumpimg;
        private HTuple _dump_params;
#endif
#if H12
        private HImage _dumpimg;
#endif
        private WriteableBitmap _wbitmap;
        private readonly ArrayPool<byte> arrayPool = ArrayPool<byte>.Create(4000 * 3000, 50);



        private HWindow _hwindow;
        private bool _delayed;

        [Category("Behavior")]
        [Description("Occurs after the HALCON window has been initialized.")]
        public event HInitWindowEventHandler HInitWindow;

        public event HSmartMouseEventHandler HMouseMove;

        public event HSmartMouseEventHandler HMouseWheel;

        public event HSmartMouseEventHandler HMouseUp;

        public event HSmartMouseEventHandler HMouseDown;


        /// <summary>
        /// Occurs when an internal error in the HSmartWindowControl WPF happens.
        /// </summary>
        public event HSmartErrorHandler HErrorNotify;

        /// <summary>
        ///   Occurs when a button is double-clicked over a HALCON window.
        ///   Note that delta is meaningless here.
        /// </summary>
        public event HSmartMouseEventHandler HMouseDoubleClick;

        public static readonly DependencyProperty HImagePartProperty = DependencyProperty.Register(nameof(HImagePart), typeof(Rect), typeof(HSmartWindowControl), new PropertyMetadata(new Rect(0.0, 0.0, 640.0, 480.0), new PropertyChangedCallback(OnHImagePartChanged)));

        ///// <summary>
        ///// Controls the behavior of zoom factor controlled by the mouse wheele
        ///// </summary>
        //public static DependencyProperty HZoomFactorProperty = DependencyProperty.Register(nameof(HZoomFactor), typeof(double), typeof(HSmartWindowControl), new PropertyMetadata(Math.Round(Math.Sqrt(2.0), 2)), new ValidateValueCallback(HZoomFactorValidation));

        public static readonly DependencyProperty WindowSizeProperty = DependencyProperty.Register(nameof(WindowSize), typeof(Size), typeof(HSmartWindowControl), new PropertyMetadata(new Size(320.0, 200.0)));

        /// <summary>Current size of the corresponding HALCON window.</summary>
        public Size WindowSize
        {
            get
            {
                return (Size)GetValue(WindowSizeProperty);
            }
            set
            {
                if (value.Width <= 0.0 || value.Height <= 0.0 || _delayed)
                    return;
                _hwindow.GetWindowExtents(out int row, out int column, out int width, out int height);
                try
                {
                    _hwindow.SetWindowExtents(0, 0, (int)value.Width, (int)value.Height);
                    SetValue(WindowSizeProperty, value);
                }
                catch (HalconException)
                {
                    _hwindow.SetWindowExtents(0, 0, width, height);
                }
            }
        }

        [EditorBrowsable(EditorBrowsableState.Always)]
        [Category("Layout")]
        [Description("Visible image part (Column, Row, Width, Height).")]
        public Rect HImagePart
        {
            get
            {
                return (Rect)GetValue(HImagePartProperty);
                //if (_hwindow == null)
                //    return (Rect)GetValue(HImagePartProperty);
                //GetFloatPart(_hwindow, out double l1, out double c1, out double l2, out double c2);
                //Rect rect = new Rect
                //{
                //    X = c1,
                //    Y = l1,
                //    Width = c2 - c1 + 1.0,
                //    Height = l2 - l1 + 1.0
                //};
                //SetValue(HImagePartProperty, rect);
                //return rect;
            }
            set
            {
                if (_hwindow != null)
                    _hwindow.SetPart((int)value.Top, (int)value.Left, (int)(value.Bottom - 1), (int)(value.Right - 1));
                SetValue(HImagePartProperty, value);
            }
        }

        //[Category("Behavior")]
        //[Description("Controls the behavior of the zoom factor controlled by the mouse wheel.")]
        //[EditorBrowsable(EditorBrowsableState.Always)]
        //public double HZoomFactor
        //{
        //    get
        //    {
        //        return (double)GetValue(HZoomFactorProperty);
        //    }
        //    set
        //    {
        //        if (value <= 1.0 || value > 100.0)
        //            return;
        //        SetValue(HZoomFactorProperty, value);
        //    }
        //}

        private void GetFloatPart(HWindow window, out double l1, out double c1, out double l2, out double c2)
        {
            window.GetPart(out int row1, out int column1, out int row2, out int column2);
            l1 = row1;
            c1 = column1;
            l2 = row2;
            c2 = column2;
        }

        private static void OnHImagePartChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((HSmartWindowControl)sender).HImagePart = (Rect)e.NewValue;
        }

        

        internal HWindow HalconWindow
        {
            get
            {
                if (_delayed)
                    HInitializeWindow();
                return _hwindow;
            }
        }

        internal IntPtr HalconID
        {
            get
            {
                if (_delayed)
                    HInitializeWindow();
                if (_hwindow != null)
                    return _hwindow.Handle;
                return HTool.UNDEF;
            }
        }


        public HSmartWindowControl()
        {
            InitializeComponent();
            HInitializeWindow();

        }


        internal void Display()
        {
            InvalidateVisual();
        }

        //internal Action<HWindow> DisplayAction { get; set; }

        /// <summary>
        /// Draw the contents of the smart buffer
        /// 
        /// Instead of using an Image control to render the contents of the
        /// buffer window, the contents are directly rendered to the
        /// DrawingContext passed to OnRender()
        /// 
        /// OnRender also handles resizing of the control. This way,
        /// set_window_extents is called far less then in the respective
        /// delegate.
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HalconControl.halcon_icon_48.png");
                if (manifestResourceStream == null)
                {
                    drawingContext.DrawRectangle(new SolidColorBrush(Colors.Black), null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));
                }
                else
                {
                    try
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.StreamSource = manifestResourceStream;
                        bitmapImage.EndInit();
                        int num1 = (int)ActualWidth / 2 - 24;
                        int num2 = (int)ActualHeight / 2 - 24;
                        drawingContext.DrawRectangle(new SolidColorBrush(Colors.Black), null, new Rect(0.0, 0.0, ActualWidth, ActualHeight));
                        if (ActualWidth <= 48.0 || ActualHeight <= 48.0)
                            return;
                        drawingContext.DrawImage(bitmapImage, new Rect(num1, num2, 48.0, 48.0));
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                if (_hwindow == null || _delayed)
                    HInitializeWindow();
                if (_hwindow == null)
                {
                    Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                else
                {
                    _hwindow.GetWindowExtents(out int row, out int column, out int width1, out int height1);
                    if (width1 != (int)ActualWidth || height1 != (int)ActualHeight)
                    {
                        WindowSize = new Size((int)ActualWidth, (int)ActualHeight);
                        //for ViewROIWPF SizeChanged Repaint
                        return;
                    }
                    //display action invoke
                    //DisplayAction?.Invoke(HalconWindow);

                    _prevsize.Width = (int)ActualWidth;
                    _prevsize.Height = (int)ActualHeight;
                    if (_dumpimg != null)
                        _dumpimg.Dispose();
#if H12
                     _dumpimg = _hwindow.DumpWindowImage();
                    if (_dumpimg.CountChannels() == 1)
                    {
                        var imageData = _dumpimg.GetImagePointer1(out string type, out int width2, out int height2);
                        //use array pool to improve performance
                        byte[] arr = arrayPool.Rent(width2 * height2);
                        //byte[] arr = new byte[width2 * height2 * 3];
                        unsafe
                        {
                            byte* p = (byte*)imageData;
                            for (int i = 0; i < width2 * height2; i++)
                            {
                                arr[i] = p[i];
                                //arr[i * 3 + 1] = p[i];
                                //arr[i * 3 + 2] = p[i];
                            }
                        }
                        if (_wbitmap == null || width2 != _wbitmap.Width || height2 != _wbitmap.Height)
                        {
                            _wbitmap = new WriteableBitmap(width2, height2, _dpiX, _dpiY, PixelFormats.Gray8, null);
                        }
                        _wbitmap.Lock();
                        _wbitmap.WritePixels(new Int32Rect(0, 0, width2, height2), arr, width2, 0);
                        _wbitmap.Unlock();
                        arrayPool.Return(arr);
                        Point pixelOffset = GetPixelOffset();
                        drawingContext.DrawImage(_wbitmap, new Rect(pixelOffset.X, pixelOffset.Y, width2, height2));
                        //_dumpimg.WriteImage("tiff", 0, "123.tif");

                    }
                    else
                    {
                        _dumpimg.GetImagePointer3(out IntPtr r, out IntPtr g, out IntPtr b, out string type, out int width2, out int height2);
#if DEBUG
                        Stopwatch sw = Stopwatch.StartNew();
#endif
                        //byte[] arr = new byte[width2 * height2 * 3];
                        //use array pool to improve performance

                        byte[] arr = arrayPool.Rent(width2 * height2 * 3);
                        unsafe
                        {
                            byte* ptr = (byte*)r;
                            byte* ptg = (byte*)g;
                            byte* ptb = (byte*)b;
                            for (int i = 0; i < width2 * height2; i++)
                            {
                                arr[i * 3] = ptb[i];
                                arr[i * 3 + 1] = ptg[i];
                                arr[i * 3 + 2] = ptr[i];
                            }
                        }
                        if (_wbitmap == null || width2 != _wbitmap.Width || height2 != _wbitmap.Height)
                        {
                            _wbitmap = new WriteableBitmap(width2, height2, _dpiX, _dpiY, PixelFormats.Bgr24, null);
                        }
                        _wbitmap.Lock();
                        _wbitmap.WritePixels(new Int32Rect(0, 0, width2, height2), arr, width2 * 3, 0);
                        _wbitmap.Unlock();
                        arrayPool.Return(arr);
#if DEBUG
                        sw.Stop();
                        Debug.WriteLine(sw.Elapsed);
#endif
                        Point pixelOffset = GetPixelOffset();
                        drawingContext.DrawImage(_wbitmap, new Rect(pixelOffset.X, pixelOffset.Y, width2, height2));

                    }
#endif
#if H17
                    
                    HOperatorSet.DumpWindowImage(out _dumpimg, _dump_params);
                    HOperatorSet.GetImagePointer1(_dumpimg, out HTuple pointer, out HTuple type, out HTuple width2, out HTuple height2);
                    IntPtr l = (IntPtr)pointer.L;
                    if (_wbitmap == null || width2 / 4 != (int)_wbitmap.Width || height2 != (int)_wbitmap.Height)
                        _wbitmap = new WriteableBitmap(width2 / 4, height2, _dpiX, _dpiY, PixelFormats.Bgra32, null);
                    _wbitmap.Lock();
                    _wbitmap.WritePixels(new Int32Rect(0, 0, width2 / 4, height2), l, width2 * height2, width2);
                    _wbitmap.Unlock();
                    Point pixelOffset = GetPixelOffset();
                    drawingContext.DrawImage(_wbitmap, new Rect(pixelOffset.X, pixelOffset.Y, width2 / 4, height2));
#endif

                }
            }
        }

        ///// <summary>
        /////   Adapt ImagePart to show the full image. If HKeepAspectRatio is on,
        /////   the contents of the HALCON window are rescaled while keeping the aspect
        /////   ratio. Otherwise, the HALCON window contents are rescaled to fill up
        /////   the HSmartWindowControlWPF.
        ///// </summary>
        //internal void SetFullImagePart(HImage reference = null)
        //{
        //    if (ActualHeight == 0.0 || ActualWidth == 0.0)
        //        return;
        //    if (reference != null)
        //    {
        //        reference.GetImageSize(out int width, out int height);
        //        HImagePart = new Rect(0.0, 0.0, width, height);
        //    }
        //    else
        //    {
        //        //this._hwindow.SetPart(0, 0, -2, -2);
        //        //this.HImagePart = this.Part2Rect();
        //    }
        //    //else
        //    //{
        //    //    this._hwindow.SetPart(0, 0, -1, -1);
        //    //    this.HImagePart = this.Part2Rect();
        //    //}
        //}

        private Rect Part2Rect()
        {
            _hwindow.GetPart(out int row1, out int column1, out int row2, out int column2);
            return new Rect(column1, row1, column2 - column1 + 1, row2 - row1 + 1);
        }

        private void HInitializeWindow()
        {
            _delayed = RenderSize.Width <= 0.0 || RenderSize.Height <= 0.0;
            if (_delayed && Parent != null)
            {
                if (Parent is FrameworkElement parent)
                {
                    RenderSize = parent.RenderSize;
                    _delayed = RenderSize.Width <= 0.0 || RenderSize.Height <= 0.0;
                }
            }
            if (_delayed)
                return;
            Size renderSize = RenderSize;
            Dispatcher.ShutdownStarted += new EventHandler(Dispatcher_ShutdownStarted);
            HImagePart = new Rect(0, 0, 640, 480);
            Rect himagePart = HImagePart;
            _hwindow = new HWindow(0, 0, (int)renderSize.Width, (int)renderSize.Height, 0, "buffer", "");
            //_hwindow.SetWindowParam("graphics_stack", "true");
            WindowSize = renderSize;
            _hwindow.SetPart((int)himagePart.Top, (int)himagePart.Left, (int)(himagePart.Bottom - 1.0), (int)(himagePart.Right - 1.0));
#if H17
            _hwindow.SetWindowParam("graphics_stack", "true");
            _dump_params = new HTuple(HalconID);
            _dump_params = _dump_params.TupleConcat("interleaved");
#endif

            _prevsize.Width = (int)ActualWidth;
            _prevsize.Height = (int)ActualHeight;
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                _dpiX = graphics.DpiX;
                _dpiY = graphics.DpiY;
            }
       
            if (HInitWindow != null)
                OnHInitWindow();
            DataContext = (this);
        }

        protected virtual void OnHInitWindow()
        {
            if (HInitWindow == null)
                return;
            HInitWindow(this, new EventArgs());
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            _hwindow.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected internal void OnPropertyChanged([CallerMemberName] string propertyname = null)
        {
            if (PropertyChanged == null)
                return;
            PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
        }

        private void HWPFWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            _left_button_down = false;
        }

        private void HWPFWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            HSmartMouseEventArgs e1 = null;
            try
            {
                e1 = ToHMouse(_last_position.X, _last_position.Y, e, 0);
            }
            catch (HalconException ex)
            {
                HErrorNotify?.Invoke(ex);
            }
            if (HMouseDoubleClick == null)
                return;
            HMouseDoubleClick(this, e1);
        }

        private HSmartMouseEventArgs ToHMouse(double x, double y, MouseEventArgs e, int delta)
        {
            MouseButton? button = new MouseButton?();
            ConvertWindowsCoordinatesToHImage(y, x, out double rowImage, out double columnImage);
            if (e != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    button = new MouseButton?(MouseButton.Left);
                else if (e.RightButton == MouseButtonState.Pressed)
                    button = new MouseButton?(MouseButton.Right);
                else if (e.MiddleButton == MouseButtonState.Pressed)
                    button = new MouseButton?(MouseButton.Middle);
                else if (e.XButton1 == MouseButtonState.Pressed)
                    button = new MouseButton?(MouseButton.XButton1);
                else if (e.XButton2 == MouseButtonState.Pressed)
                    button = new MouseButton?(MouseButton.XButton2);
            }
            return new HSmartMouseEventArgs(x, y, rowImage, columnImage, delta, button);
        }

        private Point GetPosition(MouseEventArgs e)
        {
            return e.GetPosition(this);
        }

        private void HWPFWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            HSmartMouseEventArgs e1 = null;
            try
            {
                if (_delayed)
                    return;
                _left_button_down = e.LeftButton == MouseButtonState.Pressed;
                _last_position = GetPosition(e);
                e1 = ToHMouse(_last_position.X, _last_position.Y, e, 0);
            }
            catch (HalconException ex)
            {
                HErrorNotify?.Invoke(ex);
            }
            if (HMouseDown == null)
                return;
            HMouseDown(this, e1);
        }

        private void HWPFWindow_MouseMove(object sender, MouseEventArgs e)
        {
            HSmartMouseEventArgs e1 = null;
            try
            {
                if (_delayed)
                    return;
                Point position = GetPosition(e);
                _last_position = position;
                e1 = ToHMouse(_last_position.X, _last_position.Y, e, 0);
            }
            catch (HalconException ex)
            {
                HErrorNotify?.Invoke(ex);
            }
            if (HMouseMove == null)
                return;
            HMouseMove(this, e1);
        }

        private void HWPFWindow_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HSmartMouseEventArgs e1 = null;
            try
            {
                _left_button_down = false;
                _last_position = GetPosition(e);
                e1 = ToHMouse(_last_position.X, _last_position.Y, e, 0);
                e1.Button = e.ChangedButton;
            }
            catch (HalconException ex)
            {
                HErrorNotify?.Invoke(ex);
            }
            if (HMouseUp == null)
                return;
            HMouseUp(this, e1);
        }

        private void HWPFWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            HSmartMouseEventArgs e1 = null;
            try
            {
                e1 = ToHMouse(_last_position.X, _last_position.Y, null, e.Delta);
            }
            catch (HalconException ex)
            {
                HErrorNotify?.Invoke(ex);
            }
            if (HMouseWheel == null)
                return;
            HMouseWheel(this, e1);
        }

        /// <summary>
        /// From https://blogs.msdn.microsoft.com/dwayneneed/2007/10/05/blurry-bitmaps/.
        /// Helper code to get the (subpixel) offset of the control
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private Point TryApplyVisualTransform(Point point, Visual v, bool inverse, bool throwOnError, out bool success)
        {
            success = true;
            if (v != null)
            {
                Matrix visualTransform = GetVisualTransform(v);
                if (inverse)
                {
                    if (!throwOnError && !visualTransform.HasInverse)
                    {
                        success = false;
                        return new Point(0.0, 0.0);
                    }
                    visualTransform.Invert();
                }
                point = visualTransform.Transform(point);
            }
            return point;
        }

        /// <summary>
        /// From https://blogs.msdn.microsoft.com/dwayneneed/2007/10/05/blurry-bitmaps/.
        /// Helper code to get the (subpixel) offset of the control
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private Matrix GetVisualTransform(Visual v)
        {
            if (v == null)
                return Matrix.Identity;
            Matrix trans1 = Matrix.Identity;
            Transform transform = VisualTreeHelper.GetTransform(v);
            if (transform != null)
            {
                Matrix trans2 = transform.Value;
                trans1 = Matrix.Multiply(trans1, trans2);
            }
            Vector offset = VisualTreeHelper.GetOffset(v);
            trans1.Translate(offset.X, offset.Y);
            return trans1;
        }

        /// <summary>
        /// From https://blogs.msdn.microsoft.com/dwayneneed/2007/10/05/blurry-bitmaps/.
        /// Helper code to get the (subpixel) offset of the control
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private Point ApplyVisualTransform(Point point, Visual v, bool inverse)
        {
            return TryApplyVisualTransform(point, v, inverse, true, out bool success);
        }

        /// <summary>
        /// From https://blogs.msdn.microsoft.com/dwayneneed/2007/10/05/blurry-bitmaps/.
        /// Helper code to get the (subpixel) offset of the control
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private Point GetPixelOffset()
        {
            Point point1 = new Point();
            PresentationSource presentationSource = PresentationSource.FromVisual(this);
            if (presentationSource != null)
            {
                Visual rootVisual = presentationSource.RootVisual;
                if (((FrameworkElement)rootVisual).ActualHeight > 0.0 && ((FrameworkElement)rootVisual).ActualWidth > 0.0)
                {
                    Point point2 = ApplyVisualTransform(TransformToAncestor(rootVisual).Transform(point1), rootVisual, false);
                    point1 = presentationSource.CompositionTarget.TransformToDevice.Transform(point2);
                    point1.X = Math.Round(point1.X);
                    point1.Y = Math.Round(point1.Y);
                    point1 = presentationSource.CompositionTarget.TransformFromDevice.Transform(point1);
                    point1 = ApplyVisualTransform(point1, rootVisual, true);
                    point1 = rootVisual.TransformToDescendant(this).Transform(point1);
                }
            }
            return point1;
        }

        ///// <summary>
        ///// Zoom the contents of the HALCON window keeping the point (x, y) invariant.
        ///// Please notice that the point is given in native coordinates (i.e. position of
        ///// the mouse cursor relative to the upper left corner of the control).
        ///// </summary>
        ///// <param name="x">Column coordinate of the invariant point.</param>
        ///// <param name="y">Row coordinate of the invariant point.</param>
        ///// <param name="Delta">Scaling factor (multiple of 120).</param>
        //public void HZoomWindowContents(double x, double y, int Delta)
        //{
        //    HOperatorSet.HomMat2dIdentity(out HTuple homMat2DIdentity);
        //    ConvertWindowsCoordinatesToHImage(y, x, out double ir, out double ic);
        //    double num = Delta < 0 ? HZoomFactor : 1.0 / HZoomFactor;
        //    //if (this.HZoomContent == HSmartWindowControlWPF.ZoomContent.WheelBackwardZoomsIn)
        //    //    num = 1.0 / num;
        //    for (int index = Math.Abs(Delta) / 120; index > 1; --index)
        //        num *= Delta < 0 ? HZoomFactor : 1.0 / HZoomFactor;
        //    HOperatorSet.HomMat2dScale(homMat2DIdentity, num, num, ic, ir, out HTuple homMat2DScale);
        //    GetFloatPart(_hwindow, out double l1, out double c1, out double l2, out double c2);
        //    HOperatorSet.AffineTransPoint2d(homMat2DScale, c1, l1, out HTuple qx1, out HTuple qy1);
        //    HOperatorSet.AffineTransPoint2d(homMat2DScale, c2, l2, out HTuple qx2, out HTuple qy2);
        //    try
        //    {
        //        HImagePart = new Rect(qx1.D, qy1.D, qx2.D - qx1.D + 1.0, qy2.D - qy1.D + 1.0);
        //    }
        //    catch (Exception)
        //    {
        //        try
        //        {
        //            HImagePart = new Rect(c1, l1, c2 - c1 + 1.0, l2 - l1 + 1.0);
        //        }
        //        catch (HalconException)
        //        {
        //        }
        //    }
        //}

        /// <summary>
        /// Translates WPF mouse coordinates into HALCON image coordinates.
        /// </summary>
        /// <param name="wr"></param>
        /// <param name="wc"></param>
        /// <param name="ir"></param>
        /// <param name="ic"></param>
        private void ConvertWindowsCoordinatesToHImage(double wr, double wc, out double ir, out double ic)
        {
            ir = 0; ic = 0;
            GetFloatPart(_hwindow, out double l1, out double c1, out double l2, out double c2);
            var imageHeight = l2 - l1 + 1;
            var imageWidth = c2 - c1 + 1;
            var rect = WindowSize;
            ir = wr / rect.Height * imageHeight + l1;
            ic = wc / rect.Width * imageWidth + c1;
        }

        /// <summary>
        /// Translates native encoding of mouse buttons to HALCON encoding
        /// (see get_mposition).
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private int MouseEventToInt(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Released)
                return 1;
            if (e.RightButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Released)
                return 4;
            return e.MiddleButton == MouseButtonState.Pressed || e.MiddleButton == MouseButtonState.Released ? 2 : 0;
        }

        private bool calculate_part(HTuple hv_WindowHandle, HTuple hv_WindowWidth, HTuple hv_WindowHeight)
        {
            HTuple row = null;
            HTuple column = null;
            HTuple width = null;
            HTuple height = null;
            HTuple homMat2DIdentity = null;
            HTuple homMat2DScale = null;
            HTuple qx = null;
            HTuple qy = null;
            bool flag = true;
            HOperatorSet.GetPart(hv_WindowHandle, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
            try
            {
                HTuple htuple1 = (column2 - column1 + 1) / (row2 - row1 + 1).TupleReal();
                HOperatorSet.GetWindowExtents(hv_WindowHandle, out row, out column, out width, out height);
                HTuple sx = width / hv_WindowWidth.TupleReal();
                HTuple sy = height / hv_WindowHeight.TupleReal();
                HTuple htuple2 = new HTuple().TupleConcat((row1 + row2) * 0.5).TupleConcat((column1 + column2) * 0.5);
                HOperatorSet.HomMat2dIdentity(out homMat2DIdentity);
                HOperatorSet.HomMat2dScale(homMat2DIdentity, sx, sy, htuple2.TupleSelect(1), htuple2.TupleSelect(0), out homMat2DScale);
                HOperatorSet.AffineTransPoint2d(homMat2DScale, column1.TupleConcat(column2), row1.TupleConcat(row2), out qx, out qy);
                HImagePart = new Rect(qx.TupleSelect(0), qy.TupleSelect(0), qx.TupleSelect(1) - qx.TupleSelect(0) + 1, qy.TupleSelect(1) - qy.TupleSelect(0) + 1);
            }
            catch (HalconException)
            {
                HImagePart = new Rect(column1, row1, column2 - column1 + 1, row2 - row1 + 1);
                flag = false;
            }
            return flag;
        }
    }
}
