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
    /// 

    // Enum for protocol types.
    public enum Protocol
    {
        TCP,
        UDP
    }

    // Enum to define the set of values used to indicate the type of table returned by 
    // calls made to the function 'GetExtendedTcpTable'.
    public enum TcpTableClass
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    // Enum to define the set of values used to indicate the type of table returned by calls
    // made to the function GetExtendedUdpTable.
    public enum UdpTableClass
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    // Enum for different possible states of TCP connection
    public enum MibTcpState
    {
        CLOSED = 1,
        LISTENING = 2,
        SYN_SENT = 3,
        SYN_RCVD = 4,
        ESTABLISHED = 5,
        FIN_WAIT1 = 6,
        FIN_WAIT2 = 7,
        CLOSE_WAIT = 8,
        CLOSING = 9,
        LAST_ACK = 10,
        TIME_WAIT = 11,
        DELETE_TCB = 12,
        NONE = 0
    }

    /// <summary>
    /// This class provides access an IPv4 TCP connection addresses and ports and its
    /// associated Process IDs and names.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class TcpProcessRecord
    {
        [DisplayName("Local Address")]
        public IPAddress LocalAddress { get; set; }
        [DisplayName("Local Port")]
        public ushort LocalPort { get; set; }
        [DisplayName("Remote Address")]
        public IPAddress RemoteAddress { get; set; }
        [DisplayName("Remote Port")]
        public ushort RemotePort { get; set; }
        [DisplayName("State")]
        public MibTcpState State { get; set; }
        [DisplayName("Process ID")]
        public int ProcessId { get; set; }
        [DisplayName("Process Name")]
        public string ProcessName { get; set; }

        public TcpProcessRecord(IPAddress localIp, IPAddress remoteIp, ushort localPort,
            ushort remotePort, int pId, MibTcpState state)
        {
            LocalAddress = localIp;
            RemoteAddress = remoteIp;
            LocalPort = localPort;
            RemotePort = remotePort;
            State = state;
            ProcessId = pId;
            // Getting the process name associated with a process id.
            if (Process.GetProcesses().Any(process => process.Id == pId))
            {
                ProcessName = Process.GetProcessById(ProcessId).ProcessName;
            }
        }
    }

    /// <summary>
    /// The structure contains information that describes an IPv4 TCP connection with 
    /// IPv4 addresses, ports used by the TCP connection, and the specific process ID
    /// (PID) associated with connection.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public MibTcpState state;
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public uint remoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] remotePort;
        public int owningPid;
    }

    /// <summary>
    /// The structure contains an entry from the User Datagram Protocol (UDP) listener
    /// table for IPv4 on the local computer. The entry also includes the process ID
    /// (PID) that issued the call to the bind function for the UDP endpoint.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public int owningPid;
    }

    /// <summary>
    /// The structure contains the User Datagram Protocol (UDP) listener table for IPv4
    /// on the local computer. The table also includes the process ID (PID) that issued
    /// the call to the bind function for each UDP endpoint.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct,
            SizeConst = 1)]
        public UdpProcessRecord[] table;
    }

    /// <summary>
    /// The structure contains a table of process IDs (PIDs) and the IPv4 TCP links that 
    /// are context bound to these PIDs.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct,
            SizeConst = 1)]
        public MIB_TCPROW_OWNER_PID[] table;
    }

    /// <summary>
    /// This class provides access an IPv4 UDP connection addresses and ports and its
    /// associated Process IDs and names.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class UdpProcessRecord
    {
        [DisplayName("Local Address")]
        public IPAddress LocalAddress { get; set; }
        [DisplayName("Local Port")]
        public uint LocalPort { get; set; }
        [DisplayName("Process ID")]
        public int ProcessId { get; set; }
        [DisplayName("Process Name")]
        public string ProcessName { get; set; }

        public UdpProcessRecord(IPAddress localAddress, uint localPort, int pId)
        {
            LocalAddress = localAddress;
            LocalPort = localPort;
            ProcessId = pId;
            if (Process.GetProcesses().Any(process => process.Id == pId))
                ProcessName = Process.GetProcessById(ProcessId).ProcessName;
        }
    }


    public class NetworkUtils
    {
        // The version of IP used by the TCP/UDP endpoint. AF_INET is used for IPv4.
        private const int AF_INET = 2;
        // List of Active TCP Connections.
        private static List<TcpProcessRecord> TcpActiveConnections = null;
        // List of Active UDP Connections.
        private static List<UdpProcessRecord> UdpActiveConnections = null;

        // The GetExtendedTcpTable function retrieves a table that contains a list of
        // TCP endpoints available to the application. Decorating the function with
        // DllImport attribute indicates that the attributed method is exposed by an
        // unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point.
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize,
            bool bOrder, int ulAf, TcpTableClass tableClass, uint reserved = 0);

        // The GetExtendedUdpTable function retrieves a table that contains a list of
        // UDP endpoints available to the application. Decorating the function with
        // DllImport attribute indicates that the attributed method is exposed by an
        // unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point.
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize,
            bool bOrder, int ulAf, UdpTableClass tableClass, uint reserved = 0);

        /// <summary>
        /// This function reads and parses the active TCP socket connections available
        /// and stores them in a list.
        /// </summary>
        /// <returns>
        /// It returns the current set of TCP socket connections which are active.
        /// </returns>
        /// <exception cref="OutOfMemoryException">
        /// This exception may be thrown by the function Marshal.AllocHGlobal when there
        /// is insufficient memory to satisfy the request.
        /// </exception>
        public static List<TcpProcessRecord> GetAllTcpConnections(int pid)
        {
            int bufferSize = 0;
            List<TcpProcessRecord> tcpTableRecords = new List<TcpProcessRecord>();

            // Getting the size of TCP table, that is returned in 'bufferSize' variable.
            uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET,
                TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

            // Allocating memory from the unmanaged memory of the process by using the
            // specified number of bytes in 'bufferSize' variable.
            IntPtr tcpTableRecordsPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                // The size of the table returned in 'bufferSize' variable in previous
                // call must be used in this subsequent call to 'GetExtendedTcpTable'
                // function in order to successfully retrieve the table.
                result = GetExtendedTcpTable(tcpTableRecordsPtr, ref bufferSize, true,
                    AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

                // Non-zero value represent the function 'GetExtendedTcpTable' failed,
                // hence empty list is returned to the caller function.
                if (result != 0)
                    return new List<TcpProcessRecord>();

                // Marshals data from an unmanaged block of memory to a newly allocated
                // managed object 'tcpRecordsTable' of type 'MIB_TCPTABLE_OWNER_PID'
                // to get number of entries of the specified TCP table structure.
                MIB_TCPTABLE_OWNER_PID tcpRecordsTable = (MIB_TCPTABLE_OWNER_PID)
                                        Marshal.PtrToStructure(tcpTableRecordsPtr,
                                        typeof(MIB_TCPTABLE_OWNER_PID));
                IntPtr tableRowPtr = (IntPtr)((long)tcpTableRecordsPtr +
                                        Marshal.SizeOf(tcpRecordsTable.dwNumEntries));

                // Reading and parsing the TCP records one by one from the table and
                // storing them in a list of 'TcpProcessRecord' structure type objects.
                for (int row = 0; row < tcpRecordsTable.dwNumEntries; row++)
                {
                    MIB_TCPROW_OWNER_PID tcpRow = (MIB_TCPROW_OWNER_PID)Marshal.
                        PtrToStructure(tableRowPtr, typeof(MIB_TCPROW_OWNER_PID));
                    if (tcpRow.owningPid == pid)
                    {
                        tcpTableRecords.Add(new TcpProcessRecord(
                        new IPAddress(tcpRow.localAddr),
                        new IPAddress(tcpRow.remoteAddr),
                        BitConverter.ToUInt16(new byte[2] {
                                                tcpRow.localPort[1],
                                                tcpRow.localPort[0] }, 0),
                        BitConverter.ToUInt16(new byte[2] {
                                                tcpRow.remotePort[1],
                                                tcpRow.remotePort[0] }, 0),
                        tcpRow.owningPid, tcpRow.state));
                    }
                    tableRowPtr = (IntPtr)((long)tableRowPtr + Marshal.SizeOf(tcpRow));
                }
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                /*MessageBox.Show(outOfMemoryException.Message, "Out Of Memory",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);*/
            }
            catch (Exception exception)
            {
                /*MessageBox.Show(exception.Message, "Exception",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);*/
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTableRecordsPtr);
            }
            return tcpTableRecords != null ? tcpTableRecords.Distinct()
                .ToList<TcpProcessRecord>() : new List<TcpProcessRecord>();
        }

        /// <summary>
        /// This function reads and parses the active UDP socket connections available
        /// and stores them in a list.
        /// </summary>
        /// <returns>
        /// It returns the current set of UDP socket connections which are active.
        /// </returns>
        /// <exception cref="OutOfMemoryException">
        /// This exception may be thrown by the function Marshal.AllocHGlobal when there
        /// is insufficient memory to satisfy the request.
        /// </exception>
        public static List<UdpProcessRecord> GetAllUdpConnections(int pid)
        {
            int bufferSize = 0;
            List<UdpProcessRecord> udpTableRecords = new List<UdpProcessRecord>();

            // Getting the size of UDP table, that is returned in 'bufferSize' variable.
            uint result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true,
                AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

            // Allocating memory from the unmanaged memory of the process by using the
            // specified number of bytes in 'bufferSize' variable.
            IntPtr udpTableRecordPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                // The size of the table returned in 'bufferSize' variable in previous
                // call must be used in this subsequent call to 'GetExtendedUdpTable'
                // function in order to successfully retrieve the table.
                result = GetExtendedUdpTable(udpTableRecordPtr, ref bufferSize, true,
                    AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

                // Non-zero value represent the function 'GetExtendedUdpTable' failed,
                // hence empty list is returned to the caller function.
                if (result != 0)
                    return new List<UdpProcessRecord>();

                // Marshals data from an unmanaged block of memory to a newly allocated
                // managed object 'udpRecordsTable' of type 'MIB_UDPTABLE_OWNER_PID'
                // to get number of entries of the specified TCP table structure.
                MIB_UDPTABLE_OWNER_PID udpRecordsTable = (MIB_UDPTABLE_OWNER_PID)
                    Marshal.PtrToStructure(udpTableRecordPtr, typeof(MIB_UDPTABLE_OWNER_PID));
                IntPtr tableRowPtr = (IntPtr)((long)udpTableRecordPtr +
                    Marshal.SizeOf(udpRecordsTable.dwNumEntries));

                // Reading and parsing the UDP records one by one from the table and
                // storing them in a list of 'UdpProcessRecord' structure type objects.
                for (int i = 0; i < udpRecordsTable.dwNumEntries; i++)
                {
                 
                    MIB_UDPROW_OWNER_PID udpRow = (MIB_UDPROW_OWNER_PID)
                        Marshal.PtrToStructure(tableRowPtr, typeof(MIB_UDPROW_OWNER_PID));

                    if (udpRow.owningPid == pid)
                    {
                        udpTableRecords.Add(new UdpProcessRecord(new IPAddress(udpRow.localAddr),
                        BitConverter.ToUInt16(new byte[2] { udpRow.localPort[1],
                            udpRow.localPort[0] }, 0), udpRow.owningPid));
                    }
                    tableRowPtr = (IntPtr)((long)tableRowPtr + Marshal.SizeOf(udpRow));
                }
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
            }
            catch (Exception exception)
            {

            }
            finally
            {
                Marshal.FreeHGlobal(udpTableRecordPtr);
            }
            return udpTableRecords != null ? udpTableRecords.Distinct()
                .ToList<UdpProcessRecord>() : new List<UdpProcessRecord>();
        }
    }

    public partial class ProcessDetailsWindow : Window
    {
        private static System.Timers.Timer timer;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter diskReadCounter;
        private PerformanceCounter diskWriteCounter;
        private Process curProcess;

        public ProcessDetailsWindow(Process process)
        {
            InitializeComponent();
            Title = $"Details for {process.ProcessName}";
            ProcessNameTextBlock.Text = process.ProcessName;

            cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
            diskReadCounter = new PerformanceCounter("LogicalDisk", "% Disk Read Time", "_Total", true);
            diskWriteCounter = new PerformanceCounter("LogicalDisk", "% Disk Write Time", "_Total", true);

            curProcess = process;

            timer = new System.Timers.Timer(5000);
            timer.Elapsed += MonitorResource;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Start();

            var threads = process.Threads;

            StringBuilder sb = new StringBuilder();
            foreach (ProcessThread thread in threads)
            {
                sb.Append($"Thread ID: {thread.Id}, Start Time: {thread.StartTime}\n");
            }
            ThreadsTextBlock.Text = sb.ToString();

            FilesTextBlock.Text = GetOpenFiles(process.Id);

            
            var tcpConnections = NetworkUtils.GetAllTcpConnections(process.Id);
            if (tcpConnections.Count > 0)
            {
                StringBuilder sbTCPConns = new StringBuilder();
                foreach (var tcpConn in tcpConnections)
                {
                    sbTCPConns.Append($"PID: {tcpConn.ProcessId}, Process Name: {tcpConn.ProcessName}, Local: {tcpConn.LocalAddress}:{tcpConn.LocalPort}, Remote: {tcpConn.RemoteAddress}:{tcpConn.RemotePort}\n");

                }
                TCPNetworkTextBlock.Text = sbTCPConns.ToString();
            }
            
            var udpConnections = NetworkUtils.GetAllUdpConnections(process.Id);
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

        private void MonitorResource(object sender, ElapsedEventArgs e)
        {
            // Первый вызов NextValue() может вернуть 0, поэтому вызываем дважды с задержкой
            cpuCounter.NextValue();
            Thread.Sleep(100);
            float cpu = cpuCounter.NextValue();


            float diskRead = diskReadCounter.NextValue();
            float diskWrite = diskWriteCounter.NextValue();

            // Memory Usage
            long workingSet = curProcess.WorkingSet64 / (1024 * 1024);

            string message = $"CPU: {cpu:F3}% | Память: {workingSet} MB | Чтение диска: {diskRead:F2}% | Запись диска: {diskWrite:F2}%";

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
