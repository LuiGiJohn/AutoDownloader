using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Diagnostics;
using Microsoft.Win32;
using System.Threading;
using System.Xml;
using System.Net;
using System.Collections;
using System.Globalization;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;


namespace ofp2_sync
{

    public partial class Form1 : Form
    {
        string path;
        double version = 1.05;
        List<dlc> mydlc; //list of parsed xml nodes in a list/object format
        //dlc currentDLC = new dlc(); //used to send dlc map packs to backgroundworkers
        int dlcount = 0; //used for keeping track of what mapack is being download for gui updates
        bool updateNotify = false; //have we already notified user of updates?
        //variables for xml parsing
        double clientVersion; //client version from xml.. used to determine if we need to update
        string clientNews; //can be used to display update info on users clients
        string clientURL; //url from xml
        int tickRate; //update interval pulled from dlc.xml
        
        public Form1()
        {
            InitializeComponent();

        }



        //client update function
        public void clientUpdate()
        {
            updateNotify = true;
            this.Invoke((MethodInvoker)delegate
            {

                richTextBox1.AppendText(clientNews.Replace("$version", clientVersion.ToString("#,0.00")).Replace("$url", clientURL) + "\n");
                notifyIcon1.BalloonTipText = clientNews.Replace("$version", clientVersion.ToString("#,0.00")).Replace("$url", clientURL) + "\n";
                notifyIcon1.ShowBalloonTip(5000);
                

            });
            
        }




        //this function will check the dlc.xml doc for client and map updates
        public void checkXML()
        {
            //create a webclient to download xml
            WebClient c = new WebClient();
            String dlcxml = "";
            try
            {
                c.Headers.Add("Content-Type", "application/xml");
                dlcxml = c.DownloadString("http://vengeful-spirit.com/ofp2-dlc/dlc.xml");
            }
            catch (System.Net.WebException ex)
            {
                labelStatus.Text = "Status: Failed to connect to server";
            }

            try
            {
                //if we have an xml doc lets start parsing it
                if (dlcxml != "")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(dlcxml);
                    //lets check client xml settings first
                    XmlNodeList client = doc.SelectNodes("dlcdata/client");
                    foreach (XmlNode entry in client)
                    {
                        if (entry.NodeType == XmlNodeType.Element && entry.FirstChild.NodeType == XmlNodeType.Element)
                        {
                            foreach (XmlNode node in entry)
                            {
                                try
                                {
                                    if (node.NodeType == XmlNodeType.Element)
                                    {
                                        if (node.Name == "version")
                                        {
                                            clientVersion = Convert.ToDouble(node.InnerText.ToString(), CultureInfo.InvariantCulture);
                                        }
                                        if (node.Name == "news")
                                        {
                                            clientNews = node.InnerText;
                                        }
                                        if (node.Name == "url")
                                        {
                                            clientURL = node.InnerText;
                                        }
                                        if (node.Name == "tickRate")
                                        {
                                            tickRate = Convert.ToInt32(node.InnerText, CultureInfo.InvariantCulture);
                                        }

                                    }

                                }
                                catch (XmlException ex)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        richTextBox1.AppendText("Problem with XML - An Administrator will be resolving this shortly: " + ex.Message + "\n");
                                    });
                                }


                            }
                        }
                        
                    }


                    //do we need to update?
                    if (clientVersion > version & !updateNotify)
                    {
                        clientUpdate();
                    }



