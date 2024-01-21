using System.Data.SqlClient;
using System.Data;

namespace WinFormsApp.DataAccess
{
    public class SqlDatabaseSchemaReader 
    {
        public DataTable GetTablesSchema(string connectionString)
        {
            using SqlConnection connection = new(connectionString);
            connection.Open();

            string[] restrictions = new string[4] { null, null, null, "BASE TABLE" };

            return connection.GetSchema("Tables", restrictions);
        }
    }
}
