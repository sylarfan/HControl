using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ViewROIWPF.Models;

namespace ViewROIWPF.ROIs
{
    /// <summary>
    /// This class is a base class containing virtual methods for handling
    /// ROIs. Therefore, an inheriting class needs to define/override these
    /// methods to provide the ROIController with the necessary information on
    /// its (= the ROIs) shape and position. The example project provides
    /// derived ROI shapes for rectangles, lines, circles, and circular arcs.
    /// To use other shapes you must derive a new class from the base class
    /// ROI and implement its methods.
    /// </summary>
    public abstract class ROI : IDisposable
    {

        // class members of inheriting ROI classes
        protected int NumHandles { get; set; }

        protected int ActiveHandleIdx { get; set; }

        internal double HandleSize { get; set; } = 5;

        /// <summary>
        /// Flag to define the ROI to be 'positive' or 'negative'.
        /// </summary>
        protected ROIOperatorFlag OperatorFlag { get; set; }

        /// <summary>Parameter to define the line style of the ROI.</summary>
        public HTuple FlagLineStyle { get; set; }

        protected HTuple PosOperation { get; } = new HTuple();

        protected HTuple NegOperation { get; } = new HTuple(new int[] { 2, 2 });

        public bool ShowTip { get; set; }

        public Color TipColor { get; set; }

        /// <summary>Constructor of abstract ROI class.</summary>
        public ROI() { }

        /// <summary>Creates a new ROI instance at the mouse position.</summary>
        /// <param name="midX">
        /// x (=column) coordinate for ROI
        /// </param>
        /// <param name="midY">
        /// y (=row) coordinate for ROI
        /// </param>
        public abstract void CreateROI(double midX, double midY);


        /// <summary>Paints the ROI into the supplied window.</summary>
        /// <param name="window">HALCON window</param>
        public abstract void Draw(HWindow ctrl);

        /// <summary>
        /// Returns the distance of the ROI handle being
        /// closest to the image point(x,y)
        /// </summary>
        /// <param name="x">x (=column) coordinate</param>
        /// <param name="y">y (=row) coordinate</param>
        /// <returns>
        /// Distance of the closest ROI handle.
        /// </returns>
        public abstract double DistToClosestHandle(double x, double y);

        /// <summary>
        /// Paints the active handle of the ROI object into the supplied window.
        /// </summary>
        /// <param name="window">HALCON window</param>
        public abstract void DisplayActive(HWindow window);

        /// <summary>
        /// Recalculates the shape of the ROI. Translation is
        /// performed at the active handle of the ROI object
        /// for the image coordinate (x,y).
        /// </summary>
        /// <param name="x">x (=column) coordinate</param>
        /// <param name="y">y (=row) coordinate</param>
        public abstract void MoveByHandle(double x, double y);

        public abstract void MoveTo(double x, double y);

        public abstract void RelativeMove(double x, double y);

        public abstract void Scale(double scaleFactor);

        public abstract void ReSize();

        //public abstract void ScaleTo(double to);

        /// <summary>Gets the HALCON region described by the ROI.</summary>
        public abstract HRegion GetRegion();

        public abstract void SetOnlyActiveMoveHandle(bool active);

        public abstract double GetDistanceFromStartPoint(double row, double col);

        /// <summary>
        /// Gets the model information described by
        /// the ROI.
        /// </summary>
        public abstract HTuple GetModelData();

        public abstract void CreateROI(HTuple data);


        //public abstract void UseModelData(HTuple data);

        /// <summary>Number of handles defined for the ROI.</summary>
        /// <returns>Number of handles</returns>
        public int GetNumHandles()
        {
            return NumHandles;
        }

        /// <summary>Gets the active handle of the ROI.</summary>
        /// <returns>Index of the active handle (from the handle list)</returns>
        public int GetActHandleIdx()
        {
            return ActiveHandleIdx;
        }

        //public int GetMoveHandleIdx()
        //{
        //    return MoveHandleIdx;
        //}

        /// <summary>
        /// Gets the sign of the ROI object, being either
        /// 'positive' or 'negative'. This sign is used when creating a model
        /// region for matching applications from a list of ROIs.
        /// </summary>
        public ROIOperatorFlag GetOperatorFlag()
        {
            return OperatorFlag;
        }

        /// <summary>
        /// Sets the sign of a ROI object to be positive or negative.
        /// The sign is used when creating a model region for matching
        /// applications by summing up all positive and negative ROI models
        /// created so far.
        /// </summary>
        /// <param name="flag">Sign of ROI object</param>
        public void SetOperatorFlag(ROIOperatorFlag flag)
        {
            OperatorFlag = flag;

            switch (OperatorFlag)
            {
                case ROIOperatorFlag.None:
                case ROIOperatorFlag.Positive:
                    FlagLineStyle = PosOperation;
                    break;

                case ROIOperatorFlag.Negtive:
                    FlagLineStyle = NegOperation;
                    break;

                default:
                    break;
            }
        }

        public virtual void Dispose()
        {
        }
    }//end of class
}
