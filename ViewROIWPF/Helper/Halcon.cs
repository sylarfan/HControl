using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace ViewROIWPF.Helper
{
    internal static class Halcon
    {
        private static readonly char[] split = new char[] { '\r', '\n' };

        internal static void DispMessage(IntPtr window, string message, int row = -1, int column = -1, string coorSystem = "window", string color = "black", bool box = true)
        {
            //#if H17
            //            HOperatorSet.DispText(window, message, coorSystem, row, column, color, "box", box.ToString());
            //#endif
            string coor;
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            switch (coorSystem)
            {
                case null:
                    coor = "window";
                    break;

                case "window":
                case "image":
                    coor = coorSystem;
                    break;

                default:
                    coor = "window";
                    break;
            }
            HOperatorSet.GetPart(window, out HTuple row1Part, out HTuple col1Part, out HTuple row2Part, out HTuple col2Part);
            HOperatorSet.GetWindowExtents(window, out HTuple rowWin, out HTuple colWin, out HTuple widthWin, out HTuple heightWin);
            HOperatorSet.SetPart(window, 0, 0, heightWin - 1, widthWin - 1);
            if (row < 0)
            {
                row = 12;
            }
            if (column < 0)
            {
                column = 12;
            }
            color = color ?? "";
            string[] strShow = message.Split(split).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            HOperatorSet.GetFontExtents(window, out HTuple maxAscent, out HTuple maxDescent, out HTuple maxWidth, out HTuple maxHeight);
            double factorRow = 0, factorCol = 0;
            if (coor != "window")
            {
                factorRow = 1.0 * heightWin / (row2Part - row1Part + 1);
                factorCol = 1.0 * widthWin / (col2Part - col1Part + 1);
                row = Convert.ToInt32((int)(row - row1Part.I + 0.5) * factorRow);
                column = Convert.ToInt32((int)(column - col1Part.I + 0.5) * factorCol);
            }
            bool useShadow = true;
            string shadowColor = "gray";
            string strBox = "";
            if (box)
            {
                strBox = "#fce9d4";
                shadowColor = "#f28d26";
                List<int> Width = new List<int>();
                foreach (var str in strShow)
                {
                    HOperatorSet.GetStringExtents(window, str, out HTuple ascent, out HTuple descent, out HTuple width, out HTuple height);
                    Width.Add(width);
                }
                int frameHeight = maxHeight * strShow.Length;
                int frameWidth = Width.Max();
                int r2 = row + frameHeight;
                int c2 = column + frameWidth;
                HOperatorSet.GetDraw(window, out HTuple drawMode);
                HOperatorSet.SetDraw(window, "fill");
                HOperatorSet.SetColor(window, shadowColor);
                if (useShadow)
                {
                    HOperatorSet.DispRectangle1(window, (row + 1) * 1.0, column + 1, r2 + 1, c2 + 1);
                }
                HOperatorSet.SetColor(window, strBox);
                HOperatorSet.DispRectangle1(window, row, column, r2, c2);
                HOperatorSet.SetDraw(window, drawMode);
            }
            HOperatorSet.SetColor(window, color);
            int i = 0;
            foreach (var item in strShow)
            {
                int r1 = row + maxHeight * i;
                HOperatorSet.SetTposition(window, r1, column);
                HOperatorSet.WriteString(window, item);
                i++;
            }
            HOperatorSet.SetPart(window, row1Part, col1Part, row2Part, col2Part);
        }

        internal static void SetFont(HWindow window, string fontName = "fixed", int size = 12, int slant = 0, int underline = 0, int strikeout = 0, int bold = 0)
        {
            string formatStr = $"-{fontName}-{size}-*-{slant}-{underline}-{strikeout}-{bold}-";
            window.SetFont(formatStr);
        }

        internal static bool IsImage(this HObject obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj is HImage)
            {
                return true;
            }
            if (obj.IsInitialized())
            {
                var objMsg = obj.GetObjClass();
                if (objMsg.Length == 1 && objMsg.S == "image")
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsValid(this HObject from)
        {
            bool result = false;
            if (@from != null)
            {
                result = from.Key != IntPtr.Zero;
            }

            return result;
        }

        internal static string GetHColorString(Color color)
        {
#if H12
            return $"#{color.R:x2}{color.G:x2}{color.B:x2}";
#endif
#if H17
            return $"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";
#endif
        }

        internal static double DistancePp(double row1, double col1, double row2, double col2)
        {
            return Math.Sqrt(Math.Pow((row2 - row1), 2) + Math.Pow((col2 - col1), 2));
        }

        internal static void DispXldRect2(HWindow window, double r, double c, double phi, double l1, double l2)
        {
            var xldTemp = new HXLDCont();
            xldTemp.GenRectangle2ContourXld(r - 0.5, c - 0.5, phi, l1, l2);
            xldTemp.DispObj(window);
            xldTemp.Dispose();
        }

        internal static void DispXldLine(HWindow window, double r1, double c1, double r2, double c2)
        {
            var xldTemp = new HXLDCont();
            xldTemp.GenContourPolygonXld(new double[] { r1 - 0.5, r2 - 0.5 }, new double[] { c1 - 0.5, c2 - 0.5 });
            xldTemp.DispObj(window);
            xldTemp.Dispose();
        }

        internal static void DispXldPolygonLine(HWindow window, double[] r, double[] c)
        {
            var xldTemp = new HXLDCont();
            xldTemp.GenContourPolygonXld(r, c);
            xldTemp.DispObj(window);
            xldTemp.Dispose();
        }

        public static HXLDCont GenArrowContourXld(double row1, double col1, double row2, double col2, double headLength, double headWidth)
        {
            //This procedure generates arrow shaped XLD contours,
            //pointing from (Row1, Column1) to (Row2, Column2).
            //If starting and end point are identical, a contour consisting
            //of a single point is returned.
            //
            //input parameteres:
            //Row1, Column1: Coordinates of the arrows' starting points
            //Row2, Column2: Coordinates of the arrows' end points
            //HeadLength, HeadWidth: Size of the arrow heads in pixels
            //
            //output parameter:
            //Arrow: The resulting XLD contour
            //
            //The input tuples Row1, Column1, Row2, and Column2 have to be of
            //the same length.
            //HeadLength and HeadWidth either have to be of the same length as
            //Row1, Column1, Row2, and Column2 or have to be a single element.
            //If one of the above restrictions is violated, an error will occur.
            //
            //
            //Init
            //
            //Calculate the arrow length
            //HOperatorSet.DistancePp(row1, col1, row2, col2, out hv_Length);
            var length = DistancePp(row1, col1, row2, col2);
            if (length == 0)
            {
                length = -1;
            }
            //
            //Mark arrows with identical start and end point
            //(set Length to -1 to avoid division-by-zero exception)

            //Calculate auxiliary variables.
            var dr = (row2 - row1) / length;
            var dc = (col2 - col1) / length;
            var halfHeadWidth = headWidth / 2;
            //hv_DR = (1.0 * (row2 - row1)) / hv_Length;
            //hv_DC = (1.0 * (col2 - col1)) / hv_Length;
            //hv_HalfHeadWidth = headWidth / 2.0;
            //
            //Calculate end points of the arrow head.
            var rowP1 = (row1 + ((length - headLength) * dr)) + (halfHeadWidth * dc);
            var colP1 = (col1 + ((length - headLength) * dc)) - (halfHeadWidth * dr);
            var rowP2 = (row1 + ((length - headLength) * dr)) - (halfHeadWidth * dc);
            var colP2 = (col1 + ((length - headLength) * dc)) + (halfHeadWidth * dr);
            var xld = new HXLDCont();
            if (length == -1)
            {
                //Create_ single points for arrows with identical start and end point
                //ho_TempArrow.Dispose();
                xld.GenContourPolygonXld(row1 - 0.5, col1 - 0.5);
                //HOperatorSet.GenContourPolygonXld(out ho_TempArrow, row1.TupleSelect(hv_Index),
                //    col1.TupleSelect(hv_Index));
            }
            else
            {
                //Create arrow contour
                //ho_TempArrow.Dispose();
                xld.GenContourPolygonXld(new double[]
                {
                    row1-0.5,row2-0.5,rowP1-0.5,row2-0.5,rowP2-0.5,row2-0.5
                },
                new double[]
                {
                    col1-0.5,col2-0.5,colP1-0.5,col2-0.5,colP2-0.5,col2-0.5
                });
            }
            return xld;
        }
    }
}
