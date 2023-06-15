using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using static TimeRecorder.App;

namespace TimeRecorder
{
    internal class SystemProcessesTracker // Own tracker to check when processes start and stop because WMI events use too much CPU.
    {
        static public Timer CheckAllSystemProcessesIntervals;
        public static List<SystemProcessInfo> TrackingProcessesInfo = new List<SystemProcessInfo>();

        static bool CheckingSystemProcceses = false;

        static public void CheckAllSystemProcesses(object stateInfo)
        {
            if (CheckingSystemProcceses) { return; } // Lazy quick-fix to avoid executing this method if it's already being executed.
            CheckingSystemProcceses = true;

            var plist = Processes.ProcessList;
            Process[] AllProcesses = Process.GetProcesses();

            for (int p = AllProcesses.Length - 1; p > -1; p--)
            {
                string Name = AllProcesses[p].ProcessName + ".exe";
                int pid = AllProcesses[p].Id;

                for (int i = 0; i < plist.Count; i++)
                {
                    if (plist[i].PName.Equals(Name))
                    {
                        for (int j = 0; j < TrackingProcessesInfo.Count; j++)
                        {
                            if (TrackingProcessesInfo[j].Id == pid)
                            {
                                TrackingProcessesInfo[j].Found = true;
                                goto Continue2;
                            }
                        }
                        TrackingProcessesInfo.Add(new SystemProcessInfo()
                        {
                            Id = pid,
                            Name = Name,
                            Found = true
                            //P = proc,
                        });
                        AddRunningProcess(pid, IntPtr.Zero);
                        //Console.WriteLine("Added " + Name + " " + pid + " " + Environment.TickCount);
                        Continue2:; break;
                    }
                }
                AllProcesses[p].Dispose();
            }
            for (int i = TrackingProcessesInfo.Count - 1; i > -1; i--)
            {
                if (!TrackingProcessesInfo[i].Found)
                {
                    //Console.WriteLine("Removed " + TrackingProcessesInfo[i].Name + " " + TrackingProcessesInfo[i].Id + " " + Environment.TickCount);
                    processStopEvent_EventArrived(TrackingProcessesInfo[i].Id, TrackingProcessesInfo[i].Name);
                    TrackingProcessesInfo.RemoveAt(i); continue;
                }
                TrackingProcessesInfo[i].Found = false;
            }
            CheckingSystemProcceses = false;
            CheckAllSystemProcessesIntervals.Change(1000, Timeout.Infinite);
        }

        public class SystemProcessInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool Found { get; set; }
            //public string Dir { get; set; }
        }
    }
}