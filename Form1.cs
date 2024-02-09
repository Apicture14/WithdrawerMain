using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Automation;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace WithdrawerMain
{
    public partial class Form1 : Form
    {
        #region BasicWindowInfos

        public static readonly string Identifier = "WITHDRAWER";
        public static readonly byte CryptKey = 0x6A;
        public static readonly int Version = 1;
        public static IntPtr MainWindowHandle = IntPtr.Zero;

        #endregion

        public static Config AppliedConfig = new Config();
        public WinApi.EnumWindowsCallBack callBack = new WinApi.EnumWindowsCallBack(EnumFindWindow);

        #region ObjectsCreate

        public static Timer WorkTimer = new Timer();

        public static System.Threading.Timer ListenTimer =
            new System.Threading.Timer(new TimerCallback(Listen), null, 1000, 1000);

        public static Random rand = new Random();

        public static FileStream logFile = new FileStream(
            Consts.LogFolderPath + $"\\log{DateTime.Now.ToString("HHmmss")}.txt",
            FileMode.Create, FileAccess.ReadWrite);

        public static List<Target> ValidTargets = new List<Target>();

        #endregion

        public static bool useRand = false;
        public static bool isDaemoned = false;
        public static int Counter = 0;

        public static void Log(string text, string level = "I")
        {
            string msg = $"[{DateTime.Now.ToShortTimeString()}] <{level}> {text}\r\n";

            byte[] bmsg = Encoding.UTF8.GetBytes(msg);
            logFile.Write(bmsg, 0, bmsg.Length);
            logFile.Flush();

            //Console.WriteLine(msg);
        }

        public Form1(string Start)
        {
            InitializeComponent();
            
            Log(Start);
            
            Config cfg = Configuration.Read(Consts.ConfigFilePath);
            if (cfg != null)
            {
                AppliedConfig = cfg;
                Log("LOAD SUCCESS");
            }
            else
            {
                Log("LOAD FAILED", "W");
                AppliedConfig = Configuration.CreateDefault();
            }

            Log(AppliedConfig.Shout());

            if (AppliedConfig.DelaySpan.Finish != -1)
            {
                useRand = true;
            }

            WorkTimer.Interval = AppliedConfig.DelaySpan.Start;
            WorkTimer.Elapsed += new ElapsedEventHandler(Run);

            WorkTimer.AutoReset = true;

            if (AppliedConfig.UseDaemon)
            {
                if (AppliedConfig.DaemonInfo != null)
                {
                    if (File.Exists("./" + AppliedConfig.DaemonInfo.Flag + AppliedConfig.DaemonInfo.FlagExt))
                    {
                        File.Delete("./" + AppliedConfig.DaemonInfo.Flag + AppliedConfig.DaemonInfo.FlagExt);
                    }

                    if (!File.Exists("./.daemon"))
                    {
                        DaemonUtils.AddToDaemon(AppliedConfig.DaemonInfo.Flag);
                    }

                    isDaemoned = true;
                }
                else
                {
                    Log("Wrong daemon info");
                    AppliedConfig.UseDaemon = false;
                }
            }


            //Configuration.Write(Configuration.CreateDefault(), "D:\\");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            MainWindowHandle = this.Handle;
            Console.Write(MainWindowHandle);
            WorkTimer.Start();
        }

        protected override void OnShown(EventArgs e)
        {
            if (MainWindowHandle != IntPtr.Zero)
            {
                WinApi.ShowWindow(MainWindowHandle, WinApi.SW_HIDE);
            }

            base.OnShown(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (MessageBox.Show("Real", "?", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Exit();
                e.Cancel = false;
            }
            else
            {
                e.Cancel = false;
            }
            
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            Console.WriteLine("e");
            base.OnClosed(e);
        }

        private void Run(object s, ElapsedEventArgs e)
        {
            WorkTimer.Stop();
            Counter++;
            int nowTime = Convert.ToInt32(DateTime.Now.ToString("hhmmss"));
            TimeSpan nowtimeSpan = AppliedConfig.TimeSpans[0];
            foreach (var timeSpan in AppliedConfig.TimeSpans)
            {
                if (nowTime >= timeSpan.Start && nowTime < timeSpan.Finish)
                {
                    nowtimeSpan = timeSpan;
                    Log($"Running in routine {AppliedConfig.TimeSpans.IndexOf(nowtimeSpan)}");
                    goto KillProcedure;
                }
                else
                {
                    Log("Idling");
                    goto FinishProcedure;
                }
            }

            KillProcedure:
            foreach (var target in AppliedConfig.Targets)
            {
                if (target.ExecutionMethod == Consts.ExecutionMethod.KillProcess)
                {
                    List<Process> HKillTargets = Process.GetProcesses()
                        .Where(p => p.ProcessName.ToUpper() == target.ProcessName).ToList();
                    foreach (var hKillTarget in HKillTargets)
                    {
                        string name = hKillTarget.ProcessName;
                        string pid = hKillTarget.Id.ToString();

                        try
                        {
                            hKillTarget.Kill();
                            hKillTarget.WaitForExit(1000);
                            Log($"HK Executed {name}:{pid}");
                            AntiBrowserRecov(target.Identifier);
                        }
                        catch (Exception ex)
                        {
                            Log("HKP) " + ex.Message, "E");
                        }
                    }

                    Log($"Loop{Counter} Finished");
                }
                else if (target.ExecutionMethod == Consts.ExecutionMethod.RequestClose)
                {
                    ValidTargets.Clear();
                    WinApi.EnumWindows(callBack, 0);
                    if (ValidTargets.Count == 0)
                    {
                        //Log("SKP) No Window Found");
                    }
                    else
                    {
                        Log($"{ValidTargets.Count} Window(s) Found");
                        foreach (var validTarget in ValidTargets)
                        {
                            ActionClose:
                            if (validTarget.RequestMethod == Consts.RequestMethod.SendMessage)
                            {
                                WinApi.SendMessage(validTarget.WindowInfo.Handle, WinApi.MSG_CLOSE, 0, 0);
                                Thread.Sleep(500);
                                if (WinApi.FindWindowA(validTarget.WindowInfo.ClassName,
                                        target.WindowInfo.Title) != IntPtr.Zero)
                                {
                                    validTarget.RequestMethod = Consts.RequestMethod.Automation;
                                    goto ActionClose;
                                }
                            }

                            else if (validTarget.RequestMethod == Consts.RequestMethod.Automation)
                            {
                                AutomationElement ae =
                                    AutomationElement.FromHandle(validTarget.WindowInfo.Handle);
                                ((WindowPattern)ae.GetCurrentPattern(WindowPattern.Pattern)).Close();
                            }
                            else if (validTarget.RequestMethod == Consts.RequestMethod.Destroy)
                            {
                                WinApi.DestroyWindow(validTarget.WindowInfo.Handle);
                            }

                            Log($"SK Executed {validTarget.WindowInfo.Handle}:{validTarget.WindowInfo.Title}");
                            goto FinishProcedure;
                        }
                    }
                }
            }
            FinishProcedure:
            int newInterval = 0;
            if (useRand)
            {
                newInterval = rand.Next(AppliedConfig.DelaySpan.Start, AppliedConfig.DelaySpan.Finish);
            }
            else
            {
                newInterval = AppliedConfig.DelaySpan.Start;
            }

            WorkTimer.Interval = newInterval;
            WorkTimer.Start();

            Log($"Loop{Counter} Finished, {newInterval}ms before next");
        }

        private static void Listen(object o)
        {
            // Log("Listening");
            string useFlag = "";
            FileStream ListenStream = null;
            DirectoryInfo dir = new DirectoryInfo(Consts.ControlFolderPath);
            foreach (var file in dir.GetFiles())
            {
                if (file.Extension == Consts.FlagExt && Consts.Flags.Contains(file.Name.Split('.')[0]))
                {
                    useFlag = file.Name.Split('.')[0];
                    Log($"Found Flag {useFlag}, Verifying");
                    ListenStream = new FileStream(Consts.ControlFolderPath + "\\" + useFlag + Consts.FlagExt,
                        FileMode.Open, FileAccess.ReadWrite);
                    goto VerifyProcedure;
                }
                else
                {
                    goto FinishProcedue;
                }

                VerifyProcedure:
                if (ListenStream == null)
                {
                    return;
                }

                byte[] bc = new byte[ListenStream.Length];
                ListenStream.Read(bc, 0, bc.Length);
                string c = Encoding.UTF8.GetString(bc);
                ListenStream.Close();
                // Log(FileUtils.ExCode(c, CryptKey, Encoding.UTF8) + "  " + Identifier);
                if (FileUtils.ExCode(c, CryptKey, Encoding.UTF8) == Identifier)
                {
                    Log($"{useFlag} Verified, Implementing");
                    goto HandleProcedure;
                }
                else
                {
                    Log("Invalid Control");
                    File.Copy(Consts.ControlFolderPath + "\\" + useFlag + Consts.FlagExt,
                        Consts.ControlFolderPath + "\\" + useFlag + Consts.FlagExt + "x", true);
                    File.Delete(Consts.ControlFolderPath + "\\" + useFlag + Consts.FlagExt);
                    goto FinishProcedue;
                }

                HandleProcedure:
                switch (useFlag)
                {
                    case "STOP":
                        File.Delete(Consts.ControlFolderPath + "\\" + useFlag + Consts.FlagExt);
                        Exit();
                        break;
                    case "RELOAD":
                        Config cfg = Configuration.Read(Consts.ConfigFilePath);
                        if (cfg != null)
                        {
                            AppliedConfig = cfg;
                            Log(AppliedConfig.Shout());
                            if (AppliedConfig.DelaySpan.Finish != -1)
                            {
                                useRand = true;
                            }
                            else
                            {
                                useRand = false;
                            }

                            if (AppliedConfig.UseDaemon || !isDaemoned)
                            {
                                DaemonUtils.AddToDaemon(AppliedConfig.DaemonInfo.Flag);
                            }
                        }
                        else
                        {
                            Log("RELOAD FAILED", "W");
                        }

                        break;
                }

                File.Delete(Consts.ControlFolderPath + "\\" + useFlag + Consts.FlagExt);
                goto FinishProcedue;
                FinishProcedue:
                return;
            }
        }

        private static void Exit()
        {
            WorkTimer.Stop();
            Log("Stopping...");
            if (logFile != null)
            {
                logFile.Close();
            }
            
            if (AppliedConfig.UseDaemon)
            {
                File.Create("./"+AppliedConfig.DaemonInfo.Flag+AppliedConfig.DaemonInfo.FlagExt);
            }
        }

        private static bool EnumFindWindow(int handle, int lparm)
        {
            IntPtr Phandle = new IntPtr(handle);
            int length = WinApi.GetWindowTextLength(Phandle);
            StringBuilder sbn = new StringBuilder(length + 1);
            StringBuilder sbc = new StringBuilder(256);
            WinApi.GetWindowText(Phandle, sbn, 256);
            WinApi.GetClassName(Phandle, sbc, 256);
            if (Phandle == IntPtr.Zero)
            {
                return true;
            }

            if (sbn.ToString() == "")
            {
                return true;
            }
            //Log(handle.ToString()+" "+sbn.ToString()+ " "+sbc.ToString());

            foreach (var target in AppliedConfig.Targets)
            {
                if (target.ExecutionMethod == Consts.ExecutionMethod.KillProcess)
                {
                    continue;
                }
                if (target.WindowInfo.Title != null && target.WindowInfo.ClassName == null)
                {
                    if (sbn.ToString().ToUpper().Contains(target.WindowInfo.Title.ToUpper()))
                    {
                        Target tar = new Target();
                        tar.ExecutionMethod = target.ExecutionMethod;
                        tar.RequestMethod = target.RequestMethod;
                        tar.WindowInfo = new WindowInfo()
                        {
                            ClassName = sbc.ToString(),
                            Handle = Phandle,
                            Title = sbn.ToString()
                        };
                        if (!ValidTargets.Contains(tar))
                        {
                            ValidTargets.Add(tar);
                        }
                    }
                }
                else if (target.WindowInfo.Title == null && target.WindowInfo.ClassName != null)
                {
                    if (sbc.ToString().ToUpper().Contains(target.WindowInfo.ClassName.ToUpper()))
                    {
                        Target tar = new Target();
                        tar.ExecutionMethod = target.ExecutionMethod;
                        tar.RequestMethod = target.RequestMethod;
                        tar.WindowInfo = new WindowInfo()
                        {
                            ClassName = sbc.ToString(),
                            Handle = Phandle,
                            Title = sbn.ToString()
                        };
                        if (!ValidTargets.Contains(tar))
                        {
                            ValidTargets.Add(tar);
                        }
                    }
                }
                else if (target.WindowInfo.Title != null && target.WindowInfo.ClassName != null)
                {
                    if (sbn.ToString().ToUpper().Contains(target.WindowInfo.Title.ToUpper())
                        && sbc.ToString().ToUpper().Contains(target.WindowInfo.ClassName.ToUpper()))
                    {
                        Target tar = new Target();
                        tar.ExecutionMethod = target.ExecutionMethod;
                        tar.RequestMethod = target.RequestMethod;
                        tar.WindowInfo = new WindowInfo()
                        {
                            ClassName = sbc.ToString(),
                            Handle = Phandle,
                            Title = sbn.ToString()
                        };
                        if (!ValidTargets.Contains(tar))
                        {
                            ValidTargets.Add(tar);
                        }
                    }
                }
                else
                {
                    ValidTargets.Add(target);
                }
            }

            return true;
        }

        private void AntiBrowserRecov(Consts.Identifier type)
        {
            if (type == Consts.Identifier.Any)
            {
                return;
            }

            string chrome = @"\Google\Chrome\User Data\Default\Preferences";
            string edge = @"\Microsoft\Edge\User Data\Default\Preferences";
            string RootPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string finPath = "";
            switch (type)
            {
                case Consts.Identifier.Edge:
                    finPath = RootPath + edge;
                    break;
                case Consts.Identifier.Chrome:
                    finPath = RootPath + chrome;
                    break;
            }

            using (FileStream fs = new FileStream(finPath, FileMode.Open, FileAccess.ReadWrite))
            {
                byte[] buf = new byte[fs.Length];
                fs.Read(buf, 0, buf.Length);
                string c = Encoding.UTF8.GetString(buf);
                buf = Encoding.UTF8.GetBytes(c.Replace("Crashed", "Normal"));
                fs.SetLength(0);
                fs.Write(buf, 0, buf.Length);
                fs.Close();
                Log($"Anti {type} Finished");
            }
        }
    }
}