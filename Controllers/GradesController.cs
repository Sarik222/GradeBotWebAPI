using GradeBotWebAPI.Models;
using GradeBotWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;

namespace GradeBotWebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GradesController : ControllerBase
    {
        private readonly GradeService _gradeService;
        private readonly StudentService _studentService;
        private readonly GradeCalculatorService _gradeCalculator;
        public GradesController(GradeService gradeService, StudentService studentService, GradeCalculatorService gradeCalculator)
        {
            _gradeService = gradeService;
            _studentService = studentService;
            _gradeCalculator = gradeCalculator;
        }
        private int? GetUserIdFromToken()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return null;

            var user = _studentService.GetByEmailAsync(email).Result;
            return user?.Id;
        }

        public class AddGradeRequest
        {
            [Required]
            public string Subject { get; set; } = string.Empty;
            [Required]
            public int Value { get; set; }
            [Required]
            public string WorkType { get; set; } = string.Empty;
        }

        // Студент добавляет себе оценку
        [HttpPost("Add grades for student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AddGrade([FromBody] AddGradeRequest request)
        {

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email == null)
            {
                return Unauthorized("Студента еще нет в базе");
            }

            var student = await _studentService.GetByEmailAsync(email);
            if (student == null)
            {
                return NotFound("Студента еще нет в базе");
            }

            var grade = new Grade
            {
                StudentId = student.Id,
                Subject = request.Subject,
                Value = request.Value,
                WorkType = request.WorkType
            };

            await _gradeService.AddGradeAsync(grade);

            return Ok();
        }

        [HttpGet("My grades")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyGrades()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized("Студент не найден");

            var student = await _studentService.GetByUserIdAsync(userId.Value);
            if (student == null)
                return NotFound("Студент не найден");

            var grades = await _gradeService.GetGradesByStudentIdAsync(student.Id);
            return Ok(grades);
        }


        // Админ получает оценки по ID студента
        [HttpGet("Get student's grades for admins")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGradesByStudentId([FromQuery] int studentId)
        {
            var student = await _studentService.GetByUserIdAsync(studentId);
            if (student == null)
                return NotFound("Студент c таким Id не найден");
            var grades = await _gradeService.GetGradesByStudentIdAsync(studentId);
            return Ok(grades);
        }
        public class UpdateGradeDto
        {
            public string Subject { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        // Студент может редактировать свою оценку
        [HttpPut("Update grade for student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UpdateGrade(int id, [FromBody] UpdateGradeDto dto)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized();

            var student = await _studentService.GetByUserIdAsync(userId.Value);
            if (student == null)
                return Unauthorized();

            var existing = await _gradeService.GetGradeByIdAsync(id);
            if (existing == null)
                return NotFound("Оценка не найдена");

            if (existing.StudentId != student.Id)
                return Forbid("Можно редактировать только свои оценки");

            // Создаём новый объект оценки с безопасными значениями
            var updatedGrade = new Grade
            {
                Id = id,
                StudentId = student.Id,
                Subject = dto.Subject,
                Value = dto.Value
            };

            await _gradeService.UpdateGradeAsync(updatedGrade);
            return Ok();
        }

        // Студент может удалить свою оценку
        [HttpDelete("Delete grade for student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized();

            var student = await _studentService.GetByUserIdAsync(userId.Value);
            if (student == null)
                return Unauthorized();

            var grade = await _gradeService.GetGradeByIdAsync(id);
            if (grade == null)
                return NotFound("Студент не найден");

            if (grade.StudentId != student.Id)
                return Forbid("Можно удалять только свои оценки");

            await _gradeService.DeleteGradeAsync(id);
            return Ok();
        }

        [HttpGet("Get grades by one subject for student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyGradesBySubject([FromQuery] string subject)
        {
            // Проверка параметра subject
            if (string.IsNullOrEmpty(subject))
                return BadRequest("Предмет не указан");
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) 
                    return Unauthorized("Email не найден в токене");
            var student = await _studentService.GetByEmailAsync(email);
            if (student == null) 
                return NotFound("Студент не найден");
            var grades = await _gradeService.GetGradesByStudentIdAndSubjectAsync(student.Id, subject);
            // Проверка наличия оценок
            if (grades == null || !grades.Any())
            {
                return Ok("Оценок по этому предмету еще нет");
            }
            return Ok(grades);
        }
        [HttpGet("Final grade by subject for student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetFinalGrade([FromQuery] string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return BadRequest("Не указан предмет");

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
                return Unauthorized("Пользователь не авторизован");

            var student = await _studentService.GetByEmailAsync(email);
            if (student == null)
                return NotFound("Студент не найден");

            var grades = await _gradeService.GetGradesByStudentIdAsync(student.Id);
            var subjectGrades = grades
                .Where(g => g.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var result = _gradeCalculator.Calculate(subject, subjectGrades);
            Console.WriteLine($"[DEBUG] Запрос финальной оценки по предмету: '{subject}'");
            Console.WriteLine("[DEBUG] Оценки по предмету:");
            foreach (var g in subjectGrades)
            {
                Console.WriteLine($" - {g.Subject} | {g.WorkType} | {g.Value}");
            }
            if (result == null)
                return NotFound("Для этого предмета не задана формула оценивания или нет оценок");

            return Ok(new { Subject = subject, FinalGrade = Math.Round(result.Value, 2) });
        }
    }
}

