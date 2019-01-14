using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    public sealed class ROICross : ROI
    {
        private double row1, col1;
        private double row2, col2;
        private double row3, col3;
        private double row4, col4;
        private double rowMid, colMid;
        private readonly double addLength = 50;
        private readonly double minDist = 10;
        private bool active;

        public ROICross()
        {
            NumHandles = 5;
            ActiveHandleIdx = 0;
        }

        public override void CreateROI(double midX, double midY)
        {
            rowMid = midY;
            colMid = midX;

            row1 = rowMid - 25;
            col1 = colMid - 25;

            row2 = rowMid - 25;
            col2 = colMid + 25;

            row3 = rowMid + 25;
            col3 = colMid - 25;

            row4 = rowMid + 25;
            col4 = colMid + 25;
        }

        public override void DisplayActive(HWindow window)
        {
            switch (ActiveHandleIdx)
            {
                case 0:
                    Halcon.DispXldRect2(window, rowMid, colMid, 0, HandleSize, HandleSize);
                    break;

                case 1:
                    Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                    break;

                case 2:
                    Halcon.DispXldRect2(window, row2, col2, 0, HandleSize, HandleSize);
                    break;

                case 3:
                    Halcon.DispXldRect2(window, row3, col3, 0, HandleSize, HandleSize);
                    break;

                case 4:
                    Halcon.DispXldRect2(window, row4, col4, 0, HandleSize, HandleSize);
                    break;

                default:
                    break;
            }
            if (ShowTip)
            {
                var tip = $"Center:({rowMid:0.0},{colMid:0.0})\nRWidth:{rowMid - row1:0.0}\nCWidth:{colMid - col1:0.0}";
                Halcon.DispMessage(window.Handle, tip, (int)rowMid + 10, (int)colMid + 10, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        public override double DistToClosestHandle(double x, double y)
        {
            if (!active)
            {
                double max = 10000;
                double[] dist = new double[NumHandles];
                dist[0] = Halcon.DistancePp(y, x, rowMid, colMid);
                dist[1] = Halcon.DistancePp(y, x, row1, col1);
                dist[2] = Halcon.DistancePp(y, x, row2, col2);
                dist[3] = Halcon.DistancePp(y, x, row3, col3);
                dist[4] = Halcon.DistancePp(y, x, row4, col4);
                for (int i = 0; i < NumHandles; i++)
                {
                    if (dist[i] < max)
                    {
                        max = dist[i];
                        ActiveHandleIdx = i;
                    }
                }// end of for

                return dist[ActiveHandleIdx];
            }
            else
            {
                ActiveHandleIdx = 0;
                return Halcon.DistancePp(y, x, rowMid, colMid);
            }
        }

        public override void Draw(HWindow window)
        {
            //double distance = Math.Abs(rowMid - row1);
            if (!active)
            {
                double[] rectR = new double[] { row1, row2, row3, row4 };
                double[] rectC = new double[] { col1, col2, col3, col4 };
                //double[] phi = new double[] { 0, 0, 0, 0 };
                //double[] length = new double[] { 5, 5, 5, 5 };
                for (int i = 0; i < 4; i++)
                {
                    Halcon.DispXldRect2(window, rectR[i], rectC[i], 0, HandleSize, HandleSize);
                }

                //window.DispRectangle2(rectR, rectC, phi, length, length);
            }
            double distanceR = Math.Abs(rowMid - row1);
            double distanceC = Math.Abs(colMid - col1);
            double[] r1 = new double[] { row1, row1, row2, row2, row3, row3, row4, row4 };
            double[] c1 = new double[] { col1, col1, col2, col2, col3, col3, col4, col4 };
            double[] r2 = new double[] { row1, row1 - distanceR, row2, row2 - distanceR, row3, row3 + distanceR, row4 + distanceR, row4 };
            double[] c2 = new double[] { col1 - distanceC, col1, col2 + distanceC, col2, col3 - distanceC, col3, col4, col4 + distanceC };

            distanceR = distanceR + addLength;
            distanceC = distanceC + addLength;
            double[] rr2 = new double[] { row1, row1 - distanceR, row2, row2 - distanceR, row3, row3 + distanceR, row4 + distanceR, row4 };
            double[] cc2 = new double[] { col1 - distanceC, col1, col2 + distanceC, col2, col3 - distanceC, col3, col4, col4 + distanceC };
            window.SetLineStyle(new HTuple(5, 5));
            //window.DispLine(r1, c1, rr2, cc2);
            for (int i = 0; i < r1.Length; i++)
            {
                Halcon.DispXldLine(window, r1[i], c1[i], rr2[i], cc2[i]);
            }
            window.SetLineStyle(new HTuple());
            //window.DispLine(r1, c1, r2, c2);
            for (int i = 0; i < r1.Length; i++)
            {
                Halcon.DispXldLine(window, r1[i], c1[i], r2[i], c2[i]);
            }
            HXLDCont xld = new HXLDCont();
            xld.GenCrossContourXld(rowMid - 0.5, colMid - 0.5, HandleSize * 2, 0);
            window.DispXld(xld);
            xld.Dispose();
        }

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            return 0.0;
        }

        public override HTuple GetModelData()
        {
            return new HTuple(rowMid, colMid, row1, col1, row2, col2, row3, col3, row4, col4);
        }

        public override void CreateROI(HTuple data)
        {
            rowMid = data.DArr[0];
            colMid = data.DArr[1];
            row1 = data.DArr[2];
            col1 = data.DArr[3];
            row2 = data.DArr[4];
            col2 = data.DArr[5];
            row3 = data.DArr[6];
            col3 = data.DArr[7];
            row4 = data.DArr[8];
            col4 = data.DArr[9];
        }

        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            HRegion r1 = new HRegion();
            HRegion r2 = new HRegion();
            double distanceR = Math.Abs(rowMid - row1);
            double distanceC = Math.Abs(colMid - col1);

            r1.GenRectangle1(row1, col1 - distanceC, row4, col4 + distanceC);
            r2.GenRectangle1(row1 - distanceR, col1, row4 + distanceR, col4);
            region = r1.Union2(r2);
            r1.Dispose();
            r2.Dispose();
            return region;
        }

        public override void MoveByHandle(double x, double y)
        {
            double lenR, lenC;

            switch (ActiveHandleIdx)
            {
                case 0:
                    lenR = rowMid - y;
                    lenC = colMid - x;
                    rowMid = y;
                    row1 -= lenR;
                    row2 -= lenR;
                    row3 -= lenR;
                    row4 -= lenR;
                    colMid = x;
                    col1 -= lenC;
                    col2 -= lenC;
                    col3 -= lenC;
                    col4 -= lenC;
                    break;

                case 1:
                    lenR = row1 - y;
                    lenC = col1 - x;
                    if (rowMid - y > minDist)
                    {
                        row1 = y;
                        row2 -= lenR;
                        row3 += lenR;
                        row4 += lenR;
                    }
                    if (colMid - x > minDist)
                    {
                        col1 = x;
                        col2 += lenC;
                        col3 -= lenC;
                        col4 += lenC;
                    }
                    break;

                case 2:
                    lenR = row2 - y;
                    lenC = col2 - x;
                    if (rowMid - y > minDist)
                    {
                        row2 = y;
                        row1 -= lenR;
                        row3 += lenR;
                        row4 += lenR;
                    }
                    if (x - colMid > minDist)
                    {
                        col2 = x;
                        col1 += lenC;
                        col3 += lenC;
                        col4 -= lenC;
                    }
                    break;

                case 3:
                    lenR = row3 - y;
                    lenC = col3 - x;
                    if (y - rowMid > minDist)
                    {
                        row3 = y;
                        row1 += lenR;
                        row2 += lenR;
                        row4 -= lenR;
                    }
                    if (colMid - x > minDist)
                    {
                        col3 = x;
                        col1 -= lenC;
                        col2 += lenC;
                        col4 += lenC;
                    }
                    break;

                case 4:

                    lenR = row4 - y;
                    lenC = col4 - x;
                    if (y - rowMid > minDist)
                    {
                        row4 = y;
                        row1 += lenR;
                        row2 += lenR;
                        row3 -= lenR;
                    }
                    if (x - colMid > minDist)
                    {
                        col4 = x;
                        col1 += lenC;
                        col2 -= lenC;
                        col3 += lenC;
                    }
                    break;

                default:
                    break;
            }
        }

        public override void MoveTo(double x, double y)
        {
            double lenR, lenC;

            lenR = rowMid - y;
            lenC = colMid - x;
            rowMid = y;
            row1 -= lenR;
            row2 -= lenR;
            row3 -= lenR;
            row4 -= lenR;
            colMid = x;
            col1 -= lenC;
            col2 -= lenC;
            col3 -= lenC;
            col4 -= lenC;
        }

        public override void SetOnlyActiveMoveHandle(bool active)
        {
            this.active = active;
            if (active)
            {
                ActiveHandleIdx = 0;
            }
        }

        public override void RelativeMove(double x, double y)
        {
            MoveTo(colMid + x, rowMid + y);
        }

        public override void Scale(double scaleFactor)
        {
            row1 = (1 - scaleFactor) * rowMid + scaleFactor * row1;
            col1 = (1 - scaleFactor) * colMid + scaleFactor * col1;

            row2 = (1 - scaleFactor) * rowMid + scaleFactor * row2;
            col2 = (1 - scaleFactor) * colMid + scaleFactor * col2;

            row3 = (1 - scaleFactor) * rowMid + scaleFactor * row3;
            col3 = (1 - scaleFactor) * colMid + scaleFactor * col3;

            row4 = (1 - scaleFactor) * rowMid + scaleFactor * row4;
            col4 = (1 - scaleFactor) * colMid + scaleFactor * col4;
        }

        public override void ReSize()
        {
            CreateROI(colMid, rowMid);
        }
    }
}
