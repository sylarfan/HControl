using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewROIWPF.Models
{
    public class HWindowEventArgs : EventArgs
    {
        public HWindowEvent HWindowEvent { get; }

        public HImage DialogImage { get; }

        public string ErrorMessage { get; }

        public HObject RemoveObj { get; }

        internal HWindowEventArgs(HWindowEvent hevent, string exceptionMessage = null)
        {
            HWindowEvent = hevent;
            ErrorMessage = exceptionMessage;
        }

        internal HWindowEventArgs(HImage dialogImage)
        {
            HWindowEvent = HWindowEvent.DialogUpdateImage;
            DialogImage = dialogImage;
        }
    }

    public enum HWindowEvent
    {
        GraphicStackAdd,
        GraphicStackRemove,
        GraphicStackClear,
        GraphicStackOverflow,
        DialogUpdateImage,
        DialogUpdateImageError,
        GcError,
    }

    public class ROIEventArgs : EventArgs
    {
        public ROIEvent ROIEvent { get; }

        internal ROIEventArgs(ROIEvent roiEvent)
        {
            ROIEvent = roiEvent;
        }
    }

    public enum ROIEvent
    {
        UpdateROI,
        ChangeROISign,
        MovingROI,
        RemoveActiveROI,
        RemoveAll,
        RemoveAllExceptActive,
        ActiveROI,
        CreateROI,
        ResetROI,
        InActiveROI
    }
}
