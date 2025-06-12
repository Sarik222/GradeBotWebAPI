using GradeBotWebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using GradeBotWebAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace GradeBotWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly TokenService _jwtTokenService;
        public AuthController(AuthService authService, TokenService jwtTokenService)
        {
            _authService = authService;
            _jwtTokenService = jwtTokenService;
        }

        // POST api/auth/register
        //регистрация новых пользоватлей
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var success = await _authService.RegisterAsync(request.Email, request.Password, request.Role); 
            if (!success) //проверка логина и пароля
                return BadRequest("Email недопустим или пользователь уже существует");

            return Ok("Успешная регистрация");
        }

        // POST api/auth/login
        //метод входа по логину 
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.AuthenticateAsync(request.Email, request.Password);
            if (user == null)
                return Unauthorized("Неверный логин или пароль");

            // генерируем токен
            var token = _jwtTokenService.GenerateToken(user.Email, user.Role);

            return Ok(new
            {
                token,         // возвращаем токен, а не просто Email и Role
                user.Email,
                user.Role,
            });
        }
    }

    // DTO модели

    public class RegisterRequest //создает нового пользователя 
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Student"; // по умолчанию студент
    }

    public class LoginRequest //проверка логина и пароля 
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

