using Microsoft.Data.Sqlite;

namespace GradeBotWebAPI.Database
{
    public static class DbInitializer
    {
        public static void Initialize(string connectionString)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var createStudents = @"
            CREATE TABLE IF NOT EXISTS Students (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE
            );";

            var createGrades = @"
            CREATE TABLE IF NOT EXISTS Grades (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId INTEGER NOT NULL,
                Subject TEXT NOT NULL,
                Value INTEGER NOT NULL CHECK (Value BETWEEN 0 AND 10),
                FOREIGN KEY(StudentId) REFERENCES Students(Id)
            );";

            var createUsers = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Email TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Role TEXT NOT NULL CHECK (Role IN ('Student', 'Admin'))
            );";

            using var cmd = connection.CreateCommand();

            cmd.CommandText = createStudents;
            cmd.ExecuteNonQuery();

            cmd.CommandText = createGrades;
            cmd.ExecuteNonQuery();

            cmd.CommandText = createUsers;
            cmd.ExecuteNonQuery();

            // Автоматическая миграция студентов из Users в Students
            cmd.CommandText = @"
            INSERT OR IGNORE INTO Students (Name, Email)
            SELECT '', Email FROM Users WHERE Role = 'Student';";
            cmd.ExecuteNonQuery();
        }
    }
}
