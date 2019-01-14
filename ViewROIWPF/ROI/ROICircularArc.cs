using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    /// <summary>
    /// This class implements an ROI shaped as a circular
    /// arc. ROICircularArc inherits from the base class ROI and
    /// implements (besides other auxiliary methods) all virtual methods
    /// defined in ROI.cs.
    /// </summary>
    public sealed class ROICircularArc : ROI
    {
        //handles
        private double midR, midC;       // 0. handle: midpoint

        private double sizeR, sizeC;     // 1. handle
        private double startR, startC;   // 2. handle
        private double extentR, extentC; // 3. handle

        //model data to specify the arc
        private double radius;

        private double startPhi, extentPhi; // -2*PI <= x <= 2*PI

        //display attributes
        private HXLDCont contour;

        private HXLDCont arrowHandleXLD;
        private string circDir;
        private bool active;
        private readonly double TwoPI;
        private readonly double PI;

        public ROICircularArc()
        {
            NumHandles = 4;         // midpoint handle + three handles on the arc
            ActiveHandleIdx = 0;
            contour = new HXLDCont();
            circDir = "";

            TwoPI = 2 * Math.PI;
            PI = Math.PI;

            arrowHandleXLD = new HXLDCont();
            arrowHandleXLD.GenEmptyObj();
        }

        /// <summary>Creates a new ROI instance at the mouse position</summary>
        public override void CreateROI(double midX, double midY)
        {
            midR = midY;
            midC = midX;

            radius = 100;

            sizeR = midR;
            sizeC = midC - radius;

            startPhi = PI * 0.25;
            extentPhi = PI * 1.5;
            circDir = "positive";

            DetermineArcHandles();
            UpdateArrowHandle();
        }

        /// <summary>Paints the ROI into the supplied window</summary>
        /// <param name="window">HALCON window</param>
        public override void Draw(HWindow window)
        {
            contour.Dispose();
            contour.GenCircleContourXld(midR - 0.5, midC - 0.5, radius, startPhi,
                                        (startPhi + extentPhi), circDir, 1.0);
            window.DispObj(contour);
            HXLDCont xld = new HXLDCont();
            xld.GenCrossContourXld(midR - 0.5, midC - 0.5, HandleSize * 2, 0);
            window.DispXld(xld);
            xld.Dispose();
            //Halcon.DispXldRect2(window, midR, midC, 0, 5, 5);
            //window.DispRectangle2(midR, midC, 0, 5, 5);
            if (!active)
            {
                Halcon.DispXldRect2(window, sizeR, sizeC, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, startR, startC, startPhi, HandleSize * 2, HandleSize * 0.4);
                //window.DispRectangle2(sizeR, sizeC, 0, 5, 5);
                //window.DispRectangle2(startR, startC, startPhi, 10, 2);
                window.DispObj(arrowHandleXLD);
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

                val[0] = Halcon.DistancePp(y, x, midR, midC);       // midpoint
                val[1] = Halcon.DistancePp(y, x, sizeR, sizeC);     // border handle
                val[2] = Halcon.DistancePp(y, x, startR, startC);   // border handle
                val[3] = Halcon.DistancePp(y, x, extentR, extentC); // border handle

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
                ActiveHandleIdx = 0;
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
                    Halcon.DispXldRect2(window, midR, midC, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(midR, midC, 0, 5, 5);
                    break;

                case 1:
                    Halcon.DispXldRect2(window, sizeR, sizeC, 0, HandleSize, HandleSize);
                    //window.DispRectangle2(sizeR, sizeC, 0, 5, 5);
                    break;

                case 2:
                    Halcon.DispXldRect2(window, startR, startC, startPhi, HandleSize * 2, HandleSize * 0.4);
                    //window.DispRectangle2(startR, startC, startPhi, 10, 2);
                    break;

                case 3:
                    window.DispObj(arrowHandleXLD);
                    break;
            }
            if (ShowTip)
            {
                string tip = $"Center:({ midR: 0.0},{ midC: 0.0})\nRadius:{radius:0.0}\nStartPhi:{startPhi * 180 / Math.PI:0.0}\nEndPhi:{(startPhi + extentPhi) * 180 / Math.PI:0.0}";
                int dispR = (int)midR + 10;
                int dispC = (int)midC + 10;
                Halcon.DispMessage(window.Handle, tip, dispR, dispC, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        /// <summary>
        /// Recalculates the shape of the ROI. Translation is
        /// performed at the active handle of the ROI object
        /// for the image coordinate (x,y)
        /// </summary>
        public override void MoveByHandle(double newX, double newY)
        {
            HTuple distance;
            double dirX, dirY, prior, next, valMax, valMin;

            switch (ActiveHandleIdx)
            {
                case 0: // midpoint
                    dirY = midR - newY;
                    dirX = midC - newX;

                    midR = newY;
                    midC = newX;

                    sizeR -= dirY;
                    sizeC -= dirX;

                    DetermineArcHandles();
                    break;

                case 1: // handle at circle border
                    sizeR = newY;
                    sizeC = newX;

                    HOperatorSet.DistancePp(new HTuple(sizeR), new HTuple(sizeC),
                                            new HTuple(midR), new HTuple(midC), out distance);
                    radius = distance[0].D;
                    DetermineArcHandles();
                    break;

                case 2: // start handle for arc
                    dirY = newY - midR;
                    dirX = newX - midC;

                    startPhi = Math.Atan2(-dirY, dirX);

                    if (startPhi < 0)
                        startPhi = PI + (startPhi + PI);

                    setStartHandle();
                    prior = extentPhi;
                    extentPhi = HMisc.AngleLl(midR, midC, startR, startC, midR, midC, extentR, extentC);

                    if (extentPhi < 0 && prior > PI * 0.8)
                        extentPhi = (PI + extentPhi) + PI;
                    else if (extentPhi > 0 && prior < -PI * 0.7)
                        extentPhi = -PI - (PI - extentPhi);

                    break;

                case 3: // end handle for arc
                    dirY = newY - midR;
                    dirX = newX - midC;

                    prior = extentPhi;
                    next = Math.Atan2(-dirY, dirX);

                    if (next < 0)
                        next = PI + (next + PI);

                    if (circDir == "positive" && startPhi >= next)
                        extentPhi = (next + TwoPI) - startPhi;
                    else if (circDir == "positive" && next > startPhi)
                        extentPhi = next - startPhi;
                    else if (circDir == "negative" && startPhi >= next)
                        extentPhi = -1.0 * (startPhi - next);
                    else if (circDir == "negative" && next > startPhi)
                        extentPhi = -1.0 * (startPhi + TwoPI - next);

                    valMax = Math.Max(Math.Abs(prior), Math.Abs(extentPhi));
                    valMin = Math.Min(Math.Abs(prior), Math.Abs(extentPhi));

                    if ((valMax - valMin) >= PI)
                        extentPhi = (circDir == "positive") ? -1.0 * valMin : valMin;

                    setExtentHandle();
                    break;
            }

            circDir = (extentPhi < 0) ? "negative" : "positive";
            UpdateArrowHandle();
        }

        /// <summary>Gets the HALCON region described by the ROI</summary>
        public override HRegion GetRegion()
        {
            HRegion region;
            contour.Dispose();
            contour = new HXLDCont();
            contour.GenCircleContourXld(midR, midC, radius, startPhi, (startPhi + extentPhi), circDir, 1.0);
            region = contour.GenRegionContourXld("filled");
            return region;
        }

        /// <summary>
        /// Gets the model information described by the ROI
        /// </summary>
        public override HTuple GetModelData()
        {
            return new HTuple(new double[] { midR, midC, radius, startPhi, extentPhi });
        }

        public override void CreateROI(HTuple data)
        {
            midR = data.DArr[0];
            midC = data.DArr[1];
            radius = data.DArr[2];
            startPhi = data.DArr[3];
            extentPhi = data.DArr[4];

            sizeR = midR;
            sizeC = midC - radius;
            circDir = (extentPhi < 0) ? "negative" : "positive";

            DetermineArcHandles();
            UpdateArrowHandle();
        }

        /// <summary>
        /// Auxiliary method to determine the positions of the second and
        /// third handle.
        /// </summary>
        private void DetermineArcHandles()
        {
            setStartHandle();
            setExtentHandle();
        }

        /// <summary>
        /// Auxiliary method to recalculate the start handle for the arc
        /// </summary>
        private void setStartHandle()
        {
            startR = midR - radius * Math.Sin(startPhi);
            startC = midC + radius * Math.Cos(startPhi);
        }

        /// <summary>
        /// Auxiliary method to recalculate the extent handle for the arc
        /// </summary>
        private void setExtentHandle()
        {
            extentR = midR - radius * Math.Sin(startPhi + extentPhi);
            extentC = midC + radius * Math.Cos(startPhi + extentPhi);
        }

        /// <summary>
        /// Auxiliary method to display an arrow at the extent arc position
        /// </summary>
        private void UpdateArrowHandle()
        {
            double row1, col1, row2, col2;
            double rowP1, colP1, rowP2, colP2;
            double length, dr, dc, halfHW, sign, angleRad;
            double headLength = 15;
            double headWidth = 15;

            arrowHandleXLD.Dispose();
            arrowHandleXLD.GenEmptyObj();

            row2 = extentR;
            col2 = extentC;
            angleRad = (startPhi + extentPhi) + Math.PI * 0.5;

            sign = (circDir == "negative") ? -1.0 : 1.0;
            row1 = row2 + sign * Math.Sin(angleRad) * 20;
            col1 = col2 - sign * Math.Cos(angleRad) * 20;

            length = Halcon.DistancePp(row1, col1, row2, col2);
            if (length == 0)
                length = -1;

            dr = (row2 - row1) / length;
            dc = (col2 - col1) / length;

            halfHW = headWidth / 2.0;
            rowP1 = row1 + (length - headLength) * dr + halfHW * dc;
            rowP2 = row1 + (length - headLength) * dr - halfHW * dc;
            colP1 = col1 + (length - headLength) * dc - halfHW * dr;
            colP2 = col1 + (length - headLength) * dc + halfHW * dr;

            if (length == -1)
                arrowHandleXLD.GenContourPolygonXld(row1 - 0.5, col1 - 0.5);
            else
                arrowHandleXLD.GenContourPolygonXld(new HTuple(new double[] { row1 - 0.5, row2 - 0.5, rowP1 - 0.5, row2 - 0.5, rowP2 - 0.5, row2 - 0.5 }),
                    new HTuple(new double[] { col1 - 0.5, col2 - 0.5, colP1 - 0.5, col2 - 0.5, colP2 - 0.5, col2 - 0.5 }));
        }

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            return 0.0;
        }

        public override void MoveTo(double x, double y)
        {
            double dirX, dirY;

            dirY = midR - y;
            dirX = midC - x;

            midR = y;
            midC = x;
            sizeR -= dirY;
            sizeC -= dirX;

            DetermineArcHandles();
            circDir = (extentPhi < 0) ? "negative" : "positive";
            UpdateArrowHandle();
        }

        public sealed override void Dispose()
        {
            contour?.Dispose();
            arrowHandleXLD?.Dispose();
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
            MoveTo(midC + x, midR + y);
        }

        public override void Scale(double scaleFactor)
        {
            sizeR = (1 - scaleFactor) * midR + scaleFactor * sizeR;
            sizeC = (1 - scaleFactor) * midC + scaleFactor * sizeC;

            HOperatorSet.DistancePp(new HTuple(sizeR), new HTuple(sizeC),
                                    new HTuple(midR), new HTuple(midC), out HTuple distance);
            radius = distance[0].D;
            DetermineArcHandles();
            UpdateArrowHandle();
        }

        public override void ReSize()
        {
            CreateROI(midC, midR);
        }
    }//end of class
}
