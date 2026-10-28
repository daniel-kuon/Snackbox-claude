# Seasonal themes

The kiosk can dress up for a season: Halloween, Black Friday, St. Nikolaus, Christmas and New
Year's Eve. Halloween was the first: a moonlit night with a
haunted house, graves, glowing pumpkins, bats, ghosts, a witch, a spider and the odd lightning
on the welcome screen. During a purchase there is the same night around the panels, a pumpkin
countdown, and a ghost that says BOO at every scan. Each theme brings its own achievements.

## Switching

**Admin -> Settings -> Themes**, one row per theme:

| Mode | Shows |
|---|---|
| Off | never |
| Dates | every year from the start day to the end day, both included. The end may lie in the next year (e.g. 20.12. to 6.1.) |
| Its day (`Auto`) | only for themes whose day moves: worked out every year by `ThemeCalendar` |
| Always | until switched back |

Changes are saved right away, and the kiosk follows within a minute (it asks `GET /api/themes/active`
every minute). If two themes are on at the same time, *Always* wins, then the shorter run (St.
Nikolaus during Christmas), then the older theme.

| Theme | Starts as | Extra |
|---|---|---|
| Halloween | Dates 24.10. - 1.11. | four achievements |
| Black Friday | Its day: the day after the fourth Thursday of November (27.11.2026) | 30 % off everyone's first purchase of the day |
| St. Nikolaus | Its day: 6.12., on a Saturday or Sunday the Monday after (7.12.2026) | 30 % off everyone's first purchase of the day |
| Christmas | Dates 1.12. - 26.12. | |
| New Year's Eve | Dates 27.12. - 2.1. | shows the coming year |

## Theme discounts

A discount with a `ThemeKey` only applies while that theme is on, and `FirstPurchaseOnly` limits it
to a user's first purchase of the day (the kiosk PC's local day; a purchase scanned again later
keeps it). Both are checked in `PurchaseDiscountCalculator`. The Black Friday and St. Nikolaus
discounts are seeded (ids 9001 and 9002) and show up on Admin -> Discounts, where they can be
switched off or changed like any other.

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
3. Add a scene component to `src/Snackbox.Web/Components/Themes/` (`@inherits ThemeSceneBase`) and a case for it in `ThemeScene.razor`. For a day that moves, add a rule to `ThemeCalendar` and seed the theme as `Auto`.
   - Draw behind the screen with z-index 0 and in front of it with z-index 40. Home lifts the screen itself to z-index 1.
   - Restyle the screen under the class `sb-theme-<key>`, which Home puts on its container.
   - The welcome screen's second line comes from `ThemeScene.WelcomeLine`.
   - Animate only `transform` and `opacity`: the kiosk PC is an old 4-core i5.
     Shadow pulses (`text-shadow`, `box-shadow`) and smooth changes inside SVGs repaint every
     frame (use `steps()`). Even one running animation costs WebView2 about 8 % of a core, so the
     welcome scenes, which are up all day, play for 45 s and then rest, paused, for 135 s:
     `ThemeSceneBase.Resting`, which the scene turns into `animation-play-state: paused`.
   - On a purchase, keep things that move across the screen behind the panels (z-index 0).
4. Optional achievements: seed them with `Category = Seasonal` and `ThemeKey`, then check them
   in `CheckSeasonalAchievements`.
5. Optional discount: seed a `Discount` with the `ThemeKey` (and `FirstPurchaseOnly`), with a high id.

Testing on a development PC: set the theme to *Always* (in a test database), or run the debug kiosk with the WebView2 debug port
(`WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS=--remote-debugging-port=9222`) and capture the page
over CDP.
