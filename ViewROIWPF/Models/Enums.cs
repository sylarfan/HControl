using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewROIWPF.Helper;

namespace ViewROIWPF.Models
{
    internal enum GraphicalMode
    {
        /// <summary>
        /// Graphical mode for the output color (see dev_set_color)
        /// </summary>
        Color,

        /// <summary>
        /// Graphical mode for the multi-color output (see dev_set_colored)
        /// </summary>
        Colored,

        /// <summary>
        /// Graphical mode for the line width (see set_line_width)
        /// </summary>
        LineWidth,

        /// <summary>
        /// Graphical mode for the drawing (see set_draw)
        /// </summary>
        DrawMode,

        /// <summary>
        /// Graphical mode for the drawing shape (see set_shape)
        /// </summary>
        Shape,

        /// <summary>
        /// Graphical mode for the LUT (lookup table) (see set_lut)
        /// </summary>
        Lut,

        /// <summary>
        /// Graphical mode for the line style (see set_line_style)
        /// </summary>
        LineStyle,

        /// <summary>
        /// Graphical mode for the painting (see set_paint)
        /// </summary>
        Paint
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ROIOperatorFlag
    {
        [Description("无")]
        None = 23,
        [Description("加")]
        Positive = 21,
        [Description("减")]
        Negtive = 22
    }

    //public enum ViewMode
    //{
    //    None = 10,
    //    Zoom = 11,
    //    Move = 12,
    //}

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ROIMode
    {
        [Description("包含InteractiveROI")]
        Include = 1,
        [Description("不包含InteractiveROI")]
        Exclude = 2
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum AuxiliaryLine
    {
        [Description("(无)")]
        None = 0,
        [Description("图像大十字")]
        ImageBigCross,
        [Description("窗口大十字")]
        WindowBigCross,
        [Description("窗口小十字")]
        LittleCross,
        [Description("尺子")]
        Ruler,
        [Description("其他图形")]
        Icon
    }

    public enum CoorSystem
    {
        Window = 0,
        Image
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum HRegionMode
    {
        [Description("填充")]
        Fill = 0,
        [Description("描边")]
        Margin
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum HColored : int
    {
        [Description("三色")]
        Color3 = 3,
        [Description("六色")]
        Color6 = 6,
        [Description("十二色")]
        Color12 = 12
    }

    /// <summary>
    /// Operator:QueryLut lists the names of all look-up-tables.
    /// They differ from each other in the area used for gray values.
    /// </summary>
    public enum HLut
    {
        /// <summary>
        /// As <see cref="Linear"/>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Linear increasing of gray values from 0 (black) to 255 (white).
        /// </summary>
        Linear,

        /// <summary>
        /// Inverse function of <see cref="Linear"/>.
        /// </summary>
        Inverse,

        /// <summary>
        /// Gray values increase according to square function.
        /// </summary>
        Sqr,

        /// <summary>
        /// Inverse function of <see cref="Sqr"/>.
        /// </summary>
        Inv_sqr,

        /// <summary>
        /// Gray values increase according to cubic function.
        /// </summary>
        Cube,

        /// <summary>
        /// Inverse function of <see cref="Cube"/>.
        /// </summary>
        Inv_cube,

        /// <summary>
        /// Gray values increase according to square-root function.
        /// </summary>
        Sqrt,

        /// <summary>
        /// Inverse Function of <see cref="Sqrt"/>.
        /// </summary>
        Inv_sqrt,

        /// <summary>
        /// Gray values increase according to cubic-root function.
        /// </summary>
        Cubic_root,

        /// <summary>
        /// Inverse Function of <see cref="Cubic_root"/>.
        /// </summary>
        Inv_cubic_root,

        /// <summary>
        /// Linear transition from red via green to blue.
        /// </summary>
        Color1,

        /// <summary>
        /// Smooth transition from yellow via red, blue to green.
        /// </summary>
        Color2,

        /// <summary>
        /// Smooth transition from yellow via red, blue, green, red to blue.
        /// </summary>
        Color3,

        /// <summary>
        /// Smooth transition from yellow via red to blue.
        /// </summary>
        Color4,

        /// <summary>
        /// Displaying the three colors red, green and blue.
        /// </summary>
        Three,

        /// <summary>
        /// Displaying the six basic colors yellow, red, magenta, blue, cyan and green.
        /// </summary>
        Six,

        /// <summary>
        /// Displaying 12 colors.
        /// </summary>
        Twelve,

        /// <summary>
        /// Displaying 24 colors.
        /// </summary>
        Twenty_four,

        /// <summary>
        /// Displaying the spectral colors from red via green to blue.
        /// </summary>
        Rainbow,

        /// <summary>
        /// Temperature table from black via red, yellow to white.
        /// </summary>
        Temperature,

        Cyclic_gray,
        Cyclic_temperature,
        Hsi,

        /// <summary>
        /// Color changement after every pixel within the table alternating the six basic colors.
        /// </summary>
        Change1,

        /// <summary>
        /// Fivefold color changement from green via red to blue.
        /// </summary>
        Change2,

        /// <summary>
        /// Threefold color changement from green via red to blue.
        /// </summary>
        Change3
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum HPaint
    {
        /// <summary>
        /// Optimal display on given hardware
        /// </summary>
        Default = 0,

        //Output of the gray value profile along the given line.
        //Row,
        //output of the gray value profile along the given column
        //Column,
        /// <summary>
        /// Gray value output as height lines
        /// </summary>
        Contourline,

        /// <summary>
        /// Gray values are interpreted as 3D data:
        /// </summary>
        _3d_plot_lines,

        /// <summary>
        /// Like <see cref="_3d_plot_lines"/> , but computes hidden lines
        /// </summary>
        _3d_plot_hidden_lines,

        _3d_plot_point,

        /// <summary>
        /// Gray value output as histogram. position default: max. size, in the window center
        /// </summary>
        Histogram
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum HLineStyle
    {
        [Description("实线")]
        Normal,
        [Description("虚线1")]
        Stroke0,
        [Description("虚线2")]
        Stroke1,
        [Description("虚线3")]
        Stroke2,
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum HRegionShape
    {
        /// <summary>
        /// The shape is displayed unchanged. Nevertheless modifications via parameters like set_line_width or
        /// set_line_approx can take place. This is also true for all other modes.
        /// </summary>
        [Description("原始")]
        Original = 0,

        /// <summary>
        /// Each region is displayed by the smallest surrounding circle.
        /// (See smallest_circle.)
        /// </summary>
        [Description("外接圆")]
        Outer_circle,

        /// <summary>
        /// Each region is displayed by the largest included circle.
        /// (See inner_circle.)
        /// </summary>
        [Description("内接圆")]
        Inner_circle,

        /// <summary>
        /// Each region is displayed by an ellipse with the same moments and orientation
        /// (See elliptic_axis.)
        /// </summary>
        [Description("椭圆")]
        Ellipse,

        /// <summary>
        /// Each region is displayed by the smallest surrounding rectangle parallel to the coordinate axes.
        /// (See smallest_rectangle1.)
        /// </summary>
        [Description("正交矩形")]
        Rectangle1,

        /// <summary>
        /// Each region is displayed by the smallest surrounding rectangle.
        /// (See smallest_rectangle2.)
        /// </summary>
        [Description("旋转矩形")]
        Rectangle2,

        /// <summary>
        /// Each region is displayed by its convex hull
        /// (See convexity.)
        /// </summary>
        [Description("凸包")]
        Convex,

        /// <summary>
        /// Each region is displayed by the icon set with set_icon in the center of gravity.
        /// </summary>
        [Description("其他图形")]
        Icon
    }
}
