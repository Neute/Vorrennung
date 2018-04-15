using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;

namespace Vorrennung
{
    namespace Daniel
    {


        public class GraphView : UserControl
        {
            #region definitions

            /// <summary>
            /// Defines the ZoomState of the GraphView and gets changed by mouse interaction.
            /// Can synchronize the zoom of multiple GraphViews
            /// </summary>
            public class zoomSettings
            {
                public delegate void ChangedEventHandler();
                /// <summary>
                /// Informs the attached GraphViews to update themselves
                /// </summary>
                public event ChangedEventHandler Changed;
                public virtual void OnChanged()
                {
                    
                    if (Changed != null)
                        Changed();
                }
                /// <summary>
                /// <para>How much of the not seen content is left of the visible area.
                /// If zoom is 0.1 and scroll is 0.2 then the visible area starts at ...</para>
                /// <para>(1 - 0.1) * 0.2 = 0.9 * 0.2 = 0.18</para>
                /// <para>... 18% of the content and spans 10% of the content.</para>
                /// </summary>
                public double scroll = 0;
                /// <summary>
                /// <para>How much of the content is visible;</para>
                /// 1 = all; 0 = nothing
                /// </summary>
                public double zoom = 1;
            }
            /// <summary>
            /// A Line in a graphView
            /// </summary>
            public class Line
            {
                /// <summary></summary>
                /// <param name="v">The Value of the line.</param>
                /// <param name="c">The Color of the Line.</param>
                /// <param name="e">The End of the Block; &lt;=0 if Line; if Block then &gt;Value;.</param>
                /// <param name="front">Draw Line in front or back of graph</param>
                public Line(double v, Color c, double e = -1, bool front = true)
                {
                    value = v;
                    end = e;
                    color = c;
                    this.front = front;
                }
                /// <summary></summary>
                /// <param name="v">The Value of the line.</param>
                /// <param name="c">The Color of the Line.</param>
                /// <param name="front">Draw Line in front or back of graph</param>
                public Line(double v, Color c, bool front) : this(v, c, -1, front) { }
                public double value, end;
                public Color color;
                public bool front;
            }

            /// <summary>
            /// Clicked event handler. X and Y give the position in the content data.
            /// </summary>
            public delegate void ClickedEventHandler(object Sender, double x, double y, EventArgs e);
            /// <summary>
            /// Informs about the position in the data the user clicked on
            /// </summary>
            public event ClickedEventHandler Clicked;





            private List<double> values;
            private bool inv;
            /// <summary>
            /// Gets the zoom settings. Use this to make two GraphViews zoom and pan synchronized
            /// </summary>
            public zoomSettings zoom { get; private set; }
            zoomSettings lastzoom = new zoomSettings();
            private Dictionary<String, Line> vertLines, horizLines;
            #endregion


