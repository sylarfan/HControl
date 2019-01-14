using HalconControl;
using HalconDotNet;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ViewROIWPF.Helper;
using ViewROIWPF.Models;

namespace ViewROIWPF
{

    /// <summary>
    /// This class works as a wrapper class for the HALCON window
    /// HWindow. HWndCtrl is in charge of the visualization.
    /// You can move and zoom the visible image part by using GUI component
    /// inputs or with the mouse. The class HWndCtrl uses a graphics stack
    /// to manage the iconic objects for the display. Each object is linked
    /// to a graphical context, which determines how the object is to be drawn.
    /// The context can be changed by calling changeGraphicSettings().
    /// The graphical "modes" are defined by the class GraphicsContext and
    /// map most of the dev_set_* operators provided in HDevelop.
    /// </summary>
    public class HWndCtrl
    {
        #region Fields

        private string backgroundText;
        private Font backgroundTextFont;
        private Color backgroundTextColor;
        private string drawAuxiLineMessage;

        private readonly HWindow window;

        /// <summary>
        /// 最大Hobject数量
        /// </summary>
        private const int MAXNUMOBJLIST = 20;

        /// <summary>
        /// 是否鼠标正在按下
        /// </summary>
        private bool mousePressed = false;

        private double startX, startY;

        /// <summary>HALCON window</summary>
        private readonly HSmartWindowControl viewPort;

        //   /* dispROI is a flag to know when to add the ROI models to the
        //paint routine and whether or not to respond to mouse events for
        //ROI objects */
        //   private ROIMode dispROI;

        /* Basic parameters, like dimension of window and displayed image part */
        private double windowWidth;
        private double windowHeight;
        private int imageWidth;
        private int imageHeight;

        /* Image coordinates, which describe the image part that is displayed
		   in the HALCON window */
        private double ImgRow1, ImgCol1, ImgRow2, ImgCol2;

        ///// <summary>Error message when an exception is thrown</summary>
        //private string exceptionText = "";

        private double WHFactor;

        private List<HObjectEntry> hObjList;

        /// <summary>
        /// Instance that describes the graphical context for the
        /// HALCON window. According on the graphical settings
        /// attached to each HALCON object, this graphical context list
        /// is updated constantly.
        /// </summary>
        private GraphicsContext gC;

        private int auxiLineWidth = 1;

        #endregion Fields

        #region Properties

        public event EventHandler<HWindowEventArgs> OnHWndCtrlEvent;

        /// <summary>
        /// 自定义双击窗口打开图片对话框。如果为空，则使用默认的OpenFileDialog。
        /// </summary>
        public Func<string> OpenDialogFuc { get; set; }

        public Action<HSmartMouseEventArgs> RightClickAction { get; set; }

        /// <summary>
        /// 文字
        /// </summary>
        public HText? Text { get; set; }

        /// <summary>
        /// 是否显示文字
        /// </summary>
        public bool ShowText { get; set; }

        /// <summary>
        /// 图像自适应窗口，不拉伸
        /// </summary>
        public bool KeepImageRatio { get; set; } = true;

        public bool KeepImagePart { get; set; } = false;

        /// <summary>
        /// 辅助线颜色
        /// </summary>
        public Color AuxiLineColor { get; set; } = Brushes.Green.Color;

        /// <summary>
        /// 辅助线
        /// </summary>
        public AuxiliaryLine AuxiliaryLine { get; set; } = AuxiliaryLine.None;

        public double AuxiliaryLineScale { get; set; } = 1;

        public HLineStyle AuxiLineStyle { get; set; }

        public Func<HSmartWindowControl, string> DrawAuxiLine { get; set; }

        public bool FreezeImage { get; set; } = false;

        public Font Font { get; private set; }

        /// <summary>
        /// 辅助线线宽,1-50
        /// </summary>
        public int AuxiLineWidth
        {
            get { return auxiLineWidth; }
            set
            {
                if (value < 1)
                {
                    auxiLineWidth = 1;
                }
                else if (value > 50)
                {
                    auxiLineWidth = 50;
                }
                else
                {
                    auxiLineWidth = value;
                }
            }
        }

        public ROIMode ROIMode { get; set; } = ROIMode.Include;

        public ROIController ROIController { get; }

        public bool DoubleClickOpenImage { get; set; }

        #endregion Properties

        #region Ctor

