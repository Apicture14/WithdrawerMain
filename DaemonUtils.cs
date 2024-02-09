using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace WithdrawerMain
{
    public class DaemonUtils
    {
        public static void AddToDaemon(string FlagName)
        {
            string ExecutablePath = Process.GetCurrentProcess().MainModule.FileName.Split('\\').Last();
            if (File.Exists(ExecutablePath))
            {
                DaemonInfo d = new DaemonInfo()
                {
                    Executable = ExecutablePath,
                    Flag = FlagName
                };
                using (FileStream WriteStream = new FileStream("./.daemon",FileMode.OpenOrCreate,FileAccess.ReadWrite))
                {
                    byte[] bc = Encoding.UTF8.GetBytes(Configuration.Serializer.Serialize(d));
                    WriteStream.Write(bc,0,bc.Length);
                    WriteStream.Close();
                }
            }
        }

        public static void ReleaseDaemon()
        {
            if (File.Exists("./.daemon"))
            {
                File.Delete("./.daemon");
            }
            else
            {
                return;
            }
        }
    }

    public class DaemonInfo
    {
        public string Executable { get; set; }
        public string Flag { get; set; }
        public string FlagExt { get; set; } = ".flg";
    }
}