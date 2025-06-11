namespace GradeBotWebAPI.Models
{
    public class Grade
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public int Value { get; set; }  // переименуем Grade в Value для ясности
    }
}
