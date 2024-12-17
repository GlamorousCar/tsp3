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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace SystemProcessorMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {

        public ObservableCollection<ProcessInfo> Processes { get; set; } = new ObservableCollection<ProcessInfo>();
        private ProcessManager processManager;

        public MainWindow()
        {
            InitializeComponent();

            processManager = new ProcessManager();
            DataContext = this;
            RefreshProcesses();
        }

        private async void RefreshProcesses()
        {
            SpinnerGrid.Visibility = Visibility.Visible;
            Processes.Clear();
            var processInfos = await processManager.GetProcessInfos();
            foreach (var processInfo in processInfos.Where(p => p != null))
            {
                Processes.Add(processInfo);
            }
            SpinnerGrid.Visibility = Visibility.Collapsed;
        }

        private void EndProcess_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessList.SelectedItem is ProcessInfo selectedProcess)
            {
                try
                {
                    processManager.KillProcessByPID(selectedProcess.PID);
                    LogAction($"Process terminated: {selectedProcess.Name}");
                    RefreshProcesses();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to terminate process: {ex.Message}");
                }
            }
        }

        private void ChangePriority_Click(object sender, RoutedEventArgs e)
        {
            if (ProcessList.SelectedItem is ProcessInfo selectedProcess)
            {
                try
                {
                    var process = processManager.GetProcessByPID(selectedProcess.PID);
                    var selectedPriority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

                    if (selectedPriority != null)
                    {
                        switch (selectedPriority)
                        {
                            case "Low":
                                process.PriorityClass = ProcessPriorityClass.Idle;
                                break;
                            case "Below Normal":
                                process.PriorityClass = ProcessPriorityClass.BelowNormal;
                                break;
                            case "Normal":
                                process.PriorityClass = ProcessPriorityClass.Normal;
                                break;
                            case "Above Normal":
                                process.PriorityClass = ProcessPriorityClass.AboveNormal;
                                break;
                            case "High":
                                process.PriorityClass = ProcessPriorityClass.High;
                                break;
                            case "Realtime":
                                process.PriorityClass = ProcessPriorityClass.RealTime;
                                break;
                            default:
                                MessageBox.Show("Unknown priority selected.");
                                return;
                        }
                    }

                    LogAction($"Priority changed for: {selectedProcess.Name} to {selectedPriority}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to change priority: {ex.Message}");
                }
            }
        }

        private void LaunchProcess_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var processName = ProcessNameTextBox.Text;
                if (!string.IsNullOrWhiteSpace(processName))
                {
                    processManager.LaunchProcess(processName);
                    LogAction($"Process launched: {processName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch process: {ex.Message}");
            }
        }

        private void SearchProcess_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchTextBox.Text;
            if (!string.IsNullOrWhiteSpace(query))
            {
                var results = processManager.SearchProcessInfos(query, Processes.ToList());

                Processes.Clear();
                foreach (var process in results)
                {
                    Processes.Add(process);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshProcesses();
        }

        private void ProcessList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProcessList.SelectedItem is ProcessInfo selectedProcess)
            {
                try
                {
                    var process = Process.GetProcessById(selectedProcess.PID);
                    var detailsWindow = new ProcessDetailsWindow(process);
                    detailsWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to get process details: {ex.Message}");
                }
            }
        }

        private void LogAction(string message)
        {
            LogListBox.Items.Add($"[{DateTime.Now}] {message}");
        }
    }
}
