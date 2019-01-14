using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ViewROIWPF.Helper;
using ViewROIWPF.Models;
using ViewROIWPF.ROIs;

namespace ViewROIWPF
{
    /// <summary>
    /// This class creates and manages ROI objects. It responds
    /// to  mouse device inputs using the methods mouseDownAction and
    /// mouseMoveAction. You don't have to know this class in detail when you
    /// build your own C# project. But you must consider a few things if
    /// you want to use interactive ROIs in your application: There is a
    /// quite close connection between the ROIController and the HWndCtrl
    /// class, which means that you must 'register' the ROIController
    /// with the HWndCtrl, so the HWndCtrl knows it has to forward user input
    /// (like mouse events) to the ROIController class.
    /// The visualization and manipulation of the ROI objects is done
    /// by the ROIController.
    /// This class provides special support for the matching
    /// applications by calculating a model region from the list of ROIs. For
    /// this, ROIs are added and subtracted according to their sign.
    /// </summary>
    public sealed class ROIController : IDisposable
    {
        public event EventHandler<ROIEventArgs> OnROIInteractived;

        private ROI roiMode;
        private ROIOperatorFlag stateROI;
        private double currX, currY;
        private bool showTip;
        private Color tipColor;

        /// <summary>Index of the active ROI object</summary>
        internal int activeROIidx;

        internal int deletedIdx;
        private bool active;
        private double handleSize = 5;

        /// <summary>List containing all created ROI objects so far</summary>
        //public ArrayList ROIList;

        internal List<ROI> ROIList;

        /// <summary>
        /// Region obtained by summing up all negative
        /// and positive ROI objects from the ROIList
        /// </summary>
        private HRegion modelROI;

        private string activeCol = "green";
        private string activeHdlCol = "red";
        private string inactiveCol = "yellow";

        /// <summary>
        /// Reference to the HWndCtrl, the ROI Controller is registered to
        /// </summary>
        public HWndCtrl viewController;

        /// <summary>
        /// Delegate that notifies about changes made in the model region
        /// </summary>
        //public IconicDelegate NotifyRCObserver;

        /// <summary>Constructor</summary>
        internal ROIController()
        {
            stateROI = ROIOperatorFlag.None;
            ROIList = new List<ROI>();
            activeROIidx = -1;
            modelROI = new HRegion();
            deletedIdx = -1;
            currX = currY = -1;
            tipColor = Brushes.Blue.Color;
        }

        /// <summary>Registers the HWndCtrl to this ROIController instance</summary>
        internal void SetViewController(HWndCtrl view)
        {
            viewController = view;
        }

        /// <summary>Gets the ModelROI object</summary>
        public HRegion GetModelRegion()
        {
            return modelROI;
        }

        /// <summary>Gets the List of ROIs created so far</summary>
        public IReadOnlyList<ROI> GetROIList()
        {
            return ROIList.AsReadOnly();
        }

        public int GetROIListCount()
        {
            return ROIList.Count;
        }

        /// <summary>Get the active ROI</summary>
        public ROI GetActiveROI()
        {
            if (activeROIidx != -1)
                return ROIList[activeROIidx];

            return null;
        }

        public int GetActiveROIIdx()
        {
            return activeROIidx;
        }

        public void SetActiveROIIdx(int active)
        {
            if (activeROIidx == -1 || activeROIidx < ROIList.Count)
            {
                activeROIidx = active;
                viewController.Repaint();
            }
        }

        public int GetDelROIIdx()
        {
            return deletedIdx;
        }

        /// <summary>
        /// To create a new ROI object the application class initializes a
        /// 'seed' ROI instance and passes it to the ROIController.
        /// The ROIController now responds by manipulating this new ROI
        /// instance.
        /// </summary>
        /// <param name="r">
        /// 'Seed' ROI object forwarded by the application forms class.
        /// </param>
        public void SetROIShape(ROI r)
        {
            roiMode = r;
            if (r != null)
            {
                roiMode.ShowTip = showTip;
                roiMode.TipColor = tipColor;
                roiMode.HandleSize = handleSize;
                roiMode.SetOnlyActiveMoveHandle(active);
                roiMode.SetOperatorFlag(stateROI);
            }
        }

