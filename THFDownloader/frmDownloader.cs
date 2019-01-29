using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;


namespace UltimateDownloader
{
    public partial class FrmDownloader : Form
    {
        Stopwatch sw = new Stopwatch();    // The stopwatch which we will be using to calculate the download speed
        private Queue<string> _files = new Queue<string>();

        public FrmDownloader()
        {
            InitializeComponent();
            //Prevent Flickering, This Produces Smooth Progression
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true); 
            
            try
            {
                pbBackground.ImageLocation = THFDownloader.FrmBootStrap.BASE_URL + "patcher/background.jpg";
            }
            catch
            {
                Unavailable();
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }
            pbBackground.Refresh();
            ButtonStateUpdate();
        }

// INITIAL DOWNLOADER LOGIC
        private void Downloader_Start(object sender, EventArgs e)
        {
            // Delete legacy configuration file if it exists.
            if (File.Exists(Application.StartupPath + "\\" + "UltimateDownloader.cfg"))
            {
                File.Delete(Application.StartupPath + "\\" + "UltimateDownloader.cfg");
            }

            // Check to see if eqgame.exe exists in the folder where the launcher is.
            if (!File.Exists(Application.StartupPath + "\\" + "eqgame.exe"))
            {
                ExecutableMissing();
            }
            else
            {
                VersionCheck();                 //Load .cfg to get the client version
                if (!panelNotFound.Visible)
                {
                    ExecutableFound();          // Modify UI
                    DownloadIndex();            // Download latest index
                }
            }
            try
            {
                System.Net.WebClient wc = new System.Net.WebClient();
                byte[] raw = wc.DownloadData(THFDownloader.FrmBootStrap.BASE_URL + "patcher/patch_notes.txt");

                string webData = System.Text.Encoding.UTF8.GetString(raw);
                richPatchNotes.Text = webData;
            }
            catch
            {
                Unavailable();
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }
        }

        private void Unavailable()
        {
            statusDownloader.Visible = false;
            btnLaunch.Enabled = false;
            BtnCustomPlay1.Enabled = false;
            BtnCustomPlay2.Enabled = false;
            BtnCustomPlay3.Enabled = false;
            BtnCustomPlay4.Enabled = false;
            btnLaunch.Text = "PLAY!";
            BtnSettings.Enabled = false;
            BtnPatchNotes.Enabled = false;
        }

        private void ExecutableMissing()
        {
            statusDownloader.Visible = false;
            btnLaunch.Enabled = false;
            BtnCustomPlay1.Enabled = false;
            BtnCustomPlay2.Enabled = false;
            BtnCustomPlay3.Enabled = false;
            BtnCustomPlay4.Enabled = false;
            btnLaunch.Text = "PLAY!";
            BtnSettings.Enabled = false;
            BtnPatchNotes.Enabled = false;
            panelNotFound.Visible = true;
        }

