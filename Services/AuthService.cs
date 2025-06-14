using Dapper;
using GradeBotWebAPI.Database;
using GradeBotWebAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace GradeBotWebAPI.Services
{
    public class AuthService
    {
        private readonly SqliteConnectionFactory _factory;

        public AuthService(SqliteConnectionFactory factory)
        {
            _factory = factory;
        }
        public async Task<bool> UserExistsAsync(string email)
        {
            using var connection = _factory.CreateConnection();
            var result = await connection.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Email = @Email", new { Email = email });
            return result != null;
        }

        public async Task<bool> RegisterAsync(string email, string password, string role)
        {
            if (!email.EndsWith("@edu.hse.ru")) return false;

            if (await UserExistsAsync(email)) return false;

            var firstChar = role.Trim().ToLower().FirstOrDefault();

            string normalizedRole = firstChar switch
            {
                'с' => "Student",
                'а' => "Admin",
                'С' => "Student",
                'А' => "Admin",
                _ => throw new ArgumentException("Некорректная роль. Введите роль, начинающуюся на 'с' или 'а'.")
            };

            var passwordHash = ComputeHash(password);

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Role = normalizedRole
            };

            using var connection = _factory.CreateConnection();
            string sql = "INSERT INTO Users (Email, PasswordHash, Role) VALUES (@Email, @PasswordHash, @Role)";
            await connection.ExecuteAsync(sql, user);
            if (normalizedRole == "Student")
            {
                string insertStudentSql = "INSERT INTO Students (Name, Email) Values (@Name, @Email)";
                var student = new { Name = "", Email = email };
                await connection.ExecuteAsync(insertStudentSql, student);
                
            }
            return true;
        }


        public async Task<User?> AuthenticateAsync(string email, string password)
        {
            var passwordHash = ComputeHash(password); //хеширование переданных паролей

            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Users WHERE Email = @Email AND PasswordHash = @PasswordHash"; //ищет пользователя с таким Email и PasswordHash

            return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email, PasswordHash = passwordHash }); //Если находит — возвращает объект User, иначе null
        }

        public static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create(); //преобразует строку в байты UTF-8
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input)); //Хэширует через SHA256
            return Convert.ToBase64String(bytes); //Преобразует байты в Base64(удобно хранить как строку)
        }
    }
}

