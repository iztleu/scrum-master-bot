namespace App.Features.ScrumTeam.Errors;

public static class ScrumTeamValidationErrors
{
    private const string Prefix = "scrum_team_validation_";
    public static string WithPrefix(this string value) => Prefix + value;
    public static string WithoutPrefix(this string value) => value.Replace(Prefix, "");
}