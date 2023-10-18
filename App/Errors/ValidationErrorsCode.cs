namespace App.Errors;

public static class ValidationErrorsCode
{
    public const string NameRequired = "name_required"; // имя является обязательным
    public const string UserNotFound = "user_not_found"; // пользователь не найден
    public const string TeamNotFound = "team_not_found"; // команда не найдена
    public const string TeamAlreadyExists = "team_already_exists"; // команда уже существует
    public const string UserAlreadyHasTeam = "user_already_has_team"; // пользователь уже имеет команду
    public const string NameAlreadyTaken = "name_already_taken"; // имя уже занято
    public const string UserIsNotScrumMaster = "user_is_not_scrum_master"; // пользователь не является scrum master
}