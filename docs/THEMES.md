# Seasonal themes

The kiosk can dress up for a season. Halloween is the first theme: a moonlit night with a
haunted house, graves, glowing pumpkins, bats, ghosts, a witch, a spider and the odd lightning
on the welcome screen. During a purchase there is the same night around the panels, a pumpkin
countdown, and a ghost that says BOO at every scan. Each theme brings its own achievements.

## Switching

**Admin -> Settings -> Themes**, one row per theme:

| Mode | Shows |
|---|---|
| Off | never |
| Dates | every year from the start day to the end day, both included. The end may lie in the next year (e.g. 20.12. to 6.1.) |
| Always | until switched back |

Changes are saved right away, and the kiosk follows within a minute (it asks `GET /api/themes/active`
every minute). If two themes are on at the same time, *Always* wins over *Dates*, and otherwise
the older theme wins. Halloween starts as *Dates*, from 24.10. to 1.11.

## Theme achievements

Achievements with a `ThemeKey` (category `Seasonal`) can only be earned while their theme is on,
and once per year. Halloween has four:

| Achievement | When |
|---|---|
| Trick or Treat! | any purchase while the theme is on |
| Happy Halloween! | a purchase on 31 October |
| Witching Hour | a purchase after 6 pm (kiosk time) |
| Monster Muncher | 5 purchases since the theme started this year |

They are checked in `AchievementService.CheckSeasonalAchievements`, against the local time of the
kiosk PC, where the API runs.

## Adding a theme

1. Add a key to `ThemeKeys` (Snackbox.Api.Dtos).
2. Seed a row in the `themes` table (`ApplicationDbContext`, `Theme` entity) and create a migration.
3. Add a scene component to `src/Snackbox.Web/Components/Themes/` and a case for it in `ThemeScene.razor`.
   - Draw behind the screen with z-index 0 and in front of it with z-index 40. Home lifts the screen itself to z-index 1.
   - Restyle the screen under the class `sb-theme-<key>`, which Home puts on its container.
   - The welcome screen's second line comes from `ThemeScene.WelcomeLine`.
   - Animate only `transform` and `opacity`: the kiosk PC is an old 4-core i5.
     Shadow pulses (`text-shadow`, `box-shadow`) and smooth changes inside SVGs repaint every
     frame (use `steps()`). Even one running animation costs WebView2 about 8 % of a core, so the
     Halloween welcome scene, which is up all day, plays for 45 s and then rests, paused, for 135 s.
4. Optional achievements: seed them with `Category = Seasonal` and `ThemeKey`, then check them
   in `CheckSeasonalAchievements`.

Testing on a development PC: set the theme to *Always*, or run the debug kiosk with the WebView2 debug port
(`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9222`) and capture the page
over CDP.
