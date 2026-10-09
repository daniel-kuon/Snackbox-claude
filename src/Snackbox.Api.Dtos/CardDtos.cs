namespace Snackbox.Api.Dtos;

/// <summary>
/// A numbered, pre-made card with its two purchase codes. Until someone claims it, the card
/// is an inactive user called "Karte NN".
/// </summary>
public class CardDto
{
    public int Number { get; set; }
    public int? UserId { get; set; }
    public string? Username { get; set; }

    /// <summary>True once a person has claimed the card (the user is active).</summary>
    public bool IsClaimed { get; set; }

    public string? Code30 { get; set; }
    public string? Code50 { get; set; }
}

/// <summary>The two codes scanned for a card: first the €0.30 one, then the €0.50 one.</summary>
public class SaveCardDto
{
    public string Code30 { get; set; } = "";
    public string Code50 { get; set; } = "";
}