        /// <summary>
        /// Initializes the image dimension, mouse delegation, and the
        /// graphical context setup of the instance.
        /// </summary>
        /// <param name="view"> HALCON window </param>
        public HWndCtrl(HSmartWindowControl view)
        {
            viewPort = view;
            //viewPort.DisplayAction = RepaintInternal;
            ROIController = new ROIController();
            ROIController.SetViewController(this);
            window = view.HalconWindow;
            windowWidth = viewPort.WindowSize.Width;
            windowHeight = viewPort.WindowSize.Height;
            SetImagePart(0, 0, viewPort.WindowSize.Height - 1, viewPort.WindowSize.Width - 1);
            WHFactor = viewPort.WindowSize.Width * 1.0 / viewPort.WindowSize.Height;
            viewPort.HMouseUp += MouseUp;
            viewPort.HMouseDown += MouseDown;
            viewPort.HMouseMove += MouseMoved;
            viewPort.HMouseWheel += MouseWheel;
            viewPort.HMouseDoubleClick += MouseDoubleClick;
            viewPort.DragEnter += new DragEventHandler(DragEnter);
            viewPort.Drop += new DragEventHandler(DragDrop);

            viewPort.SizeChanged += SizeChanged;

         

            //SetImagePart(0, 0, viewPort.ImagePart.Height, view.ImagePart.Width);
            hObjList = new List<HObjectEntry>(MAXNUMOBJLIST);
            gC = new GraphicsContext();

            //#if H17
            //            window.SetWindowParam("flush", "true");
            //#endif
        }



        #endregion Ctor

        #region ImageOperation

        /// <summary>
        /// Adjust window settings by the values supplied for the left
        /// upper corner and the right lower corner
        /// </summary>
        /// <param name="r1">y coordinate of left upper corner</param>
        /// <param name="c1">x coordinate of left upper corner</param>
        /// <param name="r2">y coordinate of right lower corner</param>
        /// <param name="c2">x coordinate of right lower corner</param>
        private void SetImagePart(double r1, double c1, double r2, double c2)
        {
            ImgRow1 = r1;
            ImgCol1 = c1;
            ImgRow2 = r2;
            ImgCol2 = c2;

            Rect rect = new Rect
            {
                X = ImgCol1,
                Y = ImgRow1,
                Height = imageHeight,
                Width = imageWidth
            };
            var handleSize = Math.Min(rect.Width, rect.Height) / 75;
            handleSize = handleSize > 5 ? (int)handleSize : 5;
            ROIController.SetHandleSize(handleSize, false);
            viewPort.HImagePart = rect;
        }

        private int imgH, imgW;

        private void SetImageKeepRatio(HObject image)
        {
            int h, w;
            HOperatorSet.GetImageSize(image, out HTuple wt, out HTuple ht);

            if (Environment.Is64BitProcess)
            {
                h = (int)ht.L;
                w = (int)wt.L;
            }
            else
            {
                h = ht.I;
                w = wt.I;
            }
            //if (h == imgH && w == imgW)
            //{
            //    return;
            //}
            imgH = h;
            imgW = w;
            if (h * WHFactor > w)
            {
                imageWidth = Convert.ToInt32(h * WHFactor);
                imageHeight = h;
            }
            else
            {
                imageWidth = w;
                imageHeight = Convert.ToInt32(w / WHFactor);
            }
            SetImagePart((h - imageHeight) / 2, (w - imageWidth) / 2, (imageHeight + h) / 2, (imageWidth + w) / 2);
        }

        private void ZoomImage(double x, double y, double scale)
        {
            //viewPort.HZoomWindowContents(x, y, scale);
            double lengthC, lengthR;
            double percentC, percentR;
            double lenC, lenR;

            percentC = (x - ImgCol1) / (ImgCol2 - ImgCol1);
            percentR = (y - ImgRow1) / (ImgRow2 - ImgRow1);

            lengthC = (ImgCol2 - ImgCol1) * scale;
            lengthR = (ImgRow2 - ImgRow1) * scale;
            //if (lengthC > (viewPort.WindowSize.Width * 3) || lengthR > (viewPort.WindowSize.Height * 3))
            //{
            //    return;
            //}
            ImgCol1 = x - lengthC * percentC;
            ImgCol2 = x + lengthC * (1 - percentC);

            ImgRow1 = y - lengthR * percentR;
            ImgRow2 = y + lengthR * (1 - percentR);

            lenC = Math.Round(lengthC);
            lenR = Math.Round(lengthR);

            var rect = new Rect
            {
                X = Math.Round(ImgCol1),
                Y = Math.Round(ImgRow1),
                Width = (lenC > 0) ? lenC : 1,
                Height = (lenR > 0) ? lenR : 1
            };

            viewPort.HImagePart = rect;

            var handleSize = Math.Min(rect.Width, rect.Height) / 75;
            handleSize = handleSize > 5 ? (int)handleSize : 5;
#if DEBUG1
            Debug.WriteLine(handleSize);
#endif
            ROIController.SetHandleSize(handleSize, false);
            Repaint();
        }

