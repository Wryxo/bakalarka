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
using System.ServiceProcess;
using System.Threading;
using System.ComponentModel;

namespace UserApp
{
    class UserApp
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        //https://localhost:44300/
        const int SERVICE_INSTALL_PACKAGE = 200;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        static string package;
        static string executable;
        static string keyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\SetItUp";

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
             * Zisti ci treba obnovit shortcuty alebo nainstalovat balik
             */
            string installDir;
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    executable = args[1];
                }
                if (args.Length < 2 || !File.Exists(executable)) { 
                    installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
                    package = args[0];
                    // zapis balik ktory treba nainstalovat
                    File.WriteAllText(installDir + "Last.txt", package);
                    // zavolaj sluzbu a povedz je ze treba nainstalovat balik
                    ServiceController sc = new ServiceController("SetItUpService");
                    sc.ExecuteCommand(SERVICE_INSTALL_PACKAGE);
                    WaitForService(installDir);
                }
                    // ked sluzba nainstaluje balik a do parametru sme dostali .exe programu, tak ho spustime
                if (args.Length > 1)
                {
                    //executable = args[1];
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
                string serverDir;
                RegistryKey key;
                try
                {
                    key = Registry.LocalMachine.CreateSubKey("SOFTWARE");
                    key = key.CreateSubKey("SetItUp");
                    packageDir = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                    if (packageDir == "Not Exist")
                    {
                        FolderBrowserDialog aaa = new FolderBrowserDialog();
                        aaa.ShowDialog();
                        key = Registry.LocalMachine.OpenSubKey("Software", true);
                        key = key.CreateSubKey("SetItUp");
                        key.SetValue("packageDir", aaa.SelectedPath + "\\");
                        packageDir = (string)Registry.GetValue(keyName, "packageDir", "Not Exist");
                    }
                    shortcutDir = (string)Registry.GetValue(keyName, "shortcutDir", "Not Exist");
                    if (shortcutDir == "Not Exist")
                    {
                        FolderBrowserDialog aaa = new FolderBrowserDialog();
                        aaa.ShowDialog();
                        key = Registry.LocalMachine.OpenSubKey("Software", true);
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
                        key = Registry.LocalMachine.OpenSubKey("Software", true);
                        key = key.OpenSubKey("SetItUp", true);
                        key.SetValue("serverDir", aaa.getData());
                        serverDir = (string)Registry.GetValue(keyName, "serverDir", "Not Exist");
                    }
                    installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
                    if (installDir == "Not Exist")
                    {
                        key = Registry.LocalMachine.OpenSubKey("Software", true);
                        key = key.OpenSubKey("SetItUp", true);
                        key.SetValue("installDir", Application.StartupPath);
                        installDir = (string)Registry.GetValue(keyName, "installDir", "Not Exist");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Nastala chyba pri narabani s registrami " + ex.Message + Environment.NewLine);
                    Console.ReadKey();
                    return;
                }
            }
        }

        private static void WaitForService(string installDir)
        {
            Console.WriteLine("Instalujem balik");
            string ready = "";
            while (ready != "done")
            {
                Console.WriteLine("check " + DateTime.Now);
                ready = File.ReadAllText(installDir + "Last.txt");
                Thread.Sleep(1000);
            }
        }
    }
}
