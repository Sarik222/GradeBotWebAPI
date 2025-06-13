using Dapper;
using GradeBotWebAPI.Database;
using GradeBotWebAPI.Models;

namespace GradeBotWebAPI.Services
{
    public class GradeService
    {
        private readonly SqliteConnectionFactory _factory;

        private static readonly HashSet<string> AllowedSubjects = new()
        {
            "Безопасность жизнидеятельности", "Основы российской государственности", "Английский язык", "Физическая кульутра", "Практикум по основам разработки технической документации",
            "Программирование", "Дискретная математика", "Профориентационный семинар", "История России", "Алгебра", "Теоретические основы информатики",
            "Независимый экзамен по цифровой грамотности", "Проектный семинар", "Исследовательский или прикладной проект", "Математический анализ", "Правовая грамотность", "Внутренний экзамен по английскому языку"
        };
        public GradeService(SqliteConnectionFactory factory) //kонструктор. Получает объект фабрики подключения к базе данных и сохраняет его в поле _factory.
        {
            _factory = factory;
        }

        public async Task AddGradeAsync(Grade grade) //Добавляет новую оценку в таблицу Grades
        {
            if (!AllowedSubjects.Contains(grade.Subject))
                throw new ArgumentException("Недопустимый предмет. Выберите из разрешённых или проверьте корректность ввода.");

            using var connection = _factory.CreateConnection();
            // Проверка: есть ли уже такая оценка у этого студента по этому предмету и значению
            string checkSql = @"SELECT COUNT(*) FROM Grades 
                        WHERE StudentId = @StudentId AND Subject = @Subject AND Value = @Value";

            var count = await connection.ExecuteScalarAsync<int>(checkSql, new
            {
                grade.StudentId,
                grade.Subject,
                grade.Value
            });

            if (count > 0)
                throw new InvalidOperationException("Такая оценка уже существует.");

            string sql = "INSERT INTO Grades (StudentId, Subject, Value) VALUES (@StudentId, @Subject, @Value)"; //поля: StudentId, Subject, Value
            await connection.ExecuteAsync(sql, grade);
        }

        public async Task<IEnumerable<Grade>> GetGradesByStudentIdAsync(int studentId) //Возвращает все оценки конкретного студента по его ID
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Grades WHERE StudentId = @StudentId";
            return await connection.QueryAsync<Grade>(sql, new { StudentId = studentId });
        }

        public async Task<IEnumerable<Grade>> GetGradesByStudentNameAsync(string name) //Возвращает оценки студентов, чьи имена частично совпадают с заданным name
        {
            using var connection = _factory.CreateConnection();
            string sql = @"
                SELECT g.*
                FROM Grades g
                JOIN Students s ON g.StudentId = s.Id 
                WHERE s.Name LIKE @Name";//Используется JOIN с таблицей Students, чтобы найти ID по имени
            return await connection.QueryAsync<Grade>(sql, new { Name = $"%{name}%" });
        }

        public async Task UpdateGradeAsync(Grade grade) //Обновляет существующую оценку по её Id
        {
            using var connection = _factory.CreateConnection();
            string sql = "UPDATE Grades SET Subject = @Subject, Value = @Value WHERE Id = @Id";
            await connection.ExecuteAsync(sql, grade);
        }

        public async Task DeleteGradeAsync(int id) //Удаляет оценку по её Id
        {
            using var connection = _factory.CreateConnection();
            string sql = "DELETE FROM Grades WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id });
        }
        public async Task<Grade?> GetGradeByIdAsync(int id) // Получить оценку по её ID
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Grades WHERE Id = @Id";
            return await connection.QuerySingleOrDefaultAsync<Grade>(sql, new { Id = id });
        }

    }
}
