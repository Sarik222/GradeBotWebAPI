using GradeBotWebAPI.Models;
using GradeBotWebAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GradeBotWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GradesController : ControllerBase
    {
        private readonly GradeService _gradeService;
        private readonly StudentService _studentService;

        public GradesController(GradeService gradeService, StudentService studentService)
        {
            _gradeService = gradeService;
            _studentService = studentService;
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
            public string Subject { get; set; }
            public int Value { get; set; }
        }

        // Студент добавляет себе оценку
        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AddGrade([FromBody] AddGradeRequest request)
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (email == null)
                return Unauthorized();

            var student = await _studentService.GetByEmailAsync(email);
            if (student == null)
                return Unauthorized();

            var grade = new Grade
            {
                StudentId = student.Id,
                Subject = request.Subject,
                Value = request.Value
            };

            await _gradeService.AddGradeAsync(grade);
            return Ok();
        }



        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyGrades()
        {
            var userId = GetUserIdFromToken();
            if (userId == null)
                return Unauthorized();

            var student = await _studentService.GetByUserIdAsync(userId.Value);
            if (student == null)
                return NotFound("Студент не найден");

            var grades = await _gradeService.GetGradesByStudentIdAsync(student.Id);
            return Ok(grades);
        }


        // Админ получает оценки по имени студента
        [HttpGet("by-name")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGradesByStudentName([FromQuery] string name)
        {
            var grades = await _gradeService.GetGradesByStudentNameAsync(name);
            return Ok(grades);
        }
        public class UpdateGradeDto
        {
            public string Subject { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        // Студент может редактировать свою оценку
        [HttpPut("{id}")]
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
        [HttpDelete("{id}")]
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
                return NotFound();

            if (grade.StudentId != student.Id)
                return Forbid("Можно удалять только свои оценки");

            await _gradeService.DeleteGradeAsync(id);
            return Ok();
        }
    }
}

