using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace THFDownloader
{
    public partial class frmSettings : Form
    {
        public frmSettings()
        {
            InitializeComponent();
        }

        private void frmSettings_Load(object sender, EventArgs e)
        {
            if (File.Exists("THFDownloader.cfg"))
            {
                string line;
                int configCounter = 1;

                using (StreamReader sr = new StreamReader("THFDownloader.cfg", true))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Capture config entries and split out values.
                        string[] Split = line.Split(new Char[] { '=' });
                        if (configCounter == 1) { txtLocation.Text = Convert.ToString(Split[1]); }
                        configCounter++;
                    }
                }
            }
        }

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (File.Exists("THFDownloader.cfg"))
            { //Only thing the .cfg holds is the location and Client type
                var lines = File.ReadAllLines("THFDownloader.cfg");
                lines[0] = "EQLocation=" + txtLocation.Text;
                lines[1] = "EQType=" + lblEQType.Tag;
                File.WriteAllLines("THFDownloader.cfg", lines);
                this.Close();
            }
            else
            {
                using (StreamWriter writer = new StreamWriter("THFDownloader.cfg", true))
                {
                    writer.WriteLine("EQLocation=" + txtLocation.Text);
                    writer.WriteLine("EQType=" + Convert.ToString(lblEQType.Tag));
                }
                using (StreamWriter writer = new StreamWriter("THFDownloaderDates.cfg", true))
                {

                }
                this.Close();
            }
        }

        private void btnLocation_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtLocation.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void txtLocation_TextChanged(object sender, EventArgs e)
        {

            if (txtLocation.Text == "" || !File.Exists(Path.Combine(Path.GetFullPath(txtLocation.Text).Replace(@"\", @"\\") + @"\" + "eqgame.exe")))
            {
                lblLocationError.Visible = true;
                return;
            }
            else
            {
                lblLocationError.Visible = false;
                string[] dateArray = { "10/31/2005", "09/07/2007", "12/19/2008", "6/8/2010", "2/5/2011" };

                int pos = Array.IndexOf(dateArray, Convert.ToString(RetrieveLinkerTimestamp().ToString("d")));

                if (pos == 0) // Titanium Version - Oct 31, 2005
                {
                    lblEQType.Text = "DETECTED: Titanium";
                    lblEQType.Tag = 1;
                }
                else if (pos == 1) // SoF Version - Sep 07, 2007
                {
                    lblEQType.Text = "DETECTED: Secrets of Faydwer";
                    lblEQType.Tag = 2;
                }
                else if (pos == 2) // SoD Version - Dec 19, 2008
                {
                    lblEQType.Text = "DETECTED: Seeds of Destruction";
                    lblEQType.Tag = 3;
                }
                else if (pos == 3) // UF Version - June 8, 2010
                {
                    lblEQType.Text = "DETECTED: Underfoot";
                    lblEQType.Tag = 4;
                }
                else if (pos == 4) // Steam Version - Feb 05, 2011
                {
                    lblEQType.Text = "DETECTED: UF Steam Edition";
                    lblEQType.Tag = 5;
                }
                else
                {
                    lblEQType.Text = "UNSUPPORTED: Please contact THF Admins.";
                    lblEQType.Tag = 0;
                }
            }
        }

        private DateTime RetrieveLinkerTimestamp()
        {
            string filePath = Path.Combine(Path.GetFullPath(txtLocation.Text).Replace(@"\", @"\\") + @"\" + "eqgame.exe");
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] b = new byte[2048];
            System.IO.Stream s = null;

            try
            {
                s = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                s.Read(b, 0, 2048);
            }
            finally
            {
                if (s != null)
                {
                    s.Close();
                }
            }

            int i = System.BitConverter.ToInt32(b, c_PeHeaderOffset);
            int secondsSince1970 = System.BitConverter.ToInt32(b, i + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt.ToUniversalTime();
        }
    }
}

