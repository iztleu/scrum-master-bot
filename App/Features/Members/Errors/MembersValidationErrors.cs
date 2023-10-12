namespace App.Features.Member.Errors;

public static class MembersValidationErrors
{
    private const string Prefix = "member_validation_";
    
    public const string NameRequired = Prefix + "name_required";
    public const string UserNotFound = Prefix + "user_not_found";
    public const string TeamNotFound = Prefix + "team_not_found";
    public const string ScrumMasterNotFound = Prefix + "scrum_master_not_found";
    public const string MemberNotFound = Prefix + "member_not_found";
    public const string UserAlreadyInTeam = Prefix + "user_already_in_team";
    public const string OwnerCannotLeaveTeam = Prefix + "owner_cannot_leave_team";
}