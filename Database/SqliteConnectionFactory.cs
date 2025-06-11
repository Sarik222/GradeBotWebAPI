using Microsoft.Data.Sqlite;
using System.Data;

namespace GradeBotWebAPI.Database
{
    public class SqliteConnectionFactory
    {
        private readonly string _connectionString; //указывает, где находится база данных

        public SqliteConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {  
            return new SqliteConnection(_connectionString);
        }
    }
}