            #region constructors
            public GraphView(List<double> values, Color maximumColor, Color averageColor, Color minimumColor, zoomSettings settings, bool displayInvertedValues)
            {
                zoom = settings;
                inv = displayInvertedValues;

                pMax = new Pen(new SolidBrush(maximumColor));
                pAvg = new Pen(new SolidBrush(averageColor));
                pMin = new Pen(new SolidBrush(minimumColor));

                this.MouseMove += mouseDrag;
                this.MouseWheel += mouseScroll;
                this.MouseUp += mouseUp;
                this.Click += mouseClick;

                SetStyle(ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                SetStyle(ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);

                vertLines = new Dictionary<String, Line>();
                horizLines = new Dictionary<String, Line>();

                settings.Changed += () => { this.Invalidate(); this.Update(); };

                setValues(values);
            }
            public GraphView(List<double> values, Color maximumColor, Color averageColor, Color minimumColor, zoomSettings settings) :
                this(values, maximumColor, averageColor, minimumColor, settings, false)
            { }
            public GraphView(List<double> values, Color maximumColor, Color averageColor, Color minimumColor, bool displayInvertedValues) :
                this(values, maximumColor, averageColor, minimumColor, new zoomSettings(), displayInvertedValues)
            { }
            public GraphView(List<double> values, Color maximumColor, Color averageColor, Color minimumColor) :
                this(values, maximumColor, averageColor, minimumColor, new zoomSettings(), false)
            { }
            #endregion



            #region getset
            /// <summary>
            /// Resets the visible area
            /// </summary>
            public void resetZoom()
            {
                zoom.zoom = 1;
                zoom.scroll = 0;
                zoom.OnChanged();
            }



            public void setValues(List<double> v)
            {
                if (values != null && values.Equals(v))
                    return;
                values = v;
                newBitmap = true;
                this.Invalidate();
            }




            public void setVertLine(String name, Line line)
            {
                vertLines[name] = line;
                this.Invalidate();
                this.BeginInvoke(new Action(() =>
                {
                    this.Update();
                }));
            }
            public Line getVertLine(String name)
            {
                return vertLines[name];
            }
            public void removeVertLine(String name)
            {
                vertLines.Remove(name);
                this.Invalidate();
            }
            public void setHorizLine(String name, Line line)
            {
                horizLines[name] = line;
                this.Invalidate();
                this.BeginInvoke(new Action(() =>
                {
                    this.Update();
                }));
            }
            public Line getHorizLine(String name)
            {
                return horizLines[name];
            }
            public void removeHorizLine(String name)
            {
                horizLines.Remove(name);
                this.Invalidate();
            }
            #endregion



            #region paint
            /*
		 * Declare these Variables outside paint to be faster
		 * Diese Variablen außerhalb von onPaint deklarieren, damit schneller
		 */
            bool newBitmap = true;
            Bitmap b;
            private Pen pMax, pAvg, pMin;
            private Point bottom = new Point(0, 0), top = new Point(0, 0);

            protected override void OnPaint(PaintEventArgs pe)
            {
                base.OnPaint(pe);
                if (!newBitmap)
                {
                    if (lastzoom.scroll != zoom.scroll || lastzoom.zoom != zoom.zoom)
                    {
                        lastzoom.scroll = zoom.scroll;
                        lastzoom.zoom = zoom.zoom;
                        newBitmap = true;
                    }
                    if (b==null||b.Width != this.Width || b.Height != this.Height) { newBitmap = true; }
                }
                if (newBitmap)
                {
                    Graphics g;
                    if (b != null)
                    {
                        if (b.Width != this.Width || b.Height != this.Height)
                        {
                            b = new Bitmap(this.Width, this.Height);
                            g = Graphics.FromImage(b);
                        }
                        else
                        {
                            g = Graphics.FromImage(b);
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                            g.FillRectangle(new SolidBrush(Color.FromArgb(0,0,0,0)), new Rectangle(0, 0, this.Width, this.Height));
                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        }

                    }
                    else
                    {
                        b = new Bitmap(this.Width, this.Height);
                        g = Graphics.FromImage(b);
                    }
                    newBitmap = false;

                    if (values == null)
                        return;




                    double pointsPerP = (double)values.Count * zoom.zoom / this.Width;

                    double sum = 0;
                    double max = 0;
                    double min = 0;
                    double tmp;
                    int count = 0;
                    int j, end;


                    


                    top.Y = this.Height;
                    bottom.Y = this.Height;

                    int offset = (int)((double)values.Count * (1 - zoom.zoom) * zoom.scroll);



                    /*
                     * Calculate the corresponding values for each column
                     * Für jede Spalte die gewünschten Werte errechnen
                     */
                    for (int i = 0; i < this.Width; i++)
                    {

                        j = offset + (int)Math.Ceiling((double)i * pointsPerP);
                        end = offset + (int)Math.Ceiling((double)(i + 1) * pointsPerP);

                        if (j < end)
                        {
                            sum = 0;
                            max = 0;
                            min = values.Count > j ? values[j] : inv ? 1 : 0;
                        }
                        count = 0;

                        for (; j < end && j < values.Count; j++)
                        {
                            sum += values[j];
                            if (values[j] > max)
                                max = values[j];
                            if (values[j] < min)
                                min = values[j];
                            count++;
                        }


                        if (count > 0)
                        {
                            sum /= count;

                            if (inv && sum > 0)
                            {
                                sum = 1.0 / sum;
                                tmp = 1.0 / max;
                                max = 1.0 / min;
                                min = tmp;
                            }
                        }


                        bottom.X = i;
                        top.X = i;
                        top.Y = (int)(this.Height * (1 - max));
                        g.DrawLine(pMax, bottom, top);
                        top.Y = (int)(this.Height * (1 - sum));
                        g.DrawLine(pAvg, bottom, top);
                        top.Y = (int)(this.Height * (1 - min));
                        g.DrawLine(pMin, bottom, top);
                    }
                }

                Graphics ziel = pe.Graphics;
                paintLines(ziel, false);
                ziel.DrawImageUnscaled(b, Point.Empty);
                paintLines(ziel, true);

            }


            private void paintLines(Graphics g, bool front)
            {
                /*
                 * Draw vertical lines
                 * Vertikale Linien einzeichnen
                 */
                top.Y = 0;
                double x = 0, x2 = 0;
                foreach (Line l in vertLines.Values)
                {
                    if (l.front != front)
                        continue;

                    x = (l.value - (1 - zoom.zoom) * zoom.scroll) / zoom.zoom;

                    // Draw line / Linie zeichnen
                    if (l.end < 0)
                    {
                        if (0 <= x && x < 1)
                        {
                            bottom.X = (int)(x * this.Width);
                            top.X = bottom.X;
                            g.DrawLine(new Pen(new SolidBrush(l.color)), bottom, top);
                        }
                    }

                    // Fill block / Block füllen
                    else
                    {
                        x2 = (l.end - (1 - zoom.zoom) * zoom.scroll) / zoom.zoom;
                        if (x < 1 && x2 > 0)
                        {
                            g.FillRectangle(new SolidBrush(l.color), new RectangleF(
                                (int)(x * this.Width), 0,
                                (int)((Math.Min(1, x2) - x) * this.Width),
                                this.Height));
                        }
                    }
                }

                /*
                 * draw horizontal lines
                 * Horizontale Linien einzeichnen
                 */
                bottom.X = 0;
                top.X = this.Width;
                top.Y = 0;
                foreach (Line l in horizLines.Values)
                {
                    if (l.front != front)
                        continue;


                    // Draw line / Linie zeichnen
                    if (l.end < 0)
                    {
                        bottom.Y = (int)((1 - l.value) * this.Height);
                        top.Y = bottom.Y;
                        g.DrawLine(new Pen(new SolidBrush(l.color)), bottom, top);
                    }

                    // Fill block / Block füllen
                    else
                    {
                        g.FillRectangle(new SolidBrush(l.color), new RectangleF(
                            0, (int)((1 - l.end) * this.Height),
                            this.Width, (int)(((l.end - l.value)) * this.Height)));
                    }
                }
            }
            #endregion







            #region mouse
            private int mHistX = 0;
            private void mouseDrag(object Sender, MouseEventArgs e)
            {
                if (!e.Button.Equals(MouseButtons.None))
                {
                    if (zoom.zoom < 1)
                        zoom.scroll += (double)(mHistX - e.X) / this.Width * zoom.zoom / (1 - zoom.zoom);
                    zoom.scroll = Math.Min(1, Math.Max(0, zoom.scroll));
                    zoom.OnChanged();
                }
                mHistX = e.X;
            }

            private void mouseScroll(object Sender, MouseEventArgs e)
            { // Zoom into Mouse position

                if (Control.ModifierKeys == Keys.Shift)
                { // Horizontal Scroll
                    if (zoom.zoom < 1)
                        zoom.scroll += (double)(-e.Delta / 2) / this.Width * zoom.zoom / (1 - zoom.zoom);
                    zoom.scroll = Math.Min(1, Math.Max(0, zoom.scroll));
                }
                else
                { // Zoom

                    // Calculate mouse position on theoretical full width / Mausposition auf theoretischer Gesamtbreite berechnen
                    zoom.scroll = zoom.scroll * (1 - zoom.zoom) + ((double)e.Location.X / this.Width) * zoom.zoom;

                    zoom.zoom /= Math.Pow(2, (double)e.Delta / 1000);
                    zoom.zoom = Math.Min(zoom.zoom, 1);

                    // reverse upper calculation / obige Berechnung rückwärts mit neuem zoom-Wert
                    if (zoom.zoom < 1)
                        zoom.scroll = (zoom.scroll - ((double)e.Location.X / this.Width) * zoom.zoom) / (1 - zoom.zoom);

                    zoom.scroll = Math.Min(1, Math.Max(0, zoom.scroll));
                }
                zoom.OnChanged();
            }

            private int mouseX, mouseY;
            private void mouseUp(object Sender, MouseEventArgs e)
            { // Save last Mouse Position
                mouseX = e.Location.X;
                mouseY = e.Location.Y;
            }
            private void mouseClick(object Sender, EventArgs e)
            { // Inform about Mouse click position in data; Use Coordinates from last MouseUp
                if (Clicked != null)
                { // Math from Scroll
                    Clicked(this, (zoom.scroll * (1 - zoom.zoom) + ((double)mouseX / this.Width) * zoom.zoom), (1 - (double)mouseY / this.Height), e);
                }
            }
            #endregion
        }
    }
}

