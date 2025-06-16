using GradeBotWebAPI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GradeBotWebAPI.TelegramBot
{
    public class TelegramBotService 
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITelegramBotClient _botClient;
        private readonly string _botToken;
        private string _jwtToken;

        private void SetToken(string token)
        {
            _jwtToken = token;
        }

        private readonly Dictionary<long, UserState> _userStates = new();
        private readonly Dictionary<long, string> _userTokens = new();
        private readonly Dictionary<long, string> _registrationEmails = new();
        private readonly Dictionary<long, string> _registrationPasswords = new();
        private readonly Dictionary<long, string> _loginEmails = new();
        private readonly Dictionary<long, string> _userRoles = new(); // chatId в Role
        private readonly Dictionary<long, string> _tempSubjects = new();
        private readonly Dictionary<long, int> _tempValues = new();
        private readonly Dictionary<long, string> _tempWorkTypes = new();
        private readonly Dictionary<long, string> TempSubject = new();
        private readonly Dictionary<long, int> TempGradeValue = new();
        private readonly Dictionary<long, int> _editGradeIds = new();
        private readonly Dictionary<long, string> _editSubjects = new();
        private readonly Dictionary<long, string> TempWorkType = new(); // если понадобится
        private readonly Dictionary<long, UserState> userStates = new();
        private readonly Dictionary<string, string> _messages = new()
        


        {
            ["start"] = "Привет! Выберите действие:",
            ["register_success"] = "Регистрация прошла успешно!",
            ["register_fail"] = "Ошибка регистрации. Попробуйте снова.",
            ["login_success"] = "Вы успешно вошли!",
            ["login_fail"] = "Ошибка входа. Проверьте данные.",
            ["unknown_command"] = "Неизвестная команда. Пожалуйста, выберите дейсвтие из меню."
        };
        // Словарь допустимых работ по каждому предмету
        public readonly Dictionary<string, List<string>> _allowedWorkTypes = new()
        {
            ["Безопасность жизнедеятельности"] = new List<string> { "Презентация", "Экзамен" },
            ["Основы российской государственности"] = new List<string> { "Оценка" },
            ["Английский язык"] = new List<string> { "Лексико-грамматический тест", "Монолог", "Описание графика", "Экзамен" },
            ["Практикум по основам разработки технической документации"] = new List<string> { "Оценка" },
            ["Программирование"] = new List<string> { "Отчёт1", "Защита1", "Отчёт2", "Защита2", "Отчёт3", "Защита3", "Отчёт4", "Защита4", "Отчёт5", "Защита5", "Отчёт6", "Защита6" },
            ["Дискретная математика"] = new List<string> { "КР1", "КР2" },
            ["Профориентационный семинар"] = new List<string> { "Оценка" },
            ["История России"] = new List<string> { "Оценка" },
            ["Алгебра"] = new List<string> { "КР1", "КР2", "Микроконтроль", "Самостоятельная работа", "Экзамен" },
            ["Теоретические основы информатики"] = new List<string> { "Лабораторная работа", "Самостоятельная работа", "Тест", "Экзамен" },
            ["Независимый экзамен по цифровой грамотности"] = new List<string> { "Оценка" },
            ["Проектный семинар"] = new List<string> { "Оценка" },
            ["Исследовательский или прикладной проект"] = new List<string> { "Оценка" },
            ["Математический анализ"] = new List<string> { "КР1", "КР2", "Микроконтроль", "Самостоятельная работа", "Экзамен" },
            ["Правовая грамотность"] = new List<string> { "Оценка" },
            ["Внутренний экзамен по английскому языку"] = new List<string> { "Оценка" }
        };
        private readonly HashSet<string> _allowedSubjects = new(StringComparer.OrdinalIgnoreCase)
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


        public TelegramBotService(IOptions<BotConfiguration> config, ILogger<TelegramBotService> logger, IHttpClientFactory httpClientFactory)
        {
            _botToken = config.Value.Token;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _botClient = new TelegramBotClient(_botToken);
        }

        private string FormatSubjectsList(HashSet<string> subjects, int itemsPerLine = 1)
        {
            var sb = new StringBuilder();
            int count = 0;

            foreach (var subject in subjects)
            {
                sb.Append(subject);
                count++;

                if (count % itemsPerLine == 0)
                    sb.AppendLine();
                else
                    sb.Append(", ");
            }

            return sb.ToString().TrimEnd(',', ' ', '\n', '\r');
        }
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _botClient.StartReceiving(
                HandleUpdateAsync,
                ErrorHandler,
                new ReceiverOptions { AllowedUpdates = { } },
                cancellationToken
            );

            var me = await _botClient.GetMeAsync();
            _logger.LogInformation("Бот запущен: @{BotName}", me.Username);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message || update.Message?.Text == null)
                return;

            var message = update.Message.Text;
            var chatId = update.Message.Chat.Id;

            if (!_userStates.ContainsKey(chatId))
            {
                _userStates[chatId] = UserState.None;
            }

            if (message == "/start")
            {
                if (_userTokens.ContainsKey(chatId) && _userRoles.ContainsKey(chatId))
                {
                    _userStates[chatId] = UserState.InMainMenu;
                    await ShowMainMenuAsync(chatId, _userRoles[chatId], cancellationToken);
                }
                else
                {
                    _userStates[chatId] = UserState.ChoosingAction;

                    var keyboard = new ReplyKeyboardMarkup(new[]
                    {
            new KeyboardButton[] { "Регистрация", "Вход" }
        })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                    await _botClient.SendTextMessageAsync(chatId, _messages["start"], replyMarkup: keyboard, cancellationToken: cancellationToken);
                }
                if (_userStates.TryGetValue(chatId, out var state) && state == UserState.WaitingForFinalGradeSubject)
                {
                    if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                    {
                        await _botClient.SendTextMessageAsync(chatId,
                            "Вы не авторизованы. Пожалуйста, нажмите /start и войдите в систему.",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    var subject = message.Trim();

                    if (!_allowedSubjects.Contains(subject, StringComparer.OrdinalIgnoreCase))
                    {
                        string subjectsList = string.Join("\n• ", _allowedSubjects.OrderBy(s => s));
                        await _botClient.SendTextMessageAsync(chatId,
                            $"Некорректный предмет. Доступные:\n• {subjectsList}\n\nВведите предмет заново:",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    try
                    {
                        var encodedSubject = Uri.EscapeDataString(subject);
                        var response = await httpClient.GetAsync($"Final grade by subject for student");

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var finalGrade = JsonSerializer.Deserialize<double?>(json);

                            if (finalGrade == null)
                            {
                                await _botClient.SendTextMessageAsync(chatId,
                                    $"По предмету «{subject}» пока нет оценок.",
                                    cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(chatId,
                                    $" Итоговая оценка по предмету «{subject}»: {Math.Round(finalGrade.Value, 2)}",
                                    cancellationToken: cancellationToken);
                            }
                        }
                        else
                        {
                            var errorText = await response.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId,
                                $" Ошибка при получении оценки:\n{errorText}",
                                cancellationToken: cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка запроса оценки: {ex.Message}");
                        await _botClient.SendTextMessageAsync(chatId,
                            "Произошла ошибка при получении оценки.",
                            cancellationToken: cancellationToken);
                    }

                    _userStates[chatId] = UserState.InMainMenu;
                    await ShowMainMenuAsync(chatId, _userRoles[chatId], cancellationToken);
                    return;
                }

                return;
            }

            switch (_userStates[chatId])
            {
                case UserState.ChoosingAction:
                    if (message.ToLower().Contains("регист"))
                    {
                        _userStates[chatId] = UserState.WaitingForEmail;
                        await _botClient.SendTextMessageAsync(chatId, "Введите ваш email:", cancellationToken: cancellationToken);
                    }
                    else if (message.ToLower().Contains("вход"))
                    {
                        _userStates[chatId] = UserState.WaitingForLoginEmail;
                        await _botClient.SendTextMessageAsync(chatId, "Введите email для входа:", cancellationToken: cancellationToken);
                    }
                    break;

                case UserState.WaitingForEmail:
                    var emailInput = message.Trim();

                    if (!emailInput.EndsWith("@edu.hse.ru"))
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Email должен оканчиваться на @edu.hse.ru. Повторите ввод.", cancellationToken: cancellationToken);
                        return;
                    }

                    _registrationEmails[chatId] = emailInput;
                    _userStates[chatId] = UserState.WaitingForPassword;
                    await _botClient.SendTextMessageAsync(chatId, "Введите пароль:", cancellationToken: cancellationToken);
                    break;

                case UserState.WaitingForPassword:
                    _registrationPasswords[chatId] = message.Trim();
                    _userStates[chatId] = UserState.WaitingForRole;

                    var roleKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton[] { "Студент", "Админ" }
                    })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                    await _botClient.SendTextMessageAsync(chatId, "Выберите роль:", replyMarkup: roleKeyboard, cancellationToken: cancellationToken);
                    break;

                case UserState.WaitingForRole:
                    var role = message.Trim();
                    string normalizedRole = role.ToLower() switch
                    {
                        "студент" => "Student",
                        "Студент" => "Student",
                        "админ" => "Admin",
                        "Админ" => "Admin",
                        _ => null
                    };

                    if (normalizedRole == null)
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Роль должна быть либо Студент, либо Админ. Повторите ввод.", cancellationToken: cancellationToken);
                        return;
                    }

                    var email = _registrationEmails[chatId];
                    var password = _registrationPasswords[chatId];

                    var client = _httpClientFactory.CreateClient();
                    var response = await client.PostAsJsonAsync("http://localhost:5176/api/Auth/Register", new
                    {
                        Email = email,
                        Password = password,
                        Role = normalizedRole
                    });

                    if (response.IsSuccessStatusCode)
                    {
                        await _botClient.SendTextMessageAsync(chatId, _messages["register_success"], cancellationToken: cancellationToken);
                        // После регистрации предлагаем войти
                        _userStates[chatId] = UserState.ChoosingAction;
                        await ShowStartMenuAsync(chatId, cancellationToken);
                    }
                    else
                    {
                        var errorText = await response.Content.ReadAsStringAsync();
                        await _botClient.SendTextMessageAsync(chatId, $"{_messages["register_fail"]}\n{errorText}", cancellationToken: cancellationToken);
                    }

                    _userStates[chatId] = UserState.None;
                    _registrationEmails.Remove(chatId);
                    _registrationPasswords.Remove(chatId);
                    break;

                case UserState.WaitingForLoginEmail:
                    var loginEmail = message.Trim();

                    if (!loginEmail.EndsWith("@edu.hse.ru"))
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Email должен оканчиваться на @edu.hse.ru. Повторите ввод.", cancellationToken: cancellationToken);
                        return;
                    }

                    _loginEmails[chatId] = loginEmail;
                    _userStates[chatId] = UserState.WaitingForLoginPassword;
                    await _botClient.SendTextMessageAsync(chatId, "Введите пароль:", cancellationToken: cancellationToken);
                    break;

                case UserState.WaitingForLoginPassword:
                    {
                        var loginPassword = message.Trim();
                        var loginEmail1 = _loginEmails[chatId];

                        var loginClient = _httpClientFactory.CreateClient();
                        var loginResponse = await loginClient.PostAsJsonAsync("http://localhost:5176/api/Auth/Login", new
                        {
                            Email = loginEmail1,
                            Password = loginPassword
                        });

                        if (loginResponse.IsSuccessStatusCode)
                        {
                            var json = await loginResponse.Content.ReadAsStringAsync();
                            var result = JsonDocument.Parse(json);
                            var token = result.RootElement.GetProperty("token").GetString();
                            var role1 = result.RootElement.GetProperty("role").GetString();

                            

                            _userTokens[chatId] = token!;
                            _userRoles[chatId] = role1!;
                            _userStates[chatId] = UserState.InMainMenu; // переход в главное меню

                            await _botClient.SendTextMessageAsync(chatId, _messages["login_success"], cancellationToken: cancellationToken);
                            await ShowMainMenuAsync(chatId, role1!, cancellationToken); // показать главное меню
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, _messages["login_fail"], cancellationToken: cancellationToken);
                        }

                        _loginEmails.Remove(chatId);
                        break;
                    }
                case UserState.WaitingForEditGrade_Id:
                    {
                        if (!int.TryParse(message, out var gradeId))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Некорректный ID. Введите число.", cancellationToken: cancellationToken);
                            return;
                        }

                        _editGradeIds[chatId] = gradeId;
                        _userStates[chatId] = UserState.WaitingForEditGrade_Subject;

                        string subjectsList = FormatSubjectsList(_allowedSubjects);
                        await _botClient.SendTextMessageAsync(chatId, $"Введите новый предмет. Доступные предметы:\n{subjectsList}", cancellationToken: cancellationToken);
                        return;
                    }

                case UserState.WaitingForEditGrade_Subject:
                    {
                        if (!_allowedSubjects.Contains(message))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Неверный предмет. Пожалуйста, выберите из списка.", cancellationToken: cancellationToken);
                            return;
                        }

                        _editSubjects[chatId] = message;
                        _userStates[chatId] = UserState.WaitingForEditGrade_Value;

                        await _botClient.SendTextMessageAsync(chatId, "Введите новую оценку (например, 4.5):", cancellationToken: cancellationToken);
                        return;
                    }

                case UserState.WaitingForStudentIdToDelete:
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        if (!int.TryParse(message, out int studentId))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Некорректный ID. Введите число:", cancellationToken: cancellationToken);
                            return;
                        }

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var response7 = await httpClient.DeleteAsync($"https://localhost:7006/api/Students/Delete student for admins?id={studentId}");

                        if (response7.IsSuccessStatusCode)
                        {
                            await _botClient.SendTextMessageAsync(chatId, $" Студент с ID {studentId} успешно удалён.", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var error = await response7.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId, $" Ошибка при удалении студента:\n{error}", cancellationToken: cancellationToken);
                        }

                        _userStates[chatId] = UserState.InMainMenu;
                        await ShowMainMenuAsync(chatId, _userRoles[chatId], cancellationToken);
                        break;
                    }

                case UserState.WaitingForEditGrade_Value:
                    {
                        if (!double.TryParse(message, out var value))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Некорректное значение. Введите число, например 4.5.", cancellationToken: cancellationToken);
                            return;
                        }

                        var gradeId = _editGradeIds[chatId];
                        var subject = _editSubjects[chatId];
                        var token = _userTokens[chatId];

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var dto = new
                        {
                            Subject = subject,
                            Value = value
                        };

                        var json = JsonSerializer.Serialize(dto);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response2 = await httpClient.PutAsync($"https://localhost:7006/api/Grades/Update grade for student?id={gradeId}", content);

                        if (!response2.IsSuccessStatusCode)
                        {
                            var error = await response2.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId, $" Ошибка при обновлении:\n{error}", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Оценка успешно обновлена!", cancellationToken: cancellationToken);
                        }

                        // Очистка
                        _editGradeIds.Remove(chatId);
                        _editSubjects.Remove(chatId);
                        _userStates[chatId] = UserState.InMainMenu;

                        return;
                    }
                case UserState.InMainMenu:
                    {
                        if (message == "Добавить оценку")
                        {
                            if (!_userTokens.ContainsKey(chatId))
                            {
                                await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                                return;
                            }

                            _userStates[chatId] = UserState.WaitingForAddGrade_Subject;
                            string subjectsList = FormatSubjectsList(_allowedSubjects);
                            await _botClient.SendTextMessageAsync(chatId, $"Введите предмет. Доступные предметы:\n{subjectsList}", cancellationToken: cancellationToken);
                            return;
                        }

                        if (message == "Показать все оценки")
                        {
                            if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                            {
                                await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Пожалуйста, нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                                return;
                            }

                            var httpClient = new HttpClient();
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                            var response2 = await httpClient.GetAsync("https://localhost:7006/api/Grades/My grades");

                            if (!response2.IsSuccessStatusCode)
                            {
                                var error = await response2.Content.ReadAsStringAsync();
                                await _botClient.SendTextMessageAsync(chatId, $" Ошибка при получении оценок:\n{error}", cancellationToken: cancellationToken);
                                return;
                            }

                            var gradesJson = await response2.Content.ReadAsStringAsync();
                            var grades = JsonSerializer.Deserialize<List<Grade>>(gradesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (grades == null || grades.Count == 0)
                            {
                                await _botClient.SendTextMessageAsync(chatId, "У вас пока нет ни одной оценки.", cancellationToken: cancellationToken);
                                return;
                            }

                            // Группировка по предметам, добавляем ID оценки
                            var grouped = grades
                                .GroupBy(g => g.Subject)
                                .Select(g => new
                                {
                                    Subject = g.Key,
                                    Items = g.Select(x => $"• ID{x.Id}: {x.WorkType} — {x.Value}")
                                });

                            var sb = new StringBuilder();
                            sb.AppendLine("Ваши оценки:");

                            foreach (var group in grouped)
                            {
                                sb.AppendLine($"\n*{group.Subject}*");
                                foreach (var item in group.Items)
                                {
                                    sb.AppendLine(item);
                                }
                            }

                            await _botClient.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: cancellationToken, parseMode: ParseMode.Markdown);
                            return;
                        }
                    }
                        if (message == "Изменить оценку")
                        {
                            if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                            {
                                await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                                return;
                            }

                            _userStates[chatId] = UserState.WaitingForEditGrade_Id;
                            await _botClient.SendTextMessageAsync(chatId, "Введите ID оценки, которую хотите изменить. Посмотреть ID можно через команду 'Показать все оценки'.", cancellationToken: cancellationToken);
                            return;
                        }

                        // обработка по ролям, если команда — не "Добавить оценку" и не "Показать все оценки"
                        var userRole = _userRoles.GetValueOrDefault(chatId);

                        if (userRole == "Student")
                        {
                            await HandleStudentMenuAsync(chatId, message, cancellationToken);
                        }
                        else if (userRole == "Admin")
                        {
                            await HandleAdminMenuAsync(chatId, message, cancellationToken);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Ошибка роли. Пожалуйста, войдите заново.", cancellationToken: cancellationToken);
                            _userStates[chatId] = UserState.ChoosingAction;
                            await ShowStartMenuAsync(chatId, cancellationToken);
                        }

                        break;
                    

                case UserState.WaitingForAddGrade_Subject:
                    {
                        var subject = message.Trim();

                        if (!_allowedSubjects.Contains(subject))
                        {
                            string subjectsList = string.Join(", ", _allowedSubjects);
                            await _botClient.SendTextMessageAsync(chatId, $" Некорректный предмет.\nДоступные:\n{subjectsList}\n\nВведите предмет заново:", cancellationToken: cancellationToken);
                            return;
                        }

                        _tempSubjects[chatId] = subject;
                        _userStates[chatId] = UserState.WaitingForAddGrade_Value;

                        await _botClient.SendTextMessageAsync(chatId, "Введите значение оценки (число от 1 до 10):", cancellationToken: cancellationToken);
                        break;
                    }

                case UserState.WaitingForAddGrade_Value:
                    {
                        if (!int.TryParse(message.Trim(), out int value) || value < 1 || value > 10)
                        {
                            await _botClient.SendTextMessageAsync(chatId, " Некорректное значение. Введите число от 1 до 10:", cancellationToken: cancellationToken);
                            return;
                        }

                        _tempValues[chatId] = value;
                        _userStates[chatId] = UserState.WaitingForAddGrade_WorkType;

                        var subjectInput = _tempSubjects[chatId];

                        if (_allowedWorkTypes.TryGetValue(subjectInput, out var allowedWorkTypes))
                        {
                            string types = string.Join(", ", allowedWorkTypes);
                            await _botClient.SendTextMessageAsync(chatId, $" Допустимые типы работы по предмету «{subjectInput}»:\n{types}", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, $" Для предмета «{subjectInput}» не указаны допустимые типы работ. Вы можете ввести тип вручную.", cancellationToken: cancellationToken);
                        }

                        break;
                    }
                case UserState.WaitingForDeleteGrade_Id:
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Пожалуйста, нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        if (!int.TryParse(message, out var gradeId))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Некорректный ID. Пожалуйста, введите число.", cancellationToken: cancellationToken);
                            return;
                        }

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var response3 = await httpClient.DeleteAsync($"https://localhost:7006/api/Grades/Delete grade for student?id={gradeId}");

                        if (response3.IsSuccessStatusCode)
                        {
                            await _botClient.SendTextMessageAsync(chatId, " Оценка успешно удалена.", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var error = await response3.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId, $" Ошибка при удалении оценки:\n{error}", cancellationToken: cancellationToken);
                        }

                        _userStates[chatId] = UserState.InMainMenu;
                        await ShowMainMenuAsync(chatId, _userRoles[chatId], cancellationToken);
                        break;
                    }
                case UserState.WaitingForStudentIdForGrades:
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        if (!int.TryParse(message, out int studentId))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Некорректный формат ID. Введите число.", cancellationToken: cancellationToken);
                            return;
                        }

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var url = $"https://localhost:7006/api/Grades/Get%20student%27s%20grades%20for%20admins?studentId={studentId}";
                        var response6 = await httpClient.GetAsync(url);

                        if (response6.IsSuccessStatusCode)
                        {
                            var json = await response6.Content.ReadAsStringAsync();
                            var grades = JsonSerializer.Deserialize<List<Grade>>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (grades == null || grades.Count == 0)
                            {
                                await _botClient.SendTextMessageAsync(chatId, "У этого студента пока нет оценок.", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                var messageText = $"Оценки студента с ID {studentId}:\n";
                                foreach (var grade in grades)
                                {
                                    messageText += $"- {grade.Subject} | {grade.WorkType} | {grade.Value}\n";
                                }

                                await _botClient.SendTextMessageAsync(chatId, messageText, cancellationToken: cancellationToken);
                            }
                        }
                        else
                        {
                            var error = await response6.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId, $"Ошибка при получении оценок: {error}", cancellationToken: cancellationToken);
                        }

                        _userStates[chatId] = UserState.InMainMenu;
                        await ShowMainMenuAsync(chatId, _userRoles[chatId], cancellationToken);
                        break;
                    }
                case UserState.WaitingForFinalGradeSubject:
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Пожалуйста, нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        var subject = message.Trim();
                        if (string.IsNullOrEmpty(subject))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Название предмета не может быть пустым. Пожалуйста, введите корректное название.", cancellationToken: cancellationToken);
                            return;
                        }

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var response5 = await httpClient.GetAsync($"https://localhost:7006/api/Grades/Final grade by subject for student?subject={Uri.EscapeDataString(subject)}");

                        if (response5.IsSuccessStatusCode)
                        {
                            var resultJson = await response5.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(resultJson);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("subject", out var subjectProp) && root.TryGetProperty("finalGrade", out var gradeProp))
                            {
                                var subjectName = subjectProp.GetString();
                                var finalGrade = gradeProp.GetDouble();

                                await _botClient.SendTextMessageAsync(chatId, $"Итоговая оценка по предмету \"{subjectName}\": {finalGrade}", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] Ошибка: в ответе нет нужных полей. JSON: {resultJson}");
                                await _botClient.SendTextMessageAsync(chatId, "Не удалось получить итоговую оценку по предмету. Попробуйте снова.", cancellationToken: cancellationToken);
                            }
                        }

                        _userStates[chatId] = UserState.InMainMenu;
                        await ShowMainMenuAsync(chatId, _userRoles[chatId], cancellationToken);
                        break;
                    }
                case UserState.WaitingForSubjectFilter:
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Пожалуйста, нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        var subject = message.Trim();

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var response4 = await httpClient.GetAsync($"https://localhost:7006/api/Grades/Get grades by one subject for student?subject={Uri.EscapeDataString(subject)}");

                        if (!response4.IsSuccessStatusCode)
                        {
                            var error = await response4.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId, $"Ошибка при получении оценок: {error}", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var content = await response4.Content.ReadAsStringAsync();

                            // Если сервер вернул строку, а не JSON
                            if (content.StartsWith("\"") || content.StartsWith("Оценок"))
                            {
                                await _botClient.SendTextMessageAsync(chatId, content.Trim('"'), cancellationToken: cancellationToken);
                            }
                            else
                            {
                                var grades = JsonSerializer.Deserialize<List<Grade>>(content, new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                });

                                if (grades == null || grades.Count == 0)
                                {
                                    await _botClient.SendTextMessageAsync(chatId, "Оценок по этому предмету ещё нет.", cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    var sb = new StringBuilder();
                                    sb.AppendLine($"Оценки по предмету: {subject}\n");
                                    foreach (var grade in grades)
                                    {
                                        sb.AppendLine($"ID: {grade.Id} — {grade.WorkType}: {grade.Value}");
                                    }

                                    await _botClient.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: cancellationToken);
                                }
                            }
                        }

                        _userStates[chatId] = UserState.InMainMenu;
                        await ShowMainMenuAsync(chatId, _userRoles[chatId], cancellationToken);
                        break;
                    }


                case UserState.WaitingForAddGrade_WorkType:
                    {
                        Console.WriteLine($"[LOG] Обработка WorkType для chatId {chatId}");

                        if (!_tempSubjects.ContainsKey(chatId) || !_tempValues.ContainsKey(chatId))
                        {
                            Console.WriteLine($"[ERROR] tempSubjects или tempValues отсутствуют для chatId {chatId}");
                            await _botClient.SendTextMessageAsync(chatId, " Ошибка состояния. Попробуйте снова с команды /start.", cancellationToken: cancellationToken);
                            _userStates[chatId] = UserState.None;
                            return;
                        }

                        var workType = message.Trim();

                        if (string.IsNullOrWhiteSpace(workType) || workType.Length < 2)
                        {
                            Console.WriteLine($"[WARN] Пустой или короткий WorkType: '{workType}'");
                            await _botClient.SendTextMessageAsync(chatId, " Тип работы не может быть пустым. Введите, например: \"контрольная\" или \"экзамен\".", cancellationToken: cancellationToken);
                            return;
                        }

                        if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                        {
                            Console.WriteLine($"[ERROR] Не найден токен для chatId {chatId}");
                            await _botClient.SendTextMessageAsync(chatId, " Вы не авторизованы. Войдите через /start.", cancellationToken: cancellationToken);
                            return;
                        }

                        var subject = _tempSubjects[chatId];

                        if (_allowedWorkTypes.TryGetValue(subject, out var allowedTypes))
                        {
                            if (!allowedTypes.Contains(workType, StringComparer.OrdinalIgnoreCase))
                            {
                                string allowedList = string.Join(", ", allowedTypes);
                                await _botClient.SendTextMessageAsync(chatId, $" Недопустимый тип работы для предмета «{subject}».\nДопустимые типы: {allowedList}\nВведите тип заново:", cancellationToken: cancellationToken);
                                return;
                            }
                        }

                        
                        var value = _tempValues[chatId];

                        var gradeRequest = new
                        {
                            Subject = subject,
                            Value = value,
                            WorkType = workType
                        };

                        Console.WriteLine($"[LOG] Отправка запроса на добавление оценки:");
                        Console.WriteLine($"Subject: {subject}");
                        Console.WriteLine($"Value: {value}");
                        Console.WriteLine($"WorkType: {workType}");
                        Console.WriteLine($"Token: {token.Substring(0, Math.Min(token.Length, 15))}...");

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        try
                        {
                            var response1 = await httpClient.PostAsJsonAsync("https://localhost:7006/api/Grades/Add grades for student", gradeRequest);

                            var resultText = await response1.Content.ReadAsStringAsync();
                            Console.WriteLine($"[LOG] Ответ от сервера (StatusCode: {(int)response1.StatusCode}): {resultText}");

                            if (response1.IsSuccessStatusCode)
                            {
                                await _botClient.SendTextMessageAsync(chatId, " Оценка успешно добавлена!", cancellationToken: cancellationToken);
                            }
                            else
                            {
                                await _botClient.SendTextMessageAsync(chatId, $" Ошибка добавления оценки:\n{resultText}", cancellationToken: cancellationToken);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[EXCEPTION] Ошибка при добавлении оценки: {ex.Message}");
                            await _botClient.SendTextMessageAsync(chatId, $" Исключение при отправке запроса:\n{ex.Message}", cancellationToken: cancellationToken);
                        }

                        // Очистка состояния и переход в меню
                        _userStates[chatId] = UserState.InMainMenu;
                        _tempSubjects.Remove(chatId);
                        _tempValues.Remove(chatId);
                        break;
                    }

            }
        }
        // Главное меню для Студентов
        private async Task HandleStudentMenuAsync(long chatId, string message, CancellationToken cancellationToken)
        {
            switch (message.ToLower())
            {
                case "Добавить оценку":
                    if (!_userTokens.ContainsKey(chatId))
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                        return;
                    }

                    _userStates[chatId] = UserState.WaitingForAddGrade_Subject;
                    await _botClient.SendTextMessageAsync(chatId, "Введите предмет:", cancellationToken: cancellationToken);
                    break;

                case "показать все оценки":
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token2) || string.IsNullOrEmpty(token2))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Пожалуйста, нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);

                        var response = await httpClient.GetAsync("https://localhost:7006/api/Grades/My grades");

                        if (!response.IsSuccessStatusCode)
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId, $" Ошибка при получении оценок:\n{error}", cancellationToken: cancellationToken);
                            return;
                        }

                        var gradesJson = await response.Content.ReadAsStringAsync();
                        var grades = JsonSerializer.Deserialize<List<Grade>>(gradesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (grades == null || grades.Count == 0)
                        {
                            await _botClient.SendTextMessageAsync(chatId, "У вас пока нет ни одной оценки.", cancellationToken: cancellationToken);
                            return;
                        }

                        var grouped = grades
                            .GroupBy(g => g.Subject)
                            .Select(g => new
                            {
                                Subject = g.Key,
                                Items = g.Select(x => $"• ID{x.Id}: {x.WorkType} — {x.Value}")
                            });

                        var sb = new StringBuilder();
                        sb.AppendLine("Ваши оценки:");

                        foreach (var group in grouped)
                        {
                            sb.AppendLine($"\n*{group.Subject}*");
                            foreach (var item in group.Items)
                            {
                                sb.AppendLine(item);
                            }
                        }

                        await _botClient.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: cancellationToken, parseMode: ParseMode.Markdown);
                        break;
                    }

                case "заменить оценку":
                    if (!_userTokens.TryGetValue(chatId, out var token) || string.IsNullOrEmpty(token))
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                        return;
                    }

                    _userStates[chatId] = UserState.WaitingForEditGrade_Id;
                    await _botClient.SendTextMessageAsync(chatId, "Введите ID оценки, которую хотите изменить. Посмотреть ID можно через команду 'Показать все оценки'.", cancellationToken: cancellationToken);
                    break;

                case "удалить оценку":
                    if (!_userTokens.ContainsKey(chatId))
                    {
                        await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                        return;
                    }

                    _userStates[chatId] = UserState.WaitingForDeleteGrade_Id;
                    await _botClient.SendTextMessageAsync(chatId, "Введите ID оценки, которую вы хотите удалить:", cancellationToken: cancellationToken);
                    break;

                case "оценки по предмету":
                    if (!_userTokens.ContainsKey(chatId))
                    {
                        string subjectsList = string.Join(", ", _allowedSubjects);
                        await _botClient.SendTextMessageAsync(chatId, $" Некорректный предмет.\nДоступные:\n{subjectsList}\n\nВведите предмет заново:", cancellationToken: cancellationToken);
                        await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                        return;
                    }

                    _userStates[chatId] = UserState.WaitingForSubjectFilter;
                    await _botClient.SendTextMessageAsync(chatId, "Введите название предмета:", cancellationToken: cancellationToken);
                    break;

                case "итоговая оценка по предмету":
                    {
                        if (!_userTokens.ContainsKey(chatId))
                        {
                            await _botClient.SendTextMessageAsync(chatId,
                                "Вы не авторизованы. Нажмите /start и войдите в систему.",
                                cancellationToken: cancellationToken);
                            return;
                        }

                        _userStates[chatId] = UserState.WaitingForFinalGradeSubject;

                        // Показать доступные предметы из _allowedSubjects
                        string subjectsList = string.Join("\n• ", _allowedSubjects.OrderBy(s => s));
                        await _botClient.SendTextMessageAsync(chatId,
                            $"Введите предмет, по которому хотите узнать итоговую оценку.\n\nДоступные:\n• {subjectsList}",
                            cancellationToken: cancellationToken);
                        break;
                    }
                case "/start":
                    await ShowMainMenuAsync(chatId, "Student", cancellationToken);
                    break;

                default:
                    await _botClient.SendTextMessageAsync(chatId, _messages["unknown_command"], cancellationToken: cancellationToken);
                    break;
            }
        }

        // Главное меню для Админов
        private async Task HandleAdminMenuAsync(long chatId, string message, CancellationToken cancellationToken)
        {
            switch (message.ToLower())
            {
                case "оценки студента":
                    {
                        if (!_userRoles.TryGetValue(chatId, out var role) || role != "Admin")
                        {
                            await _botClient.SendTextMessageAsync(chatId, "У вас нет прав администратора.", cancellationToken: cancellationToken);
                            return;
                        }

                        _userStates[chatId] = UserState.WaitingForStudentIdForGrades;
                        await _botClient.SendTextMessageAsync(chatId, "Введите ID студента, чьи оценки вы хотите посмотреть:", cancellationToken: cancellationToken);
                        break;
                    }

                case "список студентов":
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        if (!_userRoles.TryGetValue(chatId, out var role) || role != "Admin")
                        {
                            await _botClient.SendTextMessageAsync(chatId, "У вас нет доступа к этой команде. Только для администраторов.", cancellationToken: cancellationToken);
                            return;
                        }

                        var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        var response = await httpClient.GetAsync("https://localhost:7006/api/Students/Get all students for admins");

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var students = JsonSerializer.Deserialize<List<Student>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (students == null || students.Count == 0)
                            {
                                await _botClient.SendTextMessageAsync(chatId, "Студенты не найдены.", cancellationToken: cancellationToken);
                                return;
                            }

                            var sb = new StringBuilder();
                            sb.AppendLine(" Список студентов:");
                            foreach (var s in students)
                            {
                                sb.AppendLine($" Id: {s.Id} |  Email: {s.Email}");
                            }

                            await _botClient.SendTextMessageAsync(chatId, sb.ToString(), cancellationToken: cancellationToken);
                        }
                        else
                        {
                            var error = await response.Content.ReadAsStringAsync();
                            await _botClient.SendTextMessageAsync(chatId, $"Ошибка при получении списка студентов:\n{error}", cancellationToken: cancellationToken);
                        }

                        break;
                    }

                case "удалить студента":
                    {
                        if (!_userTokens.TryGetValue(chatId, out var token))
                        {
                            await _botClient.SendTextMessageAsync(chatId, "Вы не авторизованы. Нажмите /start и войдите в систему.", cancellationToken: cancellationToken);
                            return;
                        }

                        if (!_userRoles.TryGetValue(chatId, out var role) || role != "Admin")
                        {
                            await _botClient.SendTextMessageAsync(chatId, "У вас нет доступа к этой команде. Только для администраторов.", cancellationToken: cancellationToken);
                            return;
                        }

                        _userStates[chatId] = UserState.WaitingForStudentIdToDelete;
                        await _botClient.SendTextMessageAsync(chatId, "Введите ID студента, которого нужно удалить:", cancellationToken: cancellationToken);
                        break;
                    }

                case "/start":
                    await ShowMainMenuAsync(chatId, "Admin", cancellationToken);
                    break;

                default:
                    await _botClient.SendTextMessageAsync(chatId, _messages["unknown_command"], cancellationToken: cancellationToken);
                    break;
            }
        }


        private async Task ShowStartMenuAsync(long chatId, CancellationToken cancellationToken)
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Регистрация", "Вход" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await _botClient.SendTextMessageAsync(chatId, _messages["start"], replyMarkup: keyboard, cancellationToken: cancellationToken);
        }

        private async Task ShowMainMenuAsync(long chatId, string role, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup keyboard;

            if (role == "Student")
            {
                keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "Добавить оценку", "Показать все оценки" },
                    new KeyboardButton[] { "Заменить оценку", "Удалить оценку" },
                    new KeyboardButton[] { "Оценки по предмету", "Итоговая оценка по предмету" }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
            }
            else if (role == "Admin")
            {
                keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "Оценки студента", "Список студентов" },
                    new KeyboardButton[] { "Удалить студента" }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
            }
            else
            {
                keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton[] { "Регистрация", "Вход" }
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };
            }

            await _botClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: keyboard, cancellationToken: cancellationToken);
        }



        private Task ErrorHandler(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Telegram ошибка");
            Console.WriteLine($"Telegram Exception: {exception.Message}");
            return Task.CompletedTask;
        }
      
    }
}


