using GradeBotWebAPI.Models;
using GradeBotWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace GradeBotWebAPI.Controllers
{
    [ApiController] //������� .NET, ��� ��� API-����������: ������������� ������������ ��������� ������ � �. �.
    [Route("api/[controller]")] //��������, ��� ���� � ����� ����������� ����� api/students, ������ ��� ��� ����������� � StudentsController
    public class StudentsController : ControllerBase
    {
        private readonly StudentService _studentService;

        public StudentsController(StudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll() //���������� ������ ���� ���������
        {
            var students = await _studentService.GetAllAsync();
            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id) //���������� ������ �������� �� ��� Id
        {
            var student = await _studentService.GetByIdAsync(id);
            if (student == null)
                return NotFound();

            return Ok(student);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByName([FromQuery] string name) // ��������� ����� ��������� �� ����� (��� ����� �����)
        {
            var students = await _studentService.SearchByNameAsync(name);
            return Ok(students);
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AddStudent([FromBody] Student student) //��������� �������� � ����
        {
            await _studentService.AddStudentAsync(student);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id) //������� �������� �� Id
        {
            await _studentService.DeleteAsync(id);
            return Ok();
        }
    }
}

