using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vorrennung.Daniel;
namespace Vorrennung
{
    public partial class ModernUI : Form
    {
        VideoBeschleuniger beschleuniger = new VideoBeschleuniger();
        MultipleFileChooser fileChooser = new MultipleFileChooser();
        String ffmpegOrt;
        String ffprobeOrt;
        String indatei, outdatei;
        Infographikfenster infofenster = new Infographikfenster();
        String shortcutinitzustand;
        bool applyparams = true;
        bool bootingUp = true;
        bool geoeffnet = false;
        bool askForSimultan = false;
        int simpleSize = 260;
        int expertSize = 400;
        NumericUpDown[] updowns = new NumericUpDown[12];
        TrackBar[] trackbars = new TrackBar[12];
        public ModernUI()
        {
            InitializeComponent();
            //System.Diagnostics.Trace.WriteLine("MÖP?");
            
            trackbars[0] = trackBar1;
            trackbars[1] = trackBar2;
            trackbars[2] = trackBar3;
            trackbars[3] = trackBar4;
            trackbars[4] = trackBar5;
            trackbars[5] = trackBar6;
            trackbars[6] = trackBar7;
            trackbars[7] = trackBar8;
            trackbars[8] = trackBar9;
            trackbars[9] = trackBar10;
            trackbars[10] = trackBar11;
            trackbars[11] = trackBar12;
            for (int i = 0; i < updowns.Length; i++)
            {
                updowns[i] = new NumericUpDown();
                updowns[i].Parent = tableLayoutPanel1;
                updowns[i].Size = new Size(50,updowns[i].Height);
                updowns[i].ValueChanged += numericupdownchanged;
                tableLayoutPanel1.Controls.Add(updowns[i], tableLayoutPanel1.GetColumn(trackbars[i]) - 1, tableLayoutPanel1.GetRow(trackbars[i]));
                updowns[i].Anchor = AnchorStyles.Top;
            }
            
               for (int i=0;i< trackbars.Count() ;i++){
                    
                        double tmp = 0;
                     //   System.Diagnostics.Trace.WriteLine("MÖP!");
                        getSetTrackBarValue(trackbars[i], ref tmp, false, true, 5, true,updowns[i]);
                    
                }
            
            infofenster.Show();
            infofenster.Hide();


            beschleuniger.gesammtFortschrittChanged += gesammtfortschrittchanged;
            beschleuniger.teilFortschrittChanged += teilfortschrittchanged;
            beschleuniger.lautstaerkeVeraendert += volChanged;
            beschleuniger.loadParamsFromProperties();
            System.Diagnostics.Trace.WriteLine("_"+beschleuniger.ffmpegPfad + "-");
            receiveParams();
            beschleuniger.beschleunigungVeraendert += speedChanged;
            bootingUp = false;
            this.Text = this.Tag + " - Version " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        bool ignorechangeNumericChange=false;
        public void numericupdownchanged(object sender,EventArgs e)
        {
            //System.Diagnostics.Trace.WriteLine("Möp°^°");
            if (ignorechangeNumericChange) { return; }
           // System.Diagnostics.Trace.WriteLine("numericupdowncall");
            for (int i = 0; i < updowns.Length; i++)
            {
                if (updowns[i] == sender)
                {
                    double w=(double)updowns[i].Value ;
                    getSetTrackBarValue(trackbars[i],ref w,true);
                    sendParams(true,false);
                    break;
                }
            }
        }
        public String getTimeCode(int seconds)
        {
            int stunden = (int)Math.Floor(seconds / 3600.0);
            int minuten = ((int)Math.Floor(seconds / 60.0)) % 60;
            int sekunden = seconds % 60;
            String ergebnis = stunden.ToString("D2") + ":" + minuten.ToString("D2") + ":" + sekunden.ToString("D2");
            return ergebnis;

        }
        /// <summary>
        /// Setzt oder Liest den Wert einer Trackbar, Genauigkeit und Wertebereich werden aus dem Tag entnommen.
        /// Tag muss dem Folgenden Format entsprechen: minimum,maximum,schritt 
        /// Für die Werte wird ein Dezimalpunkt verwendet. Ist Maximum-Minimum nicht durch Schritt
        /// Teilbar, so wird abgerundet.
        /// </summary>
        /// <param name="bar">Die Trackbar</param>
        /// <param name="value">Der Wert. Er wird überschrieben wenn set auf true gesetzt ist</param>
        /// <param name="set">Ob der Wert auf die Trackbar geschrieben werden soll oder gelesen. True entspricht dabei auf die Trackbar schreiben</param>
        /// <param name="init">Ob die Trackbar zuerst Initialisiert werden soll.</param>
        /// <param name="tickcount">Wie viele Striche eingefügt werden sollen.</param>
        /// <param name="directreturn">Gibt an ob die Funktion nur Initialisieren soll.</param>
        /// <returns>Den übertragenen Wert.</returns>
        public double getSetTrackBarValue(TrackBar bar, ref double value, bool set, bool init = false,int tickcount=10,bool directreturn=true,NumericUpDown updown=null)
        {
            bool oldchange = ignorechangeNumericChange;
            ignorechangeNumericChange = true;
            String[] trennung = ((String)bar.Tag).Split(',');
            double min = double.Parse(trennung[0]);
            double max = double.Parse(trennung[1]);
            double schritt = double.Parse(trennung[2],System.Globalization.NumberStyles.Any,CultureInfo.InvariantCulture);
            if (init)
            {
                int schrittzahl = (int)Math.Floor ((max - min) / schritt);
                bar.Minimum = 0;
                //bar.Value = 0;
                bar.Maximum = schrittzahl;
                bar.TickFrequency = schrittzahl / tickcount;
                if (updown != null)
                {
                    updown.Minimum = (decimal)min;
                    updown.Maximum = (decimal)max;
                    updown.Increment = (decimal)schritt;
                    updown.DecimalPlaces = (int)-Math.Floor(Math.Min(0,Math.Log10(schritt)));
                  //  System.Diagnostics.Trace.WriteLine(schritt + " " + updown.Increment+" ");
                   
                }
                ignorechangeNumericChange = oldchange;
                if (directreturn) { return 0; }
                
            }
            double ergebnis;
            if (set ){
                if (value < min || value > max) { throw new ArgumentException("Der Wert "+value+" liegt nicht im Wertebereich ["+min+","+max+"]");}
                int ziel =(int) Math.Round((value - min) / schritt);
               // System.Diagnostics.Trace.WriteLine("write: "+ziel+" "+bar.Value+" "+value);
                bar.Value = ziel;
                ergebnis = ziel * schritt + min;
                
            }else{
                ergebnis = bar.Value * schritt + min;
                value = ergebnis;
                
            }
            if (updown != null)
            {
                updown.Value = (decimal)ergebnis;
                //System.Diagnostics.Trace.WriteLine("\n"+ergebnis + " " + updown.Value+" "+value+" "+bar.Value );
            }
            ignorechangeNumericChange = oldchange;
            return ergebnis;
        }

        public void speedChanged(object sender, List<double> parameter, double spielzeit, double zeitunterteilung, double samplingrate)
        {
            this.Invoke (new Action(()=>{
                double ersparnis = 0;
                ersparnis = beschleuniger.dauer - spielzeit * beschleuniger.beschleunigungsParameter.minspeed;
                label3.Text = "Vorraussichtliche Dauer: " + getTimeCode((int)spielzeit);
                label4.Text = "Zeitersparnis : " + getTimeCode((int)ersparnis);
                infofenster.setBeschleunigung(parameter);
            }));
        }
        public void volChanged(object sender, List<double> parameter)
        {
            infofenster.setVolumes(parameter);
        }

        int lastges = 0;
        int lastteil = 0;
        public void gesammtfortschrittchanged(object sender, double wert)
        {
            if ((int)(wert * 100) != lastges)
            {
                lastges = (int)(wert * 100);

              //  System.Diagnostics.Trace.WriteLine("geschanged");
                this.Invoke(new Action(() => toolStripProgressBar1.Value = (int)(wert * 100)));
            }
        }
        public void teilfortschrittchanged(object sender, double wert)
        {
            if ((int)(wert * 100) != lastteil)
            {
                lastteil = (int)(wert * 100);

                //System.Diagnostics.Trace.WriteLine("teilchanged");
                this.Invoke(new Action(() => toolStripProgressBar2.Value = (int)(wert * 100)));
            }
        }
        bool arbeitend = false;
        public void sendParams(bool apply=true,bool applytoupdowns=true)
        {
            
            if (bootingUp) { return; }
            ignorechangeNumericChange = true;
          //  System.Diagnostics.Trace.WriteLine("sendparams");
            beschleuniger.aktuell = false;
            beschleuniger.debuglevel = 1;
            beschleuniger.ffmpegPfad = ffmpegOrt ;
            beschleuniger.ffprobePfad = ffprobeOrt;
            beschleuniger.AdditionalFFmpegAudioParams = toolStripTextBox3.Text;
            double unwichtig = 0;
            beschleuniger.useSola = checkBox3.Checked;



            beschleuniger.beschleunigungsParameter.ableitungsglaettung = getSetTrackBarValue(trackBar8, ref unwichtig, false,false,10,true,!applytoupdowns?null:updowns[7]);// / (double)trackBar8.Maximum;
            beschleuniger.beschleunigungsParameter.maxableitung = getSetTrackBarValue(trackBar7, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[6]);
            beschleuniger.beschleunigungsParameter.minableitung = getSetTrackBarValue(trackBar9, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[8]);

            beschleuniger.beschleunigungsParameter.intensity = getSetTrackBarValue(trackBar5, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[4]);

            beschleuniger.beschleunigungsParameter.laut = getSetTrackBarValue(trackBar4, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[3]);
            beschleuniger.beschleunigungsParameter.leise = getSetTrackBarValue(trackBar3, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[2]);
            beschleuniger.beschleunigungsParameter.maxspeed = getSetTrackBarValue(trackBar2, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[1]);
            beschleuniger.beschleunigungsParameter.minspeed = getSetTrackBarValue(trackBar1, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[0]);
            beschleuniger.beschleunigungsParameter.minpausenspeed = getSetTrackBarValue(trackBar6, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[5]);
            beschleuniger.beschleunigungsParameter.fps = (int)numericUpDown1.Value;
            beschleuniger.beschleunigungsParameter.eigeneFps=checkBox4.Checked  ;

            beschleuniger.solablockdiv  =(int) getSetTrackBarValue(trackBar11, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[10]);
            beschleuniger.solasuchberdiv = (int)getSetTrackBarValue(trackBar12, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[11]);
            beschleuniger.solasuchschrdiv = (int)getSetTrackBarValue(trackBar10, ref unwichtig, false, false, 10, true, !applytoupdowns ? null : updowns[9]);
            beschleuniger.grundBeschleunigungViaFFMPEG = checkBox5.Checked;
            beschleuniger.dynaudnorm = dynaudnormVerwendenToolStripMenuItem.Checked;
            askForSimultan = Wiederverwendung.Checked;
            /*beschleuniger.beschleunigungsParameter.ableitungsglaettung = trackBar8.Value / (double)trackBar8.Maximum;
            beschleuniger.beschleunigungsParameter.maxableitung = trackBar7.Value / (double)trackBar7.Maximum;
            beschleuniger.beschleunigungsParameter.minableitung = trackBar9.Value / (double)trackBar9.Maximum;

            beschleuniger.beschleunigungsParameter.intensity = trackBar5.Value*3 / (double)trackBar5.Maximum;

            beschleuniger.beschleunigungsParameter.laut = trackBar4.Value / (double)trackBar4.Maximum;
            beschleuniger.beschleunigungsParameter.leise = trackBar3.Value / (double)trackBar3.Maximum;
            beschleuniger.beschleunigungsParameter.maxspeed = trackBar2.Value / 10.0;
            beschleuniger.beschleunigungsParameter.minspeed = trackBar1.Value / 10.0;
            beschleuniger.beschleunigungsParameter.minpausenspeed = trackBar6.Value / 10.0;*/
            beschleuniger.beschleunigungsParameter.useableitung = checkBox2.Checked;
            beschleuniger.beschleunigungsParameter.rueckpruefung = checkBox1.Checked;
            //infofenster.setHighLowVolume(beschleuniger.beschleunigungsParameter.leise, beschleuniger.beschleunigungsParameter.laut);
            ignorechangeNumericChange = false;
            //System.Diagnostics.Trace.WriteLine("endsendparams");
            if (apply && !arbeitend) { arbeitend = true; doInBackground(new Action(() => { beschleuniger.refresh(); arbeitend = false; }), false); }
            setTexte();
        }
        void setTexte()
        {
            label10.Text = label10.Tag+"";// +" " + beschleuniger.beschleunigungsParameter.minspeed;
            label8.Text = label8.Tag + "";// + " " + beschleuniger.beschleunigungsParameter.leise;
            label9.Text = label9.Tag + "";//+ " " + beschleuniger.beschleunigungsParameter.maxspeed;
            label7.Text = label7.Tag + "";//+" " + beschleuniger.beschleunigungsParameter.laut ;
            label6.Text = label6.Tag + "";//+ " " + beschleuniger.beschleunigungsParameter.minpausenspeed;
            label5.Text = label5.Tag + "";//+ " " + beschleuniger.beschleunigungsParameter.intensity;
            label11.Text = label11.Tag + "";// + " " + beschleuniger.beschleunigungsParameter.maxableitung;
            label12.Text = label12.Tag + "";//+ " " + beschleuniger.beschleunigungsParameter.ableitungsglaettung;
            label13.Text = label13.Tag + "";//+ " " + beschleuniger.beschleunigungsParameter.minableitung;
            infofenster.setHighLowVolume(beschleuniger.beschleunigungsParameter.leise, beschleuniger.beschleunigungsParameter.laut);
        }
        public void receiveParams()
        {
            checkBox3.Checked = beschleuniger.useSola;

            getSetTrackBarValue(trackBar8, ref beschleuniger.beschleunigungsParameter.ableitungsglaettung, true, false, 10, true, updowns[7]);
            getSetTrackBarValue(trackBar7, ref beschleuniger.beschleunigungsParameter.maxableitung, true, false, 10, true, updowns[6]);
            getSetTrackBarValue(trackBar9, ref beschleuniger.beschleunigungsParameter.minableitung, true, false, 10, true, updowns[8]);

            getSetTrackBarValue(trackBar5, ref beschleuniger.beschleunigungsParameter.intensity, true, false, 10, true, updowns[4]);

            getSetTrackBarValue(trackBar4, ref beschleuniger.beschleunigungsParameter.laut, true, false, 10, true, updowns[3]);
            getSetTrackBarValue(trackBar3, ref beschleuniger.beschleunigungsParameter.leise, true, false, 10, true, updowns[2]);
            getSetTrackBarValue(trackBar2, ref beschleuniger.beschleunigungsParameter.maxspeed, true, false, 10, true, updowns[1]);
            getSetTrackBarValue(trackBar1, ref beschleuniger.beschleunigungsParameter.minspeed, true, false, 10, true, updowns[0]);
            getSetTrackBarValue(trackBar6, ref beschleuniger.beschleunigungsParameter.minpausenspeed, true, false, 10, true, updowns[5]);

            double tmpvalue=beschleuniger.solablockdiv;
            getSetTrackBarValue(trackBar11, ref tmpvalue , true, false, 10, true,  updowns[10]);
            tmpvalue = beschleuniger.solasuchberdiv;
            getSetTrackBarValue(trackBar12, ref tmpvalue, true, false, 10, true, updowns[11]);
            tmpvalue = beschleuniger.solasuchschrdiv;
            getSetTrackBarValue(trackBar10, ref tmpvalue, true, false, 10, true, updowns[9]);
            checkBox5.Checked=beschleuniger.grundBeschleunigungViaFFMPEG;
            Wiederverwendung.Checked = askForSimultan;
            dynaudnormVerwendenToolStripMenuItem.Checked = beschleuniger.dynaudnorm;


            numericUpDown1.Value = beschleuniger.beschleunigungsParameter.fps;
            checkBox4.Checked = beschleuniger.beschleunigungsParameter.eigeneFps;
            
            /*
            trackBar8.Value =(int)( beschleuniger.beschleunigungsParameter.ableitungsglaettung * (double)trackBar8.Maximum);
            trackBar7.Value =(int)( beschleuniger.beschleunigungsParameter.maxableitung * (double)trackBar7.Maximum);
            trackBar9.Value =(int)(beschleuniger.beschleunigungsParameter.minableitung * (double)trackBar9.Maximum);

            trackBar5.Value =(int)(beschleuniger.beschleunigungsParameter.intensity/3* (double)trackBar5.Maximum);

            trackBar4.Value=(int)(beschleuniger.beschleunigungsParameter.laut* (double)trackBar4.Maximum);
            trackBar3.Value=(int)(beschleuniger.beschleunigungsParameter.leise  * (double)trackBar3.Maximum);
            trackBar2.Value =(int)( beschleuniger.beschleunigungsParameter.maxspeed * 10.0);
            trackBar1.Value =(int)(beschleuniger.beschleunigungsParameter.minspeed* 10.0);
            trackBar6.Value =(int)(beschleuniger.beschleunigungsParameter.minpausenspeed* 10.0);*/
            checkBox2.Checked = beschleuniger.beschleunigungsParameter.useableitung;
            checkBox1.Checked=beschleuniger.beschleunigungsParameter.rueckpruefung;
            ffmpegOrt = beschleuniger.ffmpegPfad;
            ffprobeOrt = beschleuniger.ffprobePfad;
            toolStripTextBox1.Text = ffmpegOrt;
            toolStripTextBox2.Text = ffprobeOrt;
             toolStripTextBox3.Text=beschleuniger.AdditionalFFmpegAudioParams;
            setTexte();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            sendParams(applyparams);
        }

        private void trackBar6_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 10.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 10.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 10.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 100.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 100.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(trackBar3, (trackBar3.Value / 100.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar1_Scroll_1(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(trackBar1, (trackBar1.Value / 10.0).ToString ());
            //System.Diagnostics.Trace.WriteLine("scroll");
            sendParams(applyparams);
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 1000.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar8_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 1000.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar9_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 1000.0).ToString());
            sendParams(applyparams);
        }

        private void öffnenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void beschleunigenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }
        void ffmpeginfobox()
        {
            MessageBox.Show(@"Dieses Programm benötigt eine funktionierende installation von FFmpeg. Falls FFmpeg installiert ist (oder als Portableversion vorhanden ist), dann reicht es einfach die Dateien FFmpeg.exe & FFprobe.exe in das Vorrennungfenster zu ziehen.");
        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            if ((!File.Exists(ffmpegOrt)||!File.Exists(ffprobeOrt ))&&Properties.Settings.Default.PruefePfade ) { ffmpeginfobox(); return; }
            
            label1.Text = "Dateiname: " + openFileDialog1.FileName.Split(Path.DirectorySeparatorChar).Last();
            doInBackground(new Action(() =>
            {
              indatei = openFileDialog1.FileName;
            bool lockit=false;
              if (geoeffnet&&askForSimultan )
              { lockit = MessageBox.Show("Simultan zur letzten Datei beschleunigen(überschreibt Audiospur)?", "", MessageBoxButtons.YesNo) == DialogResult.Yes; }
              beschleuniger.setInputFileName(indatei,lockit); 
              this.Invoke(new Action(() => label2.Text = "Dauer: " + getTimeCode(beschleuniger.dauer)));
              if (!geoeffnet) { this.Invoke(new Action(() => switchFastButtonText())); }
              geoeffnet = true;
                if (!Properties.Settings.Default.Expertenmodus)
                {
                    this.Invoke(new Action(()=>calibrate()));
                }
            }),true);
        }
        double lastdauer = 0;
        double laststart = 0;
        bool dateiErzeugt = false;
        String lastFileErzeugt;
        public void dateiErzeugtRoutine(String dateiname)
        {
            lastFileErzeugt = dateiname;
            dateiErzeugt = true;
            pictureBox1.Image = System.Drawing.Icon.ExtractAssociatedIcon(lastFileErzeugt).ToBitmap();
            
        }
        public String autoGenerateTargetFileName(String endung)
        {
            String dat = beschleuniger.getInputFileName();
            string[] separ = dat.Split(Path.DirectorySeparatorChar);
            int lastindex = separ[separ.Length - 1].LastIndexOf('.');
            String ergebnis;
            ergebnis = separ[separ.Length - 1];
            if (lastindex != -1) { ergebnis = separ[separ.Length - 1].Remove(lastindex); }
             
            ergebnis = ergebnis +"_fast"+ endung;
            ergebnis = beschleuniger.createTempFileName(ergebnis);
            beschleuniger.registerTempFile(ergebnis);
            return ergebnis;
            
        }
        public void calleGenerierungMitDateiname(String dateifilter,String dateiendung)
        {
            if (Properties.Settings.Default.DragDropErzeugen)
            {
                String dateiname = autoGenerateTargetFileName(dateiendung);
                saveFileDialog1.FileName = dateiname;
                saveFileDialog1_FileOk(this, null);
            }
            else
            {
                saveFileDialog1.Filter = dateifilter;
                saveFileDialog1.ShowDialog();
            }
        }
        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            if (!geoeffnet) { MessageBox.Show("Es muss zuerst eine Datei geöffnet werden."); return; }
            DateTime startzeit = DateTime.Now;
            switch (operationmode)
            {
                case opmode.Video :
            
                    doInBackground(new Action(()=>{
                        outdatei = saveFileDialog1.FileName;
                        beschleuniger.setOutputFileName(outdatei);
                        beschleuniger.beschleunige();
                        System.Diagnostics.Trace.WriteLine("Gesammtaufwand: " + DateTime.Now.Subtract(startzeit).TotalSeconds + "s");
                        this.Invoke(new Action(() => dateiErzeugtRoutine(outdatei)));
                    }),true);
                    break;
                case opmode.CompleteAudio :

                    doInBackground(new Action(() =>
                    {
                        outdatei = saveFileDialog1.FileName;
                        beschleuniger.setOutputFileName(outdatei);
                        beschleuniger.beschleunigeAudio();
                        System.Diagnostics.Trace.WriteLine("Gesammtaufwand: " + DateTime.Now.Subtract(startzeit).TotalSeconds + "s");
                        this.Invoke(new Action(() => dateiErzeugtRoutine(outdatei)));
                    }), true);
                    break;
                case opmode.Reinhoeren :
                    this.Enabled = false;
                    Zeitfinder t = new Zeitfinder(beschleuniger.dauer,infofenster);
                    if (lastdauer != 0 && laststart != 0)
                    {
                        t.setPercent(laststart, lastdauer);
                    }
                    t.FormClosed += dauerfensterClosed;
                    t.Show();
                    break;
                case opmode.GestammeltesSchweigen :
                    doInBackground(new Action(() =>
                    {

                        beschleuniger.setOutputFileName("");
                        beschleuniger.gestammeltesSchweigen();
                        System.Diagnostics.Trace.WriteLine("Gesammtaufwand: " + DateTime.Now.Subtract(startzeit).TotalSeconds + "s");
                        //this.Invoke(new Action(() => dateiErzeugtRoutine(outdatei)));
                    }), true);
                    break;
            }
           
        }

