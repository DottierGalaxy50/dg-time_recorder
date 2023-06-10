using System;
using System.Threading;
using static TimeRecorder.App.NativeMethods;

namespace TimeRecorder
{
    internal class SystemInputsRefresh
    {
        static public Timer FocusInputsIntervals;

        static uint JOY_RETURNALL = 0x00000001 | 0x00000002 | 0x00000004 | 0x00000008 | 0x00000010 | 0x00000020 | 0x00000040 | 0x00000080;

        static LPJOYINFOEX JoystickInfo = new LPJOYINFOEX() { dwSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(LPJOYINFOEX)), dwFlags = JOY_RETURNALL };
        static LPJOYCAPS JoystickCapInfo = new LPJOYCAPS();

        static long lastKeyboardChange;
        static long lastMouseChange;
        static long lastJoystickChange;

        static uint[] lastJoystickPOVlist = new uint[16];
        public static int CustomJoyDeadZone;

        static POINT lastCursorPos = new POINT();
        static POINT currentCursorPos = new POINT();

        static bool CheckAllKeyboardInputs()
        {
            for (int i = 7; i < 256; i++)
            {
                if (GetKeyState(i) < 0)
                {
                    //Console.WriteLine("keyboard: " + i + " " + Environment.TickCount);
                    lastKeyboardChange = App.TimeSinceStart.ElapsedMilliseconds;
                    return true;
                }
            }
            if (GetKeyState(3) < 0)
            {
                //Console.WriteLine("keyboard: 3 " + Environment.TickCount);
                lastKeyboardChange = App.TimeSinceStart.ElapsedMilliseconds;
                return true;
            }

            return false;
        }

        static bool CheckAllMouseInputs()
        {
            GetCursorPos(ref currentCursorPos);

            if (currentCursorPos.x != lastCursorPos.x || currentCursorPos.y != lastCursorPos.y)
            {
                //Console.WriteLine("change: " + currentCursorPos.x + " " + currentCursorPos.y + " " + Environment.TickCount);
                lastCursorPos.x = currentCursorPos.x;
                lastCursorPos.y = currentCursorPos.y;
                lastMouseChange = App.TimeSinceStart.ElapsedMilliseconds;
                return true;
            }
            if (GetKeyState(1) < 0 || GetKeyState(2) < 0 || GetKeyState(4) < 0 || GetKeyState(5) < 0 || GetKeyState(6) < 0)
            {
                //Console.WriteLine("mouse:" + Environment.TickCount);
                lastMouseChange = App.TimeSinceStart.ElapsedMilliseconds;
                return true;
            }

            return false;
        }

