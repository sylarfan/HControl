using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    /// <summary>
    /// This class demonstrates one of the possible implementations for a
    /// circular ROI. ROICircle inherits from the base class ROI and
    /// implements (besides other auxiliary methods) all virtual methods
    /// defined in ROI.cs.
    /// </summary>
    public sealed class ROICircle : ROI
    {
        private double radius;
        private double row1, col1;  // first handle
        private double midR, midC;  // second handle
        private bool active;

        public ROICircle()
        {
            NumHandles = 2; // one at corner of circle + midpoint
            ActiveHandleIdx = 1;
        }

        /// <summary>Creates a new ROI instance at the mouse position</summary>
        public override void CreateROI(double midX, double midY)
        {
            midR = midY;
            midC = midX;

            radius = 100;

            row1 = midR;
            col1 = midC + radius;
        }

        /// <summary>Paints the ROI into the supplied window</summary>
        /// <param name="window">HALCON window</param>
        public override void Draw(HWindow window)
        {
            HXLDCont xld = new HXLDCont();
            xld.GenCircleContourXld(midR, midC, radius, 0, 6.28318, "positive", 1.0);
            xld.DispObj(window);
            xld.Dispose();
            xld = new HXLDCont();
            //window.DispCircle(midR, midC, radius);
            xld.GenCrossContourXld(midR - 0.5, midC - 0.5, HandleSize * 2, 0);
            window.DispXld(xld);
            xld.Dispose();
            if (!active)
            {
                Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                //window.DispRectangle2(row1, col1, 0, 5, 5);
            }
        }

        /// <summary>
        /// Returns the distance of the ROI handle being
        /// closest to the image point(x,y)
        /// </summary>
        public override double DistToClosestHandle(double x, double y)
        {
            if (!active)
            {
                double max = 10000;
                double[] val = new double[NumHandles];

                val[0] = Halcon.DistancePp(y, x, row1, col1); // border handle
                val[1] = Halcon.DistancePp(y, x, midR, midC); // midpoint

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
                ActiveHandleIdx = 1;
                return Halcon.DistancePp(y, x, midR, midC);
            }
        }

        /// <summary>
        /// Paints the active handle of the ROI object into the supplied window
        /// </summary>
        public override void DisplayActive(HWindow window)
        {
            switch (ActiveHandleIdx)
            {
                case 0:
                    Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(row1, col1, 0, 5, 5);
                    break;

                case 1:
                    Halcon.DispXldRect2(window, midR, midC, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(midR-0.5, midC-0.5, 0, 5, 5);
                    break;
            }
            if (ShowTip)
            {
                string tip = $"Center:({ midR: 0.0},{ midC: 0.0})\nRadius:{radius:0.0}";
                int dispR = (int)midR + 10;
                int dispC = (int)midC + 10;
                Halcon.DispMessage(window.Handle, tip, dispR, dispC, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        /// <summary>Gets the HALCON region described by the ROI</summary>
        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenCircle(midR, midC, radius);
            return region;
        }

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            double sRow = midR; // assumption: we have an angle starting at 0.0
            double sCol = midC + 1 * radius;

            double angle = HMisc.AngleLl(midR, midC, sRow, sCol, midR, midC, row, col);

            if (angle < 0)
                angle += 2 * Math.PI;

            return (radius * angle);
        }

        /// <summary>
        /// Gets the model information described by
        /// the  ROI
        /// </summary>
        public override HTuple GetModelData()
        {
            return new HTuple(new double[] { midR, midC, radius });
        }

        public override void CreateROI(HTuple data)
        {
            midR = data.DArr[0];
            midC = data.DArr[1];
            radius = data.DArr[2];
            row1 = midR;
            col1 = midC + radius;
        }

        /// <summary>
        /// Recalculates the shape of the ROI. Translation is
        /// performed at the active handle of the ROI object
        /// for the image coordinate (x,y)
        /// </summary>
        public override void MoveByHandle(double newX, double newY)
        {
            HTuple distance;
            double shiftX, shiftY;

            switch (ActiveHandleIdx)
            {
                case 0: // handle at circle border

                    row1 = newY;
                    col1 = newX;
                    HOperatorSet.DistancePp(new HTuple(row1), new HTuple(col1),
                                            new HTuple(midR), new HTuple(midC),
                                            out distance);
                    radius = distance[0].D;
                    break;

                case 1: // midpoint

                    shiftY = midR - newY;
                    shiftX = midC - newX;

                    midR = newY;
                    midC = newX;

                    row1 -= shiftY;
                    col1 -= shiftX;
                    break;
            }
        }

        public override void MoveTo(double x, double y)
        {
            double shiftX, shiftY;

            shiftY = midR - y;
            shiftX = midC - x;

            midR = y;
            midC = x;
            row1 -= shiftY;
            col1 -= shiftX;
        }

        public override void SetOnlyActiveMoveHandle(bool active)
        {
            this.active = active;
            if (active)
            {
                ActiveHandleIdx = 1;
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
            HOperatorSet.DistancePp(new HTuple(row1), new HTuple(col1),
                                           new HTuple(midR), new HTuple(midC),
                                           out HTuple distance);
            radius = distance[0].D;
        }

        public override void ReSize()
        {
            CreateROI(midC, midR);
        }
    }//end of class
}
