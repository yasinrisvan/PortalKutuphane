using System.Data.SqlClient;
namespace LibraryOfPortall.Models
{
    public class MailBaglanti
    {
        public static string sql_mail()
        {
            string conn_mail = "Server=192.168.100.19; Database=TYHTALEP; User Id=perge; Password=master.12; Connection Timeout=1000";


            return conn_mail;

        }
    }
}