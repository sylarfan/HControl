using HalconDotNet;
using System;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    /// <summary>
    /// This class demonstrates one of the possible implementations for a
    /// (simple) rectangularly shaped ROI. To create this rectangle we use
    /// a center point (midR, midC), an orientation 'phi' and the half
    /// edge lengths 'length1' and 'length2', similar to the HALCON
    /// operator gen_rectangle2().
    /// The class ROIRectangle2 inherits from the base class ROI and
    /// implements (besides other auxiliary methods) all virtual methods
    /// defined in ROI.cs.
    /// </summary>
    public sealed class ROIRectangle2 : ROI
    {
        /// <summary>Half length of the rectangle side, perpendicular to phi</summary>
        private double length1;

        /// <summary>Half length of the rectangle side, in direction of phi</summary>
        private double length2;

        /// <summary>Row coordinate of midpoint of the rectangle</summary>
        private double midR;

        /// <summary>Column coordinate of midpoint of the rectangle</summary>
        private double midC;

        /// <summary>Orientation of rectangle defined in radians.</summary>
        private double phi;

        //auxiliary variables
        private HTuple rowsInit;

        private HTuple colsInit;
        private HTuple rows;
        private HTuple cols;

        private HHomMat2D hom2D, tmp;

        private bool active;

        /// <summary>Constructor</summary>
        public ROIRectangle2()
        {
            NumHandles = 6; // 4 corners +  1 midpoint + 1 rotationpoint
            ActiveHandleIdx = 4;
        }

        /// <summary>Creates a new ROI instance at the mouse position</summary>
        /// <param name="midX">
        /// x (=column) coordinate for interactive ROI
        /// </param>
        /// <param name="midY">
        /// y (=row) coordinate for interactive ROI
        /// </param>
        public override void CreateROI(double midX, double midY)
        {
            midR = midY;
            midC = midX;

            length1 = 100;
            length2 = 50;

            phi = 0.0;

            rowsInit = new HTuple(new double[] {-1.0, -1.0, 1.0,
                                                   1.0,  0.0, 0.0 });
            colsInit = new HTuple(new double[] {-1.0, 1.0,  1.0,
                                                  -1.0, 0.0, 0.6 });
            //order        ul ,  ur,   lr,  ll,   mp, arrowMidpoint
            hom2D = new HHomMat2D();
            tmp = new HHomMat2D();

            UpdateHandlePos();
        }

        /// <summary>Paints the ROI into the supplied window</summary>
        /// <param name="window">HALCON window</param>
        public override void Draw(HWindow window)
        {
            Halcon.DispXldRect2(window, midR, midC, -phi, length1, length2);

            //window.DispRectangle2(midR, midC, -phi, length1, length2);
            if (!active)
            {
                for (int i = 0; i < NumHandles; i++)
                {
                    if (i != 4)
                    {
                        Halcon.DispXldRect2(window, rows[i].D, cols[i].D, -phi, HandleSize, HandleSize);
                        //window.DispRectangle2(rows[i].D, cols[i].D, -phi, 5, 5);
                    }
                }
                var xldTemp = Halcon.GenArrowContourXld(midR, midC, midR + (Math.Sin(phi) * length1 * 1.2),
                    midC + (Math.Cos(phi) * length1 * 1.2), HandleSize, HandleSize);
                xldTemp.DispObj(window);
                xldTemp.Dispose();

                //window.DispArrow(midR, midC, midR + (Math.Sin(phi) * length1 * 1.2),
                //    midC + (Math.Cos(phi) * length1 * 1.2), 2.0);
            }
            //window.DispCross(midR, midC, 10, 0.0);
            HXLDCont xld = new HXLDCont();
            xld.GenCrossContourXld(midR - 0.5, midC - 0.5, HandleSize * 2, 0);
            window.DispXld(xld);
            xld.Dispose();
        }

        /// <summary>
        /// Returns the distance of the ROI handle being
        /// closest to the image point(x,y)
        /// </summary>
        /// <param name="x">x (=column) coordinate</param>
        /// <param name="y">y (=row) coordinate</param>
        /// <returns>
        /// Distance of the closest ROI handle.
        /// </returns>
        public override double DistToClosestHandle(double x, double y)
        {
            if (!active)
            {
                double max = 10000;
                double[] val = new double[NumHandles];

                for (int i = 0; i < NumHandles; i++)
                    val[i] = Halcon.DistancePp(y, x, rows[i].D, cols[i].D);

                for (int i = 0; i < NumHandles; i++)
                {
                    if (val[i] < max)
                    {
                        max = val[i];
                        ActiveHandleIdx = i;
                    }
                }
                return val[ActiveHandleIdx];
            }
            else
            {
                ActiveHandleIdx = 4;
                return Halcon.DistancePp(y, x, rows[4].D, cols[4].D);
            }
        }

        /// <summary>
        /// Paints the active handle of the ROI object into the supplied window
        /// </summary>
        /// <param name="window">HALCON window</param>
        public override void DisplayActive(HWindow window)
        {
            //window.DispRectangle2(rows[ActiveHandleIdx].D,
            //                      cols[ActiveHandleIdx].D,
            //                      -phi, 5, 5);
            Halcon.DispXldRect2(window, rows[ActiveHandleIdx].D, cols[ActiveHandleIdx].D, -phi, HandleSize, HandleSize);

            if (ActiveHandleIdx == 5)
            {
                var xldTemp = Halcon.GenArrowContourXld(midR, midC, midR + (Math.Sin(phi) * length1 * 1.2),
            midC + (Math.Cos(phi) * length1 * 1.2), HandleSize, HandleSize);
                xldTemp.DispObj(window);
                xldTemp.Dispose();
            }
            //window.DispArrow(midR - 0.5, midC - 0.5,
            //                 midR + (Math.Sin(phi) * length1 * 1.2) - 0.5,
            //                 midC + (Math.Cos(phi) * length1 * 1.2) - 0.5,
            //                 2.0);
            if (ShowTip)
            {
                var tip = $"Center:({midR:0.0},{midC:0.0})\nL1:{length1 * 2:0.0},L2:{length2 * 2:0.0}\nAngle:{phi * 180 / Math.PI:0.0}";
                Halcon.DispMessage(window.Handle, tip, (int)midR + 10, (int)midC + 10, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        /// <summary>Gets the HALCON region described by the ROI</summary>
        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenRectangle2(midR, midC, -phi, length1, length2);
            return region;
        }

        /// <summary>
        /// Gets the model information described by
        /// the interactive ROI
        /// </summary>
        public override HTuple GetModelData()
        {
            return new HTuple(new double[] { midR, midC, length1, length2, phi });
        }

        public override void CreateROI(HTuple data)
        {
            midR = data.DArr[0];
            midC = data.DArr[1];
            length1 = data.DArr[2];
            length2 = data.DArr[3];
            phi = data.DArr[4];


            rowsInit = new HTuple(new double[] {-1.0, -1.0, 1.0,
                                                   1.0,  0.0, 0.0 });
            colsInit = new HTuple(new double[] {-1.0, 1.0,  1.0,
                                                  -1.0, 0.0, 0.6 });
            //order        ul ,  ur,   lr,  ll,   mp, arrowMidpoint
            hom2D = new HHomMat2D();
            tmp = new HHomMat2D();

            UpdateHandlePos();
        }

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            return 0.0;
        }

        /// <summary>
        /// Recalculates the shape of the ROI instance. Translation is
        /// performed at the active handle of the ROI object
        /// for the image coordinate (x,y)
        /// </summary>
        /// <param name="newX">x mouse coordinate</param>
        /// <param name="newY">y mouse coordinate</param>
        public override void MoveByHandle(double newX, double newY)
        {
            double vX, vY, x = 0, y = 0;

            switch (ActiveHandleIdx)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    tmp = hom2D.HomMat2dInvert();
                    x = tmp.AffineTransPoint2d(newX, newY, out y);

                    length2 = Math.Abs(y);
                    length1 = Math.Abs(x);

                    CheckForRange(x, y);
                    break;

                case 4:
                    midC = newX;
                    midR = newY;
                    break;

                case 5:
                    vY = newY - rows[4].D;
                    vX = newX - cols[4].D;
                    phi = Math.Atan2(vY, vX);
                    break;
            }
            UpdateHandlePos();
        }//end of method

        /// <summary>
        /// Auxiliary method to recalculate the contour points of
        /// the rectangle by transforming the initial row and
        /// column coordinates (rowsInit, colsInit) by the updated
        /// homography hom2D
        /// </summary>
        private void UpdateHandlePos()
        {
            hom2D.HomMat2dIdentity();
            hom2D = hom2D.HomMat2dTranslate(midC, midR);
            hom2D = hom2D.HomMat2dRotateLocal(phi);
            tmp = hom2D.HomMat2dScaleLocal(length1, length2);
            cols = tmp.AffineTransPoint2d(colsInit, rowsInit, out rows);
        }

        /* This auxiliary method checks the half lengths
		 * (length1, length2) using the coordinates (x,y) of the four
		 * rectangle corners (handles 0 to 3) to avoid 'bending' of
		 * the rectangular ROI at its midpoint, when it comes to a
		 * 'collapse' of the rectangle for length1=length2=0.
		 * */

        private void CheckForRange(double x, double y)
        {
            switch (ActiveHandleIdx)
            {
                case 0:
                    if ((x < 0) && (y < 0))
                        return;
                    if (x >= 0) length1 = 0.01;
                    if (y >= 0) length2 = 0.01;
                    break;

                case 1:
                    if ((x > 0) && (y < 0))
                        return;
                    if (x <= 0) length1 = 0.01;
                    if (y >= 0) length2 = 0.01;
                    break;

                case 2:
                    if ((x > 0) && (y > 0))
                        return;
                    if (x <= 0) length1 = 0.01;
                    if (y <= 0) length2 = 0.01;
                    break;

                case 3:
                    if ((x < 0) && (y > 0))
                        return;
                    if (x >= 0) length1 = 0.01;
                    if (y <= 0) length2 = 0.01;
                    break;

                default:
                    break;
            }
        }

        public override void MoveTo(double x, double y)
        {
            midC = x;
            midR = y;
            UpdateHandlePos();
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
            length1 = scaleFactor * length1;
            length2 = scaleFactor * length2;
            UpdateHandlePos();
        }

        public override void ReSize()
        {
            CreateROI(midC, midR);
        }
    }//end of class
}
