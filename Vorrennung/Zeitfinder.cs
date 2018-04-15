using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vorrennung
{
    public partial class Zeitfinder : Form
    {
        Infographikfenster f;
        int gesdauer;
        public Zeitfinder(int dauergesammt,Infographikfenster f)
        {

            InitializeComponent();
            this.f = f;
            changeDuration(dauergesammt);
        }
        public void changeDuration(int dauergesammt)
        {
            gesdauer = dauergesammt;
            trackBar1.Maximum = dauergesammt;
            trackBar2.Maximum = dauergesammt;
            trackBar1.TickFrequency = dauergesammt / 20;
            trackBar2.TickFrequency = dauergesammt / 20;
            trackBar2.Minimum = Math.Min(dauergesammt, 20);
            label1.Text = "Startzeit: " + getTimeCode(trackBar1.Value);
            label2.Text = "Dauer: " + getTimeCode(trackBar2.Value);
            setMarker();
        }
        public int dauer { get { return trackBar2.Value; } }
        public int startzeit { get { return trackBar1.Value; } }
        public bool gueltig = false;
        public bool fertig = false;
        public void setPercent(double start,double dauer)
        {
            trackBar1.Value = (int)(start * trackBar1.Maximum);
            trackBar2.Value = (int)(dauer * trackBar2.Maximum);
            label1.Text = "Startzeit: " + getTimeCode(trackBar1.Value);
            label2.Text = "Dauer: " + getTimeCode(trackBar2.Value);
            setMarker();
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label1.Text = "Startzeit: " + getTimeCode(trackBar1.Value);
            setMarker();
        }
        void setMarker()
        {
            f.setStartStopTime(startzeit / (double)gesdauer, dauer / (double)gesdauer);
        }
        public String getTimeCode(int seconds)
        {
            int stunden = (int)Math.Floor(seconds / 3600.0);
            int minuten = ((int)Math.Floor(seconds / 60.0)) % 60;
            int sekunden = seconds % 60;
            String ergebnis = stunden.ToString("D2") + ":" + minuten.ToString("D2") + ":" + sekunden.ToString("D2");
            return ergebnis;

        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            label2.Text = "Dauer: " + getTimeCode(trackBar2.Value);
            f.setStartStopTime (startzeit/(double)gesdauer,dauer/(double)gesdauer);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (gesdauer>=startzeit+ dauer)
            {
                gueltig = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Ungültige Werte");
            }
            
        }

        private void Zeitfinder_Load(object sender, EventArgs e)
        {
            this.Icon = Properties.Resources.Vorrennung_icon;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Zeitfinder_FormClosing(object sender, FormClosingEventArgs e)
        {
            fertig = true;
        }
    }
}