        public void dauerfensterClosed(object sender, EventArgs args)
        {
            Zeitfinder t = (Zeitfinder)sender;
            int startzeit=0;
            int dauer=0;
            this.Invoke(new Action(()=>{
                startzeit = t.startzeit;
                dauer = t.dauer;
            }));
            laststart = startzeit / (double)beschleuniger.dauer;
            lastdauer = dauer / (double)beschleuniger.dauer;
            this.Enabled = true;
            if (t.gueltig)
            {
                doInBackground(new Action(() =>
                {

                    beschleuniger.setOutputFileName("");
                    beschleuniger.reinhoeren(startzeit, dauer);
                }), true);
            }
                 
        }
        bool Activated = true;
        void setActivationState(bool t)
        {
            Activated = t;
            foreach (Control c in this.Controls)
            {
                this.Invoke(new Action(()=>c.Enabled = t));
            }
        }
        void doInBackground(Action a,bool blockierend)
        {
            if (blockierend)
            {
                setActivationState(false);
            }
            Task.Factory.StartNew(new Action(() =>
            {
                try
                {
                    a.Invoke();
                }
                catch { }
                if (blockierend)
                {
                    setActivationState(true);
                }
            }
                ));
        }

        private void trackBar5_Scroll_1(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 100.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar6_Scroll_1(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 10.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar2_Scroll_1(object sender, EventArgs e)
        {
            trackBar2_Scroll(sender, e);
            
        }

        private void trackBar4_Scroll_1(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 100.0).ToString());
            sendParams(applyparams);
        }