        /// <summary>
        /// Scales the image in the HALCON window according to the
        /// value scaleFactor
        /// </summary>
        private void ZoomImage(double scaleFactor)
        {
            double midPointX, midPointY;

            if (((ImgRow2 - ImgRow1) == scaleFactor * imageHeight) &&
                ((ImgCol2 - ImgCol1) == scaleFactor * imageWidth))
            {
                Repaint();
                return;
            }

            ImgRow2 = ImgRow1 + imageHeight;
            ImgCol2 = ImgCol1 + imageWidth;

            midPointX = ImgCol1;
            midPointY = ImgRow1;

            ZoomImage(midPointX, midPointY, scaleFactor);
        }

        private void MoveImage(double motionX, double motionY)
        {
            ImgRow1 += -motionY;
            ImgRow2 += -motionY;

            ImgCol1 += -motionX;
            ImgCol2 += -motionX;

            var rect = new Rect
            {
                X = ImgCol1,
                Y = ImgRow1,
                Height = ImgRow2 - ImgRow1,
                Width = ImgCol2 - ImgCol1
            };
            viewPort.HImagePart = rect;
            Repaint();
        }

        #endregion ImageOperation

        #region Mouse Events

        private void SizeChanged(object sender, SizeChangedEventArgs e)
        {

            if (viewPort.WindowSize.Width == 0 || viewPort.WindowSize.Height == 0)
            {
                return;
            }
            WHFactor = viewPort.WindowSize.Width * 1.0 / viewPort.WindowSize.Height;

            windowWidth = viewPort.WindowSize.Width;
            windowHeight = viewPort.WindowSize.Height;
            if (KeepImageRatio && !KeepImagePart && hObjList.Count > 0 && hObjList[0].HObj.IsImage())
            {
                SetImageKeepRatio(hObjList[0].HObj);
            }
            Repaint();
        }

        private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPEG", ".BMP", ".TIF", ".PNG" };
        private string dragFilePath;

        private void DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Link;
            e.Handled = true;
        }

        private void DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                if (ImageExtensions.Contains(Path.GetExtension(fileList.FirstOrDefault()).ToUpperInvariant()))
                {
                    if (dragFilePath != fileList.First())
                    {
                        dragFilePath = fileList.First();
                        HImage image = new HImage(dragFilePath);
                        AddIconicVar(image);
                        OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(image));
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void MouseDown(object sender, HSmartMouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                mousePressed = true;
                int activeROIidx = -1;
                if (ROIController != null && (ROIMode == ROIMode.Include))
                {
                    activeROIidx = ROIController.MouseDownAction(e.Column, e.Row);
                }
                //if (activeROIidx == -1&&hObjList.Count>0)
                //{
                //}
                startX = e.Column;
                startY = e.Row;
            }
            //TO DO:右键点击时，如果有激活的ROI则把ROI中心移至鼠标点处
            else if (e.Button == MouseButton.Right && ROIController.activeROIidx != -1)
            {
                ROIController.MoveActive(e.Column, e.Row);
            }
            else if (e.Button == MouseButton.Right)
            {
                RightClickAction?.Invoke(e);
            }

        }

