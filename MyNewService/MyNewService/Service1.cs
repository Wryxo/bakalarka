using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SetItUpService
{
    public partial class SetItUpService : ServiceBase
    {

        private string keyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\SetItUp";

        public SetItUpService()
        {
            InitializeComponent();
            if (!System.Diagnostics.EventLog.SourceExists("SetItUp"))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    "SetItUp", "DebugLog");
            }
            eventLog1.Source = "SetItUp";
            eventLog1.Log = "DebugLog";
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry("Spustam sluzbu");
            Task.Run(() => InitShortcuts());
        }

        private void InitShortcuts()
        {
            string installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
            string packageDir = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
            string shortcutDir = (string)Registry.GetValue(keyName, "shortcutDir", "Not Exist");
            /*try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policy) =>
                {
                    return true;
                };
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry(ex.Message);
            }*/
            WebClient myWebClient = new WebClient();
            try
            {
                string serverDir = (string)Registry.GetValue(keyName, "serverDir", "Not Exist");
                myWebClient.DownloadFile(serverDir + "/packages.txt", packageDir + "\\packages.txt");
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry("Nastala chyba pri stahovani balicka " + ex.Message);
                return;
            }
            finally
            {
                if (myWebClient != null) myWebClient.Dispose();
            }

            
            string[] packList = File.ReadAllLines(packageDir + "\\packages.txt");
            string package = "";
            string path;
            string[] tmp;
                
            // pre kazdy balik spravime shortcut               
            foreach (string line in packList)
            {
                //tmp = line.Split('~');
                tmp = line.Split(new string[] { "~~" }, StringSplitOptions.None);
                if (line[0] == 'p')
                {
                    package = tmp[1];
                    if (line[1] == 'a')
                    {
                        //tmp = line.Split(' ');
                        path = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + package + ".lnk";
                        var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                        IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + package + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                        shortcut.Arguments = package;
                        shortcut.TargetPath = installDir;
                        shortcut.Save();
                    }
                }
                if (line[0] == 's')
                {
                    path = shortcutDir + "\\" + tmp[1] + ".lnk";
                    var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                    IWshRuntimeLibrary.IWshShortcut shortcut = 
                        wsh.CreateShortcut(shortcutDir + "\\" + tmp[1] + ".lnk")
                        as IWshRuntimeLibrary.IWshShortcut;
                    shortcut.Arguments = package + " \"" + tmp[2] + "\"";
                    shortcut.TargetPath = installDir + "UserApp.exe";
                    shortcut.Save();
                }
            }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("Zastavujem sluzbu");
        }

        protected override void OnCustomCommand(int command)
        {
            if (command == 200) Task.Run(() => InstallPackage());
        }

        private void InstallPackage()
        {
            Queue<string> fronta = new Queue<string>();
            string installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
            string folderName = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
            if (folderName == "Not Exist")
            {
                eventLog1.WriteEntry("Nenasiel som zlozku pre balicky" + Environment.NewLine);
                return;
            }
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            string[] fileContent = File.ReadAllLines(installDir + "Last.txt");
            string package = "";
            fronta.Enqueue(fileContent[0]);
            while (fronta.Count != 0) {
                package = fronta.Dequeue();
                eventLog1.WriteEntry("Instalujem balik " + package);
                //stiahni a rozbal dany balicek
                if (!File.Exists(folderName + "\\" + package + ".zip"))
                {
                    /*try
                    {
                        ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policy) =>
                        {
                            return true;
                        };
                    }
                    catch (Exception ex)
                    {
                        eventLog1.WriteEntry(ex.Message);
                    }*/
                    WebClient myWebClient = new WebClient();
                    try
                    {
                        string serverDir = (string)Registry.GetValue(keyName, "serverDir", "Not Exist"); 
                        myWebClient.DownloadFile(serverDir + package + ".zip", folderName + "\\" + package + ".zip");
                    }
                    catch (Exception ex)
                    {
                        eventLog1.WriteEntry("Nastala chyba pri stahovani balicka " + ex.Message);
                        return;
                    }
                    finally
                    {
                        if (myWebClient != null) myWebClient.Dispose();
                    }
                }
                string folderPath = System.IO.Path.Combine(folderName, package);
                try
                {
                    if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
                    ZipFile.ExtractToDirectory(folderName + "\\" + package + ".zip", folderName);
                }
                catch (Exception ex)
                {
                    eventLog1.WriteEntry("Nastala chyba pri rozbaleni archivu " + ex.Message);
                    return;
                }
                
                if (System.IO.File.Exists(System.IO.Path.Combine(folderPath, package + ".txt")))
                {
                    string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(folderPath, package + ".txt"));
                    foreach (string line in lines)
                    {
                        if (!File.Exists(line))
                        {
                            string newPath = line.Substring(3);
                            if (File.Exists(System.IO.Path.Combine(folderPath, newPath)))
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(line)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(line));
                                }
                                try
                                {
                                    System.IO.File.Copy(System.IO.Path.Combine(folderPath, newPath), line, true);
                                }
                                catch (Exception ex)
                                {
                                    eventLog1.WriteEntry("Nastala chyba pri kopirovani suboru " + ex.Message);
                                    return;
                                }
                            }
                        }
                    }
                    eventLog1.WriteEntry("Importujem registry");
                    string filePath = System.IO.Path.Combine(folderPath, package + ".reg");
                    if (File.Exists(filePath)) ImportKey(filePath);
                }
                else
                {
                    eventLog1.WriteEntry("Nenasiel som instalaciu pre " + package);
                }
                eventLog1.WriteEntry("Pridavam potrebne baliky do fronty");
                try { 
                string[] depedencies = File.ReadAllLines(System.IO.Path.Combine(folderPath,"depedencies.txt"));
                foreach (string tmp in depedencies)  if (tmp != "") fronta.Enqueue(tmp);
                }
                catch (Exception ex)
                {
                    eventLog1.WriteEntry("Chyba pri citani depedencies " + ex.Message);
                }
                
                if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
                eventLog1.WriteEntry("Instalacia baliku " + package + " dokoncena");
            }
            File.WriteAllText(installDir + "Last.txt", "done");
        }

        public static void ImportKey(string SavePath)
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
    }
}
