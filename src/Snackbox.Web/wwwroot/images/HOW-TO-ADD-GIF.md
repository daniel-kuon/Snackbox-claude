# How to Add Achievement Celebration GIF

The achievement system supports custom animated GIFs for celebration effects!

## Quick Start

1. **Find or Create Your GIF**
   - Search for celebration GIFs on GIPHY, Tenor, or create your own
   - Recommended: confetti, party poppers, fireworks, trophy animations
   - Keep it fun and lighthearted!

2. **Prepare the GIF**
   - Optimal size: 200x200 pixels (or similar square aspect ratio)
   - File size: Under 1MB for fast loading
   - Format: GIF, PNG, or JPEG

3. **Add to Project**
   - Place your GIF in: `src/Snackbox.Web/wwwroot/images/`
   - Example: `celebration.gif`

4. **Update Achievement Configuration**
   
   Edit `src/Snackbox.Api/Data/ApplicationDbContext.cs` in the `SeedData` method:

   ```csharp
   // Find the achievement you want to update
   new Achievement 
   { 
       Id = 1, 
       Code = "BIG_SPENDER_5", 
       Name = "Snack Attack!", 
       Description = "Spent €5 or more in a single purchase", 
       Category = AchievementCategory.SinglePurchase,
       ImageUrl = "/images/celebration.gif"  // <-- Add this line
   }
   ```

5. **Apply Changes**
   ```bash
   cd src/Snackbox.Api
   dotnet ef database drop --force
   dotnet ef database update
   ```
   Or simply restart the API - it will recreate the database with seed data

## Using Different GIFs for Different Achievements

You can have unique GIFs for each achievement:

```csharp
// Big spender gets confetti
new Achievement { ..., ImageUrl = "/images/confetti.gif" },

// High debt gets a funny "oh no" animation
new Achievement { ..., ImageUrl = "/images/oh-no.gif" },

// Streak achievements get fire emoji animation
new Achievement { ..., ImageUrl = "/images/fire.gif" },
```

## Default Behavior

If `ImageUrl` is not set, the system shows a default trophy emoji (🏆) with bounce animation.

## Tips

- **Keep it appropriate** - Remember this is a workplace application
- **Test file size** - Large GIFs slow down the experience
- **Match the mood** - Funny GIFs for debt achievements, celebration for spending milestones
- **Consider looping** - Ensure GIFs loop smoothly for the 4-second display duration

## Example GIF Sources

- GIPHY: https://giphy.com/search/celebration
- Tenor: https://tenor.com/search/achievement-gifs
- Flaticon (static icons): https://www.flaticon.com/search?word=trophy

## Need Help?

See the full [Achievement System Documentation](../docs/achievement-system.md) for more details.
