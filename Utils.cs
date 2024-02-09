using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace WithdrawerMain
{
    public static class FileUtils
    {
        public static string ExCode(string ori, byte key,Encoding encoding)
        {
            byte[] bori = encoding.GetBytes(ori);
            for (int i = 0; i < bori.Length; i++)
            {
                bori[i] = (byte)(bori[i] ^ key);
            }

            return encoding.GetString(bori);
        }
        public static bool Check()
        {
            try
            {
                if (!Directory.Exists(Consts.ControlFolderPath))
                {
                    Directory.CreateDirectory(Consts.ControlFolderPath);
                }

                if (!Directory.Exists(Consts.LogFolderPath))
                {
                    Directory.CreateDirectory(Consts.LogFolderPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    
}