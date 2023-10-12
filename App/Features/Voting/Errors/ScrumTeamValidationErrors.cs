namespace App.Features.Voting.Errors;

public static class VotingValidationErrors
{
    private const string Prefix = "voting_team_validation_";
    
    public const string NameRequired = Prefix + "name_required";
    public const string UserNotFound = Prefix + "user_not_found";
    public const string TeamNotFound = Prefix + "team_not_found";
    public const string TeamAlreadyExists = Prefix + "team_already_exists";
    public const string UserAlreadyHasTeam = Prefix + "user_already_has_team";
    public const string NameAlreadyTaken = Prefix + "name_already_taken";
    public const string UserIsNotScrumMaster = Prefix + "user_is_not_scrum_master";
}