        private void trackBar3_Scroll_1(object sender, EventArgs e)
        {
            toolTip1.SetToolTip((TrackBar)sender, (((TrackBar)sender).Value / 1000.0).ToString());
            sendParams(applyparams);
        }

        private void infografikenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            infofenster.Show();
        }

        private void ModernUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
           

        }

        private void ModernUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("Clean");
            beschleuniger.clean();
            System.Diagnostics.Trace.WriteLine("Prop");
            beschleuniger.setParamsToProperties();
            Properties.Settings.Default.AskForSimultan = Wiederverwendung.Checked ;
            System.Diagnostics.Trace.WriteLine("save");
            Properties.Settings.Default.Save();
            System.Diagnostics.Trace.WriteLine("done");
        }

        private void toolStripProgressBar1_Click(object sender, EventArgs e)
        {

        }

        enum opmode {CompleteAudio,Video,Reinhoeren,GestammeltesSchweigen};
        opmode operationmode = opmode.Video;
        int startzeit = 0;
        int dauer = 0;
        private void audioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!geoeffnet) { MessageBox.Show("Es muss zuerst eine Datei geöffnet werden."); return; }
            operationmode = opmode.CompleteAudio;
            //saveFileDialog1.Filter = ;
            //saveFileDialog1.ShowDialog();
            calleGenerierungMitDateiname("MP3 Dateien|*.mp3|Alle Dateien|*.*",".mp3");
        }

        private void videoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!geoeffnet) { MessageBox.Show("Es muss zuerst eine Datei geöffnet werden."); return; }
            if ((!File.Exists(ffprobeOrt)) && Properties.Settings.Default.PruefePfade) { MessageBox.Show("Der FFprobe-Pfad ist ungültig, dieser wird für die Videokonvertierung jedoch benötigt."); return; }
            //saveFileDialog1.Filter = "Avi Dateien|*.avi|Alle Dateien|*.*";
            operationmode = opmode.Video ;
            //saveFileDialog1.ShowDialog();
            calleGenerierungMitDateiname("Avi Dateien|*.avi|Alle Dateien|*.*", ".avi");
        }

        private void reinhörenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!geoeffnet) { MessageBox.Show("Es muss zuerst eine Datei geöffnet werden."); return; }
            operationmode = opmode.Reinhoeren;
            saveFileDialog1_FileOk(this, null);
        }

        /*public static String getTimeCode(int seconds)
        {
            int stunden = (int)Math.Floor(seconds / 3600.0);
            int minuten = ((int)Math.Floor(seconds / 60.0)) % 60;
            int sekunden = seconds % 60;
            String ergebnis = stunden.ToString("D2") + ":" + minuten.ToString("D2") + ":" + sekunden.ToString("D2");
            return ergebnis;

        }*/
        private void ModernUI_Load(object sender, EventArgs e)
        {
            
            generiereDragdropToolStripMenuItem.Checked = Properties.Settings.Default.DragDropErzeugen;
            Wiederverwendung.Checked = Properties.Settings.Default.AskForSimultan;
            this.Icon = Properties.Resources.Vorrennung_icon;
            switchFastButtonText();
            shortcutinitzustand = toolStripButton1.Text;
            toolStripComboBox1.SelectedIndex = Properties.Settings.Default.BevorzugterBeschleunigungsmodus;
            SetMode(Properties.Settings.Default.Expertenmodus);
            expertenmodusToolStripMenuItem.Checked = Properties.Settings.Default.Expertenmodus;
            numericUpDown2.Value = Properties.Settings.Default.Fpswert ;
            checkBox6.Checked = !Properties.Settings.Default.EigeneFPS;

            PausenLaengeTrack.Value= Properties.Settings.Default.Pausenlaengeslider;
            sprechTempoTrack.Value= Properties.Settings.Default.Beschleunigungslider ;
            audioQualiTrack.Value = Properties.Settings.Default.Audioqualislider;
            prüfeAufGültigePfadeToolStripMenuItem.Checked=Properties.Settings.Default.PruefePfade;
        }
        void switchFastButtonText()
        {
            String t = (String)toolStripButton1.Tag;
            toolStripButton1.Tag = toolStripButton1.Text;
            toolStripButton1.Text = t;
        }

        private void gestammeltesSchweigenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!geoeffnet) { MessageBox.Show("Es muss zuerst eine Datei geöffnet werden."); return; }
            operationmode = opmode.GestammeltesSchweigen;
            saveFileDialog1_FileOk(this, null);
        }

        private void gestammeltesSchweigenToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (!geoeffnet) { MessageBox.Show("Es muss zuerst eine Datei geöffnet werden."); return; }
            operationmode = opmode.GestammeltesSchweigen;
            saveFileDialog1_FileOk(this, null);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            sendParams(applyparams);
        }

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            sendParams(applyparams);
        }

        private void beendenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void überToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Ueber tmp = new Ueber();
            tmp.ShowDialog();
        }

        private void optionenToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {
            ffmpegOrt = toolStripTextBox1.Text;
            sendParams();
        }

        private void toolStripTextBox1_TextChanged(object sender, EventArgs e)
        {
            ffmpegOrt = toolStripTextBox1.Text;
            sendParams();
        }

        private void toolStripTextBox1_DoubleClick(object sender, EventArgs e)
        {
           // toolStripTextBox1.Enabled = false;
            openFileDialog2.ShowDialog();
        }

        private void openFileDialog2_FileOk(object sender, CancelEventArgs e)
        {
            toolStripTextBox1.Text = openFileDialog2.FileName;
            ffmpegOrt = toolStripTextBox1.Text;
            sendParams();
            //toolStripTextBox1.Enabled = true;
        
        }

        private void button1_Click(object sender, EventArgs e)
        {
            calibrate();
        }
        void calibrate()
        {
            if (infofenster.getDistribution() != null)
            {
                ignorechangeNumericChange = true;
                //double leise = ValueAssistant.findGoodSilenceThreshold(infofenster.getDistribution());
                double laut, leise;
                infofenster.calibrateThresholds(out leise, out laut);
                if (leise < 0.001) { leise = 0.001; }
                if (leise > 1) { leise = 1; }

                leise = Math.Floor(leise * 10000) / 10000;
                laut = Math.Floor(laut * 10000) / 10000;

                beschleuniger.beschleunigungsParameter.leise = leise;

                beschleuniger.beschleunigungsParameter.laut = laut;
                beschleuniger.refresh();
                receiveParams();
                ignorechangeNumericChange = false;
            }
        }














        private void toolStripTextBox2_DoubleClick(object sender, EventArgs e)
        {
            openFileDialog3.ShowDialog();
        }

        private void openFileDialog3_FileOk(object sender, CancelEventArgs e)
        {
            toolStripTextBox2.Text = openFileDialog3.FileName;
            ffprobeOrt = openFileDialog3.FileName;
            sendParams();
        }

        private void toolStripTextBox2_TextChanged(object sender, EventArgs e)
        {
            ffprobeOrt = toolStripTextBox2.Text ;
            sendParams();
        }

        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.BevorzugterBeschleunigungsmodus = toolStripComboBox1.SelectedIndex;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (ModernUI.ModifierKeys == Keys.Shift || ModernUI.ModifierKeys == Keys.Control || ModernUI.ModifierKeys == Keys.None) { } else { return; }
            if (ModernUI.ModifierKeys == Keys.Control) { button1_Click(sender, e); return; }
            if (!geoeffnet|| (ModernUI.ModifierKeys&Keys.Shift)==Keys.Shift)
            {
                öffnenToolStripMenuItem_Click(null, null);
            }
            else
            {
                switch (toolStripComboBox1.SelectedIndex)
                {/*Audio
Testaudio
Schweigen
Video*/
                    case 0:
                        audioToolStripMenuItem_Click(null, null);
                        break;
                    case 1:
                        reinhörenToolStripMenuItem_Click(null, null);
                        break;
                    case 2:
                        gestammeltesSchweigenToolStripMenuItem_Click(null, null);
                        break;
                    case 3:
                        videoToolStripMenuItem_Click(null, null);
                        break;
                }
            }
        }

        private void ModernUI_DragDrop(object sender, DragEventArgs e)
        {
            if (Activated== false) { return; }
            string[]werte=((string[])e.Data.GetData(DataFormats.FileDrop, true));
            if (Directory.Exists(werte[0]))
            {
                String[] dateien=Directory.GetFiles(werte[0]);
                foreach (String s in dateien)
                {
                    if (s.EndsWith("ffmpeg.exe"))
                    {
                        openFileDialog2.FileName = s;
                        openFileDialog2_FileOk(null, null);
                    }
                    else if (s.EndsWith("ffprobe.exe"))
                    {
                        openFileDialog3.FileName = s;
                        openFileDialog3_FileOk(null, null);
                    }
                }
            }else{
                if (werte[0].EndsWith(".exe"))
                {
                    for (int i = 0; i < werte.Length; i++)
                    {
                        if (werte[i].EndsWith("ffmpeg.exe"))
                        {
                            openFileDialog2.FileName = werte[i];
                            openFileDialog2_FileOk(null, null);
                        }
                        else if (werte[i].EndsWith("ffprobe.exe"))
                        {
                            openFileDialog3.FileName = werte[i];
                            openFileDialog3_FileOk(null, null);
                        }
                    }
                }
                else
                {
                    openFileDialog1.FileName = werte[0];
                    openFileDialog1_FileOk(this, null);
                
                }
            }
            
            
        }

        private void ModernUI_DragEnter(object sender, DragEventArgs e)
        {
            if (Activated == false) { return; }
            try
            {
                if (e.Data.GetFormats().Contains(DataFormats.FileDrop))
                {
                    string[] werte = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                    if ((werte.Length == 1 || (werte.Length == 2 && werte[1].EndsWith(".exe"))) && werte[0].EndsWith(".exe"))
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else if (werte.Length == 1)
                    {
                        e.Effect = DragDropEffects.Copy;
                    }

                }
            }
            catch (Exception ex)
            {
             
            }
        }

        private void ModernUI_MouseEnter(object sender, EventArgs e)
        {
            /*System.Diagnostics.Trace.WriteLine(shortcutinitzustand);
            if ((ModernUI.ModifierKeys & Keys.Shift)==Keys.Shift)
            {
                if (!shortcutinitzustand.Equals(toolStripButton1.Text)) { switchFastButtonText(); }
            }
            else
            {
                if (shortcutinitzustand.Equals(toolStripButton1.Text)&&geoeffnet) { switchFastButtonText(); }
            }*/
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
          
            
        }

        private void label14_MouseDown(object sender, MouseEventArgs e)
        {
            
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (dateiErzeugt )
            {
                
                
                
                var datenObject=new DataObject();
                string[] c = new string[1];
                c[0] = lastFileErzeugt;
                datenObject.SetData(DataFormats.FileDrop ,c);
                System.Diagnostics.Trace.WriteLine("dragdropstart");
                this.AllowDrop = false;
                DragDropEffects ergebnis = DoDragDrop(datenObject, DragDropEffects.Move );
                this.AllowDrop = true;
                System.Diagnostics.Trace.WriteLine("dragdropend");
                System.Diagnostics.Trace.WriteLine("dragdropmode: "+ergebnis);


                    dateiErzeugt = false;
                    pictureBox1.Image = new Bitmap(1, 1);

            }
        }
        public bool testeobsgeht()
        {
            if (!geoeffnet) { MessageBox.Show("Es muss zuerst eine Datei geöffnet werden."); return false; }
            if ((!File.Exists(ffprobeOrt)) && Properties.Settings.Default.PruefePfade) { MessageBox.Show("Der FFprobe-Pfad ist ungültig, dieser wird für die Videokonvertierung jedoch benötigt."); return false; }
            return true;
        }

        private void generiereDragdropToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DragDropErzeugen = generiereDragdropToolStripMenuItem.Checked;
        }

        private void toolStripDropDownButton2_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            sendParams(false);//ändert an dem ergebnis nichts außer den fps, daher sinnfrei nachzurechnen
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            sendParams(false);//ändert an dem ergebnis nichts außer den fps, daher sinnfrei nachzurechnen
        }

        private void trackBar10_Scroll(object sender, EventArgs e)
        {
            sendParams(applyparams);
        }

        private void trackBar12_Scroll(object sender, EventArgs e)
        {
            sendParams(applyparams);
        }

        private void trackBar11_Scroll(object sender, EventArgs e)
        {
            sendParams(applyparams);
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            sendParams(applyparams);
        }

        private void dynaudnormVerwendenToolStripMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            sendParams(false);
        }

        private void expertenmodusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Expertenmodus = expertenmodusToolStripMenuItem.Checked;
            SetMode(Properties.Settings.Default.Expertenmodus);
        }

        private void Wiederverwendung_CheckedChanged(object sender, EventArgs e)
        {
            sendParams(false);
        }

        private void sprechTempoTrack_Scroll(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(sprechTempoTrack, "Tempo*"+(1 + 3*(sprechTempoTrack.Value - sprechTempoTrack.Minimum) / (double)(sprechTempoTrack.Maximum - sprechTempoTrack.Minimum)));
            Properties.Settings.Default.Beschleunigungslider = sprechTempoTrack.Value;
            trackBar1.Value = (int)((trackBar1.Maximum - trackBar1.Minimum)*((sprechTempoTrack.Value-sprechTempoTrack.Minimum )/((double)sprechTempoTrack.Maximum-sprechTempoTrack.Minimum))) + trackBar1.Minimum;
            int solaBlockDiv = 120;// Math.Min((int)(40 + 160 * (sprechTempoTrack.Value - sprechTempoTrack.Minimum) / (sprechTempoTrack.Maximum  - (double)sprechTempoTrack.Minimum)),120);
            int solaSuchBerDiv = 180;//(int)(solaBlockDiv +40);
           
            trackBar11.Value = solaBlockDiv-10;
            trackBar12.Value = solaSuchBerDiv-1;
            trackBar1_Scroll(trackBar1, null);

        }

        private void audioQualiTrack_Scroll(object sender, EventArgs e)
        {
            Properties.Settings.Default.Audioqualislider  = audioQualiTrack.Value;
            int val = (audioQualiTrack.Value - audioQualiTrack.Minimum) * 1200 / (audioQualiTrack.Maximum - audioQualiTrack.Minimum) + 800;
            //Console.WriteLine(val);
            trackBar10.Value = val-50;
            trackBar10_Scroll(trackBar10, null);
        }

        private void PausenLaengeTrack_Scroll(object sender, EventArgs e)
        {
            Properties.Settings.Default.Pausenlaengeslider = PausenLaengeTrack .Value;
            double wert = (PausenLaengeTrack.Value - PausenLaengeTrack.Minimum) / (double)(PausenLaengeTrack.Maximum - PausenLaengeTrack.Minimum);
            wert = (1-wert) * .5 + (.125 / 32);
            trackBar5.Value = (int)(wert * 10000);
            trackBar5_Scroll(trackBar6, null);
            
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            checkBox4.Checked = !checkBox6.Checked;
            checkBox4_CheckedChanged(checkBox4, null);
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown1.Value = numericUpDown2.Value;
            numericUpDown1_ValueChanged(numericUpDown1, null);
        }

        private void prüfeAufGültigePfadeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.PruefePfade = prüfeAufGültigePfadeToolStripMenuItem.Checked;
        }

        private void mehrereBeschleunigenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("TODO.");
            return;
            fileChooser.ShowDialog();
        }

        private void zusätzlicheAudioparameterFürFFmpegToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripTextBox3_TextChanged(object sender, EventArgs e)
        {
            sendParams();
        }

        private void toolStripTextBox3_Click(object sender, EventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {
            
        }

        void SetMode(bool expertenmodus)
        {
            
            if (expertenmodus)
            {
                panel1.Enabled = false;
                panel1.Visible = false;
                panel1.SendToBack();
                this.Size = new Size(this.Size.Width ,expertSize);
            }
            else
            {
                panel1.Enabled = true;
                panel1.Visible = true;
                panel1.BringToFront();
                this.Size = new Size(this.Size.Width, simpleSize );
            }
        }

    }
}