                    //get list of dlc packs and mirros
                    mydlc = new List<dlc>();
                    XmlNodeList dlclist = doc.SelectNodes("dlcdata/dlcpack");
                    foreach (XmlNode entry in dlclist)
                    {
                        dlc tempdlc = new dlc();
                        if (entry.NodeType == XmlNodeType.Element)
                        {
                            foreach (XmlNode node in entry)
                            {
                                try
                                {
                                    if (node.NodeType == XmlNodeType.Element)
                                    {
                                        if (node.Name == "name")
                                        {
                                            tempdlc.name = node.InnerText;
                                        }
                                        if (node.Name == "revision")
                                        {
                                            tempdlc.revision = Convert.ToDouble(node.InnerText, CultureInfo.InvariantCulture);
                                        }
                                        if (node.Name == "mirror")
                                        {
                                            tempdlc.mirrors(node.InnerText);
                                        }
                                        if (node.Name == "uid")
                                        {
                                            tempdlc.uid = Convert.ToInt32(node.InnerText, CultureInfo.InvariantCulture);
                                        }
                                        if (node.Name == "remove")
                                        {
                                            tempdlc.removePath(node.InnerText);
                                        }
                                        if (node.Name == "notes")
                                        {
                                            tempdlc.notes = node.InnerText;
                                        }
                                    }
                                }
                                catch (XmlException ex)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        richTextBox1.AppendText("Problem with XML - An Administrator will be resolving this shortly: " + ex.Message + "\n");
                                    });
                                }





                            }
                        }
                        
                        //do we need to update this DLC?
                        int revision = 0;
                        RegistryKey dlcInfo = Registry.CurrentUser.OpenSubKey("Software\\VSDLC\\MapPack" + tempdlc.uid);
                        if (dlcInfo == null)
                        {
                            mydlc.Add(tempdlc);

                        }
                        else
                        {
                            revision = Convert.ToInt32(dlcInfo.GetValue("revision"), CultureInfo.InvariantCulture);
                        }

                        if (tempdlc.revision > revision && revision > 0)
                        {
                            mydlc.Add(tempdlc);
                            //dlcInfo.Close();
                        }



                    }
                }
            }
            catch (XmlException ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    richTextBox1.AppendText("Problem with XML - An Administrator will be resolving this shortly: " + ex.Message + "\n");
                });
            }

            
        }


        public void startOFP()
        {
            if (path.Length > 1)
            {
                Process pr = new Process();
                pr.StartInfo.FileName = path + "\\OFDR.exe";
                try
                {
                    pr.Start();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    //im gona have to move this and be cleaner about it
                    MessageBox.Show("I was unable to find Operation Flash Point Dragon Rising in the following path: " + path.ToString());

                }

            }
        }




        public void startME()
        {
            if (path.Length > 1)
            {
                Process pr = new Process();
                pr.StartInfo.FileName = path + "\\Mission Editor\\MissionEditor.exe";
                try
                {
                    pr.Start();
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    //im gona have to move this and be cleaner about it
                    MessageBox.Show("I was unable to find Operation Flash Point Dragon Rising in the following path: " + path.ToString());

                }

            }
        }



        public void setup()
        {

            //check for application settings in registry and create them as needed
            RegistryKey vsdlc = Registry.CurrentUser.OpenSubKey("Software\\VSDLC", true);
            //if key does not exist we will create it
            if (vsdlc == null)
            {
                vsdlc = Registry.CurrentUser.CreateSubKey("Software\\VSDLC");
                vsdlc.SetValue("version", version);
                vsdlc.Close();
            }
            //get the updater version number from registry
            //if (vsdlc.GetValue("version") != null)
            //{
            //    version = Convert.ToDouble(vsdlc.GetValue("version").ToString());
            //}
            
        }


        public void handlePath(bool userRequested)
        {
            if (!userRequested)
            {
                MessageBox.Show("Operation Flashpoint - Dragon Rising was not found. Please manually select the path");
            }
            FolderBrowserDialog fd = new FolderBrowserDialog();
            if (fd.ShowDialog(this) == DialogResult.OK)
            {
                if (fd.SelectedPath != null)
                {
                    path = fd.SelectedPath;
                    //set reg key for future use
                    
                        RegistryKey vsdlc = Registry.CurrentUser.OpenSubKey("Software\\VSDLC", true);
                        if (vsdlc == null)
                        {
                            vsdlc = Registry.CurrentUser.CreateSubKey("Software\\VSDLC");
                            vsdlc.SetValue("path", path);
                            vsdlc.Close();
                        }
                        else
                        {
                            vsdlc.SetValue("path", path);
                            vsdlc.Close();
                        }
                  
                    return;
                }
            }
            
            else
            {
                Application.Exit();
            }
            
        }



        private void Form1_Load(object sender, EventArgs e)
        {


            richTextBox1.ReadOnly = true;
            setup();
            notifyIcon1.BalloonTipText = "Community DLC Updater";
            notifyIcon1.ShowBalloonTip(5000);


            this.Text = this.Text + " Version: " + version;

            //add code here to look for saved path in reg under VSDLC

            RegistryKey vsdlc = Registry.CurrentUser.OpenSubKey("Software\\VSDLC", true);
            if (vsdlc != null)
            {
                if (vsdlc.GetValue("path") != null)
                {
                    path = vsdlc.GetValue("path").ToString();
                    vsdlc.Close();
                }

            }

            if (path == null)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Codemasters\\OF Dragon Rising");
                if (key == null)
                {
                    handlePath(false);
                }
                else
                {
                    if (key.GetValue("PATH_APPLICATION") != null)
                    {
                        path = key.GetValue("PATH_APPLICATION").ToString();
                    }
                    else
                    {
                        handlePath(false);
                    }
                    key.Close();
                }
            }
            

            labelGamePath.Text = "Game Path: " + path;
            //check to see if we are doing auto start
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rkApp.GetValue("VSDLC") == null)
            {
                checkBox1.Checked = false;
            }
            else
            {
                checkBox1.Checked = true;
            }
            rkApp.Close();


            this.checkXML();
            timer1.Interval = tickRate;
            timer1.Start();
            doFirstUpdate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            backgroundWorkerDownload.CancelAsync();
            RegistryKey vsdlc = Registry.CurrentUser.OpenSubKey("Software\\VSDLC");
            if (vsdlc != null)
            {
                vsdlc.Close();
                Registry.CurrentUser.DeleteSubKeyTree("Software\\VSDLC");
            }
            setup();
            checkXML();
            if (!backgroundWorkerDownload.IsBusy)
            {
                
                backgroundWorkerDownload.RunWorkerAsync();
                labelStatus.Text = "Status: Downloading updated missions";
            }
            
            
            //richTextBox1.AppendText("Updating - " + mydlc[dlcount].name + "\n");

        }

       
        
        //this thread downlaods the files
        private void backgroundworker_do_work(object sender, DoWorkEventArgs e)
        {
            if (mydlc != null && mydlc.Count > 0)
            {
                foreach (dlc currentDLC in mydlc)
                {
                    if (backgroundWorkerDownload.CancellationPending != true)
                    {
                        currentDLC.findMirror();
                        if (currentDLC.isThereAMirror)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                richTextBox1.AppendText("Updating - " + currentDLC.name);
                                notifyIcon1.BalloonTipText = "Downloading DLC Updates";
                                notifyIcon1.ShowBalloonTip(5000);

                            });

                            doDownload(currentDLC, e);
                        }
                    }


                }
                this.Invoke((MethodInvoker)delegate
                {
                    labelStatus.Text = "Status: Finished updating all missions";
                    progressBar.Value = 0;
                });
            }
            

                

        }

        private void backgroundworker_progress_changed(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            
        }



        private void backgroundworker_run_worker_completed(object sender, RunWorkerCompletedEventArgs e)
        {
            dlcount++;
        }



       

        

        
        
        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                this.ShowInTaskbar = false;
                Hide();
                
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }


        public void doFirstUpdate()
        {

            if (!backgroundWorkerDownload.IsBusy)
            {

                mydlc.Clear();
                this.checkXML();
                if (mydlc.Count == 0)
                {
                    labelStatus.Text = "Status: No updates available at this time";
                    return;
                }

                backgroundWorkerDownload.RunWorkerAsync();
                labelStatus.Text = "Status: Downloading updated missions";


            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
         
            if (!backgroundWorkerDownload.IsBusy)
            {

                mydlc.Clear();
                this.checkXML();
                if (mydlc.Count == 0)
                {
                    labelStatus.Text = "Status: No updates available at this time";
                    return;
                }
                
                backgroundWorkerDownload.RunWorkerAsync();
                labelStatus.Text = "Status: Downloading updated missions";
                

            }
           

            
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Help.ShowHelp(this, e.LinkText);
        }



















        //download files

        private void doDownload(dlc currentdlc, DoWorkEventArgs e)
        {
            //check if mirror is working
            if (currentdlc.isThereAMirror)
            {
                //take care of any remove operations

                foreach (string todel in currentdlc.remove)
                {
                    if (todel != null)
                    {
                        string fullPath = path + "\\data_win\\" + todel;
                        try
                        {
                            if(Directory.Exists(fullPath))
                            {
                                Directory.Delete(fullPath,true);
                            }
                            
                        }
                        catch(Exception ex)
                        {

                        }
                        
                    }
                    
                }
                
                
                //start downloading

                string sFilePathToWriteFileTo = Path.GetTempPath().ToString() + currentdlc.filename;
                HttpWebRequest request;
                HttpWebResponse response;
                Uri url = new Uri(currentdlc.foundMirror);
                Int64 iSize = 0;
                //add WebException here
                try
                {
                    request = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                    response = (System.Net.HttpWebResponse)request.GetResponse();
                    response.Close();
                    
                    if (response.ContentType == "text/html; charset=utf-8")
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            richTextBox1.AppendText(" - Mirror: " + currentdlc.foundMirror + "- Failed  \n");
                        });
                        return;
                    }
                    iSize = response.ContentLength;
                }
                catch (WebException ex)
                {
                    //MessageBox.Show(ex.Message);
                    this.Invoke((MethodInvoker)delegate
                    {
                        richTextBox1.AppendText(" - Failed: " + ex.Message + "\n");
                    });
                    return;
                }
                
                

                //initial size of file
                
                //counter for progress
                Int64 iRunningByteTotal = 0;
                this.Invoke((MethodInvoker)delegate
                {
                    richTextBox1.AppendText(" - Mirror: " + currentdlc.foundMirror);
                });
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    using (System.IO.Stream streamRemote = client.OpenRead(url))
                    {
                        using (Stream streamLocal = new FileStream(sFilePathToWriteFileTo, FileMode.Create, FileAccess.Write, FileShare.None))
                        {

                            //loop the steam and get the file ino the byte buffer
                            int iByteSize = 0;
                            byte[] byteBuffer = new byte[iSize];

                            while ((iByteSize = streamRemote.Read(byteBuffer, 0, byteBuffer.Length)) > 0)
                            {
                                if (backgroundWorkerDownload.CancellationPending)
                                {
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        richTextBox1.AppendText("\n\n");
                                        progressBar.Value = 0;
                                    });
                                    return;
                                }
                                streamLocal.Write(byteBuffer, 0, iByteSize);
                                iRunningByteTotal += iByteSize;

                                //calculate progress out of a base 100
                                double dIndex = (double)(iRunningByteTotal);
                                double dTotal = (double)byteBuffer.Length;
                                double dProgressPercentage = (dIndex / dTotal);
                                int iProgressPercentage = (int)(dProgressPercentage * 100);

                                //update progress bar
                                backgroundWorkerDownload.ReportProgress(iProgressPercentage);


                            }
                            //clean up the file stream
                            streamLocal.Close();

                        }
                        //close the connection to the server
                        streamRemote.Close();
                        //did DLC update properly?
                        if (doExtract(currentdlc))
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                richTextBox1.AppendText(" - Finished  ");
                                if (currentdlc.notes != null && currentdlc.notes != "")
                                {

                                    richTextBox1.AppendText("Readme: " + currentdlc.notes + "\n");
                                }
                                else
                                {
                                    richTextBox1.AppendText("\n");
                                }
                            });

                            //update registry with UID and revision

                            RegistryKey dlcInfo = Registry.CurrentUser.OpenSubKey("Software\\VSDLC\\MapPack" + currentdlc.uid,true);
                            //if key does not exists we create it here
                            if (dlcInfo == null)
                            {
                                dlcInfo = Registry.CurrentUser.CreateSubKey("Software\\VSDLC\\MapPack" + currentdlc.uid);
                                dlcInfo.SetValue("revision", currentdlc.revision);
                                dlcInfo.Close();
                            }
                            else
                            {
                                try
                                {
                                    
                                    dlcInfo.SetValue("revision", currentdlc.revision);
                                    dlcInfo.Close();

                                }
                                catch (UnauthorizedAccessException ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }

                            }
                            
                            
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                richTextBox1.AppendText(" - Failed  \n");
                            });
                        }
                        
                    }

                }
            }
            else
            {
                this.Invoke((MethodInvoker)delegate
                {
                    richTextBox1.AppendText(" - No valid mirrors found \n");
                });
                return;
            }
            
            
        }





      



        //extract files
        private bool doExtract(dlc currentdlc)
        {
            try
            {
                FastZip fz = new FastZip();
                fz.ExtractZip(Path.GetTempPath() + currentdlc.filename, path + "\\data_win\\", "");
                return true;
            }
            catch (ZipException ex)
            {
                //MessageBox.Show("error extracting " + currentdlc.filename + " from path "
                    //+ Path.GetTempPath() + currentdlc.filename + " to path " + path + "\\data_win\\" + ex.Message);
                return false;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.TaskManagerClosing && e.CloseReason != CloseReason.WindowsShutDown && e.CloseReason != CloseReason.ApplicationExitCall)
            {
                e.Cancel = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
            }
            else
            {
                this.notifyIcon1.Visible = false;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this, "http://vengeful-spirit.com/dlc/tracker");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (checkBox1.Checked)
            {
                
                rkApp.SetValue("VSDLC", Application.ExecutablePath.ToString());
            }
            else
            {
                rkApp.DeleteValue("VSDLC", false);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            startOFP();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            
            

            startOFP();
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {

        }

        private void buttonChangePath_Click(object sender, EventArgs e)
        {
            handlePath(true);
            labelGamePath.Text = path;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            startME();
        }

        private void buttonMissionEditor_Click(object sender, EventArgs e)
        {
            startME();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Help.ShowHelp(this, "http://vengeful-spirit.com/");
        }


    }
}
