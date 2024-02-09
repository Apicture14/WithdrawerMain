using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace WithdrawerMain
{
    public static class Configuration
    {
        public static Serializer Serializer = new Serializer();
        public static Deserializer Deserializer = new Deserializer();

        public static Config CreateDefault()
        {
            return new Config()
            {
                Indentifier = FileUtils.ExCode(Form1.Identifier, Form1.CryptKey, Encoding.UTF8),
                Version = Form1.Version,
                Targets = new List<Target>()
                {
                    new Target()
                    {
                        ProcessName = "CHROME",
                        WindowInfo = new WindowInfo()
                        {
                            Title = "CHROME",
                            ClassName = null
                        },
                        ExecutionMethod = Consts.ExecutionMethod.RequestClose,
                        Identifier = Consts.Identifier.Chrome
                    },
                    new Target()
                    {
                        ProcessName = "MICROSOFT EDGE",
                        WindowInfo = new WindowInfo()
                        {
                            Title = "EDGE",
                            ClassName = null
                        },
                        ExecutionMethod = Consts.ExecutionMethod.RequestClose,
                        Identifier = Consts.Identifier.Edge
                    },
                    new Target()
                    {
                        ProcessName = "HIPSMAIN",
                        WindowInfo = new WindowInfo()
                        {
                            Title = "火绒安全软件",
                            ClassName = "HLBRMainUI"
                        },
                        ExecutionMethod = Consts.ExecutionMethod.RequestClose
                    },
                    new Target()
                    {
                        ProcessName = "MSEDGE",
                        WindowInfo = new WindowInfo()
                        {
                            Title = "EDGE",
                            ClassName = null
                        },
                        ExecutionMethod = Consts.ExecutionMethod.RequestClose,
                        Identifier = Consts.Identifier.Edge
                    }
                },
                TimeSpans = new List<TimeSpan>()
                {
                    new TimeSpan()
                    {
                        Start = 000000,
                        Finish = 235959
                    }
                },
                DelaySpan = new TimeSpan()
                {
                    Start = 3000,
                    Finish = -1
                },
                UseDaemon = true,
                DaemonInfo = new DaemonInfo()
                {
                    Flag = "Flag"
                }
            };
        }

        public static Config Read(string path)
        {
            if (File.Exists(path))
            {
                using (FileStream ReadStream = new FileStream(path,FileMode.Open,FileAccess.Read))
                {
                    try
                    {
                        byte[] bcontent = new byte[ReadStream.Length];
                        ReadStream.Read(bcontent, 0, bcontent.Length);
                        ReadStream.Close();
                        string content = Encoding.UTF8.GetString(bcontent);
                        return Deserializer.Deserialize<Config>(content);
                    }
                    catch (Exception e)
                    {
                        Form1.Log(e.Message+"\r\n"+e.StackTrace,"E");
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
        }
        
        public static bool Write(Config cfg,string path)
        {
            if (Directory.Exists(path))
            {
                string nPath = path + Consts.ConfigFilePath;
                try
                {
                    using (FileStream WriteStream = new FileStream(nPath,FileMode.Create,FileAccess.Write))
                    {
                        byte[] bc = Encoding.UTF8.GetBytes(Serializer.Serialize(cfg));
                        WriteStream.Write(bc,0,bc.Length);
                        WriteStream.Close();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    
                    return false;
                }       
            }
            else
            {
                return false;
            }
        }
    }
}