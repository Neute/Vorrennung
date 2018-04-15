using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vorrennung
{
    public partial class FileFinder : UserControl
    {
        public String Caption { get { return label1.Text; } set { label1.Text = value; } }
        
        //public override String Text { get { return textBox1.Text; } set { textBox1.Text = value; } }
        //public String Filter { get { return openFileDialog1.Filter; } set { openFileDialog1.Filter = value; } }
        /*public Label _Label { get { return label1; }  }
        public TextBox _TextBox { get { return textBox1; } }
        public OpenFileDialog _OpenFileDialog { get { return openFileDialog1; } }
        public String text { get { return textBox1.Text; } set { textBox1.Text  = value; } }*/
        public FileFinder()
        {
            InitializeComponent();
        }

        private void FileFinder_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBox1.Text = openFileDialog1.FileName;
        }
        public String getFileName(bool hochkommas)
        {
            return toFileName(textBox1.Text,hochkommas);
        }
        public static String toFileName(String t,bool hochkommas)
        {
            t = t.Replace("\"", "");
            t = t.Trim();
            if (hochkommas) { t = "\"" + t + "\""; }
            return t;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
