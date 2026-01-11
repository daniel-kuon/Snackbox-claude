namespace Snackbox.Api.Dtos;

public class AchievementDto
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime EarnedAt { get; set; }
}