        private void MouseDoubleClick(object sender, HSmartMouseEventArgs e)
        {
            //TO DO:双击左键弹出加载图片对话框
            if (DoubleClickOpenImage && e.Button == MouseButton.Left)
            {
                try
                {
                    string path = null;
                    if (OpenDialogFuc == null)
                    {
                        OpenFileDialog ofd = new OpenFileDialog
                        {
                            Filter = "Image Files|*.jpg;*.tif;*.bmp;*.png;",
                            Multiselect = false,
                            CheckFileExists = true,
                        };
                        if (ofd.ShowDialog() == true)
                        {
                            path = ofd.FileName;
                        }
                    }
                    else
                    {
                        path = OpenDialogFuc.Invoke();
                    }
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        HImage image = new HImage(path);
                        AddIconicVar(image);

                        OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(image));
                    }
                }
                catch (Exception ex)
                {
                    OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(HWindowEvent.DialogUpdateImageError, ex.Message));
                }
                finally
                {
                    mousePressed = false;
                }
            }
        }

        private void MouseWheel(object sender, HSmartMouseEventArgs e)
        {
            if (FreezeImage || hObjList.Count == 0)
            {
                return;
            }
            double scale;

            if (0 < e.Delta)
            {
                scale = 0.9;
            }
            else
            {
                scale = 1 / 0.9;
            }
            ZoomImage(e.Column, e.Row, scale);
        }

        private bool activeROi;

        private void MouseUp(object sender, HSmartMouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                mousePressed = false;
                if (ROIController != null
              && (ROIController.activeROIidx != -1)
              && (ROIMode == ROIMode.Include))
                {
                    if (Halcon.DistancePp(startY, startX, e.Column, e.Row) > 0)
                    {
                        ROIController.InvokeEvent(ROIEvent.UpdateROI);
                    }
                    activeROi = true;
                }
                else if (activeROi)
                {
                    activeROi = false;
                }
            }
        }

        private void MouseMoved(object sender, HSmartMouseEventArgs e)
        {
            if (!mousePressed || Halcon.DistancePp(startY, startX, e.Column, e.Row) == 0)
            {
                return;
            }

            double motionX, motionY;
            if (ROIController != null && (ROIController.activeROIidx != -1) && (ROIMode == ROIMode.Include))
            {
                ROIController.MouseMoveAction(e.Column, e.Row);
            }
            else if (!FreezeImage && hObjList.Count > 0)
            {
                motionX = e.Column - startX;
                motionY = e.Row - startY;

                if (((int)motionX != 0) || ((int)motionY != 0))
                {
                    MoveImage(motionX, motionY);
                    startX = e.Column - motionX;
                    startY = e.Row - motionY;
                }
            }
        }

        #endregion Mouse Events

        #region Paint

        /// <summary>
        /// Triggers a repaint of the HALCON window
        /// </summary>
        public void Repaint()
        {
            if (!viewPort.Dispatcher.CheckAccess())
            {
                viewPort.Dispatcher.Invoke(new Action(() =>
                {
                    RepaintInternal(window);
                }));
            }
            else
            {
                RepaintInternal(window);
            }

        }

        /// <summary>
        /// Repaints the HALCON window 'window'
        /// </summary>
        private void RepaintInternal(HWindow window)
        {
            #region 画图像
#if H12
            HSystem.SetSystem("flush_graphic", "false");
#endif

            window.ClearWindow();
            int count = hObjList.Count;
            if (count != 0)
            {
                HObjectEntry entry;
                gC.ClearStateSettings();
                bool applyGcError = false;
                for (int i = 0; i < count; i++)
                {
                    entry = hObjList[i];
                    try
                    {
                        if (!applyGcError)
                        {
                            gC.ApplyContext(window, entry.GContext);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnHWndCtrlEvent(this, new HWindowEventArgs(HWindowEvent.GcError, ex.Message));
                    }
                    window.DispObj(entry.HObj);
                }
            }

            if (ROIController != null && (ROIMode == ROIMode.Include))
            {
                ROIController.PaintData(window);
            }

            #endregion 画图像

            #region 画文字

            if (Text != null && ShowText)
            {
#if H17
                window.DispText(Text.Value.Message, Text.Value.CoorSystem.ToString().ToLower(), Text.Value.Row, Text.Value.Column, Halcon.GetHColorString(Text.Value.Color), "box", Text.Value.UseBox.ToString());
#endif
#if H12
                Halcon.DispMessage(viewPort.HalconID, Text.Value.Message, Text.Value.Row, Text.Value.Column, Text.Value.CoorSystem.ToString().ToLower(), Halcon.GetHColorString(Text.Value.Color), Text.Value.UseBox);
#endif

            }
            //没有图像没有roi没有要显示的文字时，绘制背景文字
            else if (!string.IsNullOrWhiteSpace(backgroundText) && backgroundTextFont != null && (ROIController == null || ROIMode == ROIMode.Exclude || ROIController.ROIList.Count == 0) && count == 0)
            {
                var originalFont = window.GetFont();
                Halcon.SetFont(window, backgroundTextFont.FontFamily.Source,
                    backgroundTextFont.Size,
                    backgroundTextFont.Italic ? 1 : 0,
                    backgroundTextFont.Underline ? 1 : 0,
                    backgroundTextFont.Strikeout ? 1 : 0,
                    backgroundTextFont.Bold ? 1 : 0);
                //#if H17
                //                window.DispText(backgroundText, "window", "center", "center", Halcon.GetHColorString(backgroundTextColor), new HTuple(), new HTuple());
                //#endif
                //#if H12

                //#endif
                window.GetStringExtents(backgroundText, out int strDescent, out int strWidth, out int strHeight);
                var row = 0 + viewPort.WindowSize.Height / 2 - strHeight / 2;
                var col = 0 + viewPort.WindowSize.Width / 2 - strWidth / 2;
                Halcon.DispMessage(viewPort.HalconID, backgroundText, (int)row, (int)col, "window", Halcon.GetHColorString(backgroundTextColor), false);
                window.SetFont(originalFont);
            }

            #endregion 画文字

            #region 画辅助线

            viewPort.HalconWindow.SetColor(Halcon.GetHColorString(AuxiLineColor));
            viewPort.HalconWindow.SetLineWidth(AuxiLineWidth);
            if (AuxiliaryLineScale <= 0)
            {
                AuxiliaryLineScale = 1;
            }
            switch (AuxiliaryLine)
            {
                case AuxiliaryLine.None:
                    break;

                case AuxiliaryLine.ImageBigCross:
                    if (hObjList.Count > 0 && hObjList[0].HObj.IsImage())
                    {
                        HOperatorSet.GetImageSize(hObjList[0].HObj, out HTuple hw, out HTuple hh);
                        window.DispLine(hh / 2, 0, hh / 2, hw);
                        window.DispLine(0, hw / 2, hh, hw / 2);
                    }
                    break;

                case AuxiliaryLine.WindowBigCross:
                    var part = viewPort.HImagePart;
                    window.DispLine(part.Y + part.Height / 2.0 - 0.5, part.X - 1, part.Y + part.Height / 2.0 - 0.5, part.X + part.Width);
                    window.DispLine(part.Y - 1, part.X + part.Width / 2.0 - 0.5, part.Y + part.Height, part.X + part.Width / 2.0 - 0.5);
                    break;

                case AuxiliaryLine.LittleCross:
                    var p = viewPort.HImagePart;
                    HXLDCont x0 = new HXLDCont();
                    var crossSize = p.Height / 10 * AuxiliaryLineScale;
                    x0.GenCrossContourXld(p.Y + p.Height / 2.0 - 0.5, p.X + p.Width / 2.0 - 0.5, crossSize, 0);
                    window.DispXld(x0);
                    x0.Dispose();
                    break;

                case AuxiliaryLine.Ruler:
                    var p2 = viewPort.HImagePart;
                    var size = 30 / AuxiliaryLineScale;

                    var width = p2.Width / 30;
                    var height = p2.Height / 30;
                    HTuple beginRow = new HTuple();
                    HTuple beginCol = new HTuple();
                    HTuple endRow = new HTuple();
                    HTuple endCol = new HTuple();
                    for (int i = 1; i < 30; i++)
                    {
                        //上边缘
                        beginRow.Append(p2.Y - 0.5);
                        beginCol.Append((i * width) + p2.X - 0.5);
                        endRow.Append(p2.Height / (i % 5 == 0 ? size : 2 * size) + p2.Y - 0.5);
                        endCol.Append((i * width) + p2.X - 0.5);

                        //下边缘
                        beginRow.Append(p2.Y + p2.Height - 0.5);
                        beginCol.Append((i * width) + p2.X - 0.5);
                        endRow.Append(p2.Y + p2.Height - p2.Height / (i % 5 == 0 ? size : 2 * size) - 0.5);
                        endCol.Append((i * width) + p2.X - 0.5);

                        //左边缘
                        beginRow.Append((i * height) + p2.Y - 0.5);
                        beginCol.Append(p2.X - 0.5);
                        endRow.Append((i * height) + p2.Y - 0.5);
                        endCol.Append(p2.X + p2.Height / (i % 5 == 0 ? size : 2 * size) - 0.5);

                        //右边缘
                        beginRow.Append((i * height) + p2.Y - 0.5);
                        beginCol.Append((double)p2.X + p2.Width - 0.5);
                        endRow.Append((i * height) + p2.Y - 0.5);
                        endCol.Append(p2.X + p2.Width - p2.Height / (i % 5 == 0 ? size : 2 * size) - 0.5);
                    }
                    window.DispLine(beginRow, beginCol, endRow, endCol);
                    break;

                case AuxiliaryLine.Icon:
                    if (DrawAuxiLine != null)
                    {
                        drawAuxiLineMessage = DrawAuxiLine(viewPort);
                    }
                    break;

                default:
                    break;
            }

            #endregion 画辅助线
#if H12
            HSystem.SetSystem("flush_graphic", "true");
            window.DispLine(-100.0, -100.0, -100, -100);
#endif
            viewPort.Display();
        }

        /// <summary>
        /// 恢复图像在窗口中的初始位置
        /// </summary>
        public void RestoreImage()
        {
            if (hObjList.Count > 0 && hObjList[0].HObj.IsImage())
            {
                SetImageKeepRatio(hObjList[0].HObj);
                Repaint();
            }
        }

        #endregion Paint

        #region AuxiLine

        public string GetAuxiLineMessage()
        {
            switch (AuxiliaryLine)
            {
                case AuxiliaryLine.None:
                    return "(无辅助线)";

                case AuxiliaryLine.ImageBigCross:
                    return "图像大十字";

                case AuxiliaryLine.WindowBigCross:
                    return "窗口大十字";

                case AuxiliaryLine.LittleCross:
                    return $"小十字，半长：{viewPort.HImagePart.Height / 10 * AuxiliaryLineScale:0.#}";

                case AuxiliaryLine.Ruler:
                    return $"边缘尺，长度{30 * AuxiliaryLineScale:0.#}";

                case AuxiliaryLine.Icon:
                    return drawAuxiLineMessage;

                default:
                    break;
            }
            return "";
        }

        #endregion AuxiLine

        #region Font

        public void SetFont(Font font)
        {
            Font = font;
            if (font != null)
            {
                Halcon.SetFont(viewPort.HalconWindow, font.FontFamily.Source, (int)font.Size, font.Italic ? 1 : 0, font.Underline ? 1 : 0, font.Strikeout ? 1 : 0, font.Bold ? 1 : 0);
            }
        }

        #endregion Font

        #region Background

        public void SetWindowColor(Color color)
        {
            viewPort.HalconWindow.SetWindowParam("background_color", Halcon.GetHColorString(color));
            Repaint();
        }

        public void SetBackgroundText(string text, Font font, Color color)
        {
            //if (string.IsNullOrEmpty(text))
            //{
            //    throw new ArgumentNullException(nameof(text));
            //}
            backgroundText = text;
            backgroundTextFont = font;
            backgroundTextColor = color;
            ShouldRepaint();
        }

        //public void ShowBackgroundText(bool show)
        //{
        //    showBackgroundText = show;
        //    ShouldRepaint();
        //}

        private void ShouldRepaint()
        {
            if (hObjList.Count == 0)
            {
                Repaint();
            }
        }

        #endregion Background

        #region Graphic Context

        /// <summary>
        /// Adds an iconic object to the graphics stack similar to the way
        /// it is defined for the HDevelop graphics stack.
        /// </summary>
        /// <param name="obj">Iconic object</param>
        public void AddIconicVar(HObject obj, bool repaint = true)
        {
            if (!viewPort.CheckAccess())
            {
                viewPort.Dispatcher.Invoke(new Action(() =>
                {
                    AddIconicVarInternal(obj, repaint);
                }));
            }
            else
            {
                AddIconicVarInternal(obj, repaint);
            }
        }

        private void AddIconicVarInternal(HObject obj, bool repaint)
        {
            HObjectEntry entry;

            if (!obj.IsValid())
                return;
            entry = new HObjectEntry(obj, gC.CopyContext());
            if (!entry.HObj.IsValid())
            {
                return;
            }
            if (obj.IsImage())
            {
                if (KeepImageRatio && !KeepImagePart)
                {
                    SetImageKeepRatio(entry.HObj);
                }
                if (hObjList.Count > 0)
                {
                    ClearList(false);
                }
            }//if

            if (hObjList.Count >= MAXNUMOBJLIST)
            {
                RemoveLast();
                OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(HWindowEvent.GraphicStackOverflow));
            }
            else
            {
                hObjList.Add(entry);
                OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(HWindowEvent.GraphicStackAdd));
            }
            if (repaint)
            {
                Repaint();
            }
        }

        private void Remove(int index, bool repaint = true)
        {
            if (hObjList.Count > index)
            {
                var e = hObjList[index];
                e.HObj.Dispose();
                hObjList.RemoveAt(index);
                OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(HWindowEvent.GraphicStackRemove));
                if (repaint)
                {
                    Repaint();
                }
            }
        }

        public void Remove(HObject obj, bool repaint = true)
        {
            if (obj == null || !obj.IsInitialized())
            {
                return;
            }
            var keyList = hObjList.Select(entry => entry.OriginalKey).ToList();

            if (keyList.Contains(obj.Key))
            {
                var entry = hObjList[(keyList.IndexOf(obj.Key))];
                entry.Clear();
                hObjList.Remove(entry);
                OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(HWindowEvent.GraphicStackRemove));
            }
            if (repaint)
            {
                Repaint();
            }
        }

        public void RemoveLast(bool repaint = true)
        {
            if (hObjList.Count > 0)
            {
                Remove(hObjList.Count - 1);
            }
            OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(HWindowEvent.GraphicStackRemove));

            if (repaint)
            {
                Repaint();
            }
        }

        /// <summary>
        /// Clears all entries from the graphics stack
        /// </summary>
        public void ClearList(bool repaint = true)
        {
            foreach (var item in hObjList)
            {
                item?.HObj.Dispose();
            }
            hObjList.Clear();
            OnHWndCtrlEvent?.Invoke(this, new HWindowEventArgs(HWindowEvent.GraphicStackClear));
            if (repaint)
            {
                Repaint();
            }
        }

        /// <summary>
        /// Returns the number of items on the graphics stack
        /// </summary>
        public int GetListCount()
        {
            return hObjList.Count;
        }

        public HObject GetObject(int index)
        {
            if (index > -1 && hObjList.Count > index)
            {
                return new HObject(hObjList[index].HObj);
            }
            else
            {
                return new HObject();
            }
        }

        #endregion Graphic Context

        #region Settings

        public void SetColor(Color val)
        {
            gC.SetColor(Halcon.GetHColorString(val));
        }

        public void SetColored(HColored val)
        {
            gC.SetColored((int)val);
        }

        public void SetDrawMode(HRegionMode val)
        {
            gC.SetDrawMode(val.ToString().ToLower());
        }

        public void SetLineWidth(int val)
        {
            if (val < 1)
            {
                val = 1;
            }
            else if (val > 50)
            {
                val = 50;
            }
            gC.SetLineWidth(val);
        }

        public void SetLut(HLut val)
        {
            string lut = val.ToString().ToLower();
            if (viewPort.HalconWindow.QueryLut().SArr.Contains(lut))
            {
                gC.SetLut(lut);
            }
        }

        //public void SetPaint(HPaint val)
        //{
        //    string strVal = val.ToString().ToLower();
        //    if (strVal.StartsWith("_"))
        //    {
        //        strVal = strVal.Substring(1);
        //    }
        //    gC.SetPaint(strVal);
        //}

        public void SetShape(HRegionShape val)
        {
            string strVal = val.ToString().ToLower();
            gC.SetShape(strVal);
        }

        public void SetLineStyle(HLineStyle val)
        {
            HTuple tuple = new HTuple();
            switch (val)
            {
                case HLineStyle.Normal:
                    break;

                case HLineStyle.Stroke0:
                    tuple.Append(new int[] { 4, 1 });
                    break;

                case HLineStyle.Stroke1:
                    tuple.Append(new int[] { 10, 5 });
                    break;

                case HLineStyle.Stroke2:
                    tuple.Append(new int[] { 20, 7, 3, 7 });
                    break;

                default:
                    break;
            }
            gC.SetLineStyle(tuple);
        }

        #endregion Settings
    }//end of class
}