        public void SetROIShape(ROI r, HTuple data)
        {
            if (r != null)
            {
                r.ShowTip = showTip;
                r.TipColor = tipColor;
                r.HandleSize = handleSize;
                r.SetOnlyActiveMoveHandle(active);
                r.SetOperatorFlag(stateROI);
                r.CreateROI(data);
                ROIList.Add(r);
                activeROIidx = ROIList.Count - 1;
                viewController.Repaint();
                InvokeEvent(ROIEvent.CreateROI);
            }
        }

        /// <summary>
        /// Sets the sign of a ROI object to the value 'mode' (MODE_ROI_NONE,
        /// MODE_ROI_POS,MODE_ROI_NEG)
        /// </summary>
        public void SetROISign(ROIOperatorFlag mode)
        {
            stateROI = mode;

            if (activeROIidx != -1)
            {
                ROIList[activeROIidx].SetOperatorFlag(stateROI);
                viewController.Repaint();
                InvokeEvent(ROIEvent.ChangeROISign);
                //NotifyRCObserver(ROIController.EVENT_CHANGED_ROI_SIGN);
            }
        }

        /// <summary>
        /// Removes the ROI object that is marked as active.
        /// If no ROI object is active, then nothing happens.
        /// </summary>
        public void RemoveActive()
        {
            if (activeROIidx != -1)
            {
                ROIList[activeROIidx].Dispose();
                ROIList.RemoveAt(activeROIidx);
                deletedIdx = activeROIidx;
                activeROIidx = -1;
                viewController.Repaint();
                InvokeEvent(ROIEvent.RemoveActiveROI);
            }
        }

        /// <summary>
        /// Clears all variables managing ROI objects
        /// </summary>
        public void RemoveAll()
        {
            ROIList.ForEach(roi => roi.Dispose());
            ROIList.Clear();
            modelROI = null;
            roiMode = null;
            //if (activeROIidx != -1)
            //{
            //}
            viewController.Repaint();
            activeROIidx = -1;
            InvokeEvent(ROIEvent.RemoveAll);
        }

        public void RemoveAllExceptActive()
        {
            if (activeROIidx != -1)
            {
                var roi = ROIList[activeROIidx];
                foreach (var r in ROIList.Where(rr => rr != roi))
                {
                    r.Dispose();
                }
                ROIList.Clear();
                ROIList.Add(roi);
                activeROIidx = 0;
                viewController.Repaint();
                InvokeEvent(ROIEvent.RemoveActiveROI);
            }
            else
            {
                RemoveAll();
            }
        }

