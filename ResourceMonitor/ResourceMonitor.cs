using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SystemProcessorMonitor
{

    public class ResourceMonitor
    {
        private PerformanceCounter cpuCounter;
        private PerformanceCounter diskReadCounter;
        private PerformanceCounter diskWriteCounter;
        private Process curProcess;

        public ResourceMonitor(Process process)
        {

            cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
            diskReadCounter = new PerformanceCounter("LogicalDisk", "% Disk Read Time", "_Total", true);
            diskWriteCounter = new PerformanceCounter("LogicalDisk", "% Disk Write Time", "_Total", true);
            curProcess = process;
        }

        public string GetMonitorMessage()
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
            return message;    
        }
    }
}
