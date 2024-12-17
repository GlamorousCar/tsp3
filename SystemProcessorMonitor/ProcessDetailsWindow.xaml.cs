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
            var details = $"Process ID: {process.Id}\n" +
                          $"Name: {process.ProcessName}\n" +
                          $"Memory Usage: {process.WorkingSet64 / (1024 * 1024)} MB\n" +
                          $"Threads: {process.Threads.Count}\n" +
                          $"Start Time: {process.StartTime}";
            Content = new TextBlock
            {
                Text = details,
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };
        }
    }
}
