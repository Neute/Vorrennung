using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
namespace Vorrennung
{
    public partial class MultipleFileChooser : Form
    {
        class speedParams
        {
           public int quality = 800;
            public double speed = 1;
            public double wortlaenge = 0;
            public string filename;
        }
        
        public MultipleFileChooser()
        {
            InitializeComponent();
            listView1.LargeImageList = new ImageList();
            listView1.SmallImageList = listView1.LargeImageList;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var k = new ListViewItem("hallo");
            
            listView1.Items.Add(k);
        }

        private void MultipleFileChooser_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) { 
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void MultipleFileChooser_Load(object sender, EventArgs e)
        {

        }

        private void MultipleFileChooser_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Trace.WriteLine("files found");
            foreach (var file in files)
            {
                Trace.WriteLine(file);
                addFile(file);
            }
        }
        void addFile(string f)
        {
            try {
                if (File.Exists(f)) {
                    if (isInFilter(f)) {

                        var k = new ListViewItem(Path.GetFileNameWithoutExtension(f));
                        var pars = new speedParams();
                        pars.filename = f;
                        k.Tag = pars;
                        listView1.LargeImageList.Images.Add(Icon.ExtractAssociatedIcon(f).ToBitmap());
                        k.ImageIndex = listView1.LargeImageList.Images.Count - 1;
                        listView1.Items.Add(k);

                    }
                }
            }
            catch
            {

            }

        }
        bool isInFilter(string f)
        {
            var exts = textBox1.Text.Trim().ToLowerInvariant().Split(',');
            var tmp = f.Trim().ToLowerInvariant(); 
            foreach (var s in exts)
            {
                if (tmp.EndsWith(s))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
