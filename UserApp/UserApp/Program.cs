using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

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

        static void Main(string[] args)
        {
            if (args.Length == 2) 
            {
                package = args[0];
                executable = args[1];
                if (File.Exists(executable)) 
                {
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
                } else {
                    String tmp = Directory.GetCurrentDirectory();
                    folderName = "D:\\Bakalar\\Administration\\Administration\\bin\\Release\\Packages";
                    folderPath = System.IO.Path.Combine(folderName, package);
                    if (System.IO.File.Exists(System.IO.Path.Combine(folderPath, package + ".txt")))
                    {
                        string[] lines = System.IO.File.ReadAllLines(System.IO.Path.Combine(folderPath, package + ".txt"));
                        foreach (string line in lines)
                        {
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
                        string filePath = System.IO.Path.Combine(folderPath, package + ".reg");
                        if (File.Exists(filePath)) ImportKey(filePath);
                        Console.WriteLine(package + " nainstalovany" + Environment.NewLine);
                        Console.ReadKey();
                    }
                    else
                    {
                        Console.WriteLine("Nenasiel som instalaciu pre " + package + Environment.NewLine);
                        Console.ReadKey();
                    }
                }
            }
            else
            {
                Console.WriteLine("Zle zadane argumenty"+ Environment.NewLine);
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
