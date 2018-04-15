#define SOLA_SEHR_GENAU // für hq
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vorrennung
{
    class VideoBeschleuniger
    {


        #region datentypen

        public class IOSampleBeziehung
        {

            public int inputsamplezahl;
            public int outputsamplezahl;
        }

        #endregion







        #region variablen
        bool locked = false;
        public bool aktuell = false;
        WavReader input;
        WavWriter output;
        long speedBlockSize;
        public List<double> beschleunigungsFaktoren;
        public int dauer { get { return (int)(input.Count / input.samplingrate); } }
        public event progressInformation teilFortschrittChanged;
        public event progressInformation gesammtFortschrittChanged;
        double samplingrate;
        public List<String> tempDateiNamen;
        List<IOSampleBeziehung> InputVersusOutput;
        public String tempDateiOrdner;
        public String ffmpegPfad;
        int volumeBlockSize;
        public List<double> lautstaerken;
        public int debuglevel;
        public String ffprobePfad;
        StreamWriter errorlog = null;
        public DateTime startzeit = DateTime.Now;
        int blockzusammenfassung = 2;
        public speedupparams beschleunigungsParameter=new speedupparams ();
        public bool useSola;
        protected String inputdateiname;
        protected String outputdateiname;
        protected String tempwavdatei;

        public bool grundBeschleunigungViaFFMPEG ;
        public int solablockdiv;
        public int solasuchberdiv;
        public int solasuchschrdiv ;
        public bool dynaudnorm = true;


        public string AdditionalFFmpegAudioParams
        {
            get
            {
                return Properties.Settings.Default.ExtraFFmpegParams;
            }
            set
            {
                Properties.Settings.Default.ExtraFFmpegParams = value; 
            }
        }
        #endregion
        public delegate void progressInformation(object sender,double value);


        #region hilfsfunktionen
        void setzeGesammtFortschritt(int wert)
        {
            setzeGesammtFortschritt(wert / 100.0);
        }
        void setzeGesammtFortschritt(double wert)
        {
            if (gesammtFortschrittChanged != null) { gesammtFortschrittChanged.Invoke(this, wert); }
        }
        void setzeTeilFortschritt(int wert)
        {
            setzeTeilFortschritt(wert / 100.0);
        }
        void setzeTeilFortschritt(double wert)
        {
            if (teilFortschrittChanged != null) { teilFortschrittChanged.Invoke(this, wert); }
        }
        public System.Diagnostics.Process ffmpegCall(String param, bool start = true)
        {
            TraceExclamation("ffmpegcall");

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            /*if (!usecmd)
            {*/
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            if (debuglevel == 4)//für erweiterte debugzwecke
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = true;

                //p.StartInfo.CreateNoWindow = true;
            }
            p.StartInfo.FileName = ffmpegPfad ;

            p.StartInfo.Arguments = param;//.Replace("\\", "/");
            TraceExclamation("FFMPEG: " + param);
            if (start) { p.Start(); }
            return p;
        }
        public void extendedWaitForExit(System.Diagnostics.Process p, bool dynamic = true)
        {
            TraceExclamation("exwaitforexit");
            String ergebnis = "\nCall: " + p.StartInfo.FileName + " " + p.StartInfo.Arguments + "\n/////////////////////////////////////////////////\n";
            if (!dynamic)
            {
                p.WaitForExit();
                //return;

                try
                {

                    if (p.StartInfo.RedirectStandardOutput)
                    {
                        String stdoutput = "\nStdoutput:---------------------------------------\n";
                        while (!p.StandardOutput.EndOfStream) { stdoutput = stdoutput + p.StandardOutput.ReadLine() + "\n"; }
                    }
                    if (p.StartInfo.RedirectStandardError)
                    {
                        String stderror = "\nStderror:-----------------------------------------\n";
                        while (!p.StandardError.EndOfStream) { stderror = stderror + p.StandardError.ReadLine() + "\n"; }
                        stderror += "\n";
                        ergebnis = ergebnis + stderror;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
                ergebnis += "////////////////////////////////////////////////////\n";
                TraceExclamation(ergebnis, 4);
            }
            else
            {
                TraceExclamation(ergebnis, 4);
                while (!p.HasExited)
                {
                    p.WaitForExit(100);

                    if (p.StartInfo.RedirectStandardOutput)
                    {
                        while (!p.StandardOutput.EndOfStream)
                        {
                            String zeile = "STDOUT: " + p.StandardOutput.ReadLine() + "\n";
                            System.Diagnostics.Trace.WriteLine(zeile);
                            TraceExclamation(zeile, 4);

                        }
                    }
                    if (p.StartInfo.RedirectStandardError)
                    {
                        while (!p.StandardError.EndOfStream)
                        {
                            String zeile = "STDERR: " + p.StandardError.ReadLine() + "\n";
                            System.Diagnostics.Trace.WriteLine(zeile);
                            TraceExclamation(zeile, 4);
                        }
                    }
                }
                TraceExclamation("////////////////////////////////////////////////////\n", 4);
            }
        }
        public String getInputFileName()
        {
            return inputdateiname;
        }
        public String getTimeCode(int seconds)
        {
            int stunden = (int)Math.Floor(seconds / 3600.0);
            int minuten = ((int)Math.Floor(seconds / 60.0)) % 60;
            int sekunden = seconds % 60;
            String ergebnis = stunden.ToString("D2") + ":" + minuten.ToString("D2") + ":" + sekunden.ToString("D2");
            return ergebnis;

        }


        protected String generateTempFolder(String startpath)
        {
            String start = startpath + Path.DirectorySeparatorChar +"Vorrennungtemp-";
            String ergebnis = start;
            bool gefunden = false;
            for (int i = 0; i < 1000; i++)
            {
                ergebnis = start + i;
                if (!Directory.Exists(ergebnis))
                {
                    Directory.CreateDirectory(ergebnis);
                    gefunden = true;
                    break;
                }
            }
            if (!gefunden) { throw new Exception("Es konnte kein Tempordner angelegt werden."); }
            registerTempFile(ergebnis);
            return ergebnis;

        }
        public void deleteTempFiles()
        {
            for (int i = 0; i < tempDateiNamen.Count; i++)
            {
                try
                {
                    if (tempDateiNamen[i] != "")
                    {
                        if (File.Exists(tempDateiNamen[i])) { File.Delete(tempDateiNamen[i]); }
                        if (Directory.Exists(tempDateiNamen[i]))
                        {
                            foreach (String f in Directory.EnumerateFiles(tempDateiNamen[i]))
                            {
                                try
                                {
                                    File.Delete(f);
                                }
                                catch (Exception e)
                                {
                                    TraceExclamation(e);
                                }
                            }
                            Directory.Delete(tempDateiNamen[i]);

                        }
                    }
                }
                catch (Exception e)
                {
                    TraceExclamation("Datei \"" + tempDateiNamen[i] + "\" konnte nicht gelöscht werden. Details: \n" + e);
                }
            }
        }
        #region traces
        public void TraceExclamation(String wert)
        {

            switch (debuglevel)
            {
                case 0:
                    break;
                case 1:
                    System.Diagnostics.Trace.WriteLine(wert);
                    break;
                case 2:
                    Console.WriteLine(wert);
                    break;
                case 3:
                    MessageBox.Show(wert);
                    break;
                case 4:
                    if (errorlog == null)
                    {
                        String name = "errorlog " + startzeit.ToString().Replace(":", "-") + "";
                        while (File.Exists(name))
                        {
                            name += "-";
                        }
                        name += ".txt";
                        errorlog = new StreamWriter(File.Open(name, FileMode.Create, FileAccess.Write, FileShare.None));
                    }
                    errorlog.WriteLine(DateTime.Now.Subtract(startzeit).ToString() + "->:\n");
                    errorlog.WriteLine(wert);
                    errorlog.Flush();
                    break;
            }
        }
        public void TraceExclamation(object wert)
        {
            if (wert != null) { TraceExclamation(wert.ToString()); } else { TraceExclamation("null"); }
        }
        public void TraceExclamation(double wert)
        {
            TraceExclamation(wert.ToString());
        }
        public void TraceExclamation(decimal wert)
        {
            TraceExclamation(wert.ToString());
        }
        public void TraceExclamation(int wert)
        {
            TraceExclamation(wert.ToString());
        }
        public void TraceExclamation(object wert, int tracelevelRequired)
        {
            if (debuglevel == tracelevelRequired)
            {
                if (wert != null) { TraceExclamation(wert.ToString()); } else { TraceExclamation("null"); }
            }
        }
        #endregion
        public String createTempFileName(String dateiname)
        {
            TraceExclamation("Gentempfile");
            if (tempDateiOrdner == null || tempDateiOrdner == ""||!Directory.Exists(tempDateiOrdner))
            {
                tempDateiOrdner = "";
                tempDateiOrdner=generateTempFolder(Directory.GetCurrentDirectory());
            }
            String ergebnis = tempDateiOrdner + Path.DirectorySeparatorChar ;
            String basis = ergebnis;
            int n=0;
            ergebnis = basis + dateiname;
            if (File.Exists(ergebnis))
            {
                do
                {
                    ergebnis = basis + n + dateiname;
                    n++;
                } while (File.Exists(ergebnis) && n < 100000);
            }
            return ergebnis;
        }
        public void registerTempFile(String tempfile)
        {
            TraceExclamation("regtempfile: "+tempfile);
            if (tempDateiNamen == null) { tempDateiNamen = new List<String>(); }
            tempDateiNamen.Add(tempfile);
        }
#endregion


        
        public delegate void faktorenChanged(object sender ,List<double> parameter);
        public delegate void beschleunigungsFaktorenChanged(object sender, List<double> parameter,double spielzeit,double zeitunterteilung,double samplingrate);
        public event faktorenChanged lautstaerkeVeraendert;
        public event beschleunigungsFaktorenChanged beschleunigungVeraendert;

        protected void generiereWavInputDatei()
        {
            
                TraceExclamation("genwavinput");
                String tmpaudioin=createTempFileName("tmpaudioin.wav");
                registerTempFile(tmpaudioin);
                /*if (dynaudnorm)//deprecated
                {
                    extendedWaitForExit(ffmpegCall("-v quiet -y -i \"" + inputdateiname + "\" -acodec pcm_s16le -ac 1 -af dynaudnorm=f=100:g=5  "+AdditionalFFmpegAudioParams+" \"" + tmpaudioin + "\""));
                }
                else { */
                    extendedWaitForExit(ffmpegCall("-v quiet -y -i \"" + inputdateiname + "\" -acodec pcm_s16le -ac 1 " + AdditionalFFmpegAudioParams + " \"" + tmpaudioin + "\""));
                //}
                //input = new WavReader(File.Open(tmpaudioin, FileMode.Open, FileAccess.Read, FileShare.Read),0);
                input = new WavReader(new FileStream(tmpaudioin, FileMode.Open, FileAccess.Read, FileShare.None, 16777216),0);
            
                samplingrate = input.samplingrate;
                //speedBlockSize = input.samplingrate /20;//}-------------------------------------------------------------------------------
                //volumeBlockSize = (int)(input.samplingrate / 40);

///TODO
            // hier in zukunft werte via primzahlzerlegung generieren, sonst wirds ungenau
                speedBlockSize = input.samplingrate / 200;
                volumeBlockSize = (int)(input.samplingrate / 400);
            
                blockzusammenfassung = 2;
                Console.WriteLine("Bevor Anpassung: " + speedBlockSize + " " + blockzusammenfassung + " " + volumeBlockSize);
                if (volumeBlockSize * blockzusammenfassung != speedBlockSize) { speedBlockSize = blockzusammenfassung * volumeBlockSize; }
                Console.WriteLine("Nach Anpassung: " + speedBlockSize + " " + blockzusammenfassung + " " + volumeBlockSize);
        }

        #region externa
        public void gestammeltesSchweigen()
        {
               
            setzeGesammtFortschritt((int)(0));
            if (!aktuell) { refresh(); }
            try
            {

                //List<double> schweigensamples=new List<double>();
                String tempaudiooutfilename = createTempFileName("schweigen");
                registerTempFile(tempaudiooutfilename);
                WavWriter tempaudioout = new WavWriter(new FileStream(tempaudiooutfilename, FileMode.Create, FileAccess.Write, FileShare.None, 16777216), 0, input.samplingrate);
                List<double> beschfaktoren = new List<double>();
                double toleranz = 1.1;
                int sampleposition = 0;
                for (int i = 0; i < beschleunigungsFaktoren.Count(); i++)
                {
                    if (beschleunigungsFaktoren[i] >= toleranz * beschleunigungsParameter.minspeed)
                    {
                        beschfaktoren.Add(beschleunigungsFaktoren[i]);
                        for (int n = 0; n < speedBlockSize; n++)
                        {
                            if (sampleposition + n < input.Count) { tempaudioout.write(input[sampleposition + n]); }
                            
                        }
                    }
                    sampleposition +=(int) speedBlockSize;
                }
                tempaudioout.close(true);
                WavReader tmpaudioin = new WavReader(new FileStream(tempaudiooutfilename, FileMode.Open , FileAccess.Read, FileShare.None, 16777216), 0);
                setzeGesammtFortschritt((int)(25));
                fastenViaSola(tmpaudioin,beschfaktoren);
                setzeGesammtFortschritt((int)(100));
                tmpaudioin.close();
                spieleAb(tempwavdatei);
                
            }
            catch (Exception e)
            {
                TraceExclamation(e);
            }
        }
        public void loadParamsFromProperties()
        {
            TraceExclamation("proptoparams");
            var p = Properties.Settings.Default;
            var b = beschleunigungsParameter;
            aktuell = false;


            useSola = p.SolaVerwenden ;
            b.ableitungsglaettung = p.Ableitungsglaettung ;
            b.maxableitung = p.MaximalerAbfall ;
            b.useableitung = p.AbleitungBerueck ;
            b.minableitung = p.MinimalerAbfall;
            debuglevel = 1;
            
            ffmpegPfad = p.FFmpeg; ;
            ffprobePfad = p.FFprobe;
            b.intensity = p.Intensitaet;
            b.laut = p.Laut;
            b.leise = p.Leise;
            b.maxspeed = p.MaximaleBeschleunigung;
            b.minspeed = p.MinimaleBeschleunigung;
            b.minpausenspeed = p.MinimalePausenBeschleunigung;
            b.rueckpruefung = p.Rueckwaertspruefung;
            b.eigeneFps = p.EigeneFPS;
            b.fps = p.Fpswert;
            dynaudnorm = p.Dynaudnorm;


            solasuchberdiv = p.SolaBer;
            solablockdiv = p.SolaDiv;
            solasuchschrdiv = p.SolaSchritt;
            grundBeschleunigungViaFFMPEG=p.UseFFmpegToo;
        }
        public void setParamsToProperties()
        {
            TraceExclamation("paramstoprop");
            var p = Properties.Settings.Default;
            var b = beschleunigungsParameter;
            aktuell = false;


            p.SolaVerwenden = useSola;
            p.Ableitungsglaettung=b.ableitungsglaettung;
            p.MaximalerAbfall=b.maxableitung  ;
            p.AbleitungBerueck=b.useableitung;
            p.MinimalerAbfall = b.minableitung;
            debuglevel = 1;
                       
            p.FFmpeg = ffmpegPfad;
            p.FFprobe = ffprobePfad;
            p.Intensitaet = b.intensity;            
            p.Laut=b.laut ;
            p.Leise=b.leise;
            p.MaximaleBeschleunigung = b.maxspeed;
            p.MinimaleBeschleunigung = b.minspeed;
            p.MinimalePausenBeschleunigung = b.minpausenspeed;
            p.Rueckwaertspruefung=b.rueckpruefung ;
            p.Fpswert = b.fps;
            p.EigeneFPS = b.eigeneFps;
            p.Dynaudnorm = dynaudnorm;
            Console.WriteLine("Dynaudnorm: "+dynaudnorm);
            p.SolaBer=solasuchberdiv ;
            p.SolaDiv=solablockdiv;
            p.SolaSchritt=solasuchschrdiv ;
            p.UseFFmpegToo = grundBeschleunigungViaFFMPEG;
        }

        /// <summary>
        /// Setzt die Eingabedatei auf die angegebene Datei. Dies löscht alle temporären Dateien und schließt sämtliche offenen Dateien.
        /// </summary>
        /// <param name="dateiname"></param>
        public void setInputFileName(String dateiname,bool useLast=false)
        {
            locked = useLast;
            TraceExclamation("setinputfilename");
            try{

        
                setzeGesammtFortschritt((int)( 0));
                inputdateiname = dateiname;
                if (!locked)
                {
                    clean();
                    
                    generiereWavInputDatei();
                }
                
                setzeGesammtFortschritt((int)( 25));
                //input = new WavReader(File.Open(dateiname, FileMode.Open, FileAccess.Read, FileShare.Read),0);
            
                generiereLautstaerken();
                setzeGesammtFortschritt((int)( 50));
                refresh();
                setzeGesammtFortschritt((int)( 100));
             
            }
            catch (Exception e)
            {
                TraceExclamation(e);
                MessageBox.Show("Beim Festlegen der Eingabedatei ist ein Fehler aufgetreten: " + e);
            }
        }
        bool arbeitend = false;
        bool pending = false;
        public void refresh(){
           // TraceExclamation("refresh");
            lock (this)
            {
                if (arbeitend) { pending = true; aktuell = false; }
                if (inputdateiname != null && inputdateiname != "")
                {
                    arbeitend = true;
                    speedfaktoren(primitivespeedup);
                    if (!pending) { aktuell = true; }
                    pending = false;
                    arbeitend = false;
                }
            }
            
        }
        int nr=0;
        public void setOutputFileName(String dateiname)
        {
            TraceExclamation("setoutputfilename");
            try{
                if (output != null) { output = null; }
                outputdateiname = dateiname;
                tempwavdatei = createTempFileName("tmpwavout"+nr+".wav");
                nr++;
                registerTempFile(tempwavdatei);
               
                //output = new WavWriter(File.Open(tempwavdatei, FileMode.Create), 0, input.samplingrate);
                output = new WavWriter(new FileStream(tempwavdatei, FileMode.Create, FileAccess.Write, FileShare.None, 16777216), 0, input.samplingrate);

            }
            catch (Exception e)
            {
                TraceExclamation(e);
                MessageBox.Show("Beim Festlegen der Ausgabedatei ist ein Fehler aufgetreten: " + e);
            }
        }
        
        public void beschleunige()
        {
            TraceExclamation("beschleunige");
            setzeGesammtFortschritt((int)(0));
            if (!aktuell) { refresh(); }
            try
            {
                setzeGesammtFortschritt((int)( 25));
                
                    fastenViaSola();
               
                
                
                setzeGesammtFortschritt((int)( 50));
                createFastendVideo(inputdateiname, outputdateiname, tempwavdatei);
                setzeGesammtFortschritt((int)( 100));
            }
            catch (Exception e)
            {
                TraceExclamation(e);
                MessageBox.Show("Beim Beschleunigen ist ein Fehler aufgetreten: " + e);
            }
        }

        public void beschleunigeAudio()
        {
            TraceExclamation("beschleunige");
            setzeGesammtFortschritt((int)(0));
            if (!aktuell) { refresh(); }
            try
            {
                setzeGesammtFortschritt((int)(25));
                
                    fastenViaSola();
               

                setzeGesammtFortschritt((int)(70));
                //createFastendVideo(inputdateiname, outputdateiname, tempwavdatei);
                
                ffmpegCall("-y -v quiet -i " + FileFinder.toFileName(tempwavdatei, true) + " -ab 128k " + FileFinder.toFileName(outputdateiname, true));
                setzeGesammtFortschritt((int)(100));
            }
            catch (Exception e)
            {
                TraceExclamation(e);
                MessageBox.Show("Beim Beschleunigen ist ein Fehler aufgetreten: " + e);
            }
        }

        public void reinhoeren(int startzeit, int dauer)
        {
            TraceExclamation("beschleunige");
            setzeGesammtFortschritt((int)(0));
            if (!aktuell) { refresh(); }
            try
            {
                setzeGesammtFortschritt((int)(50));
                fastenViaSola(startzeit,dauer);
                


                
                //createFastendVideo(inputdateiname, outputdateiname, tempwavdatei);
               // ffmpegCall("-y -i " + FileFinder.toFileName(tempwavdatei, true) + " -ab 128k " + FileFinder.toFileName(outputdateiname + ".mp3", true));
                setzeGesammtFortschritt((int)(100));
                spieleAb(tempwavdatei);
            }
            catch (Exception e)
            {
                TraceExclamation(e);
                MessageBox.Show("Beim Beschleunigen ist ein Fehler aufgetreten: " + e);
            }
        }
        private void spieleAb(string tempwavout)
        {

            System.Diagnostics.Process p = new System.Diagnostics.Process();

            p.StartInfo.FileName = tempwavout;
            TraceExclamation("Spiele: " + tempwavout);
            p.Start();
            System.Threading.Thread.Sleep(1500);
            bool beende = false;
            while (!beende)
            {
                try
                {
                    FileStream t = File.Open(tempwavout, FileMode.Append, FileAccess.Write, FileShare.None);
                    t.Close();
                    beende = true;
                }
                catch (Exception e)
                {
                    TraceExclamation("Warte auf schließen des Programmes....");
                }
                System.Threading.Thread.Sleep(500);
            }


        }


        
        public void clean()
        {
            TraceExclamation("clean");
            if (input != null) { input.close(); }
            if (output != null) { output.close(true); }
            if (tempDateiNamen != null) { deleteTempFiles(); }
        }
        #endregion
        /*

#if SOLA_HD
        int solablockdiv = 20;
        int solasuchberdiv = 60;
        int solasuchschrdiv = 1600;
#elif SOLA_HD_GENAU
        int solablockdiv = 20;
        int solasuchberdiv = 160;
        int solasuchschrdiv = 1600;
#elif SOLA_SD_GENAU
        int solablockdiv = 60;
        int solasuchberdiv = 160;
        int solasuchschrdiv = 1600;
#elif SOLA_SEHR_GENAU

        int solablockdiv = 120;
        int solasuchberdiv = 320;
        int solasuchschrdiv = 2400;
#else
        int solablockdiv = 40;//sd
        int solasuchberdiv = 160; //sd
        int solasuchschrdiv = 800; //sd
#endif
        */
        int solaueberdiv = 100;
        void fastenViaSola()
        {
            TraceExclamation("sola start");
            InputVersusOutput = new List<IOSampleBeziehung>();
            SpeedUp.solaKontinuierlich(input, (int)(input.samplingrate / solablockdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchschrdiv), beschleunigungsFaktoren, (int)speedBlockSize, InputVersusOutput, solastatus,output);
            /*double[] werte = SpeedUp.solaKontinuierlich(input, (int)(input.samplingrate /solablockdiv), (int)(input.samplingrate /solasuchberdiv ),(int)( input.samplingrate /solasuchberdiv ),(int)( input.samplingrate / solasuchschrdiv), beschleunigungsFaktoren, (int)speedBlockSize ,InputVersusOutput,solastatus );
            TraceExclamation("sola write");

            setzeTeilFortschritt(.75);
            for (int i = 0; i < werte.Length; i++)
            {
                output.write(werte[i]);
                if ((i & 255) == 0)
                {
                    setzeTeilFortschritt(.75+.25*i/werte.Length );;
                }
            }*/
            output.close(true);
            setzeTeilFortschritt(1.0);
            TraceExclamation("Sola done");
        }
        void fastenViaSola(int startzeit,int dauer)
        {
            TraceExclamation("sola start");
            InputVersusOutput = new List<IOSampleBeziehung>();

            String tempaudiooutfilename = createTempFileName("schweigen");
            registerTempFile(tempaudiooutfilename);
            
            //WavWriter tempaudioout = new WavWriter(File.Open(tempaudiooutfilename, FileMode.Create, FileAccess.Write), 0, input.samplingrate);
            WavWriter tempaudioout = new WavWriter(new FileStream(tempaudiooutfilename, FileMode.Create, FileAccess.Write, FileShare.None, 16777216), 0, input.samplingrate);   


            List<double> beschleunigungstmp=new List<double>();
            for (int i=(int)(startzeit *samplingrate) ;i<(startzeit+dauer)*samplingrate;i++){

                if (i < input.Count) { tempaudioout.write(input[i]); }
                if (i % speedBlockSize == 0) { beschleunigungstmp.Add(beschleunigungsFaktoren[i /(int) speedBlockSize]); }
            }
            tempaudioout.close(true);
            //WavReader daten = new WavReader(File.Open(tempaudiooutfilename, FileMode.Open, FileAccess.Read, FileShare.Read), 0);
            WavReader daten = new WavReader(new FileStream(tempaudiooutfilename, FileMode.Open, FileAccess.Read, FileShare.Read, 16777216), 0);
            fastenViaSola(daten, beschleunigungstmp);
            daten.close();
            /*double[] werte = SpeedUp.solaKontinuierlich(daten, (int)(input.samplingrate / solablockdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchschrdiv), beschleunigungstmp, (int)speedBlockSize, InputVersusOutput, solastatus);
            
            TraceExclamation("sola write");

            setzeTeilFortschritt(.75);
            for (int i = 0; i < werte.Length; i++)
            {
                output.write(werte[i]);
                if ((i & 255) == 0)
                {
                    setzeTeilFortschritt(.75 + .25 * i / werte.Length); ;
                }
            }
            output.close(true);
            setzeTeilFortschritt(1.0);*/
            TraceExclamation("Sola done");
        }
        void fastenViaSola(WavReader daten,List<double> beschleunigung)
        {
            TraceExclamation("sola start");
            InputVersusOutput = new List<IOSampleBeziehung>();

            SpeedUp.solaKontinuierlich(daten, (int)(input.samplingrate / solablockdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchschrdiv), beschleunigung, (int)speedBlockSize, InputVersusOutput, solastatus, output);
            /*
              double[] werte = SpeedUp.solaKontinuierlich(daten, (int)(input.samplingrate / solablockdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchberdiv), (int)(input.samplingrate / solasuchschrdiv), beschleunigung, (int)speedBlockSize, InputVersusOutput, solastatus);
              TraceExclamation("sola write");
            
            setzeTeilFortschritt(.75);
            for (int i = 0; i < werte.Length; i++)
            {
                output.write(werte[i]);
                if ((i & 255) == 0)
                {
                    setzeTeilFortschritt(.75 + .25 * i / werte.Length); ;
                }
            }*/
            output.close(true);
            setzeTeilFortschritt(1.0);
            TraceExclamation("Sola done");
        }

        void solastatus(object sender, double wert)
        {
            setzeTeilFortschritt(wert * 1);
        }





        #region portierteMethoden


        #region beschleunigungsfunktion
        public delegate double speedfakt(double wert, double wertv, double wertn,speedupparams parameter, ref object scratch);//wert= lautstärke aktuell, wertv= lautstärke davor, wertn= lautstärke danach, parameter=eigene parameter. -1 = nicht vorhanden
        public class speedupparams
        {
       
            public double maxspeed = 50;
            public double minspeed = 2;
            public double leise = .02;
            public double laut = .4;
            public double intensity = .1;
            public double minpausenspeed = 1;
            public double maxableitung=.2;
            public double minableitung = .1;
            public double ableitungsglaettung = .5;
            public bool useableitung = true;
            public bool rueckpruefung = true;
            public bool eigeneFps = false;
            public int fps = 1;
        }
        double primitivespeedup(double wert, double wertv, double wertn,speedupparams parameter, ref object scratch)
        {
            
            if (scratch == null) { scratch=new double[2] ;}
            double[] tempmemory = (double[])scratch;
            double leise = parameter.leise;

            double rueckgabe = tempmemory[0] ;
            double last = tempmemory[0];
            double lastableitung = tempmemory[1];
            double aktableitung = lastableitung * parameter.ableitungsglaettung + (wertn - wertv) * (1 - parameter.ableitungsglaettung);

            if (wertv > parameter.laut || wert > parameter.laut || wertn > parameter.laut) { rueckgabe = 0; }
            else
            {

                if (wert < leise&&((!(aktableitung>-parameter.maxableitung)||!(aktableitung<-parameter.minableitung  )||!parameter.useableitung )))
                {

                    rueckgabe = ((last + 1) + parameter.intensity) * (last + 1);
                    rueckgabe = rueckgabe - 1;
                    if (rueckgabe + parameter.minspeed < parameter.minpausenspeed) { rueckgabe = parameter.minpausenspeed - parameter.minspeed; }
                }
                if (rueckgabe < 0) { rueckgabe = 0; }
                if (rueckgabe > parameter.maxspeed - parameter.minspeed) { rueckgabe = parameter.maxspeed - parameter.minspeed; }
            }

            tempmemory[0] = rueckgabe;
            tempmemory[1] = aktableitung;

            return rueckgabe + parameter.minspeed;
        }
#endregion
        class bearbeitungsfortschritt//nur damit man eine referenz zum locken hat
        {
            public int bearbeitet = 0;
        }
        public void generiereLautstaerken( )
        {
            TraceExclamation("genVolumes");
            if (!locked)
            {
                lautstaerken = new List<double>();
                double loudest = 0;
                // double minimum = 0, maximum = 0, wertv = 0, ableitung = 0;
                //long index = 0;

                int n;
                int informer = (int)((input.Count / (volumeBlockSize * 50)));
                double tmp;
                setzeTeilFortschritt((int)(0));
                Task[] threads = new Task[Environment.ProcessorCount];
                int wert = 0;

                int blockcount = (int)(input.Count / volumeBlockSize);
                double[] tempvolumes = new double[blockcount];

                bearbeitungsfortschritt bearbeitet = new bearbeitungsfortschritt();
                GC.Collect();
                GC.WaitForPendingFinalizers();

               // PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                DateTime start = DateTime.Now;
               // Console.WriteLine("Free MEmory: " + ramCounter.NextValue());
                long reqMem = (input.Count * (8 + input.header.blockalign) / 1048576 + 100);
                Console.WriteLine("Required Memory: " +reqMem);
                Console.WriteLine("Is64Bit process? " + Environment.Is64BitProcess);
                bool canReadIntoMemory = false;
                //                if ((Environment.Is64BitProcess ? (ramCounter.NextValue()) : Math.Min(ramCounter.NextValue(), 2000)) > input.Count * (8 + input.header.blockalign) / 1048576 + 100) // wenn genug speicher da ist, gilt es ihn zu nutzen 8 = für double 
                double[] wertespeicher=null;
                int inputcount = (int)input.Count;
                try
                {
                    wertespeicher = input.readValues(inputcount);
                    canReadIntoMemory = true;
                }catch{
                    canReadIntoMemory = false;
                }
                if (canReadIntoMemory) // wenn genug speicher da ist, gilt es ihn zu nutzen 8 = für double 
                {
                    Console.WriteLine("par vol");
                    
                    
                    input.seekSample(0);
                    
                    System.Diagnostics.Trace.WriteLine("gelesen");
                    //wertespeicher = new double[inputcount];

                    Parallel.For(0, blockcount, nr =>
                    {
                        double minimum = 0, maximum = 0, ableitung = 0, wertv;
                        double tmps;
                        int volversatz = nr * volumeBlockSize;


                        
                        double schnitt = 0;
                        int samplezahl = 0;
                        for (int i = 0; i < volumeBlockSize; i++)
                        {
                            if (i + volversatz < inputcount)
                            {
                                samplezahl++;
                                schnitt += wertespeicher[i + volversatz];
                            }
                            
                        }
                        schnitt /= samplezahl;
                        wertv = wertespeicher[volversatz]-schnitt;
                            for (int i = 0; i < volumeBlockSize; i++)
                            {
                                if (i + volversatz < inputcount)
                                {
                                    tmps = wertespeicher[i + volversatz]-schnitt;
                                    if (tmps < minimum) { minimum = tmps; }
                                    if (tmps > maximum) { maximum = tmps; }
                                    if (Math.Abs(tmps - wertv) > ableitung) { ableitung = Math.Abs(tmps - wertv); }

                                }


                            }

                        lock (bearbeitet)
                        {
                            bearbeitet.bearbeitet++;
                            if (bearbeitet.bearbeitet % informer == 0)
                            {
                                wert++;
                                if (wert <= 100) { setzeTeilFortschritt((int)(wert)); }
                            }
                        }
                        tempvolumes[nr] = ((maximum - minimum + ableitung) / 4);//:4  da maximum-minimum und ableitung ein maximum von jeweils 2 haben
                    });
                    Console.WriteLine("parallellisiert");
                }
                else
                {
                    Console.WriteLine("seq vol");
                    //int inputcount = (int)input.Count;

                    for (int nr = 0; nr < blockcount; nr++)
                    {
                        double minimum = 0, maximum = 0, ableitung = 0, wertv;

                        int volversatz = nr * volumeBlockSize;


                        double schnitt = 0;
                        int samplezahl = 0;
                        for (int i = 0; i < volumeBlockSize; i++)
                        {
                            if (i + volversatz < inputcount)
                            {
                                samplezahl++;
                                schnitt += input[i + volversatz];
                            }

                        }
                        schnitt /= samplezahl;
                        wertv = input[volversatz] - schnitt;
//                        wertv = input[volversatz];
                        for (int i = 0; i < volumeBlockSize; i++)
                        {
                            if (i + volversatz < inputcount)
                            {
                                tmp = input[i + volversatz];
                                if (tmp < minimum) { minimum = tmp; }
                                if (tmp > maximum) { maximum = tmp; }
                                if (Math.Abs(tmp - wertv) > ableitung) { ableitung = Math.Abs(tmp - wertv); }

                            }


                        }

                        bearbeitet.bearbeitet++;
                        if (bearbeitet.bearbeitet % informer == 0)
                        {
                            wert++;
                            if (wert <= 100) { setzeTeilFortschritt((int)(wert)); }
                        }

                        tempvolumes[nr] = ((maximum - minimum + ableitung) / 4);//:4  da maximum-minimum und ableitung ein maximum von jeweils 2 haben
                    }

                }
                Console.WriteLine("Zeit: " + DateTime.Now.Subtract(start).TotalMilliseconds / 1000);
                for (int i = 0; i < tempvolumes.Length; i++) { lautstaerken.Add(tempvolumes[i]); }

                loudest = percentalMaximum(.9, lautstaerken);
                for (n = 0; n < lautstaerken.Count; n++)
                {
                    lautstaerken[n] = Math.Min(lautstaerken[n] / loudest, 1);

                    if (n % (lautstaerken.Count / 50) == 0)
                    {

                        wert++;
                        if (wert <= 100) { setzeTeilFortschritt((int)(wert)); }


                    }
                }
                setzeTeilFortschritt((int)(100));


                if (lautstaerkeVeraendert != null) { lautstaerkeVeraendert.Invoke(this, lautstaerken); }
            }
            else
            {
                TraceExclamation("Locked.");
            }
        }


        public void speedfaktoren( speedfakt funktion)//zusammenfassung== wie viele blöcke als ein ganzes gesehen werden sollen
        {
            TraceExclamation("speedfakt");
            if (!locked)
            {
                beschleunigungsFaktoren = new List<double>();
                List<double> tmp = new List<double>();
                double aktwert = 0, aktcount = 0;
                double altMinSpeed = beschleunigungsParameter.minspeed;
                double altMaxSpeed = beschleunigungsParameter.maxspeed;
                double altMinPausenSpeed = beschleunigungsParameter.minpausenspeed;
                if (grundBeschleunigungViaFFMPEG)
                {
                    beschleunigungsParameter.maxspeed /= altMinSpeed;
                    beschleunigungsParameter.minpausenspeed /= altMinSpeed;
                    beschleunigungsParameter.minspeed = 1;
                }


                for (int i = 0; i < lautstaerken.Count; i += blockzusammenfassung)
                {
                    aktwert = 0; aktcount = 0;
                    for (int n = 0; n < blockzusammenfassung; n++)
                    {
                        if (i + n < lautstaerken.Count)
                        {
                            aktwert += lautstaerken[i + n];
                            aktcount++;
                        }
                    }
                    aktwert /= aktcount;
                    //   TraceExclamation(aktwert);
                    tmp.Add(aktwert);

                    setzeTeilFortschritt((int)(50 * i / lautstaerken.Count));

                }



                //TraceExclamation("--");
                double wertv = -1;
                double wert = tmp[0];
                double wertn = tmp[1];
                object speicher = null;
                for (int i = 0; i < tmp.Count; i++)
                {

                    if (i == tmp.Count - 1)
                    {
                        wertn = -1;
                    }
                    else
                    {
                        wertn = tmp[i + 1];
                    }
                    beschleunigungsFaktoren.Add(funktion.Invoke(wert, wertv, wertn, beschleunigungsParameter, ref  speicher));

                    wertv = wert;
                    wert = wertn;
                    setzeTeilFortschritt((int)(50 + 25 * i / tmp.Count));

                }
                wertv = -1;
                wert = tmp[tmp.Count - 1];
                wertn = tmp[tmp.Count - 2];
                double funktionswert = 0;
                speicher = null;
                if (beschleunigungsParameter.rueckpruefung)
                {
                    for (int i = tmp.Count - 1; i >= 0; i--)
                    {

                        if (i == 0)
                        {
                            wertn = -1;
                        }
                        else
                        {
                            wertn = tmp[i - 1];
                        }
                        funktionswert = funktion.Invoke(wert, wertv, wertn, beschleunigungsParameter, ref  speicher);
                        beschleunigungsFaktoren[i] = Math.Min(beschleunigungsFaktoren[i], funktionswert);

                        wertv = wert;
                        wert = wertn;
                        setzeTeilFortschritt((int)(75 + 25 * (tmp.Count - 1 - i) / tmp.Count));

                    }
                }





                setzeTeilFortschritt((int)(100));

                TraceExclamation(beschleunigungsFaktoren.Count + " " + lautstaerken.Count);
                if (beschleunigungVeraendert != null)
                {
                    double spielzeit = 0;
                    for (int i = 0; i < beschleunigungsFaktoren.Count; i++)
                    {
                        spielzeit += speedBlockSize / beschleunigungsFaktoren[i];
                    }
                    spielzeit /= samplingrate;
                    beschleunigungVeraendert.Invoke(this, beschleunigungsFaktoren, spielzeit, speedBlockSize, samplingrate);

                }
                //return beschleunigungsFaktoren;
                if (grundBeschleunigungViaFFMPEG)
                {
                    beschleunigungsParameter.maxspeed = altMaxSpeed;
                    beschleunigungsParameter.minpausenspeed = altMinPausenSpeed;
                    beschleunigungsParameter.minspeed = altMinSpeed;
                }
                System.Diagnostics.Trace.WriteLine("Fertig");
            }
            else
            {
                TraceExclamation("locked");
            }
        }


     
        public void createFastendVideo(String inputvideo, String outputvideo, String audiodatei)
        {
            DateTime startzeit = DateTime.Now;
            setzeTeilFortschritt((int)(0));
           


            System.Diagnostics.Process writeprocess, readprocess,infoprocess;
            infoprocess = new System.Diagnostics.Process();
            writeprocess = new System.Diagnostics.Process();
            readprocess = new System.Diagnostics.Process();

            readprocess.StartInfo.UseShellExecute = false;
            writeprocess.StartInfo.UseShellExecute = false;
            infoprocess.StartInfo.UseShellExecute = false;

            readprocess.StartInfo.RedirectStandardInput = true;
            readprocess.StartInfo.RedirectStandardOutput = true;
            readprocess.StartInfo.RedirectStandardError = true;
            readprocess.StartInfo.CreateNoWindow = true;
            
            writeprocess.StartInfo.RedirectStandardInput = true;
            writeprocess.StartInfo.RedirectStandardOutput = true;
            writeprocess.StartInfo.RedirectStandardError = true;
            writeprocess.StartInfo.CreateNoWindow =true;

            infoprocess.StartInfo.CreateNoWindow = true;
            infoprocess.StartInfo.RedirectStandardError = true;
            infoprocess.StartInfo.RedirectStandardOutput = true;

            writeprocess.StartInfo.FileName = ffmpegPfad;
            readprocess.StartInfo.FileName = ffmpegPfad;
            infoprocess.StartInfo.FileName = ffprobePfad;

            infoprocess.StartInfo.Arguments = "-v error -select_streams v:0 -show_entries stream=width,height,avg_frame_rate,bit_rate -of default=noprint_wrappers=1 " + FileFinder.toFileName(inputvideo, true);
            infoprocess.Start();
            infoprocess.WaitForExit();
            String daten = infoprocess.StandardOutput.ReadToEnd();
            String tmpstring = "";
            int width = 0, height = 0;
            double framerate = 1;
            String framerateString = "1/1";
            String bitrate = "64k";
            for (int i = 0; i < daten.Split('\n').Count (); i++)
            {
                tmpstring = daten.Split('\n')[i].Trim ();
                if (tmpstring.StartsWith("height="))
                {
                    height = Int32.Parse( tmpstring.Substring("height=".Count()));
                }
                else if (tmpstring.StartsWith("width="))
                {
                    width = Int32.Parse(tmpstring.Substring("width=".Count()));
                }else if (tmpstring.StartsWith("avg_frame_rate=")){
                    String t=tmpstring.Substring("avg_frame_rate=".Count ());
                    String t1=t.Split('/')[0];
                    String t2=t.Split('/')[1];
                    System.Diagnostics.Trace.WriteLine(t + " " + t1 + " " + t2);
                    framerateString = t;
                    framerate=double.Parse(t1)/double.Parse(t2);
                    System.Diagnostics.Trace.WriteLine ("Framerate: "+framerate+" t1: "+t1+" t2: "+t2+" ges: "+tmpstring+" splitted: "+t);
                }
                else if (tmpstring.StartsWith("bit_rate="))
                {
                    bitrate = tmpstring.Substring("bit_rate=".Count());
                    System.Diagnostics.Trace.WriteLine("Bitrate: " + bitrate);
                }
            }
            if (width<=0 || height <= 0) { throw new Exception("Kein Video gefunden."); }
            if (beschleunigungsParameter.eigeneFps)
            {
                framerate = beschleunigungsParameter.fps;
                framerateString = framerate.ToString ().Trim() + "/1";
            }


            


            long samples = 0;
            for (int i = 0; i < InputVersusOutput.Count; i++)
            {
                samples += InputVersusOutput[i].inputsamplezahl;
            }

            double gesammtzeit = samples / (double)samplingrate;
            int gesammtframes = (int)(Math.Floor((double)samples * framerate / samplingrate));

            double k = samplingrate / (double)framerate;// alle k samples muss 1 frame eingesetzt werden

            List<int> indizes = new List<int>();

            long realsamples = 0;
            double realzeit = 0;
            double lastzeit = 0;
            TraceExclamation(gesammtframes + " " + realzeit + " " + gesammtzeit);

            long aktoutputsample = 0;
            double aktinputsample = 0;
            //experimentell anders berechnet
            /*
            for (int i = 0; i < InputVersusOutput.Count; i++)
            {
                double faktor = InputVersusOutput[i].inputsamplezahl;
                if (InputVersusOutput[i].outputsamplezahl == 0)
                {
                }
                else
                {
                    faktor /= (double)InputVersusOutput[i].outputsamplezahl;
                    aktinputsample = realsamples;
                    for (int n = 0; n < InputVersusOutput[i].outputsamplezahl; n++)
                    {
                        if (aktoutputsample / (double)samplingrate - lastzeit >= 1.0 / framerate)
                        {
                            lastzeit = aktoutputsample / (double)samplingrate;
                            indizes.Add((int)Math.Floor(framerate * aktinputsample / samplingrate));
                        }
                        aktinputsample += faktor;
                        aktoutputsample++;
                    }
                }
                realsamples += InputVersusOutput[i].inputsamplezahl;
                setzeTeilFortschritt((int)(25 * i / InputVersusOutput.Count));
            }*/
            double currentTime = 0;
            double currentError = 0;
            for (int i = 0; i < samples; i++)
            {
                double tmpval=  beschleunigungsFaktoren[(int)(i / speedBlockSize)] ;
                currentTime = i / samplingrate;
                currentError += 1/tmpval / samplingrate;
                if (currentError > 1.0 / framerate)
                {
                    currentError -= 1.0 / framerate;
                    indizes.Add((int)Math.Floor(framerate * currentTime));

                }
                if (i % 44100 == 0)
                {
                    setzeTeilFortschritt((int)(i / (double)samples*25));
                }
            }



            /*String tempvideodatei = createTempFileName("tempvideo.avi");
            registerTempFile(tempvideodatei);*/

            readprocess.StartInfo.Arguments = "-v quiet -i "+FileFinder.toFileName(inputvideo,true) + "" +" -c:v rawvideo -pix_fmt yuv420p -f image2pipe -r "+framerateString +" pipe:1";
            //writeprocess.StartInfo.Arguments = "-y -v quiet -f rawvideo -vcodec rawvideo -s "+width.ToString().Trim()+"x"+height.ToString().Trim()+" -pix_fmt yuv420p -r 1 -i - -an " + FileFinder.toFileName(tempvideodatei , true);// "-c:v rawvideo -pix_fmt rgb24 -s 1024x768 -i pipe:0  -an " + @"D:\root\downloads\ffmpegoutput.mp4";
            writeprocess.StartInfo.Arguments = "-y -v quiet -i " + FileFinder.toFileName(audiodatei, true) + " -f rawvideo -vcodec rawvideo -s " + width.ToString().Trim() + "x" + height.ToString().Trim() + " -pix_fmt yuv420p -r " + framerateString + " -i - -acodec mp3 -ab 128k -vb "+bitrate+" -crf 0 -q:v 0 "+FileFinder.toFileName(outputvideo, true);// "-c:v rawvideo -pix_fmt rgb24 -s 1024x768 -i pipe:0  -an " + @"D:\root\downloads\ffmpegoutput.mp4";
            //-i \"" + audiodatei + "\"  -acodec mp3 -ab 128k -map 0:0 -map 1:0
            System.Diagnostics.Trace.WriteLine("rparams: " + readprocess.StartInfo.Arguments);
            System.Diagnostics.Trace.WriteLine("wparams: " + writeprocess.StartInfo.Arguments);
            readprocess.Start();
            writeprocess.Start();
            byte[] inputbuffer=new byte[1<<24];
            byte[] outputbuffer = new byte[inputbuffer.Length];
            int bytezahl;
            long aktbyte = 0;
            int aktframe = 0;
            int indexindex = 0;
            int zuschreiben = 0;
            int bildByteCount = width * height * 3 / 2;
            int aktbildbyte = 0;
            BufferedStream inputstream = new BufferedStream(readprocess.StandardOutput.BaseStream,outputbuffer.Length );
            BinaryReader inputreader = new BinaryReader(inputstream);
       
            int skipcount = 0;
            int verbleibend = 0;
            int startindex = 0;
            int moduler = indizes.Count / 100;
            Task<string> ereadtask, ewritetask;
            ereadtask = readprocess.StandardError.ReadToEndAsync();
            ewritetask = writeprocess.StandardError.ReadToEndAsync();
            int got = 0;
            int maxread = 0;
            System.Diagnostics.Trace.WriteLine("Indizes: " + indizes.Count());
            while (!readprocess.HasExited||!readprocess.StandardOutput.EndOfStream)
            {
               // if (indexindex >= indizes.Count()) { break; }
               if (ereadtask.IsCanceled||ereadtask.IsFaulted||ereadtask.IsCompleted){ ereadtask=readprocess.StandardError.ReadToEndAsync();}
                
                do
                {
                    //System.Diagnostics.Trace.WriteLine("innerLoop " );
                   // System.Diagnostics.Trace.WriteLine("reading");
                    
                    bytezahl =inputreader.Read (inputbuffer, 0, inputbuffer.Count());
                    got = bytezahl;
                    if (got > maxread) { maxread = got; }
                    if (indexindex >= indizes.Count()) { skipcount += bytezahl;
                        try {
                            readprocess.Kill();
                        }
                        catch  { }
                        break; }
                    if (bytezahl > 0)
                    {
                        verbleibend = bytezahl;
                        startindex = 0;
                        while (verbleibend > 0)
                        {

                            if (indexindex >= indizes.Count) { skipcount += verbleibend;
                                int counter=0;
                                while (!readprocess.HasExited && counter < 10)
                                {
                                    try
                                    {
                                        readprocess.Close();
                                    }
                                    catch (Exception e)
                                    {
                                        System.Diagnostics.Trace.WriteLine(e);
                                    }
                                    System.Threading.Thread.Sleep(500);
                                }
                                
                            break; }
                            zuschreiben = 0;
                            int schreibbar = Math.Min(verbleibend, bildByteCount - aktbildbyte);

                            if (aktframe == indizes[indexindex])
                            {
                                zuschreiben = schreibbar;
                            }    
                            if (schreibbar == bildByteCount - aktbildbyte)
                            {
                                aktbildbyte = 0;
                                if (aktframe == indizes[indexindex]) { 
                                    indexindex++;
                                    if (indexindex % moduler == 0) { setzeTeilFortschritt((int)(25 + indexindex * 75 / indizes.Count)); }
                                }
                                aktframe++;
                            }
                            else
                            {
                                aktbildbyte += schreibbar;
                            }
                                      
                            if (zuschreiben > 0)
                            {
                                if (ewritetask.IsCanceled || ewritetask.IsFaulted || ewritetask.IsCompleted) { ewritetask = writeprocess.StandardError.ReadToEndAsync(); }
                                writeprocess.StandardInput.BaseStream.Write(inputbuffer, startindex, zuschreiben);
                            }
                      
                            verbleibend -= schreibbar;
                            startindex += schreibbar;
                        }
                    }
                  
                    
                    
                } while (bytezahl > 0);
                System.Diagnostics.Trace.WriteLine("Warte auf wunder "+got+" "+skipcount );
           
                System.Threading.Thread.Sleep(100);
            }
            System.Diagnostics.Trace.WriteLine("Maxread: " + maxread);
            System.Diagnostics.Trace.WriteLine ("Schließe stream");
            writeprocess.StandardInput.BaseStream.Close();
            System.Diagnostics.Trace.WriteLine("Fertig gelesen");
            writeprocess.WaitForExit();
            System.Diagnostics.Trace.WriteLine("Fertig geschrieben");
            System.Diagnostics.Trace.WriteLine("Zeitaufwand: " + DateTime.Now.Subtract(startzeit).TotalSeconds + "s");
           // extendedWaitForExit(ffmpegCall(" -y -i \"" + tempvideodatei + "\" -i \"" + audiodatei + "\"  -acodec mp3 -ab 128k -map 0:0 -map 1:0 \"" + outputvideo + "\""));
            System.Diagnostics.Trace.WriteLine("Byteskip: " + skipcount + " in frames " + skipcount/bildByteCount);
            setzeTeilFortschritt(90);
            if (grundBeschleunigungViaFFMPEG){

            
                double grundBeschleunigung = this.beschleunigungsParameter.minspeed;
                String filterA = "atempo=1";
                String filterV = "setpts="+(1/grundBeschleunigung)+"*PTS";
            
                while (grundBeschleunigung >1)
                {
                    if (grundBeschleunigung > 2)
                    {
                        filterA += ",atempo=2.0";
                        grundBeschleunigung/=2;
                    }else{
                        filterA+=",atempo="+grundBeschleunigung ;
                        grundBeschleunigung=1;
                    }
                }
            }
            setzeTeilFortschritt(100);
        }



        public void createFastendVideoOld(String inputvideo, String outputvideo,   String audiodatei)
        {
            TraceExclamation("fastendvid");
            setzeTeilFortschritt((int)(0));
            int framerate = 1;
            int codesekunden = 600;
            int aktzeit = 0;

            long samples = 0;
            for (int i = 0; i < InputVersusOutput.Count; i++)
            {
                samples += InputVersusOutput[i].inputsamplezahl;
            }
            
            double gesammtzeit = samples / (double)samplingrate;
            int gesammtframes = (int)(Math.Floor((double)samples * framerate / samplingrate));
            double frameindex = 0;
            double k = samplingrate / (double)framerate;// alle k samples muss 1 frame eingesetzt werden
            double aktk = 0;
            List<int> indizes = new List<int>();
            TraceExclamation("");
            long realsamples = 0;
            double realzeit = 0;
            double lastzeit = 0;
            TraceExclamation(gesammtframes + " " + realzeit + " " + gesammtzeit);
            
            long aktoutputsample = 0;
            double aktinputsample = 0;
            for (int i = 0; i < InputVersusOutput.Count; i++)
            {
                double faktor = InputVersusOutput[i].inputsamplezahl;
                if (InputVersusOutput[i].outputsamplezahl == 0)
                {
                }
                else
                {
                    faktor /= (double)InputVersusOutput[i].outputsamplezahl;
                    aktinputsample = realsamples;
                    for (int n = 0; n < InputVersusOutput[i].outputsamplezahl; n++)
                    {
                        if (aktoutputsample / (double)samplingrate - lastzeit >= 1.0 / framerate)
                        {
                            lastzeit = aktoutputsample / (double)samplingrate;
                            indizes.Add((int)Math.Floor(framerate * aktinputsample / samplingrate));
                        }
                        aktinputsample += faktor;
                        aktoutputsample++;
                    }
                }


                realsamples += InputVersusOutput[i].inputsamplezahl;
                setzeTeilFortschritt((int)(50 * i / InputVersusOutput.Count)); 
            }
            TraceExclamation("");
            int versatz = 1;
            int GenerierteBildzahl = framerate * codesekunden;
            int subtraktor = 0;
            int realindex = 0;
            int transindex = 0;
            int maxrealindex = framerate * codesekunden;
            bool neuerzeugen = false;
            bool first = true;
            String bilddateipraefix = createTempFileName("skipbild-");
            String mergedateipraefix = createTempFileName( "mergebild-");
            String dateiendung = ".jpg";
            String tempvideo1 = createTempFileName ("tmpsmall.mp4");
            String tempvideo2 = createTempFileName("tmpmedium.mp4");
            String tempvideo3 = createTempFileName("tmplarge.mp4");
            String concatfile =createTempFileName("list.txt");
            registerTempFile (tempvideo1);
            registerTempFile (tempvideo2);
            registerTempFile (tempvideo3);
            registerTempFile (concatfile);
            for (int i = 0; i < GenerierteBildzahl; i++)
            {
                tempDateiNamen.Add(bilddateipraefix + (i + versatz) + dateiendung);
            }
            for (int i = 0; i < maxrealindex; i++)
            {
                tempDateiNamen.Add(mergedateipraefix + (i + versatz) + dateiendung);
            }

            FileStream tmpstream = new FileStream(concatfile, FileMode.Create, FileAccess.Write, FileShare.None, 16777216); // File.OpenWrite(concatfile);
            StreamWriter tw = new StreamWriter(tmpstream);

            tw.WriteLine("file '" + tempvideo2 + "'");
            tw.WriteLine("file '" + tempvideo1 + "'");
            tw.Close();


            for (int i = 0; i < indizes.Count; i++)
            {
                transindex = indizes[i] - subtraktor;
                neuerzeugen = false;
                while (transindex >= GenerierteBildzahl)
                {




                    aktzeit += codesekunden;
                    subtraktor += GenerierteBildzahl;
                    transindex -= GenerierteBildzahl;


                    neuerzeugen = true;
                }
                if (first || neuerzeugen)
                {
                    first = false;
            
                    for (int n = 0; n < GenerierteBildzahl; n++)
                    {
                        String dateiname = bilddateipraefix + (n + versatz) + dateiendung;
                        if (File.Exists(dateiname))
                        {
                            File.Delete(dateiname);
                        }
                    }
                    TraceExclamation("FFmpeg....");
                    extendedWaitForExit(ffmpegCall(" -y -r " + framerate + " -i \"" + inputvideo + "\" -r " + framerate + " -ss " + getTimeCode(aktzeit) + " -t " + codesekunden + " \"" + bilddateipraefix + "%d" + dateiendung + "\""));
                    
                    TraceExclamation("Returned.");
                }
                if (File.Exists(mergedateipraefix + (realindex + versatz) + dateiendung)) { File.Delete(mergedateipraefix + (realindex + versatz) + dateiendung); }
                try
                {
                    File.Move(bilddateipraefix + (transindex + versatz) + dateiendung, mergedateipraefix + (realindex + versatz) + dateiendung);
                    
                    realindex++;
                }
                catch (Exception e)
                {
                    TraceExclamation("------------------------------------------");
                    TraceExclamation("Eine Datei konnte nicht verschoben werden. : " + bilddateipraefix + (transindex + versatz) + dateiendung + " -> " + mergedateipraefix + (realindex + versatz) + dateiendung);
                    TraceExclamation(e);
                }
                if (realindex >= maxrealindex)
                {
                    realindex = 0;
                    
                    extendedWaitForExit(ffmpegCall(" -y -framerate " + framerate + " -i \"" + mergedateipraefix + "%d" + dateiendung + "\" \"" + tempvideo1 + "\""), true);
                    if (File.Exists(tempvideo2))
                    {
                        extendedWaitForExit(ffmpegCall(" -y -i \"" + tempvideo2 + "\" -i \"" + tempvideo1 + "\" -filter_complex \"[0:0] [1:0] concat=n=2:v=1:a=0 [v] \" -map [v] \"" + tempvideo3 + "\""));
                    
                        File.Delete(tempvideo2);
                        File.Move(tempvideo3, tempvideo2);
                    }
                    else
                    {
                        File.Move(tempvideo1, tempvideo2);
                    }
                    

                    for (int n = 0; n < maxrealindex; n++)
                    {
                        String dateiname = mergedateipraefix + (n + versatz) + dateiendung;
                        if (File.Exists(dateiname))
                        {
                            File.Delete(dateiname);
                        }
                    }
                }
                setzeTeilFortschritt((int)(50 + 50 * i / indizes.Count)); 
            }

            if (realindex != 0)
            {
                extendedWaitForExit(ffmpegCall(" -y -framerate " + framerate + " -i \"" + mergedateipraefix + "%d" + dateiendung + "\" \"" + tempvideo1 + "\""), true);
                if (File.Exists(tempvideo2))
                {
                  
                    extendedWaitForExit(ffmpegCall(" -y -i \"" + tempvideo2 + "\" -i \"" + tempvideo1 + "\" -filter_complex \"[0:0] [1:0] concat=n=2:v=1:a=0 [v] \" -map [v] \"" + tempvideo3 + "\""));
                    File.Delete(tempvideo2);
                    File.Move(tempvideo3, tempvideo2);
                }
                else
                {
                    File.Move(tempvideo1, tempvideo2);
                }
            }
           // extendedWaitForExit(ffmpegCall(" -y -i \"" + tempvideo2 + "\" -i \"" + audiodatei + "\" -vcodec copy -acodec mp3 -ab 128k -map 0:0 -map 1:0 \"" + outputvideo + "\""));
            extendedWaitForExit(ffmpegCall(" -y -i \"" + tempvideo2 + "\" -i \"" + audiodatei + "\"  -acodec mp3 -ab 128k -map 0:0 -map 1:0 \"" + outputvideo + "\""));
            setzeTeilFortschritt((int)(100)); 

        }


        #endregion
        public static double percentalMaximum(double prozentsatz, List<double> werte)
        {
            System.Diagnostics.Trace.WriteLine("Permax");
            List<double> klon = new List<double>(werte.ToArray());//arbeitskopie
            klon.Sort();
            return klon[(int)(klon.Count() * prozentsatz)];
            /*while (klon.Count > werte.Count * prozentsatz)
            {
                klon.Remove(klon.Max());
            }
            return klon.Max();*/
        }
    }

}
