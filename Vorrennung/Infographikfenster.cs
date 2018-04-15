using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vorrennung.Daniel;
namespace Vorrennung
{
    
    public partial class Infographikfenster : Form
    {
        List<double> distribution;
        public Infographikfenster()
        {
            InitializeComponent();

            lautstaerken = new GraphView(null, Color.FromArgb(0, 0, 127), Color.FromArgb(0, 64, 192), Color.FromArgb(0, 128, 255));

            verteilung = new GraphView(null, Color.FromArgb(0, 127, 0), Color.FromArgb(0, 192, 64), Color.FromArgb(0, 255, 128));
            beschleunigung = new GraphView(null, Color.FromArgb(0, 64, 64), Color.FromArgb(32, 128, 128), Color.FromArgb(64, 192, 192), true);

            lautstaerken.Dock = DockStyle.Fill;
            verteilung.Dock = DockStyle.Fill;
            beschleunigung.Dock = DockStyle.Fill;
           // lautstaerken.zoom.Changed += onChangedLautzoom;
            //beschleunigung.zoom.Changed += onChangedBeschzoom;
            
            lautstaerken.Parent = tableLayoutPanel1;
            
            beschleunigung.Parent = tableLayoutPanel1;
            verteilung.Parent = tableLayoutPanel1;
            lautstaerken.MouseWheel  += changeFromInfos;
            lautstaerken.MouseMove += changeFromInfos;

            beschleunigung.MouseWheel += changeFromInfos;
            beschleunigung.MouseMove += changeFromInfos;


            beschleunigung.MouseMove += mouseMovedOverGraphs;
            lautstaerken.MouseMove += mouseMovedOverGraphs;
            verteilung.MouseMove += mouseMovedOverGraphs;

            tableLayoutPanel1.SetRow(lautstaerken, 0);
            tableLayoutPanel1.SetRow(beschleunigung, 2);
            tableLayoutPanel1.SetRow(verteilung, 4);
        }
        public void mouseMovedOverGraphs(object sender, MouseEventArgs args)
        {
            Control c = (Control)sender;
            if (!c.Focused) { c.Focus(); }
        }
        GraphView lautstaerken, verteilung, beschleunigung;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x10)
            {
                this.Hide();
                return;
            }
                

