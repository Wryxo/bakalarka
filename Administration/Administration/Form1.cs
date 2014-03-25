using DifferenceEngine;
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
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Administration
{
    public partial class Form1 : Form
    {
        FileSystemWatcher watcher;
        FileSystemWatcher watcher2;
        System.IO.StreamWriter file;
        string folderName;
        string folderPath;
        string package;
        string[] banned = new string[] {"cache", "cookies", "temp", "tmp", "appdata"};

        public Form1()
        {
            String tmp = Directory.GetCurrentDirectory();
            folderName = tmp + "\\Packages";
            //folderName = "D:\\Bakalar\\Packages";
            if (!Directory.Exists(folderName)) System.IO.Directory.CreateDirectory(folderName);
            InitializeComponent();
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void startFileWatchers()
        {
            package = textBox3.Text;
            folderPath = System.IO.Path.Combine(folderName, package);
            System.IO.Directory.CreateDirectory(folderPath);
            if (checkBox1.Checked)
            {
                // Create a new FileSystemWatcher and set its properties.
                watcher = new FileSystemWatcher();
                watcher.Path = "C:\\";
                /* Watch for changes in LastAccess and LastWrite times, and
                   the renaming of files or directories. */
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch text files.
                //watcher.Filter = "*.txt";
                watcher.IncludeSubdirectories = true;

                // Add event handlers.
                watcher.Changed += new FileSystemEventHandler(OnChanged);
                watcher.Created += new FileSystemEventHandler(OnChanged);
                watcher.Deleted += new FileSystemEventHandler(OnChanged);
                watcher.Renamed += new RenamedEventHandler(OnRenamed);

                // Begin watching.
                watcher.EnableRaisingEvents = true; 
            }

            if (checkBox2.Checked)
            {
                watcher2 = new FileSystemWatcher();
                watcher2.Path = "D:\\";
                /* Watch for changes in LastAccess and LastWrite times, and
                   the renaming of files or directories. */
                watcher2.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                // Only watch text files.
                //watcher2.Filter = "*.txt";
                watcher2.IncludeSubdirectories = true;

                // Add event handlers.
                watcher2.Changed += new FileSystemEventHandler(OnChanged);
                watcher2.Created += new FileSystemEventHandler(OnChanged);
                watcher2.Deleted += new FileSystemEventHandler(OnChanged);
                watcher2.Renamed += new RenamedEventHandler(OnRenamed);

                // Begin watching.
                watcher2.EnableRaisingEvents = true; 
            }
        }

        // Define the event handlers. 
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed, created, or deleted.
            string[] words = e.FullPath.Split('\\');
            if (e.ChangeType == System.IO.WatcherChangeTypes.Created)
            {
                bool clean = true;
                foreach (string s in banned)
                {
                    if (e.FullPath.ToLower().Contains(s)) clean = false;
                }
                try
                {
                    if (clean && !words[words.Length - 1].Equals(package + ".txt") && !words[words.Length - 1].Equals("before.reg") && !words[words.Length - 1].Equals("after.reg") && File.Exists(e.FullPath))
                    {
                        writeToKonzole("File: " + e.FullPath + " " + e.ChangeType + Environment.NewLine);
                        file.WriteLine(e.FullPath);
                    }
                }
                catch (IOException)
                {

                }
            }
            if (e.ChangeType == System.IO.WatcherChangeTypes.Changed)
            {
                if (File.Exists(e.FullPath))
                {
                    bool nasiel = false;
                    bool clean = true;
                    file.Close();
                    string[] readText = File.ReadAllLines(folderPath + "\\" + package + ".txt");
                    file = new System.IO.StreamWriter(folderPath + "\\" + package + ".txt", true);
                    foreach (string s in readText) {
                        if (s.Equals(e.FullPath)) nasiel = true;
                    }
                    foreach (string s in banned)
                    {
                        if (e.FullPath.ToLower().Contains(s)) clean = false;
                    }
                    try
                    {
                        if (clean && !nasiel && !words[words.Length - 1].Equals(package + ".txt") && !words[words.Length - 1].Equals("before.reg") && !words[words.Length - 1].Equals("after.reg"))
                        {
                            writeToKonzole("File: " + e.FullPath + " " + e.ChangeType + Environment.NewLine);
                            file.WriteLine(e.FullPath);
                        }
                    }
                    catch (IOException)
                    {

                    }
                }    
            }
        }

        private void copyTrackedFiles()
        {
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages")) Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages");
            string[] lines = System.IO.File.ReadAllLines(folderPath + "\\" + package + ".txt");
            foreach (string line in lines)
            {
                string newPath = line.Substring(3);
                if (File.Exists(line)) { 
                    if (!Directory.Exists(Path.GetDirectoryName(System.IO.Path.Combine(folderPath, newPath))))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(System.IO.Path.Combine(folderPath, newPath)));
                    }
                    if (File.Exists(line))
                    {
                        if (line.Contains(".exe"))
                        {                            
                            string[] words = line.Split('\\');
                            Form2 dialog = new Form2();
                            dialog.StartPosition = FormStartPosition.CenterParent;
                            dialog.nazov(words[words.Length-1]);
                            if (dialog.ShowDialog(this) == DialogResult.OK)
                            {
                                writeToKonzole(dialog.shortcut() + Environment.NewLine);
                                var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                                IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(
                                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Packages\\" + dialog.shortcut() + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                                shortcut.Arguments = package + " \"" + line + "\"";
                                shortcut.TargetPath = @"D:\\Bakalar\\UserApp\\UserApp\\bin\\Debug\\UserApp.exe";
                                shortcut.Save();
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
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            bool clean = true;
            writeToKonzole("File: " + e.OldFullPath + " renamed to " + e.FullPath + Environment.NewLine);
            if (File.Exists(e.FullPath))
            {
                foreach (string s in banned)
                {
                    if (e.FullPath.ToLower().Contains(s)) clean = false;
                }
                if (clean) { 
                    file.Close();
                    string[] readText = File.ReadAllLines(folderPath + "\\" + package + ".txt");
                    for (int i = 0; i < readText.Length; i++)
                    {
                        if (readText[i].Equals(e.OldFullPath))
                        {
                            readText[i] = e.FullPath;
                        }
                        writeToKonzole(readText[i] + Environment.NewLine);
                    }
                    File.WriteAllLines(folderPath + "\\" + package + ".txt", readText);    
                    file = new System.IO.StreamWriter(folderPath + "\\" + package + ".txt", true);
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

        private void writeToKonzole(string s)
        {
            textBox1.BeginInvoke((MethodInvoker)(() => textBox1.AppendText(s)));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            startFileWatchers();
            file = new System.IO.StreamWriter(folderPath + "\\" + package + ".txt", true);
            ExportKey("HKEY_CURRENT_USER\\SOFTWARE", folderPath + "\\before.reg");
            writeToKonzole("Registre exportnute" + Environment.NewLine);
            button2.Enabled = true;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            file.Close();
            button2.Enabled = false;
            if (watcher != null) watcher.EnableRaisingEvents = false;
            if (watcher2 != null) watcher2.EnableRaisingEvents = false;
            copyTrackedFiles();
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

        private void Form1_Load(object sender, EventArgs e)
        {
        }
    }
}
