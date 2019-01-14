using HalconDotNet;
using System.Collections.Generic;
using System.Linq;
using ViewROIWPF.Models;

namespace ViewROIWPF
{
    /// <summary>
    /// This class contains the graphical context of an HALCON object. The
    /// set of graphical modes is defined by the hashlist 'graphicalSettings'.
    /// If the list is empty, then there is no difference to the graphical
    /// setting defined by the system by default. Otherwise, the provided
    /// HALCON window is adjusted according to the entries of the supplied
    /// graphical context (when calling applyContext())
    /// </summary>
    internal class GraphicsContext
    {
        /// <summary>
        /// Dictionary containing entries for graphical modes
        /// which is then linked to some HALCON object to describe its
        /// graphical context.
        /// </summary>
        private readonly Dictionary<GraphicalMode, object> graphicSettings;

        /// <summary>
        /// Backup of the last graphical context applied to the window.
        /// </summary>
        private readonly Dictionary<GraphicalMode, object> graphicStateSetting;

        /// <summary>
        /// Creates a graphical context with no initial
        /// graphical modes
        /// </summary>
        public GraphicsContext()
        {
            graphicSettings = new Dictionary<GraphicalMode, object>();
            graphicStateSetting = new Dictionary<GraphicalMode, object>();
        }

        /// <summary>
        /// Creates an instance of the graphical context with
        /// the modes defined in the hashtable 'settings'
        /// </summary>
        /// <param name="settings">
        /// List of modes, which describes the graphical context
        /// </param>
        internal GraphicsContext(Dictionary<GraphicalMode, object> settings)
        {
            graphicSettings = settings;
            graphicStateSetting = new Dictionary<GraphicalMode, object>();
        }

        /// <summary>Applies graphical context to the HALCON window</summary>
        /// <param name="window">Active HALCON window</param>
        /// <param name="settings">
        /// Collection that contains graphical modes for window
        /// </param>
        internal void ApplyContext(HWindow window, Dictionary<GraphicalMode, object> settings)
        {
            string valS = "";
            int valI = -1;
            HTuple valH = null;

            //iterator = cContext.Keys.GetEnumerator();
            try
            {
                foreach (var setting in settings)
                {
                    if (graphicStateSetting.ContainsKey(setting.Key) && graphicStateSetting[setting.Key] == setting.Value)
                    {
                        continue;
                    }
                    switch (setting.Key)
                    {
                        case GraphicalMode.Color:
                            valS = (string)setting.Value;
                            window.SetColor(valS);
                            if (graphicStateSetting.ContainsKey(GraphicalMode.Colored))
                                graphicStateSetting.Remove(GraphicalMode.Colored);
                            break;

                        case GraphicalMode.Colored:
                            valI = (int)setting.Value;
                            window.SetColored(valI);
                            if (graphicStateSetting.ContainsKey(GraphicalMode.Color))
                                graphicStateSetting.Remove(GraphicalMode.Color);
                            break;

                        case GraphicalMode.LineWidth:
                            valI = (int)setting.Value;
                            window.SetLineWidth(valI);
                            break;

                        case GraphicalMode.DrawMode:
                            valS = (string)setting.Value;
                            window.SetDraw(valS);
                            break;

                        case GraphicalMode.Shape:
                            valS = (string)setting.Value;
                            window.SetShape(valS);
                            break;

                        case GraphicalMode.Lut:
                            valS = (string)setting.Value;
                            window.SetLut(valS);
                            break;

                        case GraphicalMode.LineStyle:
                            valH = (HTuple)setting.Value;
                            window.SetLineStyle(valH);
                            break;

                        case GraphicalMode.Paint:
                            valS = (string)setting.Value;
                            window.SetPaint(valS);
                            break;

                        default:
                            break;
                    }

                    if (valI != -1)
                    {
                        if (graphicStateSetting.ContainsKey(setting.Key))
                            graphicStateSetting[setting.Key] = valI;
                        else
                            graphicStateSetting.Add(setting.Key, setting.Value);

                        valI = -1;
                    }
                    else if (valS != "")
                    {
                        if (graphicStateSetting.ContainsKey(setting.Key))
                            graphicStateSetting[setting.Key] = valI;
                        else
                            graphicStateSetting.Add(setting.Key, setting.Value);
                        valS = "";
                    }
                    else if (valH != null)
                    {
                        if (graphicStateSetting.ContainsKey(setting.Key))
                            graphicStateSetting[setting.Key] = valI;
                        else
                            graphicStateSetting.Add(setting.Key, setting.Value);
                        valH = null;
                    }
                }
            }
            catch (HOperatorException)
            {
                return;
            }
        }

