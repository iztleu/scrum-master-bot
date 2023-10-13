namespace App.Features.Voting.Errors;

public static class VotingValidationErrors
{
    private const string Prefix = "voting_team_validation_";
    
    public const string NameRequired = Prefix + "name_required";
    public const string UserNotFound = Prefix + "user_not_found";
    public const string TeamNotFound = Prefix + "team_not_found";
    public const string UserAlreadyVoted = Prefix + "user_already_voted";
    public const string UserIsNotMember = Prefix + "user_is_not_member";
    public const string NameAlreadyTaken = Prefix + "name_already_taken";
    public const string UserIsNotScrumMaster = Prefix + "user_is_not_scrum_master";
    public const string VotingNotFound = Prefix + "voting_not_found";
    public const string VotingAlreadyExists = Prefix + "voting_already_started";
}