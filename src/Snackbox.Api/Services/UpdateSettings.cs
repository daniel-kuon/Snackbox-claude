namespace Snackbox.Api.Services;

public class UpdateSettings
{
    public string RepoOwner { get; set; } = "daniel-kuon";
    public string RepoName { get; set; } = "snackbox-claude";
    public string? InstallPath { get; set; }
    public string? InstallScriptPath { get; set; }
}
