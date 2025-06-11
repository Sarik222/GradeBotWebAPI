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

        public async Task<Student?> GetByIdAsync(int id)  //возвращает всех стужентов 
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Students WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Student>(sql, new { Id = id });
        }

        public async Task<IEnumerable<Student>> SearchByNameAsync(string name) //поиск студентов по части имени 
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Students WHERE Name LIKE @Name";
            return await connection.QueryAsync<Student>(sql, new { Name = $"%{name}%" });
        }

        public async Task DeleteAsync(int id) //удалить по айди 
        {
            using var connection = _factory.CreateConnection();
            string sql = "DELETE FROM Students WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }
        public async Task<Student?> GetByEmailAsync(string email)
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Students WHERE Email = @Email";
            return await connection.QueryFirstOrDefaultAsync<Student>(sql, new { Email = email });
        }
        public async Task<Student?> GetByUserIdAsync(int userId)
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Students WHERE UserId = @UserId";
            return await connection.QueryFirstOrDefaultAsync<Student>(sql, new { UserId = userId });
        }
    }
}

