using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMSystemProcessorMonitoranager
{
    // Enum для типов протоколов.
    public enum Protocol
    {
        TCP,
        UDP
    }

    // Enum to define the set of values used to indicate the type of table returned by 
    // calls made to the function 'GetExtendedTcpTable'.
    // Определяет набор значений, которые используются для типа таблицы, которая возвращается
    // функцией GetExtendedTcpTable
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
    // То же самое, как TcpTableClass, но для UDP
    public enum UdpTableClass
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    // Enum определяет различные возможные стейты TCP соединения
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
    /// Этот класс предоставляет доступ к адресам и портам TCP-соединений IPv4 и связанным 
    /// с ними идентификаторам процессов и именам.
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
            // Получение имени процесса, связанного с идентификатором процесса.
            if (Process.GetProcesses().Any(process => process.Id == pId))
            {
                ProcessName = Process.GetProcessById(ProcessId).ProcessName;
            }
        }
    }

    /// <summary>
    /// Структура содержит информацию, описывающую TCP-соединение IPv4 с IPv4-адресами, портами, 
    /// используемыми TCP-соединением, и идентификатором конкретного процесса (PID), 
    /// связанным с подключением.
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
    /// Структура содержит запись из таблицы прослушивателя протокола пользовательских дейтаграмм (UDP) 
    /// для IPv4 на локальном компьютере. Запись также содержит идентификатор процесса (PID), 
    /// который вызвал функцию привязки для конечной точки UDP.
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
    /// Структура содержит таблицу прослушивателей протокола пользовательских дейтаграмм (UDP) для IPv4 
    /// на локальном компьютере. Таблица также содержит идентификатор процесса (PID), 
    /// который вызвал функцию привязки для каждой конечной точки UDP.
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
    /// Структура содержит таблицу идентификаторов процессов (PID) и TCP-ссылок IPv4, 
    /// которые контекстно привязаны к этим PID.
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
    /// Этот класс предоставляет доступ к адресам и портам UDP-соединений IPv4 и связанным 
    /// с ними идентификаторам процессов и именам.
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

    public class NetworkManager
    {
        // Версия IP, используемая конечной точкой TCP/UDP. AF_INET используется для IPv4.
        private const int AF_INET = 2;
        // Список активных TCP-подключений.
        private static List<TcpProcessRecord> TcpActiveConnections = null;
        // Список активных UDP-подключений.
        private static List<UdpProcessRecord> UdpActiveConnections = null;

        public NetworkManager()
        {

        }

        // Функция GetExtendedTcpTable извлекает таблицу, содержащую список конечных точек TCP,
        // доступных приложению. Добавление в функцию атрибута DllImport указывает на то,
        // что атрибутируемый метод предоставляется
        // неуправляемой динамически подключаемой библиотекой "iphlpapi.dll" в качестве статической точки входа.
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize,
            bool bOrder, int ulAf, TcpTableClass tableClass, uint reserved = 0);
        
        // Функция GetExtendedUdpTable извлекает таблицу, содержащую список конечных точек UDP,
        // доступных приложению. Присвоение функции атрибута DllImport указывает на то,
        // что атрибутируемый метод предоставляется неуправляемой библиотекой динамической компоновки
        // "iphlpapi.dll" в качестве статической точки входа.
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize,
            bool bOrder, int ulAf, UdpTableClass tableClass, uint reserved = 0);

        /// <summary>
        /// Эта функция считывает и анализирует доступные активные соединения с сокетами TCP и 
        /// сохраняет их в виде списка.
        /// </summary>
        /// <returns>
        /// Возвращает текущий набор активных подключений к сокетам TCP.
        /// </returns>
        /// <exception cref="OutOfMemoryException">
        /// Это исключение может быть вызвано функцией Marshal.AllocHGlobal, 
        /// когда недостаточно памяти для выполнения запроса.
        /// </exception>
        public List<TcpProcessRecord> GetAllTcpConnections(int pid)
        {
            int bufferSize = 0;
            List<TcpProcessRecord> tcpTableRecords = new List<TcpProcessRecord>();

            // Получаем размер таблицы TCP, который возвращается в переменной 'bufferSize'.
            uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET,
                TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

            // Выделение памяти из неуправляемой памяти процесса с использованием указанного количества байт
            // в переменной 'bufferSize'.
            IntPtr tcpTableRecordsPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                // Размер таблицы, возвращенный в переменной 'bufferSize' в предыдущем вызове,
                // должен использоваться в этом последующем вызове функции 'GetExtendedTcpTable',
                // чтобы успешно извлечь таблицу.
                result = GetExtendedTcpTable(tcpTableRecordsPtr, ref bufferSize, true,
                    AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

                // Ненулевое значение означает, что функция 'GetExtendedTcpTable' завершилась ошибкой,
                // следовательно, вызывающей функции возвращается пустой список.
                if (result != 0)
                    return new List<TcpProcessRecord>();

                // Выполняет маршалинг данных из неуправляемого блока памяти во вновь выделенный управляемый
                // объект 'tcpRecordsTable' типа 'MIB_TCPTABLE_OWNER_PID', чтобы получить количество записей
                // указанной структуры таблицы TCP.
                MIB_TCPTABLE_OWNER_PID tcpRecordsTable = (MIB_TCPTABLE_OWNER_PID)
                                        Marshal.PtrToStructure(tcpTableRecordsPtr,
                                        typeof(MIB_TCPTABLE_OWNER_PID));
                IntPtr tableRowPtr = (IntPtr)((long)tcpTableRecordsPtr +
                                        Marshal.SizeOf(tcpRecordsTable.dwNumEntries));

                // Считывание и синтаксический анализ TCP-записей из таблицы одну за другой и сохранение их
                // в списке объектов структурного типа 'TcpProcessRecord'.
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
            finally
            {
                Marshal.FreeHGlobal(tcpTableRecordsPtr);
            }
            return tcpTableRecords != null ? tcpTableRecords.Distinct()
                .ToList<TcpProcessRecord>() : new List<TcpProcessRecord>();
        }

        /// <summary>
        /// Эта функция считывает и анализирует доступные активные соединения с сокетами UDP и 
        /// сохраняет их в списке.
        /// </summary>
        /// <returns>
        /// Возвращает текущий набор активных подключений к UDP-сокету.
        /// </returns>
        /// <exception cref="OutOfMemoryException">
        /// Это исключение может быть вызвано функцией Marshal.AllocHGlobal, 
        /// когда недостаточно памяти для выполнения запроса.
        /// </exception>
        public List<UdpProcessRecord> GetAllUdpConnections(int pid)
        {
            int bufferSize = 0;
            List<UdpProcessRecord> udpTableRecords = new List<UdpProcessRecord>();

            // Получаем размер таблицы UDP, который возвращается в переменной 'bufferSize'.
            uint result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true,
                AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

            // Выделение памяти из неуправляемой памяти процесса с использованием указанного количества
            // байт в переменной 'bufferSize'.
            IntPtr udpTableRecordPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                // Размер таблицы, возвращенный в переменной 'buffer Size' в предыдущем вызове,
                // должен использоваться в этом последующем вызове функции 'GetExtendedUdpTable',
                // чтобы успешно извлечь таблицу.
                result = GetExtendedUdpTable(udpTableRecordPtr, ref bufferSize, true,
                    AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

                // Ненулевое значение означает, что функция 'GetExtendedUdpTable' завершилась ошибкой,
                // следовательно, вызывающей функции возвращается пустой список.
                if (result != 0)
                    return new List<UdpProcessRecord>();

                // Выполняет маршалинг данных из неуправляемого блока памяти во вновь выделенный управляемый
                // объект 'udpRecordsTable' типа 'MIB_UDPTABLE_OWNER_PID', чтобы получить
                // количество записей указанной структуры таблицы TCP.
                MIB_UDPTABLE_OWNER_PID udpRecordsTable = (MIB_UDPTABLE_OWNER_PID)
                    Marshal.PtrToStructure(udpTableRecordPtr, typeof(MIB_UDPTABLE_OWNER_PID));
                IntPtr tableRowPtr = (IntPtr)((long)udpTableRecordPtr +
                    Marshal.SizeOf(udpRecordsTable.dwNumEntries));

                // Считывание и синтаксический анализ UDP-записей из таблицы одну за другой и сохранение их
                // в списке объектов структурного типа 'UdpProcessRecord'.
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
            finally
            {
                Marshal.FreeHGlobal(udpTableRecordPtr);
            }
            return udpTableRecords != null ? udpTableRecords.Distinct()
                .ToList<UdpProcessRecord>() : new List<UdpProcessRecord>();
        }

    }
}
