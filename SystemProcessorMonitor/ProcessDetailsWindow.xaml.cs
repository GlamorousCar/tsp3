using NetworkMSystemProcessorMonitoranager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        private static System.Timers.Timer timer;

        private NetworkManager networkManager;
        private FileManager fileManager;
        private ResourceMonitor resourceMonitor;

        public ProcessDetailsWindow(Process process)
        {
            InitializeComponent();
            Title = $"Details for {process.ProcessName}";

            networkManager = new NetworkManager();
            fileManager = new FileManager();
            resourceMonitor = new ResourceMonitor(process);

            ProcessNameTextBlock.Text = process.ProcessName;

            StartTimer();

            ShowThreads(process);
            ShowOpenFiles(process.Id);

            ShowTCPNetworks(process.Id);
            ShowUDPNetworks(process.Id);    
        }

        private void StartTimer()
        {
            timer = new System.Timers.Timer(5000);
            timer.Elapsed += MonitorResource;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Start();
        }

        private void ShowThreads(Process process)
        {
            var threads = process.Threads;

            StringBuilder sb = new StringBuilder();
            foreach (ProcessThread thread in threads)
            {
                sb.Append($"Thread ID: {thread.Id}, Start Time: {thread.StartTime}\n");
            }
            ThreadsTextBlock.Text = sb.ToString();
        }

        private void ShowTCPNetworks(int pid)
        {
            var tcpConnections = networkManager.GetAllTcpConnections(pid);
            if (tcpConnections.Count > 0)
            {
                StringBuilder sbTCPConns = new StringBuilder();
                foreach (var tcpConn in tcpConnections)
                {
                    sbTCPConns.Append($"PID: {tcpConn.ProcessId}, Process Name: {tcpConn.ProcessName}, Local: {tcpConn.LocalAddress}:{tcpConn.LocalPort}, Remote: {tcpConn.RemoteAddress}:{tcpConn.RemotePort}\n");

                }
                TCPNetworkTextBlock.Text = sbTCPConns.ToString();
            }
        }

        private void ShowUDPNetworks(int pid) 
        {
            var udpConnections = networkManager.GetAllUdpConnections(pid);
            if (udpConnections.Count > 0)
            {
                StringBuilder sbUDPConns = new StringBuilder();
                foreach (var udpConn in udpConnections)
                {
                    sbUDPConns.Append($"PID: {udpConn.ProcessId}, Process Name: {udpConn.ProcessName}, Local: {udpConn.LocalAddress}:{udpConn.LocalPort}\n");
                }
                UDPNetworkTextBlock.Text = sbUDPConns.ToString();
            }
        }

        private void ShowOpenFiles(int processId)
        {
            FilesTextBlock.Text = fileManager.GetOpenFileInfo(processId);
        }

        private void MonitorResource(object sender, ElapsedEventArgs e)
        {
            string message = resourceMonitor.GetMonitorMessage();
            Dispatcher.Invoke(() =>
            {
                ResourceMonitoringListBox.Items.Add(message);
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            timer.Elapsed -= MonitorResource;
            timer.Stop();
        }
    }
}
