using System;
using System.IO;

namespace Carousels.App_Code
{
    public class Loger
    {
        private readonly string Folder;
        private readonly string LogFile;
        private readonly string Title;

        public Loger(string root, string folder, string IP)
        {
            Folder = string.Format(@"{0}\App_Data\Log", System.AppDomain.CurrentDomain.BaseDirectory);
            LogFile = string.Format(@"{0}\Carousels_Log_{1}.txt", Folder, DateTime.Now.ToString("yyyy-MM-dd"));
            Title = string.Format("[{0}][{1}], {2}", root, folder, IP);

            // 建立Log資料夾
            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
            }
        }

        public void WriteLine(string message)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(LogFile, true))
                {
                    sw.WriteLine("{0}, {1}, {2}", DateTime.Now.ToString(), Title, message);
                }
            }
            catch (Exception)
            {

            }
        }
    }
}