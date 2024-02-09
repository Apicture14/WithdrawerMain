using System;
using System.Collections.Generic;
using System.Text;

namespace WithdrawerMain
{
    public static class Consts
    {
        public static readonly string LogFolderPath = @".\Logs";
        public static readonly string ControlFolderPath = @".\Controls";
        public static readonly string ConfigFilePath = @".\config.yaml";
        
        public enum ExecutionMethod
        {
            KillProcess=0,
            RequestClose=1
        }
        public enum RequestMethod
        {
            SendMessage=0,
            Automation,
            Destroy
        }
        public enum Identifier
        {
            Chrome,
            Edge,
            Any
        }

        public static List<string> Flags = new List<string>(){ "STOP", "RELOAD" };
        public static readonly string FlagExt = ".ctr";
    }
    public class Config
    {
        public Config()
        {
            
        }
        public string Indentifier { get; set; }
        public int Version { get; set; }
        public List<Target> Targets { get; set; }
        public List<TimeSpan> TimeSpans { get; set; }
        public TimeSpan DelaySpan { get; set; }
        public bool UseDaemon { get; set; } = false;
        public DaemonInfo DaemonInfo { get; set; } = null;

        public string Shout()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Config At Version {Version} \r\n");
            sb.Append($"Targets {Targets.Count} \r\n");
            foreach (var tar in Targets)
            {
                sb.Append($"Proc:{tar.ProcessName}=>{tar.ExecutionMethod}\r\n");
            }

            sb.Append($"Timespans:\r\n");
            foreach (var timeSpan in TimeSpans)
            {
                sb.Append($"{timeSpan.Start}->{timeSpan.Finish}\r\n");
            }

            sb.Append($"Delay vary {DelaySpan.Start} -> {DelaySpan.Finish}");
            

            return sb.ToString();
        }
    }

    public class Target
    {
        public string ProcessName { get; set; }
        public WindowInfo WindowInfo { get; set; }
        public Consts.ExecutionMethod ExecutionMethod { get; set; }
        public Consts.RequestMethod RequestMethod { get; set; } = Consts.RequestMethod.SendMessage;
        public Consts.Identifier Identifier { get; set; } = Consts.Identifier.Any;
    }
    public class WindowInfo
    {
        public string Title { get; set; }
        public string ClassName { get; set; }
        public IntPtr Handle { get; set; }
    }

    public class TimeSpan
    {
        public int Start { get; set; }
        public int Finish { get; set; }
    }
}