        /// <summary>Sets a value for the graphical mode Color</summary>
        /// <param name="color">
        /// A single color, e.g. "blue", "green" ...etc.
        /// </param>
        internal void SetColor(string color)
        {
            if (graphicSettings.ContainsKey(GraphicalMode.Colored))
                graphicSettings.Remove(GraphicalMode.Colored);

            AddValue(GraphicalMode.Color, color);
        }

        /// <summary>Sets a value for the graphical mode Colored</summary>
        /// <param name="val">
        /// The colored mode, which can be either "3" or "6" or "12"
        /// </param>
        internal void SetColored(int val)
        {
            if (graphicSettings.ContainsKey(GraphicalMode.Color))
                graphicSettings.Remove(GraphicalMode.Color);

            AddValue(GraphicalMode.Colored, val);
        }

        /// <summary>Sets a value for the graphical mode DrawMode</summary>
        /// <param name="val">
        /// One of the possible draw modes: "margin" or "fill"
        /// </param>
        internal void SetDrawMode(string val)
        {
            AddValue(GraphicalMode.DrawMode, val);
        }

        /// <summary>Sets a value for the graphical mode LineWidth</summary>
        /// <param name="val">
        /// The line width, which can range from 1 to 50
        /// </param>
        internal void SetLineWidth(int val)
        {
            AddValue(GraphicalMode.LineWidth, val);
        }

        /// <summary>Sets a value for the graphical mode Lut</summary>
        /// <param name="val">
        /// One of the possible modes of look up tables. For
        /// further information on particular setups, please refer to the
        /// Reference Manual entry of the operator set_lut.
        /// </param>
        internal void SetLut(string val)
        {
            AddValue(GraphicalMode.Lut, val);
        }

        /// <summary>Sets a value for the graphical mode Paint</summary>
        /// <param name="val">
        /// One of the possible paint modes. For further
        /// information on particular setups, please refer refer to the
        /// Reference Manual entry of the operator set_paint.
        /// </param>
        internal void SetPaint(string val)
        {
            AddValue(GraphicalMode.Paint, val);
        }

        /// <summary>Sets a value for the graphical mode Shape</summary>
        /// <param name="val">
        /// One of the possible shape modes. For further
        /// information on particular setups, please refer refer to the
        /// Reference Manual entry of the operator set_shape.
        /// </param>
        internal void SetShape(string val)
        {
            AddValue(GraphicalMode.Shape, val);
        }

        /// <summary>Sets a value for the graphical mode LineStyle</summary>
        /// <param name="val">
        /// A line style mode, which works
        /// identical to the input for the HDevelop operator
        /// 'set_line_style'. For particular information on this
        /// topic, please refer to the Reference Manual entry of the operator
        /// set_line_style.
        /// </param>
        internal void SetLineStyle(HTuple val)
        {
            AddValue(GraphicalMode.LineStyle, val);
        }

        /// <summary>
        /// Adds a value to the Dictionary for the
        /// graphical mode described by the parameter 'key'
        /// </summary>
        /// <param name="key">
        /// A graphical mode
        /// </param>
        /// <param name="val">
        /// Defines the value as an int for this graphical
        /// mode 'key'
        /// </param>
        private void AddValue(GraphicalMode key, object val)
        {
            if (graphicSettings.ContainsKey(key))
            {
                graphicSettings[key] = val;
            }
            else
            {
                graphicSettings.Add(key, val);
            }
        }

        /// <summary>
        /// Clears the list of graphical settings.
        /// There will be no graphical changes made prior
        /// before drawing objects, since there are no
        /// graphical entries to be applied to the window.
        /// </summary>
        public void Clear()
        {
            graphicSettings.Clear();
        }

        /// <summary>
        /// Returns an exact clone of this graphicsContext instance
        /// </summary>
        public GraphicsContext Copy()
        {
            return new GraphicsContext(CopyContext());
        }

        /// <summary>
        /// If the hashtable contains the key, the corresponding
        /// hashtable value is returned
        /// </summary>
        /// <param name="key">
        /// One of the graphical keys
        /// </param>
        internal object GetGraphics(GraphicalMode key)
        {
            graphicSettings.TryGetValue(key, out object val);
            return val;
        }

        /// <summary>
        /// Returns a copy of the Dictionary that carries the
        /// entries for the current graphical context
        /// </summary>
        /// <returns> current graphical context </returns>
        internal Dictionary<GraphicalMode, object> CopyContext()
        {
            return graphicSettings.ToDictionary(entry => entry.Key, entry => entry.Value);
        }

        /// <summary>
        /// Clear the temp state of settings
        /// </summary>
        public void ClearStateSettings()
        {
            graphicStateSetting.Clear();
        }
    }//end of class
}
