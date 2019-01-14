using HalconDotNet;
using ViewROIWPF.Helper;

namespace ViewROIWPF.ROIs
{
    /// <summary>
    /// This class demonstrates one of the possible implementations for a
    /// (simple) rectangularly shaped ROI. ROIRectangle1 inherits
    /// from the base class ROI and implements (besides other auxiliary
    /// methods) all virtual methods defined in ROI.cs.
    /// Since a simple rectangle is defined by two data points, by the upper
    /// left corner and the lower right corner, we use four values (row1/col1)
    /// and (row2/col2) as class members to hold these positions at
    /// any time of the program. The four corners of the rectangle can be taken
    /// as handles, which the user can use to manipulate the size of the ROI.
    /// Furthermore, we define a midpoint as an additional handle, with which
    /// the user can grab and drag the ROI. Therefore, we declare NumHandles
    /// to be 5 and set the activeHandle to be 0, which will be the upper left
    /// corner of our ROI.
    /// </summary>
    public sealed class ROIRectangle1 : ROI
    {
        private double row1, col1;   // upper left
        private double row2, col2;   // lower right
        private double midR, midC;   // midpoint

        private bool active;

        /// <summary>Constructor</summary>
        public ROIRectangle1()
        {
            NumHandles = 5; // 4 corner points + midpoint
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

            row1 = midR - 50;
            col1 = midC - 50;
            row2 = midR + 50;
            col2 = midC + 50;
        }

        /// <summary>Paints the ROI into the supplied window</summary>
        /// <param name="window">HALCON window</param>
        public override void Draw(HWindow window)
        {
            window.DispRectangle1(row1, col1, row2, col2);
            if (!active)
            {
                Halcon.DispXldRect2(window, row1, col1, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, row1, col2, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, row2, col1, 0, HandleSize, HandleSize);
                Halcon.DispXldRect2(window, row2, col2, 0, HandleSize, HandleSize);
            }

            //window.DispRectangle2(midR, midC, 0, 5, 5);
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

        /// <summary>
        /// Paints the active handle of the ROI object into the supplied window
        /// </summary>
        /// <param name="window">HALCON window</param>
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
                    break;

                case 2:
                    Halcon.DispXldRect2(window, row2, col2, 0, HandleSize, HandleSize);
                    break;

                case 3:
                    Halcon.DispXldRect2(window, row2, col1, 0, HandleSize, HandleSize);
                    break;

                case 4:
                    Halcon.DispXldRect2(window, midR, midC, 0, HandleSize, HandleSize);
                    break;
            }
            if (ShowTip)
            {
                double[] length = new double[2];
                length[0] = Halcon.DistancePp(row1, col1, row1, col2);
                length[1] = Halcon.DistancePp(row1, col1, row2, col1);
                //var length = Halcon.DistancePp(new HTuple(row1, row1), new HTuple(col1, col1), new HTuple(row1, row2), new HTuple(col2, col1));
                string tip = $"Center:({midR:0.0},{midC:0.0})\nL1:{length[0]:0.0},L2:{length[1]:0.0}";
                Halcon.DispMessage(window.Handle, tip, (int)midR + 10, (int)midC + 10, "image", Halcon.GetHColorString(TipColor), false);
            }
        }

        /// <summary>Gets the HALCON region described by the ROI</summary>
        public override HRegion GetRegion()
        {
            HRegion region = new HRegion();
            region.GenRectangle1(row1, col1, row2, col2);
            return region;
        }

        /// <summary>
        /// Gets the model information described by
        /// the interactive ROI
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

            midR = (row2 + row1) / 2; /*+ row1;*/
            midC = (col2 + col1) / 2;
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
            double len1, len2;
            double tmp;

            switch (ActiveHandleIdx)
            {
                case 0: // upper left
                    row1 = newY;
                    col1 = newX;
                    break;

                case 1: // upper right
                    row1 = newY;
                    col2 = newX;
                    break;

                case 2: // lower right
                    row2 = newY;
                    col2 = newX;
                    break;

                case 3: // lower left
                    row2 = newY;
                    col1 = newX;
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
        }//end of method

        public override double GetDistanceFromStartPoint(double row, double col)
        {
            return 0.0;
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
    }//end of class
}
