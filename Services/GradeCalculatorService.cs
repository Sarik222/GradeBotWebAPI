using GradeBotWebAPI.Models;

namespace GradeBotWebAPI.Services
{
    public class GradeCalculatorService
    {
        // Словарь формул оценивания по предметам
        private readonly Dictionary<string, Func<List<Grade>, double?>> _formulas;

        // Словарь допустимых работ по каждому предмету
        public readonly Dictionary<string, List<string>> AllowedWorkTypes = new()
        {
            ["Безопасность жизнедеятельности"] = new List<string> { "Презентация", "Экзамен" },
            ["Основы российской государственности"] = new List<string> { "Оценка"},
            ["Английский язык"] = new List<string> {"Лексико-грамматический тест", "Монолог", "Описание графика", "Экзамен" },
            ["Практикум по основам разработки технической документации"] = new List<string> { "Оценка" },
            ["Программирование"] = new List<string> { "Отчёт1", "Защита1", "Отчёт2", "Защита2", "Отчёт3", "Защита3", "Отчёт4", "Защита4", "Отчёт5", "Защита5", "Отчёт6", "Защита6" },
            ["Дискретная математика"] = new List<string> { "КР1", "КР2" },
            ["Профориентационный семинар"] = new List<string> { "Оценка" },
            ["История России"] = new List<string> { "Оценка" },
            ["Алгебра"] = new List<string> { "КР1", "КР2", "Микроконтроль", "Самостоятельная работа", "Экзамен" },
            ["Теоретические основы информатики"] = new List<string> { "Лабораторная работа", "Самостоятельная работа", "Тест", "Экзамен"},
            ["Независимый экзамен по цифровой грамотности"] = new List<string> { "Оценка" },
            ["Проектный семинар"] = new List<string> { "Оценка" },
            ["Исследовательский или прикладной проект"] = new List<string> { "Оценка" },
            ["Математический анализ"] = new List<string> { "КР1", "КР2", "Микроконтроль", "Самостоятельная работа", "Экзамен" },
            ["Правовая грамотность"] = new List<string> { "Оценка" },
            ["Внутренний экзамен по английскому языку"] = new List<string> { "Оценка" }
        };  
        // Допустимое число работ по каждому предмету и типу работы
        public Dictionary<string, Dictionary<string, int?>> MaxAllowedGradesPerWorkType = new()
        {
            ["Безопасность жизнедеятельности"] = new Dictionary<string, int?>
            {
                ["Презентация"] = 1,
                ["Экзамен"] = 1
            },

            ["Основы российской государственности"] = new Dictionary<string, int?>
            {
                ["Оценка"] = null
            },

            ["Английский язык"] = new Dictionary<string, int?>
            {
                ["Лексико-грамматический тест"] = 3,
                ["Монолог"] = 3,
                ["Описание графика"] = 3,
                ["Экзамен"] = 1
            },

            ["Практикум по основам разработки технической документации"] = new Dictionary<string, int?>
            {
                ["Оценка"] = null
            },

            ["Программирование"] = new Dictionary<string, int?>
            {
                ["Отчёт1"] = 1,
                ["Защита1"] = 1,
                ["Отчёт2"] = 1,
                ["Защита2"] = 1,
                ["Отчёт3"] = 1,
                ["Защита3"] = 1,
                ["Отчёт4"] = 1,
                ["Защита4"] = 1,
                ["Отчёт5"] = 1,
                ["Защита5"] = 1,
                ["Отчёт6"] = 1,
                ["Защита6"] = 1,
            },

            ["Дискретная математика"] = new Dictionary<string, int?>
            {
                ["КР1"] = 1,
                ["КР2"] = 1
            },

            ["Профориентационный семинар"] = new Dictionary<string, int?>
            {
                ["Оценка"] = null
            },

            ["История России"] = new Dictionary<string, int?>
            {
                ["Оценка"] = null
            },

            ["Алгебра"] = new Dictionary<string, int?>
            {
                ["КР1"] = 1,
                ["КР2"] = 1,
                ["Микроконтроль"] = null,
                ["Самостоятельная работа"] = 1,
                ["Экзамен"] = 1
            },
           
            ["Теоретические основы информатики"] = new Dictionary<string, int?>
            {
                ["Лабораторная работа"] = null,
                ["Самостоятельная работа"] = 1,
                ["Тест"] = null,
                ["Экзамен"] = 1
            },

            ["Независимый экзамен по цифровой грамотности"] = new Dictionary<string, int?>
            {
                ["Оценка"] = 1
            },

            ["Проектный семинар"] = new Dictionary<string, int?>
            {
                ["Оценка"] = null
            },

            ["Исследовательский или прикладной проект"] = new Dictionary<string, int?>
            {
                ["Оценка"] = 1
            },

            ["Математический анализ"] = new Dictionary<string, int?>
            {
                ["КР1"] = 1,
                ["КР2"] = 1,
                ["Микроконтроль"] = null,
                ["Самостоятельная работа"] = 1,
                ["Экзамен"] = 1
            },

            ["Правовая грамотность"] = new Dictionary<string, int?>
            {
                ["Оценка"] = null
            },

            ["Внутренний экзамен по английскому языку"] = new Dictionary<string, int?>
            {
                ["Оценка"] = null
            }
        };
        //формулы для вычисления оценок
        public GradeCalculatorService()
        {
            _formulas = new Dictionary<string, Func<List<Grade>, double?>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Безопасность жизнедеятельности"] = grades =>
                {
                    var presentation = grades.FirstOrDefault(g => g.WorkType == "Презентация")?.Value ?? 0;
                    var exam = grades.FirstOrDefault(g => g.WorkType == "Экзамен")?.Value ?? 0;
                    return 0.4 * presentation + 0.6 * exam;
                },

                ["Основы российской государственности"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },

                ["Английский язык"] = grades =>
                {
                    // среднее по "Лексико-грамматический тест"
                    var avgTest = grades
                        .Where(g => g.WorkType == "Лексико-грамматический тест")
                        .Select(g => g.Value)
                        .DefaultIfEmpty(0)
                        .Average();

                    // среднее по "Монолог"
                    var avgMonologue = grades
                        .Where(g => g.WorkType == "Монолог")
                        .Select(g => g.Value)
                        .DefaultIfEmpty(0)
                        .Average();

                    // среднее по "Описание графика"
                    var avgDescription = grades
                        .Where(g => g.WorkType == "Описание графика")
                        .Select(g => g.Value)
                        .DefaultIfEmpty(0)
                        .Average();

                    // среднее по "Экзамен"
                    var avgExam = grades
                        .Where(g => g.WorkType == "Экзамен")
                        .Select(g => g.Value)
                        .DefaultIfEmpty(0)
                        .Average();

                    // итоговый балл с учётом весов
                    return 0.25 * avgTest + 0.25 * avgMonologue + 0.10 * avgDescription + 0.40 * avgExam;
                },

                ["Практикум по основам разработки технической документации"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },



                ["Программирование"] = grades =>
                {
                    // Функция для вычисления средней оценки по паре "Отчёт" и "Защита" для одной лабораторной
                    double CalcLabAverage(int labNumber)
                    {
                        var report = grades.FirstOrDefault(g => g.WorkType == $"Отчёт{labNumber}")?.Value ?? 0;
                        var defense = grades.FirstOrDefault(g => g.WorkType == $"Защита{labNumber}")?.Value ?? 0;
                        return 0.5 * report + 0.5 * defense;
                    }

                    // Считаем средние для всех 6 лабораторных
                    double total = 0;
                    for (int i = 1; i <= 6; i++)
                    {
                        total += CalcLabAverage(i);
                    }

                    // Умножаем итог на 0.167 (приблизительно 1/6)
                    return total * 0.167;
                },

                ["Дискретная математика"] = grades =>
                {
                    var kr1 = grades.FirstOrDefault(g => g.WorkType == "КР1")?.Value ?? 0;
                    var kr2 = grades.FirstOrDefault(g => g.WorkType == "КР2")?.Value ?? 0;
                    return 0.5 * kr1 + 0.5 * kr2;
                },

                ["Профориентационный семинар"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },
                
                ["История России"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },

                ["Алгебра"] = grades =>
                {
                    var kr1 = grades.FirstOrDefault(g => g.WorkType == "КР1")?.Value ?? 0;
                    var kr2 = grades.FirstOrDefault(g => g.WorkType == "КР2")?.Value ?? 0;
                    var selfWork = grades.FirstOrDefault(g => g.WorkType == "Самостоятельная работа")?.Value ?? 0;
                    var exam = grades.FirstOrDefault(g => g.WorkType == "Экзамен")?.Value ?? 0;

                    var microcontrols = grades
                        .Where(g => g.WorkType == "Микроконтроль" && g.Value != null)
                        .Select(g => (double)g.Value!)
                        .ToList();

                    double microAvg = microcontrols.Count > 0 ? microcontrols.Average() : 0;

                    return 0.15 * kr1 + 0.15 * kr2 + 0.10 * selfWork + 0.40 * exam + 0.20 * microAvg;
                },

                ["Теоретические основы информатики"] = grades =>
                {
                    // Среднее арифметическое по всем лабораторным работам
                    var labs = grades
                        .Where(g => g.WorkType == "Лабораторная работа" && g.Value != null)
                        .Select(g => (double)g.Value!)
                        .ToList();
                    var labAvg = labs.Count > 0 ? labs.Average() : 0;

                    // Самостоятельная работа — берём одну (предположим, она одна)
                    var selfWork = (double)(grades.FirstOrDefault(g => g.WorkType == "Самостоятельная работа")?.Value ?? 0);

                    // Среднее арифметическое по всем тестам
                    var tests = grades
                        .Where(g => g.WorkType == "Тест" && g.Value != null)
                        .Select(g => (double)g.Value!)
                        .ToList();
                    var testAvg = tests.Count > 0 ? tests.Average() : 0;

                    // Экзамен — берём одну оценку
                    var exam = (double)(grades.FirstOrDefault(g => g.WorkType == "Экзамен")?.Value ?? 0);

                    // Подсчёт итоговой оценки
                    return 0.3 * labAvg + 0.3 * selfWork + 0.2 * testAvg + 0.2 * exam;
                },

                ["Независимый экзамен по цифровой грамотности"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },

                ["Проектный семинар"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },

                ["Исследовательский или прикладной проект"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },

                ["Математический анализ"] = grades =>
                {
                    var kr1 = grades.FirstOrDefault(g => g.WorkType == "КР1")?.Value ?? 0;
                    var kr2 = grades.FirstOrDefault(g => g.WorkType == "КР2")?.Value ?? 0;
                    var selfWork = grades.FirstOrDefault(g => g.WorkType == "Самостоятельная работа")?.Value ?? 0;
                    var exam = grades.FirstOrDefault(g => g.WorkType == "Экзамен")?.Value ?? 0;

                    var microcontrols = grades
                        .Where(g => g.WorkType == "Микроконтроль" && g.Value != null)
                        .Select(g => (double)g.Value!)
                        .ToList();

                    double microAvg = microcontrols.Count > 0 ? microcontrols.Average() : 0;

                    return 0.15 * kr1 + 0.15 * kr2 + 0.10 * selfWork + 0.40 * exam + 0.20 * microAvg;
                },

                ["Правовая грамотность"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },

                ["Внутренний экзамен по английскому языку"] = grades =>
                {
                    if (grades.Count == 0) return 0;
                    return grades.Average(g => g.Value);
                },

            };
        }
        //расчет балла
        public double? Calculate(string subject, List<Grade> grades)
        {
            Console.WriteLine($"[DEBUG] Расчёт оценки для: '{subject}'");

            Console.WriteLine("[DEBUG] Доступные предметы с формулами:");
            foreach (var key in _formulas.Keys)
                Console.WriteLine($" - {key}");
            return _formulas.TryGetValue(subject, out var formula)
                ? formula(grades)
                : null;
        }

        public bool HasFormula(string subject) => _formulas.ContainsKey(subject);
    }
}
