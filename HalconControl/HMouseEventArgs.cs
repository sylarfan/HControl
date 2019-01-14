using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HalconControl
{
    public class HSmartMouseEventArgs:EventArgs
    {
        internal HSmartMouseEventArgs(double x, double y, double row, double column, int delta, MouseButton? button)
        {
            X = x;
            Y = y;
            Row = row;
            Column = column;
            Delta = delta;
            Button = button;
        }
        /// <summary>Gets the window x coordinate of the mouse event.</summary>
        public double X { get; }

        /// <summary>Gets the window y coordinate of the mouse event.</summary>
        public double Y { get; }

        /// <summary>Gets the row image coordinate of the mouse event.</summary>
        public double Row { get; }

        /// <summary>Gets the column image coordinate of the mouse
        /// event.
        /// </summary>
        public double Column { get; }

        /// <summary>Gets the increment for the mouse wheel change.</summary>
        public int Delta { get; }

        /// <summary>Gets which mouse button was pressed.</summary>
        public MouseButton? Button { get; internal set; }
    }

    public delegate void HSmartMouseEventHandler(object sender, HSmartMouseEventArgs e);

    /// <summary>
    /// In some situations (like a missing license in runtime), it can be the case that
    /// internal exceptions are thrown, and the user has no way of capturing them.
    /// This callback allows the user to react to such runtime errors.
    /// </summary>
    /// <param name="he"></param>
    public delegate void HSmartErrorHandler(HalconException he);
}
