using Dapper;
using GradeBotWebAPI.Database;
using GradeBotWebAPI.Models;
using System.Data;

namespace GradeBotWebAPI.Services
{
    public class StudentService
    {
        private readonly SqliteConnectionFactory _factory;

        public StudentService(SqliteConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task AddStudentAsync(Student student) //добавляем студента в базу 
        {
            using var connection = _factory.CreateConnection();
            string sql = "INSERT INTO Students (Name, Email) VALUES (@Name, @Email)";
            await connection.ExecuteAsync(sql, student);
        }

        public async Task<IEnumerable<Student>> GetAllAsync() //вовзращаем всех студентов 
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Students";
            return await connection.QueryAsync<Student>(sql);
        }

        public async Task<Student?> GetByUserIdAsync(int userId)
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Students WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Student>(sql, new { Id = userId });
        }

        //public async Task<IEnumerable<Student>> SearchByNameAsync(string name) //поиск студентов по части имени 
        //{
        //    using var connection = _factory.CreateConnection();
        //    string sql = "SELECT * FROM Students WHERE Name LIKE @Name";
        //    return await connection.QueryAsync<Student>(sql, new { Name = $"%{name}%" });
        //}

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = _factory.CreateConnection();

            // Сначала находим студента по Id, чтобы получить его Email
            var student = await GetByUserIdAsync(id);
            if (student == null)
                return false;

            var email = student.Email;

            // Удаляем все оценки студента
            await connection.ExecuteAsync("DELETE FROM Grades WHERE StudentId = @Id", new { Id = id });

            // Удаляем студента
            await connection.ExecuteAsync("DELETE FROM Students WHERE Id = @Id", new { Id = id });

            // Удаляем связанного пользователя (по email)
            await connection.ExecuteAsync("DELETE FROM Users WHERE Email = @Email", new { Email = email });

            return true;
        }
        public async Task<Student?> GetByEmailAsync(string email)
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Students WHERE Email = @Email";
            return await connection.QueryFirstOrDefaultAsync<Student>(sql, new { Email = email });
        }
    }
}

