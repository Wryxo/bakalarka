using DifferenceEngine;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Administration
{
    public partial class Form1 : Form
    {
        List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        System.IO.StreamWriter file;
        Hashtable changes = new Hashtable();
        string folderName;
        string folderPath;
        string package;
        string instType;
        string keyName = @"HKEY_CURRENT_USER\Software\SetItUp";
        // slova ktore sa nemozu vyskytnut v ceste k suboru alebo v nazve suboru    
        string[] banned = new string[] {"cache", "cookies", "temp", "tmp", "appdata"};
        List<string> shortcuts = new List<string>();
        string[] packList;

        public Form1()
        {
            // server zatial nema certifikat, automaticky akceptujeme
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policy) =>
            {
                return true;
            };
            try { 
            folderName = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
            if (!Directory.Exists(folderName)) System.IO.Directory.CreateDirectory(folderName);
            }
            catch (Exception)
            {
                MessageBox.Show("Nastala chyba pri nacitani/vytvoreni zlozky pre balicky");
                return;
            }
            // stiahneme najnovsi zoznam balickov
            WebClient myWebClient = null;
            try  {
                myWebClient = new WebClient();
                myWebClient.DownloadFile("https://localhost:44300/packages.txt", folderName + "\\packages.txt");
                packList = File.ReadAllLines(folderName + "\\packages.txt");
            } 
            catch (Exception) 
            {
                MessageBox.Show("Nastala chyba pri stahovani zoznamu balickov");
            }
            finally
            {
                if (myWebClient != null) myWebClient.Dispose();   
            }   
            InitializeComponent();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void startFileWatchers()
        {
            package = textBox3.Text;
            folderPath = System.IO.Path.Combine(folderName, package);
            System.IO.Directory.CreateDirectory(folderPath);

            DriveInfo[] allDrives = DriveInfo.GetDrives();
            // pre kazdy pevny disk spustime watchera
            foreach (DriveInfo d in allDrives)
            {
                if (d.DriveType == DriveType.Fixed) 
                { 
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    
                    // rekurzivne pocuvame na danej ceste - disku
                    watcher.Path = d.Name;
                    watcher.IncludeSubdirectories = true;
                    
                    // pocuvame na zapis, zmenu mena alebo zmenu zlozky
                    watcher.NotifyFilter = NotifyFilters.LastWrite
                       | NotifyFilters.FileName | NotifyFilters.DirectoryName;

                    // pridame funkcie k listenerom
                    watcher.Changed += new FileSystemEventHandler(OnChanged);
                    watcher.Created += new FileSystemEventHandler(OnChanged);
                    watcher.Deleted += new FileSystemEventHandler(OnChanged);
                    watcher.Renamed += new RenamedEventHandler(OnRenamed);

                    watcher.EnableRaisingEvents = true;
                    watchers.Add(watcher);
                }
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            string[] words = e.FullPath.Split('\\');
            if (e.ChangeType == System.IO.WatcherChangeTypes.Created)
            {
                bool clean = true;
                // skontrolujeme ci neobsahuje zakazane slovo
                foreach (string s in banned)
                {
                    if (e.FullPath.ToLower().Contains(s)) clean = false;
                }
                // ak to nema zakazane slovo, existuje a nie je to subor ktory sme my vytvorili, zapiseme si ho
                if (clean && !words[words.Length - 1].Equals(package + ".txt") && !words[words.Length - 1].Equals("before.reg") && !words[words.Length - 1].Equals("after.reg") && File.Exists(e.FullPath))
                {
                    writeToKonzole("File: " + e.FullPath + " " + e.ChangeType + Environment.NewLine);
                    changes.Add(e.FullPath, e.FullPath);
                }
            } else if (e.ChangeType == System.IO.WatcherChangeTypes.Changed)
            {
                if (File.Exists(e.FullPath))
                {
                    bool nasiel = false;
                    bool clean = true;
                    // skontrolujeme ci neobsahuje zakazane slovo
                    foreach (string s in banned)
                    {
                        if (e.FullPath.ToLower().Contains(s)) clean = false;
                    }
                    // ak to nema zakazane slovo, existuje a nie je to subor ktory sme my vytvorili, zapiseme si ho
                    if (clean && !nasiel && !words[words.Length - 1].Equals(package + ".txt") && !words[words.Length - 1].Equals("before.reg") && !words[words.Length - 1].Equals("after.reg"))
                    {
                        writeToKonzole("File: " + e.FullPath + " " + e.ChangeType + Environment.NewLine);
                        if (!changes.Contains(e.FullPath)) changes.Add(e.FullPath, e.FullPath);
                    }
                }    
            }
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            bool clean = true;
            if (File.Exists(e.FullPath))
            {
                foreach (string s in banned)
                {
                    if (e.FullPath.ToLower().Contains(s)) clean = false;
                }
                if (clean)
                {
                    writeToKonzole("File: " + e.OldFullPath + " renamed to " + e.FullPath + Environment.NewLine);
                    if (changes.Contains(e.OldFullPath)) changes.Remove(e.OldFullPath);
                    if (!changes.Contains(e.FullPath)) changes.Add(e.FullPath, e.FullPath);
                }
            }
        }

        private void copyTrackedFiles()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages")) Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages");
            foreach (DictionaryEntry de in changes)
            {
                string line = (string)de.Value;
                string newPath = line.Substring(3);
                // prekopirujeme vsetky subory co bolo vytvorene alebo zmenene
                if (File.Exists(line)) { 
                    if (!Directory.Exists(Path.GetDirectoryName(System.IO.Path.Combine(folderPath, newPath))))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(System.IO.Path.Combine(folderPath, newPath)));
                    }
                        // ak je dany subor spustitelny, opytame sa ci nan treba spravit odkaz
                        if (line.Contains(".exe"))
                        {                            
                            string[] words = line.Split('\\');
                            ExecutableDialog dialog = new ExecutableDialog();
                            dialog.StartPosition = FormStartPosition.CenterParent;
                            dialog.nazov(words[words.Length-1]);
                            if (dialog.ShowDialog(this) == DialogResult.OK)
                            {
                                shortcuts.Add(dialog.shortcut() + " " + line);
                            }
                            else
                            {
                                writeToKonzole("Cancelled" + Environment.NewLine);
                            }
                        }
                        System.IO.File.Copy(line, System.IO.Path.Combine(folderPath, newPath), true);
                }
            }
        }

        public void ExportKey(string RegKey, string SavePath)
        {
            string path = "\"" + SavePath + "\"";
            string key = "\"" + RegKey + "\"";

            var proc = new Process();
            try
            {
                proc.StartInfo.FileName = "regedit.exe";
                proc.StartInfo.UseShellExecute = false;
                proc = Process.Start("regedit.exe", "/e " + path + " " + key + "");

                if (proc != null) proc.WaitForExit();
            }
            finally
            {
                if (proc != null) proc.Dispose();
            }

        }

        public void ImportKey(string SavePath)
        {
            string path = "\"" + SavePath + "\"";

            var proc = new Process();
            try
            {
                proc.StartInfo.FileName = "regedit.exe";
                proc.StartInfo.UseShellExecute = false;
                proc = Process.Start("regedit.exe", path + "");

                if (proc != null) proc.WaitForExit();
            }
            finally
            {
                if (proc != null) proc.Dispose();
            }

        }

        private string[] getResults(DiffList_TextFile destination, ArrayList DiffLines)
        {
            int i, j=1;
            string[] res = new string[4000];
            res[0] = "Windows Registry Editor Version 5.00";
            foreach (DiffResultSpan drs in DiffLines)
            {
                switch (drs.Status)
                {
                    case DiffResultSpanStatus.AddDestination:
                        for (i = 0; i < drs.Length; i++)
                        {
                            res[j] = ((TextLine)destination.GetByIndex(drs.DestIndex + i)).Line.ToString();
                            j++;
                        }
                        break;
                }
            }
            if (j == 1) return null;
            return res;
        }

        private void TextDiff(string sFile, string dFile)
        {
            DiffList_TextFile sLF = null;
            DiffList_TextFile dLF = null;
            try
            {
                sLF = new DiffList_TextFile(sFile);
                dLF = new DiffList_TextFile(dFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File Error");
                return;
            }

            try
            {
                double time = 0;
                DiffEngine de = new DiffEngine();
                time = de.ProcessDiff(sLF, dLF, DiffEngineLevel.FastImperfect);
                ArrayList rep = de.DiffReport();
                string[] res = getResults(dLF, rep);
                if (res != null) File.WriteAllLines(folderPath + "\\" + package + ".reg", res);
            }
            catch (Exception ex)
            {
                string tmp = string.Format("{0}{1}{1}***STACK***{1}{2}",
                    ex.Message,
                    Environment.NewLine,
                    ex.StackTrace);
                MessageBox.Show(tmp, "Compare Error");
                return;
            }
        }

        // Vypis do textboxu co sluzi ako konzola
        private void writeToKonzole(string s)
        {
            textBox1.BeginInvoke((MethodInvoker)(() => textBox1.AppendText(s)));
        }

        // Start Listen
        private void button1_Click(object sender, EventArgs e)
        {
            changes.Clear();
            shortcuts.Clear();
            button1.Enabled = false;
            try 
            { 
                startFileWatchers();
                ExportKey("HKEY_CURRENT_USER\\SOFTWARE", folderPath + "\\before.reg");
                writeToKonzole("Registre exportnute" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastala chyba v spusteni odchytavania " + ex.Message);
            }       
            button2.Enabled = true;
        }

        // Stop Listen
        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            foreach (FileSystemWatcher watcher in watchers) {
                watcher.EnableRaisingEvents = false;
            }
            // spytame sa na zavislosti
            DepedenciesDialog dependDialog = new DepedenciesDialog();
            dependDialog.addData(packList);
            dependDialog.StartPosition = FormStartPosition.CenterParent;
            if (dependDialog.ShowDialog(this) == DialogResult.OK)
            {
                try 
                { 
                    File.WriteAllLines(folderPath + "\\depedencies.txt", dependDialog.getData());
                } 
                catch (Exception ex)
                {
                    MessageBox.Show("Nastala chyba pri zapise zavislosti " + ex.Message);
                }
            }
            // spytame sa na typ instalacie na vyziadanie alebo po starte
            InstallTypeDialog instTypeDialog = new InstallTypeDialog();
            instTypeDialog.StartPosition = FormStartPosition.CenterParent;
            if (instTypeDialog.ShowDialog(this) == DialogResult.OK)
            {
                instType = "m";
            }
            else
            {
                instType = "a";
            }
            // odkopirujeme subory
            try
            {
                copyTrackedFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nastala chyba pri kopirovani suborov " + ex.Message);
            }
            // zapiseme zmeny ktore balik spravil
            try
            { 
                file = new System.IO.StreamWriter(folderPath + "\\" + package + ".txt", false);
                foreach (DictionaryEntry de in changes)
                {
                    file.WriteLine(de.Value);
                }
                file.Close();
            }
            catch (IOException)
            {
                MessageBox.Show("Nastala chyba v zapise do zoznamu suborov");
            }
            // pridame balik medzi zoznam balikov
            try
            {
                file = new System.IO.StreamWriter(folderName + "\\packages.txt", true);
                file.WriteLine("p"+ instType + " " + package);
                foreach (string sc in shortcuts)
                {
                    file.WriteLine("s " + sc);
                }
                file.Close();
            }
            catch (IOException)
            {
                MessageBox.Show("Nastala chyba v zapise do zoznamu balickov");
            }
            // spravime rozdiel registrov a vymazeme docasne subory
            writeToKonzole("Subory odkopirovane" + Environment.NewLine);
            ExportKey("HKEY_CURRENT_USER\\SOFTWARE", folderPath + "\\after.reg");
            writeToKonzole("Registre exportnute" + Environment.NewLine);
            TextDiff(folderPath + "\\before.reg", folderPath + "\\after.reg");
            writeToKonzole("Registre diffnute" + Environment.NewLine);
            File.Delete(folderPath + "\\after.reg");
            File.Delete(folderPath + "\\before.reg");
            button1.Enabled = true;
            writeToKonzole(package + " zaznamenany" + Environment.NewLine);
        }
    }
}