        static bool CheckAllJoysticksInputs()
        {
            for (uint i = 0; i < joyGetNumDevs(); ++i)
            {
                if (joyGetPosEx(i, ref JoystickInfo) == 0)
                {
                    if (JoystickInfo.dwButtonNumber != 0)
                    {
                        //Console.WriteLine("Pushing button: " + i + " " + Environment.TickCount);
                        lastJoystickChange = App.TimeSinceStart.ElapsedMilliseconds;
                        return true;
                    }

                    int JoystickThreshold = 0;
                    joyGetThreshold(i, ref JoystickThreshold);
                    joyGetDevCaps(i, ref JoystickCapInfo, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(LPJOYCAPS)));

                    if ((Math.Abs((int)JoystickInfo.dwXpos-(JoystickCapInfo.wXmin+JoystickCapInfo.wXmax)/2) > JoystickThreshold+CustomJoyDeadZone) ||
                    (Math.Abs((int)JoystickInfo.dwYpos-(JoystickCapInfo.wYmin+JoystickCapInfo.wYmax)/2) > JoystickThreshold+CustomJoyDeadZone) ||
                    ((JoystickCapInfo.wCaps & 1) != 0 && Math.Abs((int)JoystickInfo.dwZpos-(JoystickCapInfo.wZmin+JoystickCapInfo.wZmax)/2) > JoystickThreshold+CustomJoyDeadZone) ||
                    ((JoystickCapInfo.wCaps & 2) != 0 && Math.Abs((int)JoystickInfo.dwRpos-(JoystickCapInfo.wRmin+JoystickCapInfo.wRmax)/2) > JoystickThreshold+CustomJoyDeadZone) ||
                    ((JoystickCapInfo.wCaps & 4) != 0 && Math.Abs((int)JoystickInfo.dwUpos-(JoystickCapInfo.wUmin+JoystickCapInfo.wUmax)/2) > JoystickThreshold+CustomJoyDeadZone) ||
                    ((JoystickCapInfo.wCaps & 8) != 0 && Math.Abs((int)JoystickInfo.dwVpos-(JoystickCapInfo.wVmin+JoystickCapInfo.wVmax)/2) > JoystickThreshold+CustomJoyDeadZone))
                    {
                        //Console.WriteLine("Moving Axis: " + i + " " + Environment.TickCount);
                        lastJoystickChange = App.TimeSinceStart.ElapsedMilliseconds;
                        return true;
                    }

                    if ((JoystickCapInfo.wCaps & 16) != 0)
                    {
                        if ((JoystickCapInfo.wCaps & 64) != 0)
                        {
                            if (lastJoystickPOVlist[(int)i] != JoystickInfo.dwPOV)
                            {
                                //Console.WriteLine("POV Continuous: " + i + " " + Environment.TickCount);
                                lastJoystickChange = App.TimeSinceStart.ElapsedMilliseconds;
                                lastJoystickPOVlist[(int)i] = JoystickInfo.dwPOV;
                                return true;
                            }
                        }
                        else if (JoystickInfo.dwPOV != 65535)
                        {
                            //Console.WriteLine("POV Directional: " + i + " " + Environment.TickCount);
                            lastJoystickChange = App.TimeSinceStart.ElapsedMilliseconds;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        static public void RefreshAllFocusInputs(object stateInfo)
        {
            bool NewMouseInput = CheckAllMouseInputs();
            bool NewKeyboardInput = CheckAllKeyboardInputs();
            bool NewJoystickInput = CheckAllJoysticksInputs();

            var plist = Processes.ProcessList;
            var rlist = RunningProcesses.RunningProcessesList;

            for (int i = 0; i < rlist.Count; i++)
            {
                if (rlist[i].IsFocus)
                {
                    bool NewInput = false;

                    if (NewJoystickInput)
                    {
                        rlist[i].LastInputJoyTick = lastJoystickChange; NewInput = true;
                    }
                    if (NewKeyboardInput)
                    {
                        rlist[i].LastInputKeyTick = lastKeyboardChange; NewInput = true;
                    }
                    if (NewMouseInput)
                    {
                        rlist[i].LastInputMouseTick = lastMouseChange; NewInput = true;
                    }
                    if (NewInput)
                    {
                        for (int i2 = 0; i2 < plist.Count; i2++)
                        {
                            string wndname = plist[i2].WndName; if (!plist[i2].RecordWnd) { wndname = null; } // just want to be done with this
                            if (rlist[i].PName.Equals(plist[i2].PName) && rlist[i].WndName == wndname)
                            {
                                byte count1 = 0;
                                byte count2 = 0;

                                if (!rlist[i].IsInputKey)
                                {
                                    if (NewKeyboardInput)
                                    {
                                        rlist[i].IsInputKey = true;
                                        rlist[i].AddInputKeyH = plist[i2].InputKeyH;
                                        rlist[i].InputKeyTick = lastKeyboardChange;
                                        //Console.WriteLine("***Added Inputs Key*** " + i + " " + i2);
                                    }
                                    count1++; count2++;
                                }
                                if (!rlist[i].IsInputMouse)
                                {
                                    if (NewMouseInput)
                                    {
                                        rlist[i].IsInputMouse = true;
                                        rlist[i].AddInputMouseH = plist[i2].InputMouseH;
                                        rlist[i].InputMouseTick = lastMouseChange;
                                        //Console.WriteLine("***Added Inputs Mouse*** " + i + " " + i2);
                                    }
                                    count1++; count2++;
                                }
                                if (!rlist[i].IsInputJoy)
                                {
                                    if (NewJoystickInput)
                                    {
                                        rlist[i].IsInputJoy = true;
                                        rlist[i].AddInputJoyH = plist[i2].InputJoyH;
                                        rlist[i].InputJoyTick = lastJoystickChange;
                                        //Console.WriteLine("***Added Inputs Joy*** " + i + " " + i2);
                                    }
                                    count1++;
                                }
                                if (count1 == 3)
                                {
                                    long InputTick = Math.Max(Math.Max(lastKeyboardChange, lastMouseChange), lastJoystickChange);
                                    rlist[i].AddInputH = plist[i2].InputH;
                                    rlist[i].InputTick = InputTick;
                                    //Console.WriteLine("***Added Inputs*** " + i + " " + i2);
                                }
                                if (count2 == 2 && (NewKeyboardInput || NewMouseInput))
                                {
                                    long InputTick = Math.Max(lastKeyboardChange, lastMouseChange);
                                    rlist[i].AddInputKMH = plist[i2].InputKMH;
                                    rlist[i].InputKMTick = InputTick;
                                    //Console.WriteLine("***Added Inputs KM*** " + i + " " + i2);
                                }

                                break;
                            }
                        }
                    }
                }
                CheckRemoveInputOnProcess(i, rlist[i].InputWaitT, -1);
            }
        }

        public static void CheckRemoveInputOnProcess(int i, long wait, long save)
        {
            var plist = Processes.ProcessList;
            var rlist = RunningProcesses.RunningProcessesList;

            bool UseProcessInputSave = true; // Not used anymore, had to adapt it to new code but i decided not to as it had very little effect on the things i use this for. Only used for more "accurate" time-
            if (save != -1) { UseProcessInputSave = false; } // saving when an element had an InputWait going and the element stopped or changed and resulted on an element from the RunningProcessList getting removed.

            bool Save = false;
            bool SaveKey = false;
            bool SaveJoy = false;
            bool SaveMouse = false;

            if (rlist[i].IsInputKey)
            {
                long tSinceChange = (App.TimeSinceStart.ElapsedMilliseconds)-rlist[i].LastInputKeyTick;
                //Console.WriteLine("InputWaitT Key: " + wait + " " + tSinceChange);
                if (wait <= tSinceChange)
                {
                    SaveKey = true; Save = true;
                }
            }
            if (rlist[i].IsInputJoy)
            {
                long tSinceChange = (App.TimeSinceStart.ElapsedMilliseconds)-rlist[i].LastInputJoyTick;
                //Console.WriteLine("InputWaitT Joy: " + wait + " " + tSinceChange);
                if (wait <= tSinceChange)
                {
                    SaveJoy = true; Save = true;
                }
            }
            if (rlist[i].IsInputMouse)
            {
                long tSinceChange = (App.TimeSinceStart.ElapsedMilliseconds)- rlist[i].LastInputMouseTick;
                //Console.WriteLine("InputWaitT Mouse: " + wait + " " + tSinceChange);
                if (wait <= tSinceChange)
                {
                    SaveMouse = true; Save = true;
                }
            }
            if(Save)
            {
                for (int i2 = 0; i2 < plist.Count; i2++)
                {
                    string wndname = plist[i2].WndName; if (!plist[i2].RecordWnd) { wndname = null; } // just want to be done with this
                    if (rlist[i].PName.Equals(plist[i2].PName) && rlist[i].WndName == wndname)
                    {
                        if (UseProcessInputSave) { save = plist[i2].InputSaveT; }

                        long elapsedTicks;
                        long newInputH;

                        if (SaveKey)
                        {
                            rlist[i].IsInputKey = false;
                            elapsedTicks = (rlist[i].LastInputKeyTick - rlist[i].InputKeyTick) + save;
                            newInputH = rlist[i].AddInputKeyH + elapsedTicks;
                            plist[i2].InputKeyH = newInputH;
                            plist[i2].ViewInputKeyH = (float)newInputH / 3600000;
                            //Console.WriteLine("elapsedKeyTicks: " + (newInputH));
                            //Console.WriteLine("***Removed Inputs Key*** " + i + " " + i2);
                        }
                        if (SaveMouse)
                        {
                            rlist[i].IsInputMouse = false;
                            elapsedTicks = (rlist[i].LastInputMouseTick - rlist[i].InputMouseTick) + save;
                            newInputH = rlist[i].AddInputMouseH + elapsedTicks;
                            plist[i2].InputMouseH = newInputH;
                            plist[i2].ViewInputMouseH = (float)newInputH / 3600000;
                            //Console.WriteLine("elapsedMouseTicks: " + (newInputH));
                            //Console.WriteLine("***Removed Inputs Mouse*** " + i + " " + i2);
                        }
                        if (SaveJoy)
                        {
                            rlist[i].IsInputJoy = false;
                            elapsedTicks = (rlist[i].LastInputJoyTick - rlist[i].InputJoyTick) + save;
                            newInputH = rlist[i].AddInputJoyH + elapsedTicks;
                            plist[i2].InputJoyH = newInputH;
                            plist[i2].ViewInputJoyH = (float)newInputH / 3600000;
                            //Console.WriteLine("elapsedJoyTicks: " + (newInputH));
                            //Console.WriteLine("***Removed Inputs Joy*** " + i + " " + i2);
                        }
                        if (!rlist[i].IsInputKey && !rlist[i].IsInputMouse)
                        {
                            long LastInputTick = Math.Max(rlist[i].LastInputKeyTick, rlist[i].LastInputMouseTick);

                            elapsedTicks = (LastInputTick - rlist[i].InputKMTick) + save;
                            newInputH = rlist[i].AddInputKMH + elapsedTicks;
                            plist[i2].InputKMH = newInputH;
                            plist[i2].ViewInputKMH = (float)newInputH / 3600000;
                            //Console.WriteLine("elapsedKMTicks: " + (newInputH));
                            //Console.WriteLine("***Removed Inputs KM*** " + i + " " + i2);

                            if (!rlist[i].IsInputJoy)
                            {
                                LastInputTick = Math.Max(LastInputTick, rlist[i].LastInputJoyTick);

                                elapsedTicks = (LastInputTick - rlist[i].InputTick) + save;
                                newInputH = rlist[i].AddInputH + elapsedTicks;
                                plist[i2].InputH = newInputH;
                                plist[i2].ViewInputH = (float)newInputH / 3600000;
                                //Console.WriteLine("elapsedTicks: " + (newInputH));
                                //Console.WriteLine("***Removed Inputs*** " + i + " " + i2);
                            }
                        }
                    }
                }
            }
        }
    }
}
