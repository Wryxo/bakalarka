using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;
using Microsoft.Win32;
using System.Security.Permissions;
using System.IO.Compression;
using System.Windows.Forms;
using System.Security.AccessControl;
using System.Security.Principal;

namespace UserApp
{
    class UserApp
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        //https://localhost:44300/
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        static string folderName;
        static string folderPath;
        static string package;
        static string executable;
        static string keyName = @"HKEY_CURRENT_USER\Software\SetItUp";
        static WebClient myWebClient;

        [STAThread]
        static void Main(string[] args)
        {
            try 
            { 
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policy) =>
                {
                    return true;
                };
            } 
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            /*
             * Zisti ci treba spravit update alebo nainstalovat balik
             */
            if (args.Length > 0) 
            {
                package = args[0];
                folderName = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                if (folderName == "Not Exist")
                {
                    Console.WriteLine("Nenasiel som zlozku pre balicky" + Environment.NewLine);
                    Console.ReadKey();
                    return;
                }
                //stiahni a rozbal dany balicek
                using (WebClient myWebClient = new WebClient())
                {
                    string serverDir = (string)Registry.GetValue(keyName, "serverDir", "Not Exist");
                    myWebClient.DownloadFile(serverDir+package+".zip", folderName+"Temp\\"+package+".zip");
                }
                try 
                { 
                    ZipFile.ExtractToDirectory(folderName+"Temp\\" + package + ".zip", folderName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nastala chyba pri rozbaleni archivu " + ex.Message + Environment.NewLine);
                    Console.ReadKey();
                    return;
                }
                folderPath = System.IO.Path.Combine(folderName, package);
                if (System.IO.File.Exists(System.IO.Path.Combine(folderPath, package + ".txt")))
                {
                    string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(folderPath, package + ".txt"));
                    // nakopiruj vsetky subory zo zoznamu na ich miesto
                    foreach (string line in lines)
                    {
                        if (!File.Exists(line)) { 
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
                                    Console.WriteLine("Nastala chyba pri kopirovani suboru " + ex.Message + Environment.NewLine);
                                    Console.ReadKey();
                                    return;
                                }
                            }
                        }
                    }
                    // vloz registry
                    string filePath = System.IO.Path.Combine(folderPath, package + ".reg");
                    if (File.Exists(filePath)) ImportKey(filePath);