        private void ExecutableFound()
        {
            statusDownloader.Visible = true;
            btnLaunch.Enabled = false;
            BtnCustomPlay1.Enabled = false;
            BtnCustomPlay2.Enabled = false;
            BtnCustomPlay3.Enabled = false;
            BtnCustomPlay4.Enabled = false;
            btnLaunch.Text = "Updating ...";
            toolLocation.Text = Application.StartupPath.Replace(@"\\", @"\");
            panelNotFound.Visible = false;
        }

        // INITIAL SETUP OF CONFIG FILE
        private void VersionCheck()
        {
            if (File.Exists(Application.StartupPath + @"\" + "eqgame.exe"))
            {
                string[] dateArray = { "05/09/2013", "05/10/2013", "05/11/2013" };
                
                int pos = Array.IndexOf(dateArray, DataVerify(Convert.ToString(RetrieveLinkerTimestamp(Path.Combine(Path.GetFullPath(Application.StartupPath).Replace(@"\", @"\\") + @"\" + "eqgame.exe")).ToString("MM/dd/yyyy"))));

                if (pos >= 0 && pos <= 2) // RoF2 - May 10, 2013 --- 0,1,2
                {
                    lblEQType.Text = "DETECTED: RoF2";
                    toolType.Text = "RoF2";
                    toolClientVersion.Text = "RoF2";
                    toolType.Tag = 6;
                }
                else
                {
                    toolType.Text = "";
                    toolClientVersion.Text = "";
                    toolType.Tag = 0;
                    btnInstallROF2.Enabled = true;

                    ExecutableMissing(); //Hide main UI and display search panel for executable.
                }
            }
            else
            {
                ExecutableMissing();
            }
        }

        static string DataVerify(string date)
        {
            StringBuilder s  = new StringBuilder(date);
            s.Replace(@"-", @"/");
            s.Replace(@".", @"/");
            s.Replace(@",", @"/");
            s.Replace(@"_", @"/");
            return s.ToString();
        }

        private void DownloadIndex()
        {
            lblDownload.Text = "Initializing, Please Wait...";
            pbTotal.Tag = "INDEX";
            DownloadFiles();
        }

        private void CompareIndex()
        {
            var lines = File.ReadLines(toolClientVersion.Text + ".index");
            foreach (var line in lines)
            {
                string[] lineName = line.Split(new Char[] { '|' });

                if (!File.Exists(Application.StartupPath + "\\" + Convert.ToString(lineName[0]))) // IF FILE DOESN'T EXIST, ADD TO QUEUE.
                {
                    if (Convert.ToInt32(toolType.Tag) == 6) // Adding SoD+ Files
                    {
                        _files.Enqueue(THFDownloader.FrmBootStrap.BASE_URL + "patcher/ROF2/" + Convert.ToString(lineName[0]));
                    }
                }
                else // IF FILE EXISTS AND HASH DOES NOT MATCH, ADD TO QUEUE.
                {
                    string localHash = GetMD5HashFromFile(Application.StartupPath + "\\" + Convert.ToString(lineName[0]));
                    if (lineName[1] != localHash) 
                    {
                        if (Convert.ToInt32(toolType.Tag) == 6) // Adding SoD+ Files
                        {
                            _files.Enqueue(THFDownloader.FrmBootStrap.BASE_URL + "patcher/ROF2/" + Convert.ToString(lineName[0]));
                        }
                    }
                }
            }
            if (File.Exists("ROF2.index"))
            {
                File.Delete("ROF2.index");
            }
            DownloadFiles();
        }

// CLIENT LOGIC
        public void DownloadFiles()
        {
            try
            {
                WebClient client = new WebClient();
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                client.DownloadFileCompleted += Client_DownloadFileCompleted;

                btnLaunch.Enabled = false;
                BtnCustomPlay1.Enabled = false;
                BtnCustomPlay2.Enabled = false;
                BtnCustomPlay3.Enabled = false;
                BtnCustomPlay4.Enabled = false;
                btnCheckForUpdate.Enabled = false;

                if (pbTotal.Tag.ToString() == "INDEX") // If the file is an index file...
                {
                    sw.Start();
                    if (Convert.ToInt32(toolType.Tag) == 6) // Adding RoF2 Files
                    {
                        client.DownloadFileAsync(new Uri(THFDownloader.FrmBootStrap.BASE_URL + "patcher/ROF2.index"), "ROF2.index");
                    }
                }
                else
                {
                    if (_files.Any())
                    {
                        toolFilesToUpdate.Tag = _files.Count;
                    }
                    // Cycle through files that need to be downloaded.
                    if (_files.Any())
                    {
                        toolFilesToUpdate.Text = "Files to Update: ";
                        toolFilesRemaining.Text = Convert.ToString(_files.Count);
                        var url = _files.Dequeue();
                        sw.Start();

                        if (Convert.ToInt32(toolType.Tag) == 6) // Adding RoF2 Files
                        {
                            string[] FileName = url.Split(new string[] { "ROF2/" }, StringSplitOptions.None);
                            lblDownload.Text = "Downloading: " + Convert.ToString(FileName[1]);
                            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(Application.StartupPath + "\\" + Convert.ToString(FileName[1])));
                            client.DownloadFileAsync(new Uri(url), Application.StartupPath + "\\" + Convert.ToString(FileName[1]));
                        }
                        return;
                    }
                    else
                    {
                        // End of the download
                        ButtonStateUpdate();
                        
                        btnCheckForUpdate.Enabled = true;
                        toolFilesToUpdate.Text = "   ";
                        toolFilesRemaining.Text = "   ";
                        lblDownload.Text = "Up To Date!";
                        ButtonStateUpdate();

                        if (Convert.ToInt32(toolFilesToUpdate.Tag) > 0)
                        {
                            panelPatch.Visible = true;
                        }
                        
                    }
                }
            }
            catch
            {
                Unavailable();
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }
        }
 
 // MANAGE BUTTON STATE BETWEEN TRANSITIONS
        void ButtonStateUpdate()
        {
            string strLaunchSetting = ConfigurationManager.AppSettings["LaunchSetting"];
            string strName1 = ConfigurationManager.AppSettings["Name1"];
            string strName2 = ConfigurationManager.AppSettings["Name2"];
            string strName3 = ConfigurationManager.AppSettings["Name3"];
            string strName4 = ConfigurationManager.AppSettings["Name4"];

            if (string.IsNullOrEmpty(strName1) && string.IsNullOrEmpty(strName2) && string.IsNullOrEmpty(strName3) && string.IsNullOrEmpty(strName4) || strLaunchSetting == "none" || string.IsNullOrEmpty(strLaunchSetting))
            {
                panelPlay.Visible = true;
                panelCustom.Visible = false;
                BtnSettings.Enabled = true;
                BtnPatchNotes.Enabled = true;
                btnLaunch.Enabled = true;
                btnLaunch.BackgroundImage = UltimateDownloader.Properties.Resources.btn_play;
                btnLaunch.Text = "PLAY!";
            }

            if (!string.IsNullOrEmpty(strName1) && strLaunchSetting == "shortcuts")
            {
                panelCustom.Visible = true;
                BtnSettings.Enabled = true;
                BtnPatchNotes.Enabled = true;
                BtnCustomPlay1.Enabled = true;
                BtnCustomPlay1.Text = strName1;
                BtnCustomPlay1.BackgroundImage = UltimateDownloader.Properties.Resources.btn_play;
            }
            else
            {
                BtnCustomPlay1.BackgroundImage = UltimateDownloader.Properties.Resources.btn_disabled;
                BtnCustomPlay1.Text = "";
                BtnCustomPlay1.Enabled = false;
            }

            if (!string.IsNullOrEmpty(strName2) && strLaunchSetting == "shortcuts")
            {
                panelCustom.Visible = true;
                BtnSettings.Enabled = true;
                BtnPatchNotes.Enabled = true;
                BtnCustomPlay2.Enabled = true;
                BtnCustomPlay2.Text = strName2;
                BtnCustomPlay2.BackgroundImage = UltimateDownloader.Properties.Resources.btn_play;
            }
            else
            {
                BtnCustomPlay2.BackgroundImage = UltimateDownloader.Properties.Resources.btn_disabled;
                BtnCustomPlay2.Text = "";
                BtnCustomPlay2.Enabled = false;
            }

            if (!string.IsNullOrEmpty(strName3) && strLaunchSetting == "shortcuts")
            {
                panelCustom.Visible = true;
                BtnSettings.Enabled = true;
                BtnPatchNotes.Enabled = true;
                BtnCustomPlay3.Enabled = true;
                BtnCustomPlay3.Text = strName3;
                BtnCustomPlay3.BackgroundImage = UltimateDownloader.Properties.Resources.btn_play;
            }
            else
            {
                BtnCustomPlay3.BackgroundImage = UltimateDownloader.Properties.Resources.btn_disabled;
                BtnCustomPlay3.Text = "";
                BtnCustomPlay3.Enabled = false;
            }

            if (!string.IsNullOrEmpty(strName4) && strLaunchSetting == "shortcuts")
            {
                panelCustom.Visible = true;
                BtnSettings.Enabled = true;
                BtnPatchNotes.Enabled = true;
                BtnCustomPlay4.Enabled = true;
                BtnCustomPlay4.Text = strName4;
                BtnCustomPlay4.BackgroundImage = UltimateDownloader.Properties.Resources.btn_play;
            }
            else
            {
                BtnCustomPlay4.BackgroundImage = UltimateDownloader.Properties.Resources.btn_disabled;
                BtnCustomPlay4.Text = "";
                BtnCustomPlay4.Enabled = false;
            }
        }
// DOWNLOAD IN PROGRESS UPDATES/LOGIC
        void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                // Update Progress Bar
                if (pbTotal.Value != e.ProgressPercentage) {
                    pbTotal.Value = e.ProgressPercentage;
                    pbTotal.Refresh();
                 }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

// DOWNLOAD COMPLETE LOGIC
        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            sw.Reset();

            if (e.Error != null)
            {
                // handle error scenario
                throw e.Error;
            }

            if (e.Cancelled == true)
            {
                lblDownload.Text = "Download Cancelled!";
            }
            else
            {

                if (pbTotal.Tag.ToString() == "INDEX")
                {
                    pbTotal.Tag = "";
                    CompareIndex();
                    return;
                }
                DownloadFiles(); // Continue downloading files in queue (if applicable)
            }
        }

// MD5 CRYPTO LOGIC - PASS IN FILE TO RETURN MD5
        protected string GetMD5HashFromFile(string fileName)
        {
            // Method for generating MD5 Hash
            FileStream file = new FileStream(fileName, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }

// EQGAME CHECKER - COUNTS SECONDS FROM 1970 AND CREATES DATE BASED OFF OF ASSEMBLY.
        private DateTime RetrieveLinkerTimestamp(string filePath)
        {
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
            Console.WriteLine(dt.ToUniversalTime());
            return dt.ToUniversalTime();
        }

// CHECK FOR UPDATES - CLICKING CHECK
        private void BtnCheckForUpdate_Click(object sender, EventArgs e)
        {
            if (!File.Exists("UltimateDownloader.cfg")) //Check for the configuration file, if not found then push the settings box
            {
                //If no .cfg, then force player to set location of eqgame.exe
                ExecutableMissing();
            }
            else
            {
                ExecutableFound();      // Validating Executable
                VersionCheck();         //Load .cfg to get the client version
                DownloadIndex();        // Download latest index
            }
        }

// DOWNLOAD FILE
        public void DownloadFile(string url, string targetfolder)
        {
            WebClient wc = new WebClient();
            try
            {
                wc.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadCompleted);
                wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadChanged);

                wc.DownloadFileAsync(new Uri(url), targetfolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: \n\n" + ex.Message);
            }
        }

// DOWNLOAD CHANGE EVENT
        private void DownloadChanged(object sender, DownloadProgressChangedEventArgs e)
        {

        }

// DOWNLOAD COMPLETE EVENT
        private void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {

        }

        private void TxtLocation_TextChanged(object sender, EventArgs e)
        {
            if (txtLocation.Text == "" || !File.Exists(Path.Combine(Path.GetFullPath(Application.StartupPath).Replace(@"\", @"\\") + @"\" + "eqgame.exe")))
            {
                return;
            }
            else
            {
                if (File.Exists(Application.StartupPath + "\\" + "eqgame.exe"))
                {
                    string[] dateArray = { "05/09/2013", "05/10/2013", "05/11/2013" };

                    int pos = Array.IndexOf(dateArray, DataVerify(Convert.ToString(RetrieveLinkerTimestamp(Path.Combine(Path.GetFullPath(Application.StartupPath).Replace(@"\", @"\\") + @"\" + "eqgame.exe")).ToString("MM/dd/yyyy"))));

                    if (pos >= 0 && pos <= 2) // RoF2 - May 10, 2013 --- 0,1,2
                    {
                        lblEQType.Text = "DETECTED: RoF2";
                        toolType.Text = "RoF2";
                        toolClientVersion.Text = "RoF2";
                        toolType.Tag = 6;
                    }
                    else
                    {
                        toolType.Text = "";
                        toolClientVersion.Text = "";
                        toolType.Tag = 0;

                        ExecutableMissing(); //Hide main UI and display search panel for executable.
                    }
                }
                else
                {
                    ExecutableMissing();
                }
            }
        }

        private void BtnLocation_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.txtLocation.Text = folderBrowserDialog1.SelectedPath;
                btnInstallROF2.Enabled = true;
            }
        }

// DOWNLOAD INSTALLER FILES (OBSOLETE)
        private void BtnComplete_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtLocation.Text) || string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                // Edge case: Disable install button and pop up alert stating to select a path for install.
                btnInstallROF2.Enabled = false;
                MessageBox.Show("Please browse to where you would like to install RoF2", "Error: No Selected Destination",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                // Download torrent files to get RoF2 client
                WebClient myWebClient = new WebClient();

                myWebClient.DownloadFile(new Uri(THFDownloader.FrmBootStrap.BASE_URL + "installer/aria2c.exe"), txtLocation.Text + "\\aria2c.exe");
                myWebClient.DownloadFile(new Uri(THFDownloader.FrmBootStrap.BASE_URL + "installer/client_download.bat"), txtLocation.Text + "\\client_download.bat");
                myWebClient.DownloadFile(new Uri(THFDownloader.FrmBootStrap.BASE_URL + "installer/everquest_rof2.torrent"), txtLocation.Text + "\\everquest_rof2.torrent");
            }
            // Run torrent downloader to install client
            Process.Start(txtLocation.Text + "\\" + "client_download.bat", "");
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            // Load saved parameters into fields
            if(ConfigurationManager.AppSettings["LaunchSetting"] == "none")
            {
                radioNone.Checked = true;
            }
            else if (ConfigurationManager.AppSettings["LaunchSetting"] == "shortcuts")
            {
                radioShortcuts.Checked = true;
            }
            else if (ConfigurationManager.AppSettings["LaunchSetting"] == "wineq2")
            {
                radioWinEQ2.Checked = true;
            }
            else if (ConfigurationManager.AppSettings["LaunchSetting"] == "isboxer")
            {
                radioISBoxer.Checked = true;
            }
            else if (ConfigurationManager.AppSettings["LaunchSetting"] == "advanced")
            {
                radioAdvanced.Checked = true;
            }
            else
            {
                radioNone.Checked = true;
            }

            txtName1.Text = ConfigurationManager.AppSettings["Name1"];
            txtName2.Text = ConfigurationManager.AppSettings["Name2"];
            txtName3.Text = ConfigurationManager.AppSettings["Name3"];
            txtName4.Text = ConfigurationManager.AppSettings["Name4"];
            txtLogin1.Text = ConfigurationManager.AppSettings["Login1"];
            txtLogin2.Text = ConfigurationManager.AppSettings["Login2"];
            txtLogin3.Text = ConfigurationManager.AppSettings["Login3"];
            txtLogin4.Text = ConfigurationManager.AppSettings["Login4"];

            if (ConfigurationManager.AppSettings["LaunchRename"] == "true")
            {
                chkRename.Checked = true;
            }
            else
            {
                chkRename.Checked = false;
            }

            BtnSettings.Enabled = false;
            BtnPatchNotes.Enabled = false;
            panelSettings.Visible = true;
        }

        private void BtnPatchNotes_Click(object sender, EventArgs e)
        {
            try
            {
                System.Net.WebClient wc = new System.Net.WebClient();
                byte[] raw = wc.DownloadData(THFDownloader.FrmBootStrap.BASE_URL + "patcher/patch_notes.txt");

                string webData = System.Text.Encoding.UTF8.GetString(raw);
                richPatchNotes.Text = webData;
            }
            catch
            {
                Unavailable();
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }


            BtnPatchNotes.Enabled = false;
            BtnSettings.Enabled = false;
            panelPatch.Visible = true;
        }

        private void BtnExitPatch_Click(object sender, EventArgs e)
        {
            panelPatch.Visible = false;
        }

        private void BtnExitApp_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void BtnForums_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://ultimateeq.com/phpbb3/index.php");
            }
            catch
            {
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }
        }

        private void BtnWiki_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://ultimateeq.com/wiki/index.php/Main_Page");
            }
            catch
            {
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }
        }

        private void BtnWebsite_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("http://ultimateeq.com/");
            }
            catch
            {
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }
        }

        private void llDownloadURL_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start("http://ultimateeq.com/patcher.html");
            }
            catch
            {
                MessageBox.Show("ERROR (2): Please check your internet connection or server may be unavailable.");
            }
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            string executeFile = Application.StartupPath + "\\" + "eqgame.exe";
            Process.Start(executeFile, "patchme");
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, String lpString);

        private void LaunchGame(string strLogin, string strMethod)
        {
            string strRename = ConfigurationManager.AppSettings["LaunchRename"];

            Process proc = new Process();
            proc.StartInfo.FileName = Application.StartupPath + "\\" + "eqgame.exe";

            if (strMethod == "default")
            {
                proc.StartInfo.Arguments = "patchme";
            }
            if (strMethod == "login")
            {
                proc.StartInfo.Arguments = "patchme /login:" + strLogin;
            }
            proc.Start();
            proc.WaitForInputIdle();
            if (strRename == "true")
            {
                SetWindowText(Process.GetProcessById(proc.Id).MainWindowHandle, strLogin);
            }
        }

        private void BtnCustomPlay1_Click(object sender, EventArgs e)
        {
            string strLogin1 = ConfigurationManager.AppSettings["Login1"];

            if (string.IsNullOrEmpty(strLogin1))
            {
                LaunchGame(strLogin1, "default");
            }
            else
            {
                LaunchGame(strLogin1, "login");
            }
        }

        private void BtnCustomPlay2_Click(object sender, EventArgs e)
        {
            string strLogin2 = ConfigurationManager.AppSettings["Login2"];

            if (string.IsNullOrEmpty(strLogin2))
            {
                LaunchGame(strLogin2, "default");
            }
            else
            {
                LaunchGame(strLogin2, "login");
            }
        }

        private void BtnCustomPlay3_Click(object sender, EventArgs e)
        {
            string strLogin3 = ConfigurationManager.AppSettings["Login3"];

            if (string.IsNullOrEmpty(strLogin3))
            {
                LaunchGame(strLogin3, "default");
            }
            else
            {
                LaunchGame(strLogin3, "login");
            }
        }

        private void BtnCustomPlay4_Click(object sender, EventArgs e)
        {
            string strLogin4 = ConfigurationManager.AppSettings["Login4"];

            if (string.IsNullOrEmpty(strLogin4))
            {
                LaunchGame(strLogin4, "default");
            }
            else
            {
                LaunchGame(strLogin4, "login");
            }
        }

        private void BtnSaveShortcuts_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            config.AppSettings.Settings.Remove("Name1");
            config.AppSettings.Settings.Add("Name1", txtName1.Text);
            config.AppSettings.Settings.Remove("Name2");
            config.AppSettings.Settings.Add("Name2", txtName2.Text);
            config.AppSettings.Settings.Remove("Name3");
            config.AppSettings.Settings.Add("Name3", txtName3.Text);
            config.AppSettings.Settings.Remove("Name4");
            config.AppSettings.Settings.Add("Name4", txtName4.Text);
            config.AppSettings.Settings.Remove("Login1");
            config.AppSettings.Settings.Add("Login1", txtLogin1.Text);
            config.AppSettings.Settings.Remove("Login2");
            config.AppSettings.Settings.Add("Login2", txtLogin2.Text);
            config.AppSettings.Settings.Remove("Login3");
            config.AppSettings.Settings.Add("Login3", txtLogin3.Text);
            config.AppSettings.Settings.Remove("Login4");
            config.AppSettings.Settings.Add("Login4", txtLogin4.Text);
            config.AppSettings.Settings.Remove("LaunchRename");
            if (chkRename.Checked)
            {
                config.AppSettings.Settings.Add("LaunchRename", "true");
            }
            else
            {
                config.AppSettings.Settings.Add("LaunchRename", "false");
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            BtnSettings.Enabled = true;
            BtnPatchNotes.Enabled = true;
            panelSettings.Visible = false;

            ButtonStateUpdate();
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

            config.AppSettings.Settings.Remove("LaunchSetting");

            if (radioNone.Checked)
            {
                config.AppSettings.Settings.Add("LaunchSetting", "none");
            }
            if (radioShortcuts.Checked)
            {
                config.AppSettings.Settings.Add("LaunchSetting", "shortcuts");
            }
            if (radioWinEQ2.Checked)
            {
                config.AppSettings.Settings.Add("LaunchSetting", "wineq2");
            }
            if (radioISBoxer.Checked)
            {
                config.AppSettings.Settings.Add("LaunchSetting", "isboxer");
            }
            if (radioAdvanced.Checked)
            {
                config.AppSettings.Settings.Add("LaunchSetting", "advanced");
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            BtnSettings.Enabled = true;
            BtnPatchNotes.Enabled = true;
            panelSettings.Visible = false;

            ButtonStateUpdate();

        }

        private void BtnExitShortcuts_Click(object sender, EventArgs e)
        {
            BtnSettings.Enabled = true;
            BtnPatchNotes.Enabled = true;
            panelSettings.Visible = false;

            ButtonStateUpdate();
        }

        private void BtnExitWinEQ2_Click(object sender, EventArgs e)
        {
            BtnSettings.Enabled = true;
            BtnPatchNotes.Enabled = true;
            panelSettings.Visible = false;

            ButtonStateUpdate();
        }

        private void BtnExitISBoxer_Click(object sender, EventArgs e)
        {
            BtnSettings.Enabled = true;
            BtnPatchNotes.Enabled = true;
            panelSettings.Visible = false;

            ButtonStateUpdate();
        }

        private void BtnExitAdvanced_Click(object sender, EventArgs e)
        {
            BtnSettings.Enabled = true;
            BtnPatchNotes.Enabled = true;
            panelSettings.Visible = false;

            ButtonStateUpdate();
        }

        private void BtnExitSettings_Click(object sender, EventArgs e)
        {
            BtnSettings.Enabled = true;
            BtnPatchNotes.Enabled = true;
            panelSettings.Visible = false;

            ButtonStateUpdate();
        }

        private void radioNone_CheckedChanged(object sender, EventArgs e)
        {
            if (radioNone.Checked)
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                config.AppSettings.Settings.Remove("LaunchSetting");
                config.AppSettings.Settings.Add("LaunchSetting", "none");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                BtnNext.Enabled = false;
            }
        }

        private void radioShortcuts_CheckedChanged(object sender, EventArgs e)
        {
            if (radioShortcuts.Checked)
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                config.AppSettings.Settings.Remove("LaunchSetting");
                config.AppSettings.Settings.Add("LaunchSetting", "shortcuts");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                BtnNext.Enabled = true;
            }
        }

        private void radioWinEQ2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioWinEQ2.Checked)
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                config.AppSettings.Settings.Remove("LaunchSetting");
                config.AppSettings.Settings.Add("LaunchSetting", "wineq2");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                BtnNext.Enabled = true;
            }
        }

        private void radioISBoxer_CheckedChanged(object sender, EventArgs e)
        {
            if (radioISBoxer.Checked)
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                config.AppSettings.Settings.Remove("LaunchSetting");
                config.AppSettings.Settings.Add("LaunchSetting", "isboxer");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                BtnNext.Enabled = true;
            }
        }

        private void radioAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            if (radioAdvanced.Checked)
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath);

                config.AppSettings.Settings.Remove("LaunchSetting");
                config.AppSettings.Settings.Add("LaunchSetting", "advanced");
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                BtnNext.Enabled = true;
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (radioNone.Checked)
            {
                tabLaunch.SelectedIndex = 0;
            }
            if (radioShortcuts.Checked)
            {
                tabLaunch.SelectedIndex = 1;
            }
            if (radioWinEQ2.Checked)
            {
                tabLaunch.SelectedIndex = 2;
            }
            if (radioISBoxer.Checked)
            {
                tabLaunch.SelectedIndex = 3;
            }
            if (radioAdvanced.Checked)
            {
                tabLaunch.SelectedIndex = 4;
            }

            
        }
    }
}
