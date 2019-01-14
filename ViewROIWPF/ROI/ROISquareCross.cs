using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    public class ROISquareCross : ROI
    {
        private double row1, col1;
        private double row2, col2;
        private double row3, col3;
        private double row4, col4;

        private double rowSize, colSize;

        private double rowMid, colMid;
        private readonly double minDist = 10;
        private bool activeOnlyMove;

        public ROISquareCross()
        {
            NumHandles = 3;
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

            rowSize = rowMid - 25 - 50;
            colSize = colMid - 25;
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
                    Halcon.DispXldRect2(window, rowSize, colSize, 0, HandleSize, HandleSize);
                    break;

                default:
                    break;
            }
            if (ShowTip)
            {
                var tip = $"Center:({rowMid:0.0},{colMid:0.0})\nRWidth:{rowMid - row1:0.0},CWidth:{colMid - col1:0.0}\nLineLength:{row1 - rowSize:0.0}";
                Halcon.DispMessage(window.Handle, tip, (int)rowMid + 10, (int)colMid + 10, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        public override double DistToClosestHandle(double x, double y)
        {
            if (!activeOnlyMove)
            {
                double max = 10000;
                double[] dist = new double[NumHandles];
                dist[0] = Halcon.DistancePp(y, x, rowMid, colMid);
                dist[1] = Halcon.DistancePp(y, x, row1, col1);
                dist[2] = Halcon.DistancePp(y, x, rowSize, colSize);
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
            if (!activeOnlyMove)
            {
                double[] rectR = new double[] { row1, rowSize };
                double[] rectC = new double[] { col1, colSize };
                for (int i = 0; i < 2; i++)
                {
                    Halcon.DispXldRect2(window, rectR[i], rectC[i], 0, HandleSize, HandleSize);
                }
            }
            //Halcon.DispXldRect2(window, rowMid, colMid, 0, 5, 5);

            double distanceR = Math.Abs(rowMid - row1);
            double distanceC = Math.Abs(colMid - col1);
            double lineLength = row1 - rowSize;
            window.DispPolygon(new double[] { row1, row1, rowSize }, new double[] { col1 - lineLength, col1, col1 });
            window.DispPolygon(new double[] { row2 - lineLength, row2, row2 }, new double[] { col2, col2, col2 + lineLength });
            window.DispPolygon(new double[] { row3, row3, row3 + lineLength }, new double[] { col3 - lineLength, col3, col3 });
            window.DispPolygon(new double[] { row4 + lineLength, row4, row4 }, new double[] { col4, col4, col4 + lineLength });
            //Halcon.DispXldPolygonLine(window, new double[] { row1, row1, rowSize }, new double[] { col1 - lineLength, col1, col1 });
            //Halcon.DispXldPolygonLine(window, new double[] { row2 - lineLength, row2, row2 }, new double[] { col2, col2, col2 + lineLength });
            //Halcon.DispXldPolygonLine(window, new double[] { row3, row3, row3 + lineLength }, new double[] { col3 - lineLength, col3, col3 });
            //Halcon.DispXldPolygonLine(window, new double[] { row4 + lineLength, row4, row4 }, new double[] { col4, col4, col4 + lineLength });

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

            rowSize = rowMid - 25 - 50;
            colSize = colMid - 25;
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
                    rowSize -= lenR;
                    colMid = x;
                    col1 -= lenC;
                    col2 -= lenC;
                    col3 -= lenC;
                    col4 -= lenC;
                    colSize -= lenC;
                    break;

                case 1:
                    lenR = rowMid - y;
                    lenC = colMid - x;
                    if (lenR > 0 && lenC > 0 && (lenR > minDist || lenC > minDist))
                    {
                        var max = Math.Max(lenR, lenC);
                        lenR = lenC = max;
                        var lenth = row1 - rowSize;
                        row1 = rowMid - lenR;
                        col1 = colMid - lenC;

                        row2 = rowMid - lenR;
                        col2 = colMid + lenC;

                        row3 = rowMid + lenR;
                        col3 = colMid - lenC;

                        row4 = rowMid + lenR;
                        col4 = colMid + lenC;

                        rowSize = row1 - lenth;
                        colSize = col1;
                    }
                    break;

                case 2:
                    if (row1 - y >= minDist)
                    {
                        rowSize = y;
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
            rowSize -= lenR;
            colMid = x;
            col1 -= lenC;
            col2 -= lenC;
            col3 -= lenC;
            col4 -= lenC;
            colSize -= lenC;
            //lenR = rowMid - y;
            //lenC = colMid - x;
            //rowMid = y;
            //row1 -= lenR;
            //rowSize -= lenR;
            //colMid = x;
            //col1 -= lenC;
            //colSize -= lenC;
        }

        public override void SetOnlyActiveMoveHandle(bool active)
        {
            activeOnlyMove = active;
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
            double length = (row1 - rowSize) * scaleFactor;

            row1 = (1 - scaleFactor) * rowMid + scaleFactor * row1;
            col1 = (1 - scaleFactor) * colMid + scaleFactor * col1;
            row2 = (1 - scaleFactor) * rowMid + scaleFactor * row2;
            col2 = (1 - scaleFactor) * colMid + scaleFactor * col2;
            row3 = (1 - scaleFactor) * rowMid + scaleFactor * row3;
            col3 = (1 - scaleFactor) * colMid + scaleFactor * col3;
            row4 = (1 - scaleFactor) * rowMid + scaleFactor * row4;
            col4 = (1 - scaleFactor) * colMid + scaleFactor * col4;

            rowSize = row1 - length;
            colSize = col1 - length;
        }

        public override void ReSize()
        {
            CreateROI(colMid, rowMid);
        }
    }
}