                    // ak sme dostali do parametru .exe programu, tak ho spustime
                    if (args.Length == 2) { 
                        executable = args[1];

                        var handle = GetConsoleWindow();

                        // Hide
                        ShowWindow(handle, SW_HIDE);
                        var proc = new Process();
                        try
                        {
                            proc.StartInfo.FileName = executable;
                            //proc.StartInfo.UseShellExecute = false;
                            try 
                            { 
                                proc = Process.Start(executable);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Nastala chyba pri spusteni programu " + ex.Message + Environment.NewLine);
                                Console.ReadKey();
                                return;
                            }

                            if (proc != null) proc.WaitForExit();
                        }
                        finally
                        {
                            if (proc != null) proc.Dispose();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Nenasiel som instalaciu pre " + package + Environment.NewLine); 
                    Console.ReadKey();
                }
            }
            else
            {
                // ideme updatovat, skontrolujeme/nastavime registry
                string packageDir;
                string shortcutDir;
                string installDir;
                string serverDir;
                try { 
                RegistryKey keyy = Registry.CurrentUser.OpenSubKey("Software", true);
                keyy = keyy.OpenSubKey("SetItUp", true);
                if (keyy == null) keyy.CreateSubKey("SetItUp");
                    } catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                try 
                { 
                    packageDir = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                    if (packageDir == "Not Exist")
                    {
                        FolderBrowserDialog aaa = new FolderBrowserDialog();
                        aaa.ShowDialog();
                        RegistryKey key = Registry.CurrentUser.OpenSubKey("Software",true);
                        key = key.OpenSubKey("SetItUp", true);
                        key.SetValue("packageDir", aaa.SelectedPath + "\\");
                        packageDir = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                    }
                    shortcutDir = (string)Registry.GetValue(keyName, "shortcutDir", "Not Exist");
                    if (shortcutDir == "Not Exist")
                    {
                        FolderBrowserDialog aaa = new FolderBrowserDialog();
                        aaa.ShowDialog();
                        RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                        key = key.OpenSubKey("SetItUp", true);
                        key.SetValue("shortcutDir", aaa.SelectedPath + "\\");
                        shortcutDir = (string)Registry.GetValue(keyName, "shortcutDir", "Not Exist");
                    }
                    serverDir = (string)Registry.GetValue(keyName, "serverDir", "Not Exist");
                    if (serverDir == "Not Exist")
                    {
                        Form1 aaa = new Form1();
                        aaa.StartPosition = FormStartPosition.CenterParent;
                        aaa.ShowDialog();
                        RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                        key = key.OpenSubKey("SetItUp", true);
                        key.SetValue("serverDir", aaa.getData());
                        serverDir = (string)Registry.GetValue(keyName, "serverDir", "Not Exist");
                    }
                    installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
                    if (installDir == "Not Exist")
                    {
                        RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                        key = key.OpenSubKey("SetItUp", true);
                        key.SetValue("installDir", Application.StartupPath+"\\UserApp.exe");
                        installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nastala chyba pri narabani s registrami " + ex.Message + Environment.NewLine);
                    Console.ReadKey();
                    return;
                }
                // stiahneme novy zoznam balickov
                try
                {
                    myWebClient = new WebClient();
                    myWebClient.DownloadFile("https://localhost:44300/packages.txt", packageDir + "\\packages.txt");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Nastala chyba pri stahovani zoznamu balickov");
                    return;
                }
                finally
                {
                    if (myWebClient != null) myWebClient.Dispose();
                }
                string[] packList = File.ReadAllLines(packageDir + "\\packages.txt");
                string package = "";
                string[] tmp;
                string computerName = System.Environment.MachineName;
                string account = Environment.GetEnvironmentVariable("UserName");
                WindowsIdentity mkds = WindowsIdentity.GetCurrent();
                string userName = WindowsIdentity.GetCurrent().Name;
                string path;
                MessageBox.Show(account + " " + userName + " " + mkds.Owner);
                MessageBox.Show(mkds.Actor + " " + mkds.User);
                // pre kazdy balik spravime shortcut               
                foreach (string line in packList)
                {
                    tmp = line.Split(' ');
                    if (line[0] == 'p')
                    {                
                        package = tmp[1];
                        if (line[1] == 'a')
                        {
                            //tmp = line.Split(' ');
                            path = Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + package + ".lnk";
                            var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                            IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + package + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                            shortcut.Arguments =  "/user:" + computerName + "\\AdminTest /savecred \"" + installDir + "\" " + package;
                            shortcut.TargetPath = "C:\\Windows\\System32\\runas.exe";
                            shortcut.Save();
                            FileSecurity fs = File.GetAccessControl(path);
                            fs.AddAccessRule(new FileSystemAccessRule(userName, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                            fs.AddAccessRule(new FileSystemAccessRule(@"Wryxo-PC\AdminTest", FileSystemRights.FullControl, AccessControlType.Allow));
                            fs.SetAccessRuleProtection(true, false);
                            File.SetAccessControl(path, fs);
                        }
                    }
                    if (line[0] == 's')
                    {
                        path = shortcutDir + "\\" + tmp[1] + ".lnk";
                        var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                        IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(shortcutDir + "\\" + tmp[1] + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                        shortcut.Arguments = "/user:Wryxo-PC\\AdminTest /savecred \"" + installDir + "\" " + package + " \"" + tmp[2] + "\"";
                        shortcut.TargetPath = "C:\\Windows\\System32\\runas.exe";
                        shortcut.Save();                    
                        FileSecurity fs = File.GetAccessControl(path);
                        fs.AddAccessRule(new FileSystemAccessRule(@"Wryxo-PC\wryxsk@gmail.com", FileSystemRights.FullControl, AccessControlType.Allow));
                        fs.AddAccessRule(new FileSystemAccessRule(userName, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
                        fs.SetAccessRuleProtection(true, false);
                        File.SetAccessControl(path, fs);
                    }
                }
            }
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
