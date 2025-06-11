using GradeBotWebAPI.Models;
using GradeBotWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GradeBotWebAPI.Controllers
{
    [ApiController] //говорит .NET, что это API-контроллер: автоматически обрабатывает валидацию модели и т. д.
    [Route("api/[controller]")] //означает, что путь к этому контроллеру будет api/students, потому что имя контроллера — StudentsController
    public class StudentsController : ControllerBase
    {
        private readonly StudentService _studentService;

        public StudentsController(StudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll() //Возвращает список всех студентов
        {
            var students = await _studentService.GetAllAsync();
            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) //Возвращает одного студента по его Id
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null)
                return NotFound();

            return Ok(student);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByName([FromQuery] string name) // Выполняет поиск студентов по имени (или части имени)
        {
            var students = await _studentService.SearchByNameAsync(name);
            return Ok(students);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AddStudent([FromBody] Student student) //Добавляет студента в базу
        {
            await _studentService.AddStudentAsync(student);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id) //Удаляет студента по Id
        {
            await _studentService.DeleteAsync(id);
            return Ok();
        }
    }
}

