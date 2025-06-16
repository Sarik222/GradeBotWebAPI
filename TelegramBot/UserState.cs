namespace GradeBotWebAPI.TelegramBot
{
    public enum UserState
    {
        None,
        ChoosingAction,
        WaitingForEmail,
        WaitingForPassword,
        WaitingForRole,
        WaitingForLoginEmail,
        WaitingForLoginPassword,
        // Добавим состояние ожидания команды в меню
        InMainMenu,
        WaitingForAddGrade_Subject,
        WaitingForAddGrade_Value,
        WaitingForAddGrade_WorkType,
        WaitingForEditGrade_Id,
        WaitingForEditGrade_Subject,
        WaitingForEditGrade_Value,
        WaitingForDeleteGrade_Id,
        WaitingForSubjectFilter,
        WaitingForFinalGradeSubject,
        AwaitingSubjectForFinalGrade,
        WaitingForStudentIdForGrades,
        WaitingForStudentIdToDelete
    }
}