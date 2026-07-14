using System.Configuration;
using System.Data.SqlClient;

namespace JSONWebAPI
{
    public class DBConnect
    {

        private static SqlConnection NewCon ;

        public static SqlConnection getConnection()
        {
            // credentials removed for publication — pointed at a university-hosted SQL Server that no longer exists
            NewCon = new SqlConnection(@"Data Source=REDACTED;Initial Catalog=ADoc;Persist Security Info=True;User ID=REDACTED;Password=REDACTED");
            return NewCon;
        }
        public DBConnect()
        {

        }

    }
}