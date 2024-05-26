using Carousels.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing.Drawing2D;
using System.IO;
using System.Web.Mvc;

namespace Carousels.App_Code
{
    public class Core
    {
        private readonly string RootDomain;

        public Core()
        {
            RootDomain = null;
        }

        public Core(string rootDomain)
        {
            RootDomain = rootDomain;
        }

        /// <summary>
        /// 取得節目表
        /// </summary>
        public void GetProgramme(Loger Log, List<ArchiveInfo> FS, string C_NO)
        {
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT A.C_NO, B.P_NAME, C.PY_SECOND, D.F_TYPE, D.F_PATH
FROM PLAYER_LIST A
LEFT JOIN PROGRAMME B ON A.P_NO = B.P_NO
LEFT JOIN PROGRAMME_LIST C ON B.P_NO = C.P_NO
LEFT JOIN PROGRAMME_DATA D ON C.F_NO = D.F_NO
WHERE A.C_NO = @C_NO AND C.STAT IS NULL AND D.STAT IS NULL
ORDER BY A.ITEM, C.ITEM
                    "))
                    {
                        cmd.Parameters.AddWithValue("@C_NO", C_NO);

                        using (SQLiteDataReader data = cmd.ExecuteReader())
                        {
                            while (data.Read())
                            {
                                // 讀取資料夾
                                if (data["F_TYPE"].ToString() == "Folder")
                                {
                                    GetFiles(Log, FS, data["F_PATH"].ToString(), data["PY_SECOND"].ToString());
                                }
                                // 讀取網址
                                else if(data["F_TYPE"].ToString() == "URL")
                                {
                                    FS.Add(new ArchiveInfo
                                    {
                                        // 副檔名
                                        Type = ".html",
                                        // 網域位置 + 檔名
                                        Url = data["F_PATH"].ToString(),
                                        // 播放時間
                                        Second = data["PY_SECOND"].ToString()
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(string.Format("節目讀取失敗 -> {0}", ex.Message));
            }
        }


        /// <summary>
        /// 取得資料夾底下所有檔案
        /// </summary>
        public void GetFiles(Loger Log, List<ArchiveInfo> FS, string path, string second)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string wwwFolderPath = Path.Combine(RootDomain, "Assets", "DataHome");                          // 資料夾網域位置            
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "DataHome");  // 資料夾位置

            if (path.IndexOf("\\\\nas05\\T_電子看板內容") == 0)
            {
                path = path.Replace("\\\\nas05\\T_電子看板內容", folderPath);
            }
            else if (path.IndexOf("~") == 0)
            {
                path = folderPath + path.Substring(1);
            }

            path = Path.GetFullPath(path);

            if (!Directory.Exists(path))
            {
                return;
            }

            try
            {
                DirectoryInfo dif = new DirectoryInfo(path);
                // 資料夾內所有檔案抓取
                foreach (System.IO.FileInfo file in dif.GetFiles("*", SearchOption.AllDirectories))
                {

                    FS.Add(new ArchiveInfo
                    {
                        // 副檔名
                        Type = file.Extension.ToLower(),
                        // 網域位置 + 檔名
                        Url = file.FullName.Replace(folderPath, wwwFolderPath),
                        // 播放時間
                        Second = second
                    });
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(string.Format("資料夾讀取失敗 -> {0}", ex.Message));
            }
        }
        
        /// <summary>
        /// 取得版本
        /// </summary>
        public object GetVersion()
        {
            object result = null;

            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT C_NAME 
FROM PLAYER
WHERE C_NO = 'C00000000'
            "))
                    {
                        result = cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Loger Log = new Loger("API", "GetVersion", "system");

                Log.WriteLine(ex.Message);
            }

            return result;
        }
        
        /// <summary>
        /// 輪播內容最新的更新時間
        /// </summary>
        public object GetUpdateTime(string C_NO)
        {
            object result = null;

            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
UPDATE PLAYER
SET Client = @LINK_TIME
WHERE C_NO = @C_NO
            "))
                    {
                        cmd.Parameters.AddWithValue("@C_NO", C_NO);
                        cmd.Parameters.AddWithValue("@LINK_TIME", DateTime.Now);

                        // 更新使用端連線時間
                        cmd.ExecuteNonQuery();

                        // 取得節目版本最新時間
                        cmd.CommandText = @"
SELECT MAX(C.UpdateTime) UpdateTime
FROM PLAYER_LIST A
LEFT JOIN PROGRAMME_LIST B ON A.P_NO = B.P_NO
LEFT JOIN PROGRAMME_DATA C ON B.F_NO = C.F_NO
WHERE A.C_NO = @C_NO
                        ";
                        result = cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Loger Log = new Loger(C_NO, "GetUpdateTime", "system");

                Log.WriteLine(ex.Message);
            }

            return result;
        }
    }
}