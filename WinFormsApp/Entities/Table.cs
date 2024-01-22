namespace WinFormsApp.Entities
{
    public class Table
    {
        public string TableName { get; set; }
        public string PrimaryKey { get; set; }
        public List<Tuple<string, Table>> ForeignKeys { get; set; }
        public List<string> Columns { get; set; }
        public Table()
        {
            
            ForeignKeys = new List<Tuple<string, Table>>();
        }
        public Table(string tableName)
        {
            TableName = tableName;
        }

        public Table(string tableName, string primaryKey, List<Tuple<string, Table>> foreignKeys)
        {
            TableName = tableName;
            PrimaryKey = primaryKey;
            ForeignKeys = foreignKeys;
        }

        public Table(string tableName, string primaryKey, List<string> columns, List<Tuple<string, Table>> foreignKeys)
    {
        TableName = tableName;
        PrimaryKey = primaryKey;
        Columns = columns;
        ForeignKeys = foreignKeys;
    }
    }
}
