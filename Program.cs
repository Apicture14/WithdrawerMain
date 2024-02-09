using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WithdrawerMain
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (!FileUtils.Check())
            {
                return;
            }

            try
            {
                if (args.Length!=0&&args[0] == "Gen")
                {
                    Configuration.Write(Configuration.CreateDefault(), "./");
                }
                else if (args.Length!=0&&args[0] == "RV")
                {
                    Application.EnableVisualStyles(); 
                    Application.SetCompatibleTextRenderingDefault(false); 
                    Application.Run(new Form1("Restarted By Daemon"));
                }
                else 
                {
                     Application.EnableVisualStyles(); 
                     Application.SetCompatibleTextRenderingDefault(false); 
                     Application.Run(new Form1("Normal"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        
    }
}