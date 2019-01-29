using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace THFDownloader
{
    public partial class FrmBootStrap : Form
    {
        private string[] args;
        private string tempFolderName;
        public static String BASE_URL = "http://ultimateeq.com/files/"; // URL to root folder for downloader

        public FrmBootStrap(string[] mArgs)
        {
            InitializeComponent();
            args = mArgs;
        }

        public static int CompareVersions(String strA, String strB)
        {
            Version vA = new Version(strA.Replace(",", "."));
            Version vB = new Version(strB.Replace(",", "."));

            return vA.CompareTo(vB);
        }

        private void FrmBootStrap_Load(object sender, EventArgs e)
        {
            try
            {
                string versionCurrent = Assembly.GetEntryAssembly().GetName().Version.ToString();
                string tempFolderName = System.IO.Path.GetRandomFileName();
                bool update = false;

                if (args.Length == 0)
                {
                    var versionURL = BASE_URL + "patcher/version.cfg";

                    System.Net.WebClient wc = new System.Net.WebClient();
                    byte[] raw = wc.DownloadData(THFDownloader.FrmBootStrap.BASE_URL + "patcher/version.cfg");

                    string webVersion = System.Text.Encoding.UTF8.GetString(raw);
                    string[] webVersionShort = webVersion.Split(new Char[] { '=' });
                    if (!versionCurrent.Equals(webVersionShort[1]))
                    {
                        update = true;
                    }
                }

                if (args.Length > 0) { update = true; }

                if (!update)
                {
                    this.Close();
                }
                else
                {
                    this.Tag = tempFolderName;
                    timer1.Start();
                }
            }
            catch
            {
                MessageBox.Show("ERROR (4): Please check your internet connection or server may be unavailable.");
                Application.Exit();
            }


        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (args.Length == 0)
            {
                System.Uri downloadURL = new System.Uri(BASE_URL + "patcher/DOWNLOADER/UltimateDownloader.exe");
                tempFolderName = Convert.ToString(this.Tag);

                if (!Directory.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + tempFolderName))
                {
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + tempFolderName);
                }

                try
                {
                    WebClient Client = new WebClient();
                    Client.DownloadFile(downloadURL, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + tempFolderName + "\\" + "UltimateDownloader.exe");
                }
                catch
                {
                    MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
                    Application.Exit();
                }
                if (File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "UltimateDownloader.exe"))
                {
                    Process.Start(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + tempFolderName + "\\" + "UltimateDownloader.exe", tempFolderName + " 1");
                }
                timer1.Enabled = false;
            }
            else
            {
                // LOAD PATCHER FORM
            }

            if (args.Length != 0)
            {
                if (Convert.ToInt32(args[1]) == 1)
                {
                    Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension("UltimateDownloader.exe"));

                    foreach (Process proc in procs)
                    {
                        if (proc.Id != Convert.ToInt32(Process.GetCurrentProcess().Id))
                        {
                            proc.Kill();
                        }
                    }

                    System.Threading.Thread.Sleep(200);

                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..") + "\\" + "UltimateDownloader.exe"))
                    {
                        try
                        {
                            File.SetAttributes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..") + "\\" + "UltimateDownloader.exe", FileAttributes.Normal);
                            File.Delete(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..") + "\\" + "UltimateDownloader.exe");
                        }
                        catch
                        {
                            MessageBox.Show("ERROR (1): Access Denied - Cannot delete file.");
                        }
                    }

                    File.Copy(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + "UltimateDownloader.exe", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..") + "\\" + "UltimateDownloader.exe",true);
                    File.SetAttributes(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..") + "\\" + "UltimateDownloader.exe", FileAttributes.Normal);

                    System.Threading.Thread.Sleep(300);
                    
                    if (File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..") + "\\" + "UltimateDownloader.exe"))
                    {
                        timer1.Enabled = false;
                        args[1] = "5";
                        Process.Start(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..") + "\\" + "UltimateDownloader.exe ", Convert.ToString(args[0]) + " 2");
                    }

                }

                if (Convert.ToInt32(args[1]) == 2)
                {
                    Process[] procs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension("UltimateDownloader.exe"));

                    foreach (Process proc in procs)
                    {
                        if (proc.Id != Convert.ToInt32(Process.GetCurrentProcess().Id))
                        {
                            proc.Kill();
                        }
                    }

                    System.Threading.Thread.Sleep(200);

                    if (File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + Convert.ToString(args[0]) + "\\" + "UltimateDownloader.exe"))
                    {
                        try
                        {
                            File.Delete(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + Convert.ToString(args[0]) + "\\" + "UltimateDownloader.exe");
                        }
                        catch
                        {
                            MessageBox.Show("ERROR (2): Access Denied - Cannot delete file.");
                        }
                    }
                    if (Directory.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + Convert.ToString(args[0])))
                    {
                        try
                        {
                            Directory.Delete(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + Convert.ToString(args[0]));
                        }
                        catch
                        {
                            MessageBox.Show("ERROR (2): Access Denied - Cannot delete folder.");
                        }
                    }
                    timer1.Enabled = false;
                    this.Close();
                }
            }
        }
    }
}