            base.WndProc(ref m);
        }
        private void Infographikfenster_Load(object sender, EventArgs e)
        {
            this.Icon = Properties.Resources.Vorrennung_icon;
        }
        public void setStartStopTime(double start, double dauer)
        {
            double stop=Math.Min(1,start+dauer);
           
            GraphView.Line tmp=new GraphView.Line(start,Color.FromArgb(128,255,192,128),stop,true);
            lautstaerken.setVertLine("block", tmp);
            beschleunigung.setVertLine("block", tmp);
            /*tmp = new GraphView.Line(stop, Color.FromArgb(255, 255, 255),true);
            lautstaerken.setVertLine("stop", tmp);
            beschleunigung.setVertLine("stop", tmp);*/
            
            
            
            
        }
        public void setHighLowVolume(double low,double high)
        {
            GraphView.Line l;
            l = new GraphView.Line(low,Color.FromArgb(255,192,127),true);
            verteilung.setVertLine("low", l);
            lautstaerken.setHorizLine("low", l);
            l = new GraphView.Line(high, Color.FromArgb(64, 255, 255), true);
            verteilung.setVertLine("high", l);
            lautstaerken.setHorizLine("high", l);
        }
        public void changeFromInfos(object sender,EventArgs e)
        {
            if (sender == beschleunigung)
            {
                onChangedBeschzoom();
                //System.Diagnostics.Trace.WriteLine("besch");
            }
            else if (sender==lautstaerken)
            {
                onChangedLautzoom();
                //System.Diagnostics.Trace.WriteLine("vol");
            }
            else
            {
               // System.Diagnostics.Trace.WriteLine("unbekannt");
            }
        }
        public void onChangedLautzoom()
        {
            if (beschleunigung.zoom.scroll != lautstaerken.zoom.scroll || beschleunigung.zoom.zoom != lautstaerken.zoom.zoom)
            {
                beschleunigung.zoom.scroll = lautstaerken.zoom.scroll;
                beschleunigung.zoom.zoom = lautstaerken.zoom.zoom;
                beschleunigung.zoom.OnChanged();
            }
            
        }
        public void onChangedBeschzoom()
        {
            if (beschleunigung.zoom.scroll != lautstaerken.zoom.scroll || beschleunigung.zoom.zoom != lautstaerken.zoom.zoom)
            {
                lautstaerken.zoom.scroll = beschleunigung.zoom.scroll;
                lautstaerken.zoom.zoom = beschleunigung.zoom.zoom;
                lautstaerken.zoom.OnChanged();
            }
        }
        double Ew = 0;
        double empVar = 0;
        
        public void setVolumes(List<double> vol)
        {
            
            lautstaerken.setValues(vol);
            int werte = 10000;
            distribution = new List<double>(werte);
          
            for (int i = 0; i < werte; i++) { distribution.Add(0); }
            int index;
            int max = 0;
            
            for (int i = 0; i < vol.Count; i++)
            {
                index = (int)Math.Abs(vol[i] *(werte));
                if (index < 0) { index = 0; }
                if (index >= (werte )) { }
                else
                {
                    //  System.Diagnostics.Trace.WriteLine(index);
                    distribution[index]++;
                    if (distribution[index] > max) { max = (int)distribution[index]; }
                    Ew += index;
                    
                }
            }
            Ew = Ew / (double)werte / (double)vol.Count;
            
            Console.WriteLine(distribution.Count);
            for (int i = 0; i < distribution.Count ; i++)
            {
             
                empVar += distribution[i] * Math.Pow(((i / (double)werte) - Ew), 2);
            }
            
            empVar = (empVar / (vol.Count  - 1));
            Console.WriteLine("STDAbw: " + empVar + " EW: " + Ew);
        
            Parallel.For(0, distribution.Count, i =>
            {
                distribution[i] /= max;
            });
            
            /*for (int i = 0; i < quantity.Count ; i++)
            {
                System.Diagnostics.Trace.WriteLine(quantity[i]);
            }*/
            double sum = 0;
            GraphView.Line lein=null,EWLine=new GraphView.Line(Ew ,Color.Red),stdAbwLine=new GraphView.Line(Ew-empVar ,Color.White);
            for (int i=0;i< werte; i++)
            {
                sum += distribution[i]*max/(double)vol.Count;
                if (sum > .5)
                {
                   lein = new GraphView.Line(i/(double)werte, Color.Green );
                    
                    break;
                }
            }
                verteilung.setValues(distribution);
          //  verteilung.setVertLine("50%", lein);
            //verteilung.setVertLine("EW", EWLine);
            //verteilung.setVertLine("stdabw", stdAbwLine);
        }
        public void calibrateThresholds(out double leise,out double laut)
        {
            leise = (Ew - empVar)*.9;
            laut = Ew * .9;
            if (leise < 0)
            {
                leise = Ew * .9*.7;
            }
            if (leise < 0) { leise = 0; }
            if (leise > 1) { leise = 1; }
            if (laut < 0) { laut = 0; }
            if (laut > 1) { laut = 1; }

        }
        public void setBeschleunigung(List<double> besch)
        {
            beschleunigung.setValues(besch);
        }

        private void Infographikfenster_MouseMove(object sender, MouseEventArgs e)
        {
            /*Control c = this.GetChildAtPoint(new Point(e.X, e.Y));
            System.Diagnostics.Trace.WriteLine("Koords: " + e.X + ", " + e.Y + " cntrl: " + c);
            if (c!=null){c.Focus();}*/
        }
        public List<double> getDistribution()
        {
            return distribution;
        }
    }
}