        /// <summary>
        /// Calculates the ModelROI region for all objects contained
        /// in ROIList, by adding and subtracting the positive and
        /// negative ROI objects.
        /// </summary>
        public bool DefineModelROI()
        {
            HRegion tmpAdd, tmpDiff, tmp;

            if (stateROI == ROIOperatorFlag.None)
                return true;

            tmpAdd = new HRegion();
            tmpDiff = new HRegion();
            tmpAdd.GenEmptyRegion();
            tmpDiff.GenEmptyRegion();

            for (int i = 0; i < ROIList.Count; i++)
            {
                switch (ROIList[i].GetOperatorFlag())
                {
                    case ROIOperatorFlag.Positive:
                        tmp = ROIList[i].GetRegion();
                        tmpAdd = tmp.Union2(tmpAdd);
                        break;

                    case ROIOperatorFlag.Negtive:
                        tmp = ROIList[i].GetRegion();
                        tmpDiff = tmp.Union2(tmpDiff);
                        break;

                    default:
                        break;
                }//end of switch
            }//end of for

            modelROI = null;

            if (tmpAdd.AreaCenter(out double row, out double col) > 0)
            {
                tmp = tmpAdd.Difference(tmpDiff);
                if (tmp.AreaCenter(out row, out col) > 0)
                    modelROI = tmp;
            }

            //in case the set of positiv and negative ROIs dissolve
            if (modelROI == null || ROIList.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Deletes this ROI instance if a 'seed' ROI object has been passed
        /// to the ROIController by the application class.
        /// </summary>
        public void ResetROI()
        {
            if (activeROIidx != -1)
            {
                viewController.Repaint();
            }
            activeROIidx = -1;
            roiMode = null;

            InvokeEvent(ROIEvent.ResetROI);
        }

        /// <summary>Defines the colors for the ROI objects</summary>
        /// <param name="activeColor">Color for the active ROI object</param>
        /// <param name="inactiveColor">Color for the inactive ROI objects</param>
        /// <param name="aHdlColor">
        /// Color for the active handle of the active ROI object
        /// </param>
        public void SetDrawColor(Color activeColor, Color aHdlColor, Color inactiveColor)
        {
            activeCol = Halcon.GetHColorString(activeColor);
            activeHdlCol = Halcon.GetHColorString(aHdlColor);
            inactiveCol = Halcon.GetHColorString(inactiveColor);
        }

        public void SetShowToolTip(bool shouldTip)
        {
            showTip = shouldTip;
            if (roiMode != null)
            {
                roiMode.ShowTip = shouldTip;
            }
            ROIList.ForEach(roi => roi.ShowTip = shouldTip);
            if (activeROIidx != -1)
            {
                viewController.Repaint();
            }
        }

        public void SetOnlyActiveMoveHandle(bool active)
        {
            this.active = active;
            if (roiMode != null)
            {
                roiMode.SetOnlyActiveMoveHandle(active);
            }
            ROIList.ForEach(roi => roi.SetOnlyActiveMoveHandle(active));
            if (ROIList.Count > 0)
            {
                viewController.Repaint();
            }
        }

        public bool GetOnlyActiveMoveHandle()
        {
            return active;
        }

        public bool GetShowToolTip()
        {
            return showTip;
        }

        public void SetToolTipColor(Color color)
        {
            tipColor = color;
            if (roiMode != null)
            {
                roiMode.TipColor = color;
            }
            ROIList.ForEach(roi => roi.TipColor = color);
            if (activeROIidx != -1)
            {
                viewController.Repaint();
            }
        }

        public Color GetToolTipColor()
        {
            return tipColor;
        }

        private readonly HTuple emptyLineStyle = new HTuple();

        /// <summary>
        /// Paints all objects from the ROIList into the HALCON window
        /// </summary>
        /// <param name="window">HALCON window</param>
        internal void PaintData(HWindow window)
        {
            var draw = window.GetDraw();
            window.SetDraw("margin");
            window.SetLineWidth(1);

            if (ROIList.Count > 0)
            {
                window.SetColor(inactiveCol);
                window.SetDraw("margin");

                for (int i = 0; i < ROIList.Count; i++)
                {
                    window.SetLineStyle(ROIList[i].FlagLineStyle);
                    ROIList[i].Draw(window);
                }

                if (activeROIidx != -1)
                {
                    window.SetColor(activeCol);
                    window.SetLineStyle(ROIList[activeROIidx].FlagLineStyle);
                    ROIList[activeROIidx].Draw(window);

                    window.SetColor(activeHdlCol);
                    ROIList[activeROIidx].DisplayActive(window);
                }
            }
            window.SetLineStyle(emptyLineStyle);
            window.SetDraw(draw);
        }

        private bool activeROi = true;

        /// <summary>
        /// Reaction of ROI objects to the 'mouse button down' event: changing
        /// the shape of the ROI and adding it to the ROIList if it is a 'seed'
        /// ROI.
        /// </summary>
        /// <param name="imgX">x coordinate of mouse event</param>
        /// <param name="imgY">y coordinate of mouse event</param>
        /// <returns></returns>
        public int MouseDownAction(double imgX, double imgY)
        {
            int idxROI = -1;
            double max = 10000, dist = 0;
            double epsilon = handleSize * 1.3;          //maximal shortest distance to one of
                                                        //the handles

            if (roiMode != null)             //either a new ROI object is created
            {
                roiMode.CreateROI(imgX, imgY);
                ROIList.Add(roiMode);
                roiMode = null;
                activeROIidx = ROIList.Count - 1;
                viewController.Repaint();
                InvokeEvent(ROIEvent.CreateROI);
            }
            else if (ROIList.Count > 0)     // ... or an existing one is manipulated
            {
                for (int i = 0; i < ROIList.Count; i++)
                {
                    dist = ROIList[i].DistToClosestHandle(imgX, imgY);
                    if ((dist < max) && (dist < epsilon))
                    {
                        max = dist;
                        idxROI = i;
                    }
                }//end of for
                //activeROi = true;

                if (idxROI >= 0)
                {
                    activeROIidx = idxROI;
                    activeROi = true;

                    viewController.Repaint();

                    InvokeEvent(ROIEvent.ActiveROI);

                    //NotifyRCObserver(ROIController.EVENT_ACTIVATED_ROI);
                }
                else if (activeROi)
                {
                    activeROi = false;
                    activeROIidx = -1;

                    viewController.Repaint();

                    InvokeEvent(ROIEvent.InActiveROI);
                }
            }

            return activeROIidx;
        }

        public void MoveActive(double x, double y)
        {
            if (activeROIidx != -1)
            {
                ROIList[activeROIidx].MoveTo(x, y);
                viewController.Repaint();
            }
        }

        public void RelativeMoveActive(double x, double y)
        {
            if (activeROIidx != -1)
            {
                ROIList[activeROIidx].RelativeMove(x, y);
                viewController.Repaint();
            }
        }

        public void ScaleActive(double scale)
        {
            if (activeROIidx != -1 && scale > 0)
            {
                ROIList[activeROIidx].Scale(scale);
                viewController.Repaint();
            }
        }

        public void ResizeActive()
        {
            if (activeROIidx != -1)
            {
                ROIList[activeROIidx].ReSize();
                viewController.Repaint();
            }
        }

        public void SetHandleSize(double size)
        {
            SetHandleSize(size, true);

        }

        internal void SetHandleSize(double size, bool repaint)
        {
            if (size > 0)
            {
                handleSize = size;
                if (roiMode != null)
                {
                    roiMode.HandleSize = handleSize;
                }
                ROIList.ForEach(roi => roi.HandleSize = handleSize);
                if (repaint && ROIList.Count > 0)
                {
                    viewController.Repaint();
                }
            }
        }

        public double GetHandleSize()
        {
            return handleSize;
        }

        /// <summary>
        /// Reaction of ROI objects to the 'mouse button move' event: moving
        /// the active ROI.
        /// </summary>
        /// <param name="newX">x coordinate of mouse event</param>
        /// <param name="newY">y coordinate of mouse event</param>
        public void MouseMoveAction(double newX, double newY)
        {
            if ((newX == currX) && (newY == currY))
                return;

            ROIList[activeROIidx].MoveByHandle(newX, newY);
            viewController.Repaint();
            currX = newX;
            currY = newY;
            InvokeEvent(ROIEvent.MovingROI);
        }

        internal void InvokeEvent(ROIEvent roiEvent)
        {
            OnROIInteractived?.Invoke(this, new ROIEventArgs(roiEvent));
        }

        public void Dispose()
        {
            ROIList.ForEach(roi => roi.Dispose());
            roiMode?.Dispose();
            modelROI?.Dispose();
        }
    }//end of class
}
