using Dapper;
using GradeBotWebAPI.Database;
using GradeBotWebAPI.Models;

namespace GradeBotWebAPI.Services
{
    public class GradeService
    {
        private readonly SqliteConnectionFactory _factory;
        private readonly GradeCalculatorService _gradeCalculator;

        private static readonly HashSet<string> AllowedSubjects = new()
        {
            "Безопасность жизнедеятельности", 
            "Основы российской государственности", 
            "Английский язык", 
            "Физическая кульутра", 
            "Практикум по основам разработки технической документации",
            "Программирование", 
            "Дискретная математика", 
            "Профориентационный семинар", 
            "История России", 
            "Алгебра", 
            "Теоретические основы информатики",
            "Независимый экзамен по цифровой грамотности", 
            "Проектный семинар", 
            "Исследовательский или прикладной проект", 
            "Математический анализ", 
            "Правовая грамотность", 
            "Внутренний экзамен по английскому языку"
        };
        public GradeService(SqliteConnectionFactory factory, GradeCalculatorService gradeCalculator) //kонструктор. Получает объект фабрики подключения к базе данных и сохраняет его в поле _factory.
        {
            _factory = factory;
            _gradeCalculator = gradeCalculator ?? throw new ArgumentNullException(nameof(gradeCalculator));
        }

        public async Task AddGradeAsync(Grade grade) //Добавляет новую оценку в таблицу Grades
        {
            if (!AllowedSubjects.Contains(grade.Subject))
                throw new ArgumentException("Недопустимый предмет. Выберите из разрешённых.");

            if (grade.Value < 0 || grade.Value > 10)
                throw new ArgumentException("Оценка должна быть от 0 до 10 включительно");

            // тип работы разрешён для этого предмета?
            if (_gradeCalculator.AllowedWorkTypes.TryGetValue(grade.Subject, out var allowedWorks))
            {
                if (!allowedWorks.Contains(grade.WorkType))
                    throw new ArgumentException($"Тип работы '{grade.WorkType}' не разрешён для предмета '{grade.Subject}'");
            }
            else
            {
                throw new ArgumentException($"Для предмета '{grade.Subject}' не заданы типы работ");
            }

            if (!_gradeCalculator.HasFormula(grade.Subject))
                throw new ArgumentException("Этот предмет не имеет своей формулы");

            using var connection = _factory.CreateConnection();

            // Получаем текущие оценки по этому студенту, предмету и типу работы
            var existingCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Grades WHERE StudentId = @StudentId AND Subject = @Subject AND WorkType = @WorkType",
                new { grade.StudentId, grade.Subject, grade.WorkType }
            );

            // Узнаём ограничение
            if (_gradeCalculator.MaxAllowedGradesPerWorkType.TryGetValue(grade.Subject, out var workTypeLimits))
            {
                if (workTypeLimits.TryGetValue(grade.WorkType, out var maxCount) && maxCount is not null)
                {
                    if (existingCount >= maxCount)
                        throw new InvalidOperationException($"Нельзя добавить больше {maxCount} оценок типа '{grade.WorkType}' по предмету '{grade.Subject}'");
                }
            }

            string sql = "INSERT INTO Grades (StudentId, Subject, Value, WorkType) VALUES (@StudentId, @Subject, @Value, @WorkType)"; //поля: StudentId, Subject, Value, WorkType
            await connection.ExecuteAsync(sql, grade);
        }

        public async Task<IEnumerable<Grade>> GetGradesByStudentIdAsync(int studentId) //Возвращает все оценки конкретного студента по его ID
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Grades WHERE StudentId = @StudentId";
            return await connection.QueryAsync<Grade>(sql, new { StudentId = studentId });
        }

        //public async Task<IEnumerable<Grade>> GetGradesByStudentNameAsync(string name) //Возвращает оценки студентов, чьи имена частично совпадают с заданным name
        //{
        //    using var connection = _factory.CreateConnection();
        //    string sql = @"
        //        SELECT g.*
        //        FROM Grades g
        //        JOIN Students s ON g.StudentId = s.Id 
        //        WHERE s.Name LIKE @Name";//Используется JOIN с таблицей Students, чтобы найти ID по имени
        //    return await connection.QueryAsync<Grade>(sql, new { Name = $"%{name}%" });
        //}

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
        public async Task<IEnumerable<Grade>> GetGradesByStudentIdAndSubjectAsync(int studentId, string subject)
        {
            using var connection = _factory.CreateConnection();
            string sql = "SELECT * FROM Grades WHERE StudentId = @StudentId AND Subject = @Subject";
            return await connection.QueryAsync<Grade>(sql, new { StudentId = studentId, Subject = subject });
        }
    }
}
