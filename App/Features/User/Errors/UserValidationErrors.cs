namespace App.Features.User.Errors;

public static class UserValidationErrors
{
    private const string Prefix = "user_validation_";
    
    public const string UserNotFound = Prefix + "user_not_found";
    public const string AlreadyExists = Prefix + "user_already_exists";
}