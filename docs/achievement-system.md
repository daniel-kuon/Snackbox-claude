# Achievement System

The Snackbox achievement system rewards users with fun, humorous achievements based on their purchasing behavior. Achievements are automatically awarded when certain criteria are met during purchase completion.

## Features

### Achievement Categories

1. **Single Purchase** - Based on the amount spent in a single purchase
   - **Snack Attack!** - €5 or more in one purchase
   - **Hunger Games Champion** - €10 or more in one purchase  
   - **Snack Hoarder** - €15 or more in one purchase

2. **Daily Activity** - Based on number of purchases in a day
   - **Frequent Flyer** - 5 or more purchases in a single day
   - **Snack Marathon** - 10 or more purchases in a single day

3. **Streaks** - Based on purchase consistency
   - **Three-peat** - Made a purchase 3 days in a row
   - **Week Warrior** - Made a purchase 7 days in a row
   - **Monthly Muncher** - At least one purchase per week for 4 weeks

4. **Comeback** - Based on time since last purchase
   - **Long Time No See** - First purchase after 1 month away
   - **The Return** - First purchase after 2 months away
   - **Lazarus Rising** - First purchase after 3 months away

5. **High Debt** - Based on unpaid balance (humorous!)
   - **Credit Card Lifestyle** - €50 or more unpaid
   - **Financial Freedom? Never Heard of It** - €100 or more unpaid
   - **Living on the Edge** - €150 or more unpaid

6. **Total Spent** - Based on lifetime spending
   - **Century Club** - €100 or more spent in total
   - **Snack Connoisseur** - €150 or more spent in total
   - **Snackbox Legend** - €200 or more spent in total

## Implementation

### Backend

**Models:**
- `Achievement` - Defines an achievement (code, name, description, category, optional image)
- `UserAchievement` - Tracks which achievements each user has earned
- `AchievementCategory` - Enum for achievement types

**Service:**
- `AchievementService` - Detects and awards achievements based on purchase data
- Checks are performed when a purchase times out and is marked as completed
- Achievements are only awarded once per user

**Database:**
- Achievements are seeded in `ApplicationDbContext`
- User achievements are tracked with timestamps
- `HasBeenShown` flag prevents re-showing achievements

### Frontend

**UI Component:**
- `AchievementNotification.razor` - Animated overlay that displays earned achievements
- Automatically shows when new achievements are detected
- Displays achievement details with celebratory animations
- Shows current scan info so users know what triggered it
- Auto-hides after 4 seconds, then shows next achievement if multiple were earned

**Integration:**
- Scanner page checks for new achievements on each scan response
- Achievements are displayed as an overlay without hiding the QR code or scan info
- Multiple achievements queue and display sequentially

## Customization

### Adding Custom Achievement Images

1. Add your GIF or image to `/wwwroot/images/`
2. Update the achievement seed data in `ApplicationDbContext.cs`:
   ```csharp
   new Achievement 
   { 
       Id = 1, 
       Code = "BIG_SPENDER_5",
       Name = "Snack Attack!",
       Description = "Spent €5 or more in a single purchase",
       Category = AchievementCategory.SinglePurchase,
       ImageUrl = "/images/celebration.gif"  // Add this
   }
   ```
3. Apply migrations and restart the API

### Adding New Achievements

1. Add the achievement to the seed data in `ApplicationDbContext.cs`
2. Implement the detection logic in `AchievementService.cs`
3. Create and apply a new migration
4. The UI will automatically display any new achievements earned

## User Experience

When a user earns an achievement:

1. The achievement notification overlay appears with celebration effects
2. Shows achievement icon/GIF (or default trophy emoji)
3. Displays achievement name and description
4. Shows what triggered it (e.g., "Current scan: €10.00")
5. Auto-dismisses after 4 seconds
6. If multiple achievements earned, shows them sequentially

## Technical Notes

- Achievements are checked server-side to prevent tampering
- Each achievement can only be earned once per user
- Detection runs after purchase timeout (when purchase is completed)
- The `HasBeenShown` flag ensures notifications aren't repeated
- Achievements are included in the scan response only when newly earned
- Frontend clears achievements from display after showing them

## Testing

Unit tests are provided in `AchievementServiceTests.cs` that cover:
- Single purchase amount detection
- Total spent detection
- High debt detection
- Already-earned achievement handling
- Multiple achievement scenarios

## Future Enhancements

Potential additions:
- Achievement sharing/social features
- Leaderboards showing top achievers
- Seasonal/special event achievements
- Achievement rarity tiers
- Sound effects for achievements
- Achievement history view
