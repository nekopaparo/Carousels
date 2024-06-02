using Carousels.App_Code;
using Carousels.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Web.Mvc;

namespace Carousels.Controllers
{
    public class ApiController : Controller
    {
        // GET: Api
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 取得版本資訊
        /// </summary>
        /// <param name="root"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetVersionInfo(string C_NO)
        {
            AJAXResponse ajaxResult = new AJAXResponse();

            try
            {
                Core core = new Core();

                object version = core.GetVersion();             // 取得主程式版本號
                object updateTime = core.GetUpdateTime(C_NO);   // 取得節目內容最新的更新時間

                ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(new VersionInfo
                {
                    Version = version == null ? "NULL" : version.ToString(),
                    UpdateTime = updateTime == null ? "NULL" : updateTime.ToString()
                });

                ajaxResult.Result = "true";
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        // 專案位置 -> Server.MapPath("~") = "D:\\Data\\Visual Studio\\Carousels\\Carousels\\"
        // 取得DataHome\root\folder底下所有檔案路徑
        [HttpPost]
        public JsonResult GetFilePath(string C_NO)
        {
            AJAXResponse ajaxResult = new AJAXResponse();

            if (string.IsNullOrWhiteSpace(C_NO))
            {
                ajaxResult.ErrorMessage = "讀取失敗 -> C_NO 不可空白";
            }
            else if (C_NO == "C00000000")
            {
                ajaxResult.ErrorMessage = "無效參數";
            }
            else
            {
                try
                {
                    Core core = new Core(Request.Url.Scheme + @":\\" + Request.Url.Authority);
                    Loger Log = new Loger(C_NO, "GetFilePath", GetIPAddress());
                    List<ArchiveInfo> FS = new List<ArchiveInfo>();

                    DateTime StartTime = DateTime.Now;

                    Log.WriteLine(string.Format("[{0}][{1}]開始讀取", C_NO, "GetPLAYER_LIST"));

                    // 讀取節目表
                    core.GetProgramme(Log, FS, C_NO);

                    Log.WriteLine(string.Format("[{0}][{1}]讀取結束 -> 執行總共花費時間: {2}秒", C_NO, "GetPLAYER_LIST", (DateTime.Now - StartTime).TotalSeconds));

                    ajaxResult.Result = "true";
                    ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(FS);
                }
                catch (Exception ex)
                {
                    ajaxResult.ErrorMessage = ex.Message;
                }
            }
            return Json(ajaxResult);
        }

        // 取得IP https://stackoverflow.com/questions/735350/how-to-get-a-users-client-ip-address-in-asp-net
        private string GetIPAddress()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;

            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        /// <summary>
        /// 取得連線資訊
        /// </summary>
        [HttpPost]
        public JsonResult GetLinkInfo()
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT C_NO, C_NAME, STAT
,CASE WHEN STRFTIME('%s', @LINK_TIME) 
		   - STRFTIME('%s', Client) >= 1200 /* 20 分鐘 */
 THEN 0 ELSE 1 END [LINK_STAT]
FROM PLAYER
WHERE C_NO != 'C00000000'
ORDER BY C_NAME
                    "))
                    {
                        cmd.Parameters.AddWithValue("@LINK_TIME", DateTime.Now);

                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 取得播放器
        /// </summary>
        [HttpPost]
        public JsonResult GetPlayer()
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT C_NO, C_NAME
FROM PLAYER
WHERE STAT IS NULL
ORDER BY C_NAME
                    "))
                    {
                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 取得節目
        /// </summary>
        [HttpPost]
        public JsonResult GetProgramme()
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT P_NO, P_NAME, STAT
,STRFTIME('%Y-%m-%d %H:%M:%S', UpdateTime) UpdateTime
FROM PROGRAMME
ORDER BY P_NAME
                    "))
                    {
                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 修改節目
        /// </summary>
        [HttpPost]
        public JsonResult UpdateProgramme(PROGRAMME data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
UPDATE PROGRAMME
SET P_NAME = @P_NAME, STAT = @STAT, UpdateTime = @UpdateTime
WHERE P_NO = @P_NO
                    "))
                    {
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);
                        cmd.Parameters.AddWithValue("@P_NAME", data.P_NAME);
                        if (string.IsNullOrEmpty(data.STAT))
                        {
                            cmd.Parameters.AddWithValue("@STAT", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@STAT", data.STAT);
                        }
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 新增節目
        /// </summary>
        [HttpPost]
        public JsonResult CreateProgramme(PROGRAMME data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {

                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT SUBSTRING(MAX(P_NO), 2)
FROM PROGRAMME
                    "))
                    {
                        int id = ToInt(cmd.ExecuteScalar().ToString());
                        id += 1;

                        cmd.CommandText = @"
INSERT INTO PROGRAMME
(P_NO, P_NAME, STAT, UpdateTime)
VALUES
(@P_NO, @P_NAME, NULL, @UpdateTime)
                        ";

                        cmd.Parameters.AddWithValue("@P_NO", "P" + ToHexadecimal(id, 8));
                        cmd.Parameters.AddWithValue("@P_NAME", data.P_NAME);
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }

            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 取得節目表
        /// </summary>
        [HttpPost]
        public JsonResult GetPLAYER_LIST(string C_NO)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT A.C_NO, A.C_NAME, C.P_NO, C.P_NAME, CAST(B.ITEM AS nvarchar) [ITEM]
,B.STAT, C.STAT [P_STAT]
,STRFTIME('%Y-%m-%d %H:%M:%S', B.UpdateTime) UpdateTime
FROM PLAYER A
LEFT JOIN PLAYER_LIST B ON B.C_NO = A.C_NO
LEFT JOIN PROGRAMME C ON C.P_NO = B.P_NO
WHERE A.C_NO = @C_NO
ORDER BY A.C_NO, B.ITEM
                    "))
                    {
                        cmd.Parameters.AddWithValue("@C_NO", C_NO);
                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 修改播放清單
        /// </summary>
        [HttpPost]
        public JsonResult UpdatePLAYER_LIST(PLAYER_LIST data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
UPDATE PLAYER_LIST 
SET ITEM = @ITEM, STAT = @STAT, UpdateTime = @UpdateTime
WHERE C_NO = @C_NO AND P_NO = @P_NO
                    "))
                    {
                        cmd.Parameters.AddWithValue("@C_NO", data.C_NO);
                        cmd.Parameters.AddWithValue("@ITEM", data.ITEM);
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);
                        if (string.IsNullOrEmpty(data.STAT))
                        {
                            cmd.Parameters.AddWithValue("@STAT", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@STAT", data.STAT);
                        }
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 修改播放清單
        /// </summary>
        [HttpPost]
        public JsonResult CreatePLAYER_LIST(PLAYER_LIST data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT COUNT(*)
FROM PLAYER_LIST
WHERE C_NO = @C_NO AND P_NO = @P_NO
                    "))
                    {
                        cmd.Parameters.AddWithValue("@C_NO", data.C_NO);
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);

                        int count = int.Parse(cmd.ExecuteScalar().ToString());

                        if (count > 0)
                        {
                            throw new Exception("重複新增，已終止操作");
                        }

                        cmd.CommandText = @"
INSERT INTO PLAYER_LIST
(C_NO, ITEM, P_NO, STAT, UpdateTime)
VALUES
(@C_NO, @ITEM, @P_NO, NULL, @UpdateTime)
                        ";

                        cmd.Parameters.AddWithValue("@ITEM", data.ITEM);
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 刪除播放清單
        /// </summary>
        [HttpPost]
        public JsonResult DeletePLAYER_LIST(PLAYER_LIST data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT COUNT(*)
FROM PLAYER_LIST
WHERE C_NO = @C_NO AND P_NO = @P_NO
                    "))
                    {
                        cmd.Parameters.AddWithValue("@C_NO", data.C_NO);
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);

                        int count = int.Parse(cmd.ExecuteScalar().ToString());

                        if (count > 1)
                        {
                            throw new Exception("刪除數超過1筆，資料異常");
                        }

                        cmd.CommandText = @"
DELETE
FROM PLAYER_LIST
WHERE C_NO = @C_NO AND P_NO = @P_NO
                        ";

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        [HttpPost]
        public JsonResult GetProgramme_LIST(PROGRAMME_LIST data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT A.P_NO, CAST(A.ITEM AS nvarchar) [ITEM], A.F_NO, B.F_NAME, A.PY_SECOND, B.STAT [F_STAT], A.STAT
,STRFTIME('%Y-%m-%d %H:%M:%S', A.UpdateTime) UpdateTime
FROM PROGRAMME_LIST A
LEFT JOIN PROGRAMME_DATA B ON B.F_NO = A.F_NO
WHERE A.P_NO = @P_NO
ORDER BY A.ITEM
                    "))
                    {
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);
                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 修改節目內容
        /// </summary>
        [HttpPost]
        public JsonResult UpdateProgramme_LIST(PROGRAMME_LIST data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT COUNT(*)
FROM PROGRAMME_LIST
WHERE P_NO = @P_NO AND F_NO = @F_NO_key
                    "))
                    {
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);
                        cmd.Parameters.AddWithValue("@F_NO", data.F_NO);
                        cmd.Parameters.AddWithValue("@F_NO_key", data.F_NO_key);

                        int count = int.Parse(cmd.ExecuteScalar().ToString());

                        if (count > 1)
                        {
                            throw new Exception("修改數超過1筆，資料異常");
                        }

                        if (data.F_NO != data.F_NO_key)
                        {
                            cmd.CommandText = @"
SELECT COUNT(*)
FROM PROGRAMME_LIST
WHERE P_NO = @P_NO AND F_NO = @F_NO
                            ";

                            count = int.Parse(cmd.ExecuteScalar().ToString());

                            if (count != 0)
                            {
                                throw new Exception("修改後會造成資料重複，已取消修改");
                            }
                        }

                        cmd.CommandText = @"
UPDATE PROGRAMME_LIST
SET ITEM = @ITEM, F_NO = @F_NO, PY_SECOND = @PY_SECOND, STAT = @STAT, UpdateTime = @UpdateTime
WHERE P_NO = @P_NO AND F_NO = @F_NO_key
                        ";

                        cmd.Parameters.AddWithValue("@ITEM", data.ITEM);
                        cmd.Parameters.AddWithValue("@PY_SECOND", data.PY_SECOND);
                        if (string.IsNullOrEmpty(data.STAT))
                        {
                            cmd.Parameters.AddWithValue("@STAT", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@STAT", data.STAT);
                        }
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 新增節目內容
        /// </summary>
        [HttpPost]
        public JsonResult CreateProgramme_LIST(PROGRAMME_LIST data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT COUNT(*)
FROM PROGRAMME_LIST
WHERE P_NO = @P_NO AND F_NO = @F_NO
                    "))
                    {
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);
                        cmd.Parameters.AddWithValue("@F_NO", data.F_NO);

                        int count = int.Parse(cmd.ExecuteScalar().ToString());

                        if (count > 0)
                        {
                            throw new Exception("重複新增，已終止操作");
                        }

                        cmd.CommandText = @"
INSERT INTO PROGRAMME_LIST
(P_NO, ITEM, F_NO, PY_SECOND, STAT, UpdateTime)
VALUES
(@P_NO, @ITEM, @F_NO, @PY_SECOND, NULL, @UpdateTime)
                        ";

                        cmd.Parameters.AddWithValue("@ITEM", data.ITEM);
                        cmd.Parameters.AddWithValue("@PY_SECOND", data.PY_SECOND);
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 刪除節目內容
        /// </summary>
        [HttpPost]
        public JsonResult DeleteProgramme_LIST(PROGRAMME_LIST data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT COUNT(*)
FROM PROGRAMME_LIST
WHERE P_NO = @P_NO AND F_NO = @F_NO
                    "))
                    {
                        cmd.Parameters.AddWithValue("@P_NO", data.P_NO);
                        cmd.Parameters.AddWithValue("@F_NO", data.F_NO);

                        int count = int.Parse(cmd.ExecuteScalar().ToString());

                        if (count > 1)
                        {
                            throw new Exception("刪除數超過1筆，資料異常");
                        }

                        cmd.CommandText = @"
DELETE
FROM PROGRAMME_LIST
WHERE P_NO = @P_NO AND F_NO = @F_NO
                        ";

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 檔案內容
        /// </summary>
        [HttpPost]
        public JsonResult GetProgramme_DATA()
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT F_NO, F_NAME, F_TYPE, F_PATH, STAT, STRFTIME('%Y-%m-%d %H:%M:%S', UpdateTime) UpdateTime
FROM PROGRAMME_DATA
ORDER BY F_NAME
                    "))
                    {
                        DataTable dt = new DataTable();
                        dt.Load(cmd.ExecuteReader());
                        ajaxResult.Data = Newtonsoft.Json.JsonConvert.SerializeObject(dt);
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 修改檔案
        /// </summary>
        [HttpPost]
        public JsonResult UpdateProgramme_DATA(PROGRAMME_DATA data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {
                if (data.F_TYPE != "URL")
                {
                    throw new Exception("僅能修改網頁類型");
                }

                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
UPDATE PROGRAMME_DATA
SET F_NAME = @F_NAME, F_PATH = @F_PATH, STAT = @STAT, UpdateTime = @UpdateTime
WHERE F_NO = @F_NO AND F_TYPE = @F_TYPE
                    "))
                    {
                        cmd.Parameters.AddWithValue("@F_NO", data.F_NO);
                        cmd.Parameters.AddWithValue("@F_NAME", data.F_NAME);
                        cmd.Parameters.AddWithValue("@F_TYPE", data.F_TYPE);
                        cmd.Parameters.AddWithValue("@F_PATH", data.F_PATH);
                        if (string.IsNullOrEmpty(data.STAT))
                        {
                            cmd.Parameters.AddWithValue("@STAT", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@STAT", data.STAT);
                        }
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }
            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 新增檔案
        /// </summary>
        [HttpPost]
        public JsonResult CreateProgramme_DATA(PROGRAMME_DATA data)
        {
            AJAXResponse ajaxResult = new AJAXResponse();
            try
            {

                using (CarouselDbConnection db = new CarouselDbConnection())
                {
                    using (SQLiteCommand cmd = db.GetCommand(@"
SELECT SUBSTRING(MAX(F_NO), 2)
FROM PROGRAMME_DATA 
                    "))
                    {
                        int id = ToInt(cmd.ExecuteScalar().ToString());
                        id += 1;

                        cmd.CommandText = @"
INSERT INTO PROGRAMME_DATA
(F_NO, F_NAME, F_TYPE, F_PATH, STAT, UpdateTime)
VALUES
(@F_NO, @F_NAME, @F_TYPE, @F_PATH, NULL, @UpdateTime)
                        ";

                        cmd.Parameters.AddWithValue("@F_NO", "F" + ToHexadecimal(id, 8));
                        cmd.Parameters.AddWithValue("@F_NAME", data.F_NAME);
                        cmd.Parameters.AddWithValue("@F_TYPE", "URL");
                        cmd.Parameters.AddWithValue("@F_PATH", data.F_PATH);
                        cmd.Parameters.AddWithValue("@UpdateTime", DateTime.Now);

                        ajaxResult.Data = String.Format("受影響行數: {0}", cmd.ExecuteNonQuery());
                        ajaxResult.Result = "true";
                    }
                }

            }
            catch (Exception ex)
            {
                ajaxResult.ErrorMessage = ex.Message;
            }

            return Json(ajaxResult);
        }

        /// <summary>
        /// 轉16進位制
        /// </summary>
        private string ToHexadecimal(int i, int size)
        {
            List<char> sb = new List<char>();
            while (i > 0)
            {
                switch (i % 16)
                {
                    case 0: sb.Add('0'); break;
                    case 1: sb.Add('1'); break;
                    case 2: sb.Add('2'); break;
                    case 3: sb.Add('3'); break;
                    case 4: sb.Add('4'); break;
                    case 5: sb.Add('5'); break;
                    case 6: sb.Add('6'); break;
                    case 7: sb.Add('7'); break;
                    case 8: sb.Add('8'); break;
                    case 9: sb.Add('9'); break;
                    case 10: sb.Add('A'); break;
                    case 11: sb.Add('B'); break;
                    case 12: sb.Add('C'); break;
                    case 13: sb.Add('D'); break;
                    case 14: sb.Add('E'); break;
                    case 15: sb.Add('F'); break;
                }
                i /= 16;
            }

            while (sb.Count < size)
            {
                sb.Add('0');
            }

            sb.Reverse();

            return string.Join("", sb);
        }

        /// <summary>
        /// 轉10進位制
        /// </summary>
        private int ToInt(string hex)
        {
            List<char> sb = new List<char>(hex);
            sb.Reverse();

            int i = 0;

            int score = 1;

            for (int index = 0; index < sb.Count; index++)
            {
                switch (sb[index])
                {
                    case '0': i += score * 0; break;
                    case '1': i += score * 1; break;
                    case '2': i += score * 2; break;
                    case '3': i += score * 3; break;
                    case '4': i += score * 4; break;
                    case '5': i += score * 5; break;
                    case '6': i += score * 6; break;
                    case '7': i += score * 7; break;
                    case '8': i += score * 8; break;
                    case '9': i += score * 9; break;
                    case 'A': i += score * 10; break;
                    case 'B': i += score * 11; break;
                    case 'C': i += score * 12; break;
                    case 'D': i += score * 13; break;
                    case 'E': i += score * 14; break;
                    case 'F': i += score * 15; break;
                }
                score *= 16;
            }

            return i;
        }
    }
}