using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    /// <summary>
    /// This class demonstrates one of the possible implementations for a
    /// linear ROI. ROILine inherits from the base class ROI and
    /// implements (besides other auxiliary methods) all virtual methods
    /// defined in ROI.cs.
    /// </summary>
    public sealed class ROILine : ROI
    {
        private double row1, col1;   // first end point of line
        private double row2, col2;   // second end point of line
        private double midR, midC;   // midPoint of line
        private bool active;

        //private HXLDCont arrowHandleXLD;
        private HXLDCont endLineXld;

        public ROILine()
        {
            NumHandles = 3;        // two end points of line
            ActiveHandleIdx = 2;
            //arrowHandleXLD = new HXLDCont();
            endLineXld = new HXLDCont();
            //arrowHandleXLD.GenEmptyObj();
        }

        /// <summary>Creates a new ROI instance at the mouse position.</summary>
        public override void CreateROI(double midX, double midY)
        {
            midR = midY;
            midC = midX;

            row1 = midR;
            col1 = midC - 50;
            row2 = midR;
            col2 = midC + 50;
            UpdateEndLine();
            //UpdateArrowHandle();
        }

        /// <summary>Paints the ROI into the supplied window.</summary>
        public override void Draw(HWindow window)
        {
            //window.DispLine(row1 - 0.5, col1 - 0.5, row2, col2);
            Halcon.DispXldLine(window, row1, col1, row2, col2);
            //window.DispObj(arrowHandleXLD);
            if (!active)
            {
                window.DispObj(endLineXld);
            }
            Halcon.DispXldRect2(window, midR, midC, 0, HandleSize, HandleSize);
            //window.DispRectangle2(midR, midC, 0, 5, 5);
        }

        /// <summary>
        /// Returns the distance of the ROI handle being
        /// closest to the image point(x,y).
        /// </summary>
        public override double DistToClosestHandle(double x, double y)
        {
            if (!active)
            {
                double max = 10000;
                double[] val = new double[NumHandles];

                val[0] = Halcon.DistancePp(y, x, row1, col1); // upper left
                val[1] = Halcon.DistancePp(y, x, row2, col2); // upper right
                val[2] = Halcon.DistancePp(y, x, midR, midC); // midpoint

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
                ActiveHandleIdx = 2;
                return Halcon.DistancePp(y, x, midR, midC);
            }
        }

        /// <summary>
        /// Paints the active handle of the ROI object into the supplied window.
        /// </summary>
        public override void DisplayActive(HWindow window)
        {
            switch (ActiveHandleIdx)
            {
                case 0:
                    window.DispObj(endLineXld);
                    break;

                case 1:
                    window.DispObj(endLineXld);
                    break;

                case 2:
                    Halcon.DispXldRect2(window, midR, midC, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(midR, midC, 0, 5, 5);
                    break;
            }
            if (ShowTip)
            {
                double distance = Halcon.DistancePp(row1, col1, row2, col2);
                string tip = $"P1:{row1:0.0},{col1:0.0}\nP2:{row2:0.0},{col2:0.0}\nDist:{distance:0.0}";
                Halcon.DispMessage(window.Handle, tip, (int)midR + 10, (int)midC + 10, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        /// <summary>Gets the HALCON region described by the ROI.</summary>
        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenRegionLine(row1, col1, row2, col2);
            return region;
        }

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            double distance = Halcon.DistancePp(row, col, row1, col1);
            return distance;
        }

        /// <summary>
        /// Gets the model information described by
        /// the ROI.
        /// </summary>
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

            midR = (row2 - row1) / 2;
            midC = (col2 - col1) / 2;

            UpdateEndLine();
        }

        /// <summary>
        /// Recalculates the shape of the ROI. Translation is
        /// performed at the active handle of the ROI object
        /// for the image coordinate (x,y).
        /// </summary>
        public override void MoveByHandle(double newX, double newY)
        {
            double lenR, lenC;

            switch (ActiveHandleIdx)
            {
                case 0: // first end point
                    row1 = newY;
                    col1 = newX;

                    midR = (row1 + row2) / 2;
                    midC = (col1 + col2) / 2;
                    break;

                case 1: // last end point
                    row2 = newY;
                    col2 = newX;

                    midR = (row1 + row2) / 2;
                    midC = (col1 + col2) / 2;
                    break;

                case 2: // midpoint
                    lenR = row1 - midR;
                    lenC = col1 - midC;

                    midR = newY;
                    midC = newX;

                    row1 = midR + lenR;
                    col1 = midC + lenC;
                    row2 = midR - lenR;
                    col2 = midC - lenC;
                    break;
            }
            //UpdateArrowHandle();
            UpdateEndLine();
        }

        private void UpdateEndLine()
        {
            double lineLength = HandleSize * 3;
            endLineXld.Dispose();
            var lineAngle = HMisc.AngleLx(row1, col1, row2, col2);

            var row = Math.Cos(lineAngle) * lineLength / 2;
            var col = Math.Sin(lineAngle) * lineLength / 2;
            var r = new HTuple((row1 - row) - 0.5, row1 + row - 0.5);
            var c = new HTuple(col1 - col - 0.5, col1 + col - 0.5);
            var r1 = new HTuple((row2 - row) - 0.5, row2 + row - 0.5);
            var c1 = new HTuple(col2 - col - 0.5, col2 + col - 0.5);
            var tempXld = new HXLDCont();
            var temp2Xld = new HXLDCont();
            tempXld.GenContourPolygonXld(r, c);
            temp2Xld.GenContourPolygonXld(r1, c1);
            endLineXld = tempXld.ConcatObj(temp2Xld);
            tempXld.Dispose();
            temp2Xld.Dispose();
        }

        public override void MoveTo(double x, double y)
        {
            double lenR, lenC;

            lenR = row1 - midR;
            lenC = col1 - midC;

            midR = y;
            midC = x;

            row1 = midR + lenR;
            col1 = midC + lenC;
            row2 = midR - lenR;
            col2 = midC - lenC;
            UpdateEndLine();
        }

        public sealed override void Dispose()
        {
            endLineXld?.Dispose();
        }

        public override void SetOnlyActiveMoveHandle(bool active)
        {
            this.active = active;
            if (active)
            {
                ActiveHandleIdx = 2;
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

            midR = (row1 + row2) / 2;
            midC = (col1 + col2) / 2;
            UpdateEndLine();
        }

        public override void ReSize()
        {
            CreateROI(midC, midR);
        }
    }//end of class
}
