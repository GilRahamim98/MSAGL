using System.Data.SqlClient;
using System.Data;
using WinFormsApp.Entities;

namespace WinFormsApp.DataAccess
{
    public class TableExtractor
    {
        private readonly SqlDatabaseSchemaReader _schemaReader;

        public TableExtractor(SqlDatabaseSchemaReader schemaReader)
        {
            _schemaReader = schemaReader;
        }
       // public List<Table> GetTables(string connectionString)
      //  {
         //   List<Table> tables = new();

          //  DataTable schema = _schemaReader.GetTablesSchema(connectionString);

          //  foreach (DataRow row in schema.Rows)
           // {
            //    string tableName = (string)row[2];

                //Table table = new(tableName, GetPrimaryKey(connectionString, tableName), GetForeignKeys(connectionString, tableName));
          //      tables.Add(table);
          //  }

          //  return tables;
       //}

        public List<Table> GetTables(string connectionString)
        {
            List<Table> tables = new();

            DataTable schema = _schemaReader.GetTablesSchema(connectionString);

            foreach (DataRow row in schema.Rows)
            {
                string tableName = (string)row[2];
                List<string> columns = GetColumns(connectionString, tableName); // Fetch columns

                Table table = new(tableName, GetPrimaryKey(connectionString, tableName), columns, GetForeignKeys(connectionString, tableName));
                tables.Add(table);
            }

            return tables;
        }

        private List<string> GetColumns(string connectionString, string tableName)
        {
            List<string> columns = new List<string>();

            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                // Query to get column names
                string query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";

                SqlCommand command = new(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string columnName = reader.GetString(0);
                    columns.Add(columnName);
                }

                reader.Close();
            }

            return columns;
        }
        private string GetPrimaryKey(string connectionString, string tableName)
        {
            string primaryKey = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string query = $@"SELECT COLUMN_NAME
                              FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                              WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1
                              AND TABLE_NAME = '{tableName}'";

                SqlCommand command = new(query, connection);
                object result = command.ExecuteScalar();

                if (result != null)
                {
                    primaryKey = (string)result;
                }
            }

            return primaryKey;
        }

       


        private List<Tuple<string, Table>> GetForeignKeys(string connectionString, string tableName)
        {
            List<Tuple<string, Table>> foreignKeys = new();

            using (SqlConnection connection = new(connectionString))
            {
                connection.Open();

                string query = $@"
                                SELECT
                                    OBJECT_NAME(fkc.parent_object_id) AS TableName,
                                    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                                    OBJECT_NAME(fkc.referenced_object_id) AS ReferencedTable,
                                    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
                                FROM
                                    sys.foreign_key_columns fkc
                                WHERE
                                    OBJECT_NAME(fkc.referenced_object_id) = '{tableName}'
                                ";

                

                SqlCommand command = new(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    string referencedTable = reader.GetString(0);
                    string foreignKeyName = reader.GetString(1);

                    Table referencedTableEntity = new(referencedTable);

                    foreignKeys.Add(Tuple.Create(foreignKeyName, referencedTableEntity));
                }

                reader.Close();
            }

            return foreignKeys;
        }
    }
}
