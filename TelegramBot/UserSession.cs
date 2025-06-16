namespace GradeBotWebAPI.TelegramBot
{
    public enum BotState
    {
        None,
        AwaitingAuthChoice,
        AwaitingEmail,
        AwaitingPassword,
        AwaitingRole,
        Completed
    }

    public class UserSession
    {
        public BotState State { get; set; } = BotState.None;
        public string Action { get; set; } = string.Empty; // "login" or "register"
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}