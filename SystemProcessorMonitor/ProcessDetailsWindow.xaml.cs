using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SystemProcessorMonitor
{
    /// <summary>
    /// Interaction logic for ProcessDetailsWindow.xaml
    /// </summary>
    public partial class ProcessDetailsWindow : Window
    {
        public ProcessDetailsWindow(Process process)
        {
            InitializeComponent();
            Title = $"Details for {process.ProcessName}";
            ProcessNameTextBlock.Text = process.ProcessName;

            var threads = process.Threads;

            StringBuilder sb = new StringBuilder();
            foreach (ProcessThread thread in threads)
            {
                sb.Append($"Thread ID: {thread.Id}, Start Time: {thread.StartTime}\n");
            }
            ThreadsTextBlock.Text = sb.ToString();

            FilesTextBlock.Text = GetOpenFiles(process.Id);
        }

        private string GetOpenFiles(int processId)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "handle.exe",
                Arguments = $"-p {processId}",
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
