using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    public class ROISquare : ROI
    {
        private double row1, col1, row2, col2;
        private double midR, midC;
        private bool active;

        public ROISquare()
        {
            NumHandles = 5;
            ActiveHandleIdx = 4;
        }

        public override void CreateROI(double midX, double midY)
        {
            midR = midY;
            midC = midX;

            row1 = midR - 50;
            col1 = midC - 50;
            row2 = midR + 50;
            col2 = midC + 50;
        }

        public override void DisplayActive(HWindow window)
        {
            switch (ActiveHandleIdx)
            {
                case 0:
                    Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(row1, col1, 0, 5, 5);
                    break;

                case 1:
                    Halcon.DispXldRect2(window, row1, col2, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(row1, col2, 0, 5, 5);
                    break;

                case 2:
                    Halcon.DispXldRect2(window, row2, col2, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(row2, col2, 0, 5, 5);
                    break;

                case 3:
                    Halcon.DispXldRect2(window, row2, col1, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(row2, col1, 0, 5, 5);
                    break;

                case 4:
                    Halcon.DispXldRect2(window, midR, midC, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(midR, midC, 0, 5, 5);
                    break;
            }
            if (ShowTip)
            {
                double[] length = new double[2];
                length[0] = Halcon.DistancePp(row1, col1, row1, col2);
                length[1] = Halcon.DistancePp(row1, col1, row2, col1);
                string tip = $"Center:({midR:0.0},{midC:0.0})\nL1:{length[0]:0.0}L2:{length[1]:0.0}";
#if HALCON17

#else

#endif

                Halcon.DispMessage(window.Handle, tip, (int)midR + 10, (int)midC + 10, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        public override double DistToClosestHandle(double x, double y)
        {
            if (!active)
            {
                double max = 10000;
                double[] val = new double[NumHandles];

                midR = ((row2 - row1) / 2) + row1;
                midC = ((col2 - col1) / 2) + col1;

                val[0] = Halcon.DistancePp(y, x, row1, col1); // upper left
                val[1] = Halcon.DistancePp(y, x, row1, col2); // upper right
                val[2] = Halcon.DistancePp(y, x, row2, col2); // lower right
                val[3] = Halcon.DistancePp(y, x, row2, col1); // lower left
                val[4] = Halcon.DistancePp(y, x, midR, midC); // midpoint

                for (int i = 0; i < NumHandles; i++)
                {
                    if (val[i] < max)
                    {
                        max = val[i];
                        ActiveHandleIdx = i;
                    }
                }// end of for

                return val[ActiveHandleIdx];
            }
            else
            {
                ActiveHandleIdx = 4;
                return Halcon.DistancePp(y, x, midR, midC);
            }
        }

        public override void Draw(HWindow window)
        {
            window.DispRectangle1(row1, col1, row2, col2);
            if (!active)
            {
                //window.DispCircle(row1, col1, 5);
                //window.DispCircle(row1, col2, 5);
                //window.DispCircle(row2, col1, 5);
                //window.DispCircle(row2, col2, 5);
                Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, row1, col2, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, row2, col1, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, row2, col2, 0, HandleSize, HandleSize);

                //window.DispRectangle2(row1, col1, 0, 5, 5);
                //window.DispRectangle2(row1, col2, 0, 5, 5);
                //window.DispRectangle2(row2, col2, 0, 5, 5);
                //window.DispRectangle2(row2, col1, 0, 5, 5);
            }

            HXLDCont xld = new HXLDCont();
            xld.GenCrossContourXld(midR - 0.5, midC - 0.5, HandleSize * 2, 0);
            window.DispXld(xld);
            xld.Dispose();
        }

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            return 0.0;
        }

        public override HTuple GetModelData()
        {
            return new HTuple(new double[] { row1, col1, row2, col2 });
        }

        public override void CreateROI(HTuple data)
        {
            row1 = data.DArr[0];
            col1 = data.DArr[1];
            row2 = data.DArr[2];
            col2 = data.DArr[3];

            midR = (row2 + row1) / 2; /*+ row1;*/
            midC = (col2 + col1) / 2;
        }

        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenRectangle1(row1, col1, row2, col2);
            return region;
        }

        public override void MoveByHandle(double newX, double newY)
        {
            double len1, len2;
            double tmp;

            switch (ActiveHandleIdx)
            {
                case 0: // upper left
                    len1 = midR - newY;
                    len2 = midC - newX;
                    if (len1 > 0 && len2 > 0)
                    {
                        var max = Math.Max(len1, len2);
                        len1 = len2 = max;
                    }
                    else
                    {
                        var min = Math.Min(len1, len2);
                        len1 = len2 = min;
                    }

                    row1 = midR - len1;
                    col1 = midC - len1;
                    row2 = midR + len1;
                    col2 = midC + len1;
                    break;

                case 1: // upper right
                    len1 = midR - newY;
                    len2 = midC - newX;
                    if (len1 > 0 && len2 < 0)
                    {
                        var max = Math.Max(len1, 0 - len2);
                        len1 = len2 = max;
                    }
                    else
                    {
                        var min = Math.Min(len1, 0 - len2);
                        len1 = len2 = min;
                    }

                    row1 = midR - len1;
                    col1 = midC - len1;
                    row2 = midR + len1;
                    col2 = midC + len1;
                    break;

                case 2: // lower right
                    len1 = midR - newY;
                    len2 = midC - newX;
                    if (len1 < 0 && len2 > 0)
                    {
                        var max = Math.Max(0 - len1, len2);
                        len1 = len2 = max;
                    }
                    else
                    {
                        var min = Math.Min(0 - len1, len2);
                        len1 = len2 = min;
                    }

                    row1 = midR - len1;
                    col1 = midC - len1;
                    row2 = midR + len1;
                    col2 = midC + len1;

                    break;

                case 3: // lower left
                    len1 = midR - newY;
                    len2 = midC - newX;
                    if (len1 < 0 && len2 < 0)
                    {
                        var max = Math.Max(0 - len1, 0 - len2);
                        len1 = len2 = max;
                    }
                    else
                    {
                        var min = Math.Min(0 - len1, 0 - len2);
                        len1 = len2 = min;
                    }

                    row1 = midR - len1;
                    col1 = midC - len1;
                    row2 = midR + len1;
                    col2 = midC + len1;
                    break;

                case 4: // midpoint
                    len1 = ((row2 - row1) / 2);
                    len2 = ((col2 - col1) / 2);

                    row1 = newY - len1;
                    row2 = newY + len1;

                    col1 = newX - len2;
                    col2 = newX + len2;

                    break;
            }

            if (row2 <= row1)
            {
                tmp = row1;
                row1 = row2;
                row2 = tmp;
            }

            if (col2 <= col1)
            {
                tmp = col1;
                col1 = col2;
                col2 = tmp;
            }

            midR = ((row2 - row1) / 2) + row1;
            midC = ((col2 - col1) / 2) + col1;
        }

        public override void MoveTo(double x, double y)
        {
            double len1, len2;
            double tmp;

            len1 = ((row2 - row1) / 2);
            len2 = ((col2 - col1) / 2);

            row1 = y - len1;
            row2 = y + len1;

            col1 = x - len2;
            col2 = x + len2;
            if (row2 <= row1)
            {
                tmp = row1;
                row1 = row2;
                row2 = tmp;
            }

            if (col2 <= col1)
            {
                tmp = col1;
                col1 = col2;
                col2 = tmp;
            }

            midR = ((row2 - row1) / 2) + row1;
            midC = ((col2 - col1) / 2) + col1;
        }

        public override void SetOnlyActiveMoveHandle(bool active)
        {
            this.active = active;
            if (active)
            {
                ActiveHandleIdx = 4;
            }
        }

        public override void RelativeMove(double x, double y)
        {
            MoveTo(midC + x, midR + y);
        }

        public override void Scale(double scaleFactor)
        {
            row1 = (1 - scaleFactor) * midR + scaleFactor * row1;
            col1 = (1 - scaleFactor) * midC + scaleFactor * col1;

            row2 = (1 - scaleFactor) * midR + scaleFactor * row2;
            col2 = (1 - scaleFactor) * midC + scaleFactor * col2;
        }

        public override void ReSize()
        {
            CreateROI(midC, midR);
        }
    }
}
