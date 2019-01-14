using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    public sealed class ROIAngle : ROI
    {
        private double row1, col1;
        private double rowVertex, colVertex;
        private double row2, col2;
        private bool active;

        public ROIAngle()
        {
            NumHandles = 3;
            ActiveHandleIdx = 1;
        }

        public override void SetOnlyActiveMoveHandle(bool active)
        {
            this.active = active;
            if (active)
            {
                ActiveHandleIdx = 1;
            }
        }

        public override void CreateROI(double midX, double midY)
        {
            rowVertex = midY;
            colVertex = midX;
            row1 = rowVertex - 50;
            col1 = colVertex;
            row2 = rowVertex;
            col2 = colVertex + 50;
        }

        public override void DisplayActive(HWindow window)
        {
            //var xldTemp = new HXLDCont();
            switch (ActiveHandleIdx)
            {
                case 0:
                    Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(row1, col1, 0, 5, 5);
                    break;

                case 1:
                    Halcon.DispXldRect2(window, rowVertex, colVertex, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(rowVertex, colVertex, 0, 5, 5);
                    break;

                case 2:
                    Halcon.DispXldRect2(window, row2, col2, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(row2, col2, 0, 5, 5);
                    break;

                default:
                    break;
            }
            if (ShowTip)
            {
                var angle = HMisc.AngleLl(row2, col2, rowVertex, colVertex, row1, col1, rowVertex, colVertex) * 180 / Math.PI;
                var tip = $"Vertex:({rowVertex:0.0},{colVertex:0.0})\nAngle:{angle:0.0}";
                Halcon.DispMessage(window.Handle, tip, (int)rowVertex, (int)colVertex, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        public override double DistToClosestHandle(double x, double y)
        {
            if (!active)
            {
                double max = 10000;
                double[] dist = new double[NumHandles];
                dist[0] = Halcon.DistancePp(y, x, row1, col1);
                dist[1] = Halcon.DistancePp(y, x, rowVertex, colVertex);
                dist[2] = Halcon.DistancePp(y, x, row2, col2);
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
                ActiveHandleIdx = 1;
                return Halcon.DistancePp(y, x, rowVertex, colVertex);
            }
        }

        public override void Draw(HWindow window)
        {
            Halcon.DispXldLine(window, row1, col1, rowVertex, colVertex);
            Halcon.DispXldLine(window, row2, col2, rowVertex, colVertex);
            //window.DispLine(row1, col1, rowVertex, colVertex);
            //window.DispLine(row2, col2, rowVertex, colVertex);
            if (!active)
            {
                Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, row2, col2, 0, HandleSize, HandleSize);
                //window.DispRectangle2(row1, col1, 0, 5, 5);
                //window.DispRectangle2(row2, col2, 0, 5, 5);
            }
            Halcon.DispXldRect2(window, rowVertex, colVertex, 0, HandleSize, HandleSize);
            //window.DispRectangle2(rowVertex, colVertex, 0, 5, 5);
        }

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            return 0.0;
        }

        public override HTuple GetModelData()
        {
            return new HTuple(row1, col1, rowVertex, colVertex, row2, col2);
        }

        public override void CreateROI(HTuple data)
        {
            row1 = data.DArr[0];
            col1 = data.DArr[1];
            rowVertex = data.DArr[2];
            colVertex = data.DArr[3];
            row2 = data.DArr[4];
            col2 = data.DArr[5];
        }

        public override HRegion GetRegion()
        {
            HRegion r = new HRegion();
            r.GenRegionLine(new double[] { row1, row2 }, new double[] { col1, col2 }, new double[] { rowVertex, rowVertex }, new double[] { colVertex, colVertex });
            HRegion region = r.Union1();
            r.Dispose();

            return region;
        }

        public override void MoveByHandle(double x, double y)
        {
            double lenR, lenC;
            switch (ActiveHandleIdx)
            {
                case 0:
                    row1 = y;
                    col1 = x;
                    break;

                case 1:
                    lenR = rowVertex - y;
                    lenC = colVertex - x;
                    rowVertex = y;
                    colVertex = x;
                    row1 -= lenR;
                    col1 -= lenC;
                    row2 -= lenR;
                    col2 -= lenC;
                    break;

                case 2:
                    row2 = y;
                    col2 = x;
                    break;

                default:
                    break;
            }
        }

        public override void MoveTo(double x, double y)
        {
            double lenR, lenC;
            lenR = rowVertex - y;
            lenC = colVertex - x;
            rowVertex = y;
            colVertex = x;
            row1 -= lenR;
            col1 -= lenC;
            row2 -= lenR;
            col2 -= lenC;
        }

        public override void RelativeMove(double x, double y)
        {
            MoveTo(colVertex + x, rowVertex + y);
        }

        public override void Scale(double scaleFactor)
        {
            row1 = (1 - scaleFactor) * rowVertex + scaleFactor * row1;
            col1 = (1 - scaleFactor) * colVertex + scaleFactor * col1;
            row2 = (1 - scaleFactor) * rowVertex + scaleFactor * row2;
            col2 = (1 - scaleFactor) * colVertex + scaleFactor * col2;
        }

        public override void ReSize()
        {
            CreateROI(colVertex, rowVertex);
        }

     
    }
}
