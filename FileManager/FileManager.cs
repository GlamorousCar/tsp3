using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemProcessorMonitor
{
    public class FileManager
    {
        public string GetOpenFileInfo(int pid)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "handle.exe",
                Arguments = $"-p {pid}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                return string.Join("\n", output.Split('\n').Skip(5));
            }

        }
    }
}
