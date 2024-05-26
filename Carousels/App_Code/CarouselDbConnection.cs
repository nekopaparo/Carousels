using System;
using System.Data.SQLite;
using System.IO;

namespace Carousels.App_Code
{
    public class CarouselDbConnection : IDisposable
    {
        private readonly string ConnectionStringOfLinkDB;

        private SQLiteConnection con;

        public CarouselDbConnection()
        {
            ConnectionStringOfLinkDB = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Carousels.db");

            string conStr = "data source=" + ConnectionStringOfLinkDB;

            con = new SQLiteConnection(conStr);

            con.Open();
        }

        ~CarouselDbConnection()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        public SQLiteCommand GetCommand(string sql)
        {
            return con == null ? null : new SQLiteCommand(sql, con);
        }
    }
}