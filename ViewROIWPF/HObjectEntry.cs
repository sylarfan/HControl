using HalconDotNet;
using System;
using System.Collections.Generic;
using ViewROIWPF.Helper;
using ViewROIWPF.Models;

namespace ViewROIWPF
{

    /// <summary>
    /// This class is an auxiliary class, which is used to
    /// link a graphical context to an HALCON object. The graphical
    /// context is described by a hashtable, which contains a list of
    /// graphical modes (e.g GC_COLOR, GC_LINEWIDTH and GC_PAINT)
    /// and their corresponding values (e.g "blue", "4", "3D-plot"). These
    /// graphical states are applied to the window before displaying the
    /// object.
    /// </summary>
    internal class HObjectEntry
    {
        /// <summary>Hashlist defining the graphical context for HObj</summary>
        internal Dictionary<GraphicalMode, object> GContext { get; }

        /// <summary>HALCON object</summary>
        internal HObject HObj { get; }

        internal IntPtr OriginalKey { get; }

        /// <summary>Constructor</summary>
        /// <param name="obj">
        /// HALCON object that is linked to the graphical context gc.
        /// </param>
        /// <param name="gc">
        /// Hashlist of graphical states that are applied before the object
        /// is displayed.
        /// </param>
        internal HObjectEntry(HObject obj, Dictionary<GraphicalMode, object> gc)
        {
            GContext = gc;
            if (obj.IsValid())
            {
                HObj = obj.CopyObj(1, -1);
                OriginalKey = obj.Key;
            }
            else

            {
                HObj = new HObject();
            }
        }

        /// <summary>
        /// Clears the entries of the class members Hobj and gContext
        /// </summary>
        public void Clear()
        {
            GContext?.Clear();
            if (HObj != null)
            {
                HObj.Dispose();
            }
        }
    }//end of class
}
