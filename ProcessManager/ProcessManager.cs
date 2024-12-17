using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SystemProcessorMonitor
{

    public class ProcessInfo
    {
        public int PID { get; set; }
        public string Name { get; set; }
        public double Memory { get; set; }
        public string CPU { get; set; }
    }

    public class ProcessManager
    {

        public ProcessManager()
        {

        }

        public async Task<ProcessInfo[]> GetProcessInfos()
        {
            var processes = Process.GetProcesses();

            var tasks = processes.Select(async process =>
            {
                try
                {
                    var cpuUsage = await GetCpuUsageAsync(process);
                    return new ProcessInfo
                    {
                        PID = process.Id,
                        Name = process.ProcessName,
                        Memory = process.WorkingSet64 / (1024 * 1024),
                        CPU = cpuUsage.ToString("0.000")
                    };
                }
                catch
                {
                    return null;
                }
            }).ToArray();

            return await Task.WhenAll(tasks);
        }

        private async Task<double> GetCpuUsageAsync(Process process)
        {
            return await Task.Run(() =>
            {
                var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
                cpuCounter.NextValue(); // Первая выборка всегда 0, ждем для получения актуального значения
                System.Threading.Thread.Sleep(50);
                return cpuCounter.NextValue() / Environment.ProcessorCount;
            });
        }

        public void KillProcessByPID(int pid)
        {
            var process = Process.GetProcessById(pid);
            process.Kill();
        }

        public Process GetProcessByPID(int PID)
        {
            return Process.GetProcessById(PID);
        }

        public void LaunchProcess(string processName)
        {
            if (!string.IsNullOrWhiteSpace(processName)) {
                Process.Start(processName);
            }
        }

        public List<ProcessInfo> SearchProcessInfos(string query, List<ProcessInfo> processInfos)
        {
            var result = new List<ProcessInfo>();
            if (!string.IsNullOrWhiteSpace(query))
            {
                result = processInfos
                    .Where(p =>
                    p.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    ||
                    p.PID.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToList();
            }

            return result;
        }
    }

}
