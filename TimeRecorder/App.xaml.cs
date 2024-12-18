﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Forms = System.Windows.Forms;
using static TimeRecorder.App.NativeMethods;
using static TimeRecorder.SystemInputsRefresh;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Shapes;
using System.Data.SqlTypes;

namespace TimeRecorder
{
    public partial class App : Application
    {
        private readonly Forms.NotifyIcon _notifyIcon;

        //ManagementEventWatcher processStartEvent = new ManagementEventWatcher(@"\\.\root\CIMV2", "SELECT * FROM __InstanceCreationEvent WITHIN .025 WHERE TargetInstance ISA 'Win32_Process'");
        //ManagementEventWatcher processStopEvent = new ManagementEventWatcher(@"\\.\root\CIMV2", "SELECT * FROM __InstanceDeletionEvent WITHIN .025 WHERE TargetInstance ISA 'Win32_Process'");

        static List<PHook> PHooks = new List<PHook>();

        public App()
        {
            InitializeAppSettingsFile();

            _notifyIcon = new Forms.NotifyIcon();

            //processStartEvent.EventArrived += processStartEvent_EventArrived;
            //processStartEvent.Start();
            //processStopEvent.EventArrived += processStopEvent_EventArrived;
            //processStopEvent.Start();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(Directory.GetCurrentDirectory() + @"\" + Process.GetCurrentProcess().ProcessName + ".exe");
            _notifyIcon.Text = "Time Recorder";
            _notifyIcon.MouseClick += NotifyIcon_Click;

            _notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add("Show", null, Show_Click);
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, Exit_Click);

            _notifyIcon.Visible = true;
        }

        private void InitializeAppSettingsFile()
        {
            string programPath = Directory.GetCurrentDirectory();
            string configFile = @"\settings.txt";

            var file = programPath + configFile;

            if (!File.Exists(file))
            {
                File.AppendAllText(file,
                    $"50" +
                $"\n");
            }

            string[] lines = File.ReadAllLines(file);
            CustomJoyDeadZone = int.Parse(lines[0]);
        }

        static public void CleanUpOnStartUp()
        {
            //DirectoryInfo DataDir = new DirectoryInfo(programPath+@"\data");
            //foreach (FileInfo file in DataDir.GetFiles())
            //{
                //if (file.Name.ToLower().Contains(".tmp"))
                //{
                    //File.Delete(file.FullName);
                //}
            //}

            DirectoryInfo IconsDir = new DirectoryInfo(programPath+@"\icons");
            var plist = Processes.ProcessList;

            if (!IconsDir.Exists) { return; }

            foreach(FileInfo file in IconsDir.GetFiles())
            {
                for(int i = 0; i < plist.Count; i++)
                {
                    if (plist[i].IcoDir != null && plist[i].IcoDir.Contains(file.Name))
                    {
                        goto Continue;
                    }
                }
                File.Delete(file.FullName);
                Continue:;
            }
        }

