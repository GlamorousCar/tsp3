using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TaskManager
{
    public class TaskManager
    {
        public Process[] getProcesses()
        {
            Process process = Process.GetProcesses()[0];

            return Process.GetProcesses();
        }
    }
}
