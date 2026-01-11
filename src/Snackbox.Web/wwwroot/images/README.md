# Achievement Images

This directory contains images and GIFs for achievement notifications.

## Default Achievement Image

To add a custom achievement celebration GIF:

1. Add your GIF file to this directory (e.g., `celebration.gif`)
2. Update the `AchievementNotification.razor` component to reference the image
3. Or set the `ImageUrl` property in the Achievement seed data in `ApplicationDbContext.cs`

## Recommended Specifications

- **Format**: GIF, PNG, or JPEG
- **Size**: 200x200 pixels (or similar square dimensions)
- **File Size**: Keep under 1MB for fast loading
- **Content**: Fun, celebratory animations work best!

## Example Usage

In `ApplicationDbContext.cs` seed data:
```csharp
new Achievement 
{ 
    Id = 1, 
    Code = "BIG_SPENDER_5", 
    Name = "Snack Attack!", 
    Description = "Spent €5 or more in a single purchase", 
    Category = AchievementCategory.SinglePurchase,
    ImageUrl = "/images/celebration.gif"  // Add this line
}
```