        private void ShowWindow()
        {
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }
        private void NotifyIcon_Click(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                ShowWindow();
            }
        }
        private void Show_Click(object sender, EventArgs e)
        {
            ShowWindow();
        }
        private void Exit_Click(object sender, EventArgs e)
        {
            Current.Shutdown();
        }

        private static string GetWindowTitle(IntPtr hWnd)
        {
            int textLength = NativeMethods.GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(textLength + 1);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static long GetTimeSinceSysStart()
        {
            ulong time = 0;
            QueryUnbiasedInterruptTime(ref time);
            return (long)(time/10000);
        }

        internal static class NativeMethods
        {
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            internal static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadWndProc lpfn, IntPtr lParam);
            internal delegate bool EnumThreadWndProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool IsIconic(IntPtr hWnd);

            [DllImport("User32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetCursorPos(ref POINT lpPoint);

            [DllImport("User32.dll")]
            internal static extern short GetKeyState(int nVirtKey);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            internal static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

            [DllImport("user32.dll")]
            internal static extern IntPtr GetLastActivePopup(IntPtr hWnd);

            [DllImport("user32.dll")]
            internal static extern uint GetClassLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            internal static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            internal delegate void WinEventProc(IntPtr hWinEventHook, int iEvent, IntPtr hWnd, int idObject, int idChild, int dwEventThread, int dwmsEventTime);
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern IntPtr SetWinEventHook(int eventMin, int eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, int idProcess, int idThread, SetWinEventHookFlags dwflags);
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool UnhookWinEvent(IntPtr hWinEventHook);
            internal enum SetWinEventHookFlags
            {
                WINEVENT_INCONTEXT = 4,
                WINEVENT_OUTOFCONTEXT = 0,
                WINEVENT_SKIPOWNPROCESS = 2,
                WINEVENT_SKIPOWNTHREAD = 1
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;
            }

            [DllImport("Winmm.dll")]
            internal static extern uint joyGetNumDevs();

            [DllImport("Winmm.dll")]
            internal static extern uint joyGetPosEx(uint uJoyID, ref LPJOYINFOEX pji);

            [StructLayout(LayoutKind.Sequential)]
            public struct LPJOYINFOEX
            {
                public uint dwSize;
                public uint dwFlags;
                public uint dwXpos;
                public uint dwYpos;
                public uint dwZpos;
                public uint dwRpos;
                public uint dwUpos;
                public uint dwVpos;
                public uint dwButtons;
                public uint dwButtonNumber;
                public uint dwPOV;
                public uint dwReserved1;
                public uint dwReserved2;
            }

            [DllImport("Winmm.dll")]
            internal static extern uint joyGetThreshold(uint uJoyID, ref int joyGetThreshold);

            [DllImport("Winmm.dll", CharSet = CharSet.Unicode)]
            internal static extern uint joyGetDevCaps(uint uJoyID, ref LPJOYCAPS pjc, uint cbjc);

            [StructLayout(LayoutKind.Sequential)]
            public struct LPJOYCAPS
            {
                public ushort wMid;
                public ushort wPid;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32 * 2)]
                public char[] szPname;
                public uint wXmin;
                public uint wXmax;
                public uint wYmin;
                public uint wYmax;
                public uint wZmin;
                public uint wZmax;
                public uint wNumButtons;
                public uint wPeriodMin;
                public uint wPeriodMax;
                public uint wRmin;
                public uint wRmax;
                public uint wUmin;
                public uint wUmax;
                public uint wVmin;
                public uint wVmax;
                public uint wCaps;
                public uint wMaxAxes;
                public uint wNumAxes;
                public uint wMaxButtons;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32 * 2)]
                public char[] szRegKey;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260 * 2)]
                public char[] szOEMVxD;
            }

            [DllImport("Kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryUnbiasedInterruptTime(ref ulong UnbiasedTime);
        }

        IntPtr FocusPEventHook = SetWinEventHook(0x0003, 0x0003, IntPtr.Zero, (hWinEventHook, iEvent, hWnd, idObject, idChild, dwEventThread, dwmsEventTime) =>
        {
            uint ProcessId = 0;
            GetWindowThreadProcessId(hWnd, ref ProcessId);

            Process proc;
            try { proc = Process.GetProcessById((int)ProcessId); } catch { return; }

            RefreshAllFocus(proc.ProcessName + ".exe", (int)hWnd);
            //Console.WriteLine("***Window Foreground*** " + hWnd + " " + ProcessId);

        }, 0, 0, SetWinEventHookFlags.WINEVENT_OUTOFCONTEXT);

        static bool IsAltTabWindow(IntPtr hwnd)
        {
            // Start at the root owner
            IntPtr hwndWalk = GetAncestor(hwnd, 3);
            // See if we are the last active visible popup
            IntPtr hwndTry;
            while ((hwndTry = GetLastActivePopup(hwndWalk)) != hwndTry)
            {
                if (IsWindowVisible(hwndTry)) break;
                hwndWalk = hwndTry;
            }

            return hwndWalk == hwnd;
        }

        //void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        //{
        //var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
        //int pid = Convert.ToInt32(targetInstance.Properties["ProcessID"].Value);
        //AddRunningProcess(pid,IntPtr.Zero);
        //}

        public static void AddRunningProcess(int pid, IntPtr Wnd)
        {
            var plist = Processes.ProcessList;
            var rlist = RunningProcesses.RunningProcessesList;

            Process p;
            string Name;
            string WndName;
            ProcessThreadCollection pThreads;

            try
            {
                p = Process.GetProcessById(pid);
                Name = p.ProcessName + ".exe";
                WndName = p.MainWindowTitle;
                pThreads = p.Threads;
            }
            catch //process doesn't exist anymore so we finish the code
            {
                return;
            }

            bool WndExist = false;
            List<IntPtr> PhWndsList = new List<IntPtr>();

            if ((int)Wnd != 0)
            {
                WndExist = true;
                PhWndsList.Add(Wnd);
            }
            else
            {
                foreach (ProcessThread thread in pThreads)
                {
                    EnumThreadWindows((uint)thread.Id, (hWnd, lParam) =>
                    {
                        if ((int)hWnd != 0 && IsAltTabWindow(hWnd))
                        {
                            WndExist = true;
                            PhWndsList.Add(hWnd);
                            //Console.WriteLine("**Found Window: " + GetWindowTitle(hWnd) + " " + Name + " " + hWnd + " " + IsAltTabWindow(hWnd) + " " + IsIconic(hWnd));
                        }
                        return true;

                    }, IntPtr.Zero);
                }
            }

            void AddWndToHook(int i2)
            {
                foreach (PHook hook in PHooks)
                {
                    if (hook.Id == pid)
                    {
                        if (!hook.hWnds.Contains((int)PhWndsList[i2]))
                        {
                            hook.hWnds.Add((int)PhWndsList[i2]);
                            //Console.WriteLine("Added: "+(int)PhWndsList[i2]);
                        }
                        break;
                    }
                }
            }

            void WndHooks() // ------------------------------------------------ Process Hooks
            {
                foreach (PHook hook in PHooks)
                {
                    if (hook.Id == pid) { return; }
                }

                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    IntPtr hook_wnd_creation = SetWinEventHook(0x8000, 0x8000, IntPtr.Zero, (hWinEventHook, iEvent, hWnd, idObject, idChild, dwEventThread, dwmsEventTime) =>
                    {
                        uint ProcessId = 0;
                        GetWindowThreadProcessId(hWnd, ref ProcessId);

                        Process proc;
                        try { proc = Process.GetProcessById((int)ProcessId); } catch { return; }

                        if ((int)hWnd != 0 && ProcessId != 0 && idObject == 0 && IsAltTabWindow(hWnd))
                        {
                            //Console.WriteLine("***Window Created*** "+ idObject + " " + IsAltTabWindow(hWnd) + " " + GetWindowTitle(hWnd));
                            AddRunningProcess((int)ProcessId, hWnd);
                        }

                    }, pid, 0, SetWinEventHookFlags.WINEVENT_OUTOFCONTEXT);

                    IntPtr hook_wnd_namechange = SetWinEventHook(0x800c, 0x800c, IntPtr.Zero, (hWinEventHook, iEvent, hWnd, idObject, idChild, dwEventThread, dwmsEventTime) =>
                    {
                        uint ProcessId = 0;
                        GetWindowThreadProcessId(hWnd, ref ProcessId);

                        Process proc;
                        string procName;
                        try { proc = Process.GetProcessById((int)ProcessId); procName = proc.ProcessName + ".exe"; } catch { return; }

                        if (ProcessId != 0 && idObject == 0 && IsAltTabWindow(hWnd))
                        {
                            //Console.WriteLine("***Window Name Changed*** " + GetWindowTitle(hWnd));
                            RemoveProcessWnd(procName, (int)hWnd);
                            AddRunningProcess((int)ProcessId, hWnd);
                            RefreshAllFocus(procName, (int)hWnd);
                        }

                    }, pid, 0, SetWinEventHookFlags.WINEVENT_OUTOFCONTEXT);

                    IntPtr hook_wnd_destroy = SetWinEventHook(0x8001, 0x8001, IntPtr.Zero, (hWinEventHook, iEvent, hWnd, idObject, idChild, dwEventThread, dwmsEventTime) =>
                    {
                        uint ProcessId = 0;
                        GetWindowThreadProcessId(hWnd, ref ProcessId);

                        Process proc;
                        string procName;
                        try { proc = Process.GetProcessById((int)ProcessId); procName = proc.ProcessName + ".exe"; } catch { return; }

                        if (ProcessId != 0 && idObject == 0 && IsAltTabWindow(hWnd))
                        {
                            //Console.WriteLine("***Window Destroyed*** " + hWnd + " " + idObject + " " + ProcessId);
                            foreach (PHook hook in PHooks)
                            {
                                if (hook.Id == ProcessId)
                                {
                                    hook.hWnds.Remove((int)hWnd);
                                }
                            }
                            RemoveProcessWnd(procName, (int)hWnd);
                        }

                    }, pid, 0, SetWinEventHookFlags.WINEVENT_OUTOFCONTEXT);

                    IntPtr hook_wnd_minimized = SetWinEventHook(0x0016, 0x0016, IntPtr.Zero, (hWinEventHook, iEvent, hWnd, idObject, idChild, dwEventThread, dwmsEventTime) =>
                    {
                        uint ProcessId = 0;
                        GetWindowThreadProcessId(hWnd, ref ProcessId);

                        Process proc;
                        string procName;
                        try { proc = Process.GetProcessById((int)ProcessId); procName = proc.ProcessName + ".exe"; } catch { return; }

                        if (ProcessId != 0 && idObject == 0)
                        {
                            //Console.WriteLine("***Window Minimized*** " + hWnd + " " + ProcessId);
                            CheckProcessMinimized(procName, (int)hWnd, proc, true);
                        }

                    }, pid, 0, SetWinEventHookFlags.WINEVENT_OUTOFCONTEXT);

                    IntPtr hook_wnd_restored = SetWinEventHook(0x0017, 0x0017, IntPtr.Zero, (hWinEventHook, iEvent, hWnd, idObject, idChild, dwEventThread, dwmsEventTime) =>
                    {
                        uint ProcessId = 0;
                        GetWindowThreadProcessId(hWnd, ref ProcessId);

                        Process proc;
                        string procName;
                        try { proc = Process.GetProcessById((int)ProcessId); procName = proc.ProcessName + ".exe"; } catch { return; }

                        if (ProcessId != 0 && idObject == 0)
                        {
                            //Console.WriteLine("***Window Restored*** " + hWnd + " " + ProcessId);
                            CheckProcessMinimized(procName, (int)hWnd, proc, false);
                        }

                    }, pid, 0, SetWinEventHookFlags.WINEVENT_OUTOFCONTEXT);

                    var hookstruct = new PHook()
                    {
                        Id = pid,
                        hWnds = new List<int>(),
                        Hooks = new List<IntPtr>()
                        {
                            hook_wnd_namechange,
                            hook_wnd_creation,
                            hook_wnd_destroy,
                            hook_wnd_minimized,
                            hook_wnd_restored,
                        }
                    };
                    PHooks.Add(hookstruct);

                }));
            }

            bool MatchModeOne(string str1, string str2)
            {
                return str1.Equals(str2);
            }
            bool MatchModeTwo(string str1, string str2)
            {
                Regex regex = new Regex(str1);
                return regex.IsMatch(str2);
            }

            Func<string, string, bool> MatchByMode;
            bool HasHooks = false;

            for (int i = 0; i < plist.Count; i++)
            {
                if (plist[i].PName.Equals(Name))
                {
                    if (!HasHooks)
                    {
                        WndHooks();
                        HasHooks = true;
                    }

                    if (plist[i].RecordWnd)
                    {
                        if (!WndExist)
                        {
                            continue;
                        }

                        if (plist[i].MatchMode)
                        {
                            MatchByMode = MatchModeOne;
                        }
                        else
                        {
                            MatchByMode = MatchModeTwo;
                        }

                        for (int i2 = 0; i2 < PhWndsList.Count(); i2++)
                        {
                            if (MatchByMode(plist[i].WndName, GetWindowTitle(PhWndsList[i2])))
                            {
                                for (int i3 = 0; i3 < rlist.Count; i3++)
                                {
                                    string wndname = plist[i].WndName; if (!plist[i].RecordWnd) { wndname = null; } // just want to be done with this
                                    if (rlist[i3].PName.Equals(plist[i].PName) && rlist[i3].WndName == wndname)
                                    {
                                        if (!rlist[i3].hWnds.Contains((int)PhWndsList[i2]))
                                        {
                                            //Console.WriteLine("***Adding Window*** " + PhWndsList[i2]);
                                            rlist[i3].hWnds.Add((int)PhWndsList[i2]);
                                            AddWndToHook(i2);
                                            CheckProcessMinimized(Name, (int)PhWndsList[i2], p, IsIconic(PhWndsList[i2]));

                                            if (!rlist[i3].IsFocus && (GetForegroundWindow() == PhWndsList[i2]))
                                            {
                                                SetFocusOnProcess(i, i3);
                                            }
                                        }
                                        goto Continue1;
                                    }
                                }

                                Wnd = PhWndsList[i2];
                                AddWndToHook(i2);
                                //Console.WriteLine("***Creating Adding Window*** " + PhWndsList[i2]);
                                CreateRunningProcess(i, true);
                                CheckProcessMinimized(Name, (int)Wnd, p, IsIconic(Wnd));

                                if (!rlist.Last().IsFocus && (GetForegroundWindow() == Wnd))
                                {
                                    SetFocusOnProcess(i, rlist.Count - 1);
                                }
                            }
                        Continue1:;
                        }
                    }
                    else
                    {
                        //Console.WriteLine("***Added Normal***");
                        for (int i2 = 0; i2 < rlist.Count; i2++)
                        {
                            if (rlist[i2].PName.Equals(Name) && rlist[i2].WndName == null)
                            {
                                if (!rlist[i2].IDs.Contains(pid))
                                {
                                    rlist[i2].IDs.Add(pid);
                                    CheckProcessMinimized(Name, (int)Wnd, p, IsIconic(Wnd));

                                    if (!rlist[i2].IsFocus)
                                    {
                                        foreach (IntPtr hWnd in PhWndsList)
                                        {
                                            if (GetForegroundWindow() == hWnd)
                                            {
                                                SetFocusOnProcess(i, i2);
                                            }
                                        }
                                    }
                                }
                                goto Continue2;
                            }
                        }
                        CreateRunningProcess(i, false);
                        CheckProcessMinimized(Name, (int)Wnd, p, IsIconic(Wnd));

                        if (!rlist.Last().IsFocus)
                        {
                            foreach (IntPtr hWnd in PhWndsList)
                            {
                                if (GetForegroundWindow() == hWnd)
                                {
                                    SetFocusOnProcess(i, rlist.Count - 1);
                                }
                            }
                        }
                    }
                Continue2:;
                }
            }

            void CreateRunningProcess(int i, bool IsWnd)
            {
                //Console.WriteLine("***Creating RunningProcess***");
                if (IsWnd)
                {
                    rlist.Add(new RunningProcess()
                    {
                        Index = i,
                        hWnds = new List<int> { (int)Wnd },
                        PName = Name,
                        WndName = plist[i].WndName,
                        AddHours = plist[i].Hours,
                        TimeTick = GetTimeSinceSysStart(),
                        InputWaitT = plist[i].InputWaitT,
                        InputSaveT = plist[i].InputSaveT
                    });
                }
                else
                {
                    rlist.Add(new RunningProcess()
                    {
                        Index = i,
                        IDs = new List<int> { pid },
                        PName = Name,
                        AddHours = plist[i].Hours,
                        TimeTick = GetTimeSinceSysStart(),
                        InputWaitT = plist[i].InputWaitT,
                        InputSaveT = plist[i].InputSaveT
                    });
                }

                if (plist[i].First == "--")
                {
                    string datenow = DateTime.Now.ToString();
                    plist[i].First = datenow;
                    plist[i].Last = datenow;
                }
                if (rlist.Count == 1)
                {
                    rtimer.Change(5000, Timeout.Infinite);
                }
            }
        }

        public static void SetFocusOnProcess(int i, int i2)
        {
            var plist = Processes.ProcessList;
            var rlist = RunningProcesses.RunningProcessesList;

            rlist[i2].IsFocus = true;
            rlist[i2].AddFocusH = plist[i].FocusH;
            rlist[i2].FocusTick = GetTimeSinceSysStart();
            //Console.WriteLine("***Added Focus*** " + i + " " + i2);
        }

        public static void RefreshAllFocus(string Name, int wnd)
        {
            var plist = Processes.ProcessList;
            var rlist = RunningProcesses.RunningProcessesList;

            for (int i = 0; i < rlist.Count; i++)
            {
                if (rlist[i].PName.Equals(Name))
                {
                    if (rlist[i].WndName != null)
                    {
                        if (rlist[i].hWnds.Contains(wnd))
                        {
                            if (!rlist[i].IsFocus)
                            {
                                for (int i2 = 0; i2 < plist.Count; i2++)
                                {
                                    string wndname = plist[i2].WndName; if (!plist[i2].RecordWnd) { wndname = null; } // just want to be done with this
                                    if (rlist[i].PName.Equals(plist[i2].PName) && rlist[i].WndName == wndname)
                                    {
                                        SetFocusOnProcess(i2, i);
                                        break;
                                    }
                                }
                            }
                            continue;
                        }
                    }
                    else
                    {
                        if (!rlist[i].IsFocus)
                        {
                            for (int i2 = 0; i2 < plist.Count; i2++)
                            {
                                string wndname = plist[i2].WndName; if (!plist[i2].RecordWnd) { wndname = null; } // just want to be done with this
                                if (rlist[i].PName.Equals(plist[i2].PName) && rlist[i].WndName == wndname)
                                {
                                    SetFocusOnProcess(i2, i);
                                    break;
                                }
                            }
                        }
                        continue;
                    }
                }

                if (rlist[i].IsFocus)
                {
                    for (int i2 = 0; i2 < plist.Count; i2++)
                    {
                        string wndname = plist[i2].WndName; if (!plist[i2].RecordWnd) { wndname = null; } // just want to be done with this
                        if (rlist[i].PName.Equals(plist[i2].PName) && rlist[i].WndName == wndname)
                        {
                            rlist[i].IsFocus = false;
                            long elapsedTicks = GetTimeSinceSysStart() - rlist[i].FocusTick;
                            long newFocusH = rlist[i].AddFocusH + elapsedTicks;
                            plist[i2].FocusH = newFocusH;
                            plist[i2].ViewFocusH = (float)newFocusH / 3600000;
                            //Console.WriteLine("***Removed Focus Name*** " + rlist[i].WndName + " " + wndname);
                            //Console.WriteLine("***Removed Focus*** " + i + " " + i2);
                            break;
                        }
                    }
                }
            }
        }

        public static void CheckProcessMinimized(string name, int wnd, Process p, bool IsWndMin)
        {
            var plist = Processes.ProcessList;
            var rlist = RunningProcesses.RunningProcessesList;

            for (int i = 0; i < rlist.Count; i++)
            {
                if (rlist[i].PName.Equals(name))
                {
                    if (rlist[i].WndName != null)
                    {
                        for (int i2 = 0; i2 < rlist[i].hWnds.Count; i2++)
                        {
                            if (rlist[i].hWnds[i2] == wnd)
                            {
                                bool AllMin = true;

                                for (int i3 = 0; i3 < rlist[i].hWnds.Count; i3++)
                                {
                                    if (i3 == i2 && !IsWndMin)
                                    {
                                        AllMin = false;
                                        //Console.WriteLine("***Process Not Minimized*** " + i);
                                        break;
                                    }
                                    if (!IsIconic((IntPtr)rlist[i].hWnds[i3]))
                                    {
                                        AllMin = false;
                                        //Console.WriteLine("***Process Not Minimized*** " + i);
                                        break;
                                    }
                                }

                                CheckAllMinimized(AllMin, i);
                                break;
                            }
                        }
                    }
                    else
                    {
                        ProcessThreadCollection pThreads;

                        try { pThreads = p.Threads; } //idk if the process can stop while this is running. Simple fail check.
                        catch { continue; }

                        bool AllMin = true;

                        foreach (ProcessThread thread in pThreads)
                        {
                            EnumThreadWindows((uint)thread.Id, (hWnd, lParam) =>
                            {
                                if (!IsIconic(hWnd) && IsAltTabWindow(hWnd))
                                {
                                    AllMin = false;
                                    //Console.WriteLine("***ProcessF Not Minimized*** " + i);
                                    return false;
                                }
                                return true;

                            }, IntPtr.Zero);
                        }

                        CheckAllMinimized(AllMin, i);
                    }
                }
            }

            void CheckAllMinimized(bool AllMin, int i)
            {
                if (AllMin)
                {
                    if (!rlist[i].IsMin)
                    {
                        rlist[i].IsMin = true;
                        rlist[i].MinTick = GetTimeSinceSysStart();

                        for (int i2 = 0; i2 < plist.Count; i2++)
                        {
                            string wndname = plist[i2].WndName; if (!plist[i2].RecordWnd) { wndname = null; } // just want to be done with this
                            if (rlist[i].PName.Equals(plist[i2].PName) && rlist[i].WndName == wndname)
                            {
                                rlist[i].AddMinH = plist[i2].MinH;
                                //Console.WriteLine("***Added Min*** " + i2 + " " + i);
                            }
                        }
                    }
                }
                else
                {
                    if (rlist[i].IsMin)
                    {
                        //Console.WriteLine("***Process Restored*** " + i);
                        rlist[i].IsMin = false;

                        for (int i2 = 0; i2 < plist.Count; i2++)
                        {
                            string wndname = plist[i2].WndName; if (!plist[i2].RecordWnd) { wndname = null; } // just want to be done with this
                            if (rlist[i].PName.Equals(plist[i2].PName) && rlist[i].WndName == wndname)
                            {
                                long elapsedTicks = GetTimeSinceSysStart() - rlist[i].MinTick;
                                long newMinH = rlist[i].AddMinH + elapsedTicks;
                                plist[i2].MinH = newMinH;
                                plist[i2].ViewMinH = (float)newMinH / 3600000;
                                //Console.WriteLine("***Removed Min*** " + i2 + " " + i);
                            }
                        }
                    }
                }
            }
        }

        public static void RemoveProcessWnd(string name, int wnd)
        {
            var rlist = RunningProcesses.RunningProcessesList;

            for (int i = 0; i < rlist.Count; i++)
            {
                if (rlist[i].PName.Equals(name) && rlist[i].hWnds != null && rlist[i].hWnds.Contains(wnd))
                {
                    Process proc;
                    uint ProcessID = 0;
                    GetWindowThreadProcessId((IntPtr)wnd, ref ProcessID);
                    try { proc = Process.GetProcessById((int)ProcessID); } catch { return; }

                    CheckProcessMinimized(rlist[i].PName, wnd, proc, true);
                    rlist[i].hWnds.Remove(wnd);
                    //Console.WriteLine("***Removed Window*** "+ wnd);

                    if (rlist[i].hWnds.Count == 0)
                    {
                        //long SaveTime = Environment.TickCount-rlist[i].LastInputTick;

                        //if (SaveTime > rlist[i].InputSaveT)
                        //{
                        //SaveTime = rlist[i].InputSaveT;
                        //}
                        //CheckRemoveInputOnProcess(i, 0, SaveTime); //We stop waiting for InputWait, we just save.

                        rlist.RemoveAt(i);
                        //Console.WriteLine("***Removed Process*** " + i);
                    }
                }
            }

            if (rlist.Count == 0)
            {
                saveListFile(false);
                rtimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public static void processStopEvent_EventArrived(int pid, string Name)
        {
            var rlist = RunningProcesses.RunningProcessesList;
            //var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            //int pid = Convert.ToInt32(targetInstance.Properties["ProcessID"].Value);
            //string Name = (string)targetInstance.Properties["Name"].Value;

            for (int i = rlist.Count - 1; i > -1; i--)
            {
                if (rlist[i].PName.Equals(Name))
                {
                    if (rlist[i].WndName == null)
                    {
                        rlist[i].IDs.Remove(pid);

                        if (rlist[i].IDs.Count == 0)
                        {
                            //long SaveTime = Environment.TickCount-rlist[i].LastInputTick;

                            //if (SaveTime > rlist[i].InputSaveT)
                            //{
                            //SaveTime = rlist[i].InputSaveT;
                            //}
                            //CheckRemoveInputOnProcess(i, 0, SaveTime); //We stop waiting for InputWait, we just save.

                            //Console.WriteLine("Removed ProcessF: " + i);
                            rlist.RemoveAt(i);
                        }
                    }
                    else
                    {
                        foreach (PHook hook in PHooks)
                        {
                            if (hook.Id == pid)
                            {
                                for (int i2 = rlist[i].hWnds.Count - 1; i2 > -1; i2--)
                                {
                                    foreach (int hwndId in hook.hWnds)
                                    {
                                        if (hwndId == rlist[i].hWnds[i2])
                                        {
                                            //Console.WriteLine("Removed: " + hwndId);
                                            rlist[i].hWnds.RemoveAt(i2);

                                            if (rlist[i].hWnds.Count == 0)
                                            {
                                                //long SaveTime = Environment.TickCount-rlist[i].LastInputTick;

                                                //if (SaveTime > rlist[i].InputSaveT)
                                                //{
                                                //SaveTime = rlist[i].InputSaveT;
                                                //}
                                                //CheckRemoveInputOnProcess(i, 0, SaveTime); //We stop waiting for InputWait, we just save.

                                                rlist.RemoveAt(i);
                                                //Console.WriteLine("Removed Process: " + i);
                                            }
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }

            if (rlist.Count == 0)
            {
                saveListFile(false);
                rtimer.Change(Timeout.Infinite, Timeout.Infinite);
            }

            for (int i2 = PHooks.Count - 1; i2 > -1; i2--)
            {
                PHook hook = PHooks[i2];

                if (hook.Id == pid)
                {
                    foreach (IntPtr actualhook in hook.Hooks)
                    {
                        UnhookWinEvent(actualhook);
                    }
                    PHooks.RemoveAt(i2);
                    //Console.WriteLine("Removed Process Hooks: " + i2);

                    break;
                }
            }
        }

        //static public AutoResetEvent autoEvent = new AutoResetEvent(false);
        static public Timer rtimer = new Timer(rtimer_Tick, null, Timeout.Infinite, Timeout.Infinite);

        static string programPath = Directory.GetCurrentDirectory();
        static string dataFolder = @"\data";
        static string listFile = @"\processlist.csv";

        static string file = programPath + dataFolder + listFile;
        //static string tempfile = programPath + dataFolder + @"\processlist.tmp";

        private const int NumOfRetries = 3;
        private const int DelayOnRetry = 10;

        static private void rtimer_Tick(object stateInfo)
        {
            saveListFile(true);
            rtimer.Change(5000, Timeout.Infinite);
        }

        static private void saveListFile(bool doBackup)
        {
            string[] fileLines = new string[] { };

            long currentTick = GetTimeSinceSysStart();
            string currentDate = DateTime.Now.ToString();

            for (int i = 0; i <= NumOfRetries; ++i) // try catch statements had to be used on this function because of erros while opening or exiting games.
            {
                try
                {
                    fileLines = File.ReadAllLines(file);
                    break;
                }
                catch (IOException) when (i <= NumOfRetries)
                {
                    if (i == NumOfRetries)
                    {
                        return;
                    }
                    Thread.Sleep(DelayOnRetry);
                }
            }

            var rlist = RunningProcesses.RunningProcessesList;

            for (int i = 0; i < rlist.Count; i++)
            {
                foreach (var line in fileLines)
                {
                    //string dir = line.Split(',').ElementAt(6);
                    string PName = line.Split(',').ElementAt(4);

                    if (PName.Equals(rlist[i].PName))
                    {
                        string RecordWnd = line.Split(',').ElementAt(1);
                        string WndName = line.Split(',').ElementAt(5);

                        if (RecordWnd == "0")
                        {
                            if (rlist[i].WndName != null)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (rlist[i].WndName != WndName)
                            {
                                continue;
                            }
                        }

                        int pid = Array.FindIndex(fileLines, m => m == line);

                        string realhours = line.Split(',').ElementAt(8);
                        string realminh = line.Split(',').ElementAt(9);
                        string realfocush = line.Split(',').ElementAt(10);

                        string realinputh = line.Split(',').ElementAt(11);
                        string realinputkeyh = line.Split(',').ElementAt(12);
                        string realinputmouseh = line.Split(',').ElementAt(13);
                        string realinputkmh = line.Split(',').ElementAt(14);
                        string realinputjoyh = line.Split(',').ElementAt(15);

                        string realfirst = line.Split(',').ElementAt(18);
                        string reallast = line.Split(',').ElementAt(19);

                        long elapsedTicks = currentTick - rlist[i].TimeTick;
                        long newHours = rlist[i].AddHours + elapsedTicks;

                        long newMinH = Processes.ProcessList[pid].MinH;
                        long newFocusH = Processes.ProcessList[pid].FocusH;
                        long newInputH = Processes.ProcessList[pid].InputH;
                        long newInputKeyH = Processes.ProcessList[pid].InputKeyH;
                        long newInputMouseH = Processes.ProcessList[pid].InputMouseH;
                        long newInputKMH = Processes.ProcessList[pid].InputKMH;
                        long newInputJoyH = Processes.ProcessList[pid].InputJoyH;

                        if (rlist[i].IsMin)
                        {
                            elapsedTicks = currentTick - rlist[i].MinTick;
                            newMinH = rlist[i].AddMinH + elapsedTicks;
                            Processes.ProcessList[pid].MinH = newMinH;
                            Processes.ProcessList[pid].ViewMinH = (float)newMinH / 3600000;
                        }
                        if (rlist[i].IsFocus)
                        {
                            elapsedTicks = currentTick - rlist[i].FocusTick;
                            newFocusH = rlist[i].AddFocusH + elapsedTicks;
                            Processes.ProcessList[pid].FocusH = newFocusH;
                            Processes.ProcessList[pid].ViewFocusH = (float)newFocusH / 3600000;
                        }
                        if (rlist[i].IsInputKey)
                        {
                            elapsedTicks = currentTick - rlist[i].InputKeyTick;
                            newInputKeyH = rlist[i].AddInputKeyH + elapsedTicks;
                            Processes.ProcessList[pid].InputKeyH = newInputKeyH;
                            Processes.ProcessList[pid].ViewInputKeyH = (float)newInputKeyH / 3600000;
                        }
                        if (rlist[i].IsInputMouse)
                        {
                            elapsedTicks = currentTick - rlist[i].InputMouseTick;
                            newInputMouseH = rlist[i].AddInputMouseH + elapsedTicks;
                            Processes.ProcessList[pid].InputMouseH = newInputMouseH;
                            Processes.ProcessList[pid].ViewInputMouseH = (float)newInputMouseH / 3600000;
                        }
                        if (rlist[i].IsInputJoy)
                        {
                            elapsedTicks = currentTick - rlist[i].InputJoyTick;
                            newInputJoyH = rlist[i].AddInputJoyH + elapsedTicks;
                            Processes.ProcessList[pid].InputJoyH = newInputJoyH;
                            Processes.ProcessList[pid].ViewInputJoyH = (float)newInputJoyH / 3600000;
                        }
                        if (rlist[i].IsInputKey || rlist[i].IsInputMouse)
                        {
                            elapsedTicks = currentTick - rlist[i].InputKMTick;
                            newInputKMH = rlist[i].AddInputKMH + elapsedTicks;
                            Processes.ProcessList[pid].InputKMH = newInputKMH;
                            Processes.ProcessList[pid].ViewInputKMH = (float)newInputKMH / 3600000;
                        }
                        if (rlist[i].IsInputKey || rlist[i].IsInputMouse || rlist[i].IsInputJoy)
                        {
                            elapsedTicks = currentTick - rlist[i].InputTick;
                            newInputH = rlist[i].AddInputH + elapsedTicks;
                            Processes.ProcessList[pid].InputH = newInputH;
                            Processes.ProcessList[pid].ViewInputH = (float)newInputH / 3600000;
                        }

                        string newLine = line.Replace($",{realhours},{realminh},{realfocush},{realinputh},{realinputkeyh},{realinputmouseh},{realinputkmh},{realinputjoyh},", $",{newHours},{newMinH},{newFocusH},{newInputH},{newInputKeyH},{newInputMouseH},{newInputKMH},{newInputJoyH},");
                        newLine = newLine.Replace($"{realfirst},{reallast}", $"{Processes.ProcessList[pid].First},{currentDate}");

                        Processes.ProcessList[pid].Hours = newHours;
                        Processes.ProcessList[pid].ViewHours = (float)newHours / 3600000;

                        if (!TimeRecorder.MainWindow.IsOpen)
                        {
                            Processes.ProcessList[pid].Last = currentDate;
                        }

                        if (line.Split(',').ElementAt(7) == "waitWnd")
                        {
                            IntPtr hWnd = IntPtr.Zero;
                            string pngName;

                            if (RecordWnd == "0")
                            {
                                try
                                {
                                    Process p = Process.GetProcessById(rlist[i].IDs[0]);
                                    pngName = p.ProcessName;
                                    hWnd = p.MainWindowHandle;
                                }
                                catch { fileLines[pid] = newLine; break; }
                            }
                            else
                            {
                                try
                                {
                                    pngName = rlist[i].WndName;
                                    hWnd = (IntPtr)rlist[i].hWnds[0];
                                }
                                catch { fileLines[pid] = newLine; break; }
                            }

                            IntPtr hIcon;
                            uint WM_GETICON = 0x007f;
                            IntPtr ICON_SMALL2 = new IntPtr(2);

                            hIcon = SendMessage(hWnd, WM_GETICON, ICON_SMALL2, IntPtr.Zero);

                            if (hIcon == IntPtr.Zero)
                            {
                                hIcon = (IntPtr)GetClassLong(hWnd, -14);
                            }

                            BitmapSource bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                                hIcon,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());

                            bitmapSource.Freeze();

                            pngName = pngName.Trim().Replace(" ", "_").ToLower() + "_" + new Random(Environment.TickCount).Next(10000000, 99999999) + "\"";
                            pngName = Regex.Replace(pngName, @"\/|/|:|\*|\?|\""|<|>|\|", "");

                            string pngPath = programPath + @"\icons\" + pngName + ".png";

                            try //Had an issue with this, not anymore but i left it just to make sure it is impossible it crashes.
                            {
                                Directory.CreateDirectory(programPath + @"\icons\");
                                using (var fileStream = new FileStream(pngPath, FileMode.Create))
                                {
                                    BitmapEncoder encoder = new PngBitmapEncoder();
                                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                                    encoder.Save(fileStream);
                                }
                            }
                            catch
                            { 
                                fileLines[pid] = newLine; 
                                break; 
                            }

                            if (TimeRecorder.MainWindow.IsOpen)
                            {
                                Processes.ProcessList[pid].Ico = bitmapSource;
                            }
                            Processes.ProcessList[pid].IcoDir = @".\icons\" + pngName + ".png";

                            newLine = newLine.Replace($",waitWnd,", $",{@".\icons\" + pngName + ".png"},");
                        }

                        fileLines[pid] = newLine;
                        break;
                    }
                }
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(file);
                bool nullChar = false; 

                for (int i = 0; i < fileBytes.Length; i++)
                {
                    if (fileBytes[i] == 0)
                    {
                        nullChar = true;
                        //Console.WriteLine("1");
                        break;
                    }
                }

                if (doBackup && !nullChar)
                {
                    //Console.WriteLine("2");
                    // Create a FileInfo  
                    System.IO.FileInfo fi = new System.IO.FileInfo(file);
                    // Check if file is there  
                    if (fi.Exists)
                    {
                        // Move file with a new name. Hence renamed.
                        File.Delete(file + ".bak");
                        fi.MoveTo(  file + ".bak");
                        //Console.WriteLine("3");
                    }
                    doBackup = false;
                }
                //Console.WriteLine("E");
                System.IO.FileInfo fi2 = new System.IO.FileInfo(file);
                if (!fi2.Exists)
                {
                    using (File.Create(file)){}
                    //Console.WriteLine("4");
                }
                else
                {
                    FileStream fs = File.Open(file, FileMode.Open);
                    fs.SetLength(0);
                    fs.Close();
                    //Console.WriteLine("5");
                }

                //for (int i = 0; i < fileLines.Length; i++)
                //{
                    string str = string.Join("\n", fileLines);
                    str = str + "\n";
                    
                    byte[] data = new UTF8Encoding(true).GetBytes(str);
                    using (FileStream fs = new FileStream(file, FileMode.Append, FileAccess.Write,
                                                    FileShare.Read, data.Length, FileOptions.WriteThrough))
                    {
                        fs.Write(data, 0, data.Length);
                        //Console.WriteLine("6");
                    }
                //}
            }
            catch (IOException)
            {
                return;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            _notifyIcon?.Dispose();
            rtimer?.Dispose();
            UnhookWinEvent(FocusPEventHook);

            if (RunningProcesses.RunningProcessesList.Count > 0)
            {
                saveListFile(false);
            }
        }
    }

    public class RunningProcesses
    {
        public static List<RunningProcess> RunningProcessesList { get; set; } = GetRunningProcessList();
        public static List<RunningProcess> GetRunningProcessList()
        {
            var list = new List<RunningProcess>();
            return list;
        }
    }

    public class RunningProcess
    {
        public int Index { get; set; }
        public List<int> IDs { get; set; }
        public List<int> hWnds { get; set; }
        public string Dir { get; set; }
        public string PName { get; set; }
        public string WndName { get; set; }
        public long AddHours { get; set; }
        public long TimeTick { get; set; }
        public bool IsMin { get; set; }
        public long AddMinH { get; set; }
        public long MinTick { get; set; }
        public bool IsFocus { get; set; }
        public long AddFocusH { get; set; }
        public long FocusTick { get; set; }
        public long InputWaitT { get; set; }
        public long InputSaveT { get; set; }
        //Any
        public long AddInputH { get; set; }
        public long LastInputTick { get; set; }
        public long InputTick { get; set; }
        //Keyboard&Mouse
        public long AddInputKMH { get; set; }
        public long LastInputKMTick { get; set; }
        public long InputKMTick { get; set; }
        //Mouse
        public long AddInputMouseH { get; set; }
        public long LastInputMouseTick { get; set; }
        public long InputMouseTick { get; set; }
        public bool IsInputMouse { get; set; }
        //Keyboard
        public long AddInputKeyH { get; set; }
        public long LastInputKeyTick { get; set; }
        public long InputKeyTick { get; set; }
        public bool IsInputKey { get; set; }
        //Controller
        public long AddInputJoyH { get; set; }
        public long LastInputJoyTick { get; set; }
        public long InputJoyTick { get; set; }
        public bool IsInputJoy { get; set; }
    }

    internal class PHook
    {
        public int Id { get; set; }
        public List<IntPtr> Hooks { get; set; }
        public List<int> hWnds { get; set; }
    }
}
