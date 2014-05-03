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

namespace UserApp
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        static string folderName;
        static string folderPath;
        static string package;
        static string executable;
        static string keyName = @"HKEY_CURRENT_USER\Software\SetItUp";

        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, policy) =>
            {
                return true;
            };
            if (args.Length > 0) 
            {
                package = args[0];
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile("https://localhost:44300/"+package+".zip", "D:\\Bakalar\\Temp\\"+package+".zip");
                }
                folderName = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                ZipFile.ExtractToDirectory("D:\\Bakalar\\Temp\\" + package + ".zip", folderName);
                folderPath = System.IO.Path.Combine(folderName, package);
                if (System.IO.File.Exists(System.IO.Path.Combine(folderPath, package + ".txt")))
                {
                    string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(folderPath, package + ".txt"));
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
                                System.IO.File.Copy(System.IO.Path.Combine(folderPath, newPath), line, true);
                            }
                        }
                    }
                    string filePath = System.IO.Path.Combine(folderPath, package + ".reg");
                    if (File.Exists(filePath)) ImportKey(filePath);

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
                            proc = Process.Start(executable);

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
                /*Console.WriteLine("Zle zadane argumenty"+ Environment.NewLine);
                Console.ReadKey();*/
                string packageDir = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                if (packageDir == null) {
                    Console.WriteLine("kluc neexistuje, vytvaram novy");
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software",true);
                    key.CreateSubKey("SetItUp");
                    key = key.OpenSubKey("SetItUp", true);
                    key.SetValue("packageDir", "D:\\Bakalar\\Packages");
                    packageDir = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                }
                string shortcutDir = (string)Registry.GetValue(keyName, "shortcutDir", "Not Exist");
                if (shortcutDir == "Not Exist")
                {
                    Console.WriteLine("kluc neexistuje, vytvaram novy - Shortcut");
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                    key = key.OpenSubKey("SetItUp", true);
                    key.SetValue("shortcutDir", "D:\\Bakalar\\Shortcuts");
                    shortcutDir = (string)Registry.GetValue(keyName, "shortcutDir", "Not Exist");
                }
                string installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
                if (installDir == "Not Exist")
                {
                    Console.WriteLine("kluc neexistuje, vytvaram novy - Install");
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                    key = key.OpenSubKey("SetItUp", true);
                    key.SetValue("installDir", "C:\\Users\\Wryxo\\Documents\\GitHub\\bakalarka\\UserApp\\UserApp\\bin\\Debug\\UserApp.exe");
                    installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
                }
                int lastUpdate = (int)Registry.GetValue(keyName, "lastUpdate", -1337);
                if (lastUpdate == -1337)
                {
                    Console.WriteLine("kluc neexistuje, vytvaram novy - lastUpdate");
                    RegistryKey key = Registry.CurrentUser.OpenSubKey("Software", true);
                    key = key.OpenSubKey("SetItUp", true);
                    key.SetValue("lastUpdate", 1);
                    lastUpdate = (int)Registry.GetValue(keyName, "lastUpdate", -1337);
                }
                
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile("https://localhost:44300/packages.txt", packageDir+"\\packages.txt");
                }
                string[] packList = File.ReadAllLines(packageDir + "\\packages.txt");
                string package = "";
                string[] tmp;
                foreach (string line in packList)
                {
                    tmp = line.Split(' ');
                    if (line[0] == 'p')
                    {                
                        package = tmp[1];
                        if (line[1] == 'a')
                        {
                            tmp = line.Split(' ');
                            var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                            IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\" + package + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                            shortcut.Arguments = package;
                            shortcut.TargetPath = installDir;
                            shortcut.Save();
                        }
                    }
                    if (line[0] == 's')
                    {                       
                        var wsh = new IWshRuntimeLibrary.IWshShell_Class();
                        IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(shortcutDir + "\\" + tmp[1] + ".lnk") as IWshRuntimeLibrary.IWshShortcut;
                        shortcut.Arguments = package + " \"" + tmp[2] + "\"";
                        shortcut.TargetPath = installDir;
                        shortcut.Save();
                    }
                }
                Console.WriteLine("snad sa podarilo " + Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                Console.ReadKey();
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
