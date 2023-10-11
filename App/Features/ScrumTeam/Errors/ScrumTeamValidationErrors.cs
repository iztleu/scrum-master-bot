namespace App.Features.ScrumTeam.Errors;

public static class ScrumTeamValidationErrors
{
    private const string Prefix = "scrum_team_validation_";
    
    public const string NameRequired = Prefix + "name_required";
    public const string UserNotFound = Prefix + "user_not_found";
    public const string TeamNotFound = Prefix + "team_not_found";
    public const string TeamAlreadyExists = Prefix + "team_already_exists";
    public const string UserAlreadyHasTeam = Prefix + "user_already_has_team";
    public const string NameAlreadyTaken = Prefix + "name_already_taken";
}