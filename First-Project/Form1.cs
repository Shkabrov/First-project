using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using ImageLib;

namespace First_Project
{
    public partial class Form1 : Form
    {
        MyImage myImg;

        string imageJson;
        string fileName = "";

        public Form1()
        {
            InitializeComponent();
        }

        private void loadImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string _fileName = "";
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open image";
                dlg.Filter = "Image files (*.jpg, *.png) | *.jpg; *.png";
                dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (dlg.ShowDialog() == DialogResult.OK)
                    _fileName = dlg.FileName;
            }

            if (_fileName != "")
            {
                fileName = "";

                if (myImg != null)
                {
                    myImg.ReleaseObjects();
                    myImg = null;
                }

                myImg = new MyImage(_fileName);

                myImg.LoadControls(this, pictureBox, dataGridView);
            }
        }

        private void saveOnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myImg != null && fileName == "")
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save a file";
                    dlg.Filter = "Json file|*.json";
                    dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    if (dlg.ShowDialog() == DialogResult.OK)
                        fileName = dlg.FileName;
                }
            }

            if (myImg != null && fileName != "")
            {
                imageJson = JsonConvert.SerializeObject(myImg, Formatting.Indented);//Serialization my class to json

                File.WriteAllText(fileName, imageJson);//Save json in file
            }
        }

        private void openJsonfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open json-file";
                dlg.Filter = "Json file|*.json";
                dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                if (dlg.ShowDialog() == DialogResult.OK)
                    fileName = dlg.FileName;
            }

            if (fileName != "")
            {
                if (myImg != null)
                {
                    myImg.ReleaseObjects();
                    myImg = null;
                }

                try
                {
                    imageJson = File.ReadAllText(fileName);//Read json in file

                    myImg = JsonConvert.DeserializeObject<MyImage>(imageJson);//Deserialize json to hex

                    pictureBox.Image = myImg.Image;

                    myImg.LoadControls(this, pictureBox, dataGridView);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error, invalid file.");
                }
            }
        }

        private void saveInAnotherJsonfileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myImg != null)
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Title = "Save a file";
                    dlg.Filter = "Json file|*.json";
                    dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    if (dlg.ShowDialog() == DialogResult.OK)
                        fileName = dlg.FileName;
                }
            }

            if (myImg != null && fileName != "")
            {
                imageJson = JsonConvert.SerializeObject(myImg, Formatting.Indented);//Serialization my class to json

                File.WriteAllText(fileName, imageJson);//Save json in file
            }

        }
    }
}
