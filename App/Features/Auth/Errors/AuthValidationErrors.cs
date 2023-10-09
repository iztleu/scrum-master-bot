namespace App.Features.Auth.Errors;

public static class AuthValidationErrors
{
    private const string Prefix = "auth_validation_";

    public const string WrongCredentials = Prefix + "wrong_credentials";
    public const string UserNameRequired = Prefix + "user_name_required";
    public const string UserNameDoesNotExist = Prefix + "user_name_does_not_exist";
}