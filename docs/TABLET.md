# Tablet next to the scanner

An Android tablet on a USB cable shows the products on the shelf with picture and price.
Users who are switched to *picks on the tablet* scan any of their codes as before; the scan then
books nothing itself, the tablet greets them and every tap books that product's price. They can
change the list with − / + / ✕ until the purchase ends - by **Done**, by the next person's scan or
by the scanner timeout (60 s after the last tap or scan), exactly like scanning cards.

## Setting up the tablet (once)

1. On the tablet: *Settings → About tablet*, tap *Build number* seven times, then
   *Settings → Developer options → USB debugging* on.
2. Connect it to the Snackbox PC with the USB cable and allow USB debugging for this PC when the
   tablet asks (tick *Always allow*). Android 7 or newer works.
   If `adb devices` lists nothing and Windows shows the tablet only as a media device (MTP),
   USB debugging is still off, or (some no-name tablets) Windows lacks the ADB driver: install
   Google's USB driver from the Android SDK manager or the tablet maker's driver.
3. The PC needs `adb.exe` (Android *platform-tools*). The updater looks on `PATH`, in
   `%LOCALAPPDATA%\Android\Sdk\platform-tools`, in `C:\platform-tools`, or wherever
   `SNACKBOX_ADB` points.
4. Run once (afterwards `start` and every watchdog run do it by themselves):
   ```powershell
   & "C:\Snackbox\tools\Snackbox.Updater\bin\Release\net10.0\Snackbox.Updater.exe" tablet
   ```
   It runs `adb reverse tcp:5186 tcp:5186`: the tablet's `localhost:5186` is now the PC's website.
5. On the tablet open **`http://127.0.0.1:5186/tablet`** - not `localhost`: Firefox resolves that
   to the IPv6 address, which `adb reverse` does not forward ("connection interrupted"). Then
   switch off *⋮ → Desktop-Website*: the page is laid out for the tablet's own width, and Firefox
   drops the desktop-mode scaling in full screen, so with it on everything changes size when the
   page goes full screen. (*Add to Home screen* gives only a shortcut that opens a normal tab -
   Firefox installs a page as an app only over HTTPS with a certificate it trusts.)
   Browser: the Chrome that comes with old tablets is too old for the page, and after a reset the
   Play Store may refuse to sign in. Firefox 143 (the last version for Android 7, official APK from
   `archive.mozilla.org/pub/fenix/releases/143.0.4/android/`, armeabi-v7a for 32-bit tablets)
   installs over the cable with `adb install`.
6. Screen: *Settings → Security → Screen lock → None* (a PIN or pattern cannot be skipped), and
   a short *Settings → Display → Screen timeout* (e.g. 1 minute). Leave *Developer options →
   Stay awake* off. Every card scan wakes the screen through the same USB connection, so no app
   is needed on the tablet: a press of the power key written straight into the tablet's input
   device (`sendevent`; the devices are found once with `getevent -pl`), because Android's own
   `input` command starts a Java process and takes 2 seconds on a cheap tablet. `wm
   dismiss-keyguard` exists only from Android 8, so on Android 7 the screen lock really has to be
   *None*. After a long sleep the page reconnects (or reloads) by itself within a few
   seconds of waking. `Tablet:WakeOnScan=false` in the API's settings turns the wake-up off;
   `Tablet:AdbPath` points it at adb if it is not in one of the usual places.

**Trust model**: like the kiosk's scan, the tablet needs no login - it can only add to the
session a card scan opened in the last minute. That is fine while the website is reachable only
on the PC itself (the default: Aspire binds it to 127.0.0.1, and the tablet comes in through
`adb reverse`). If the website is ever opened to the Wi-Fi for the phone app, anyone there could
open `/tablet` and tap onto someone's open purchase - restrict `/tablet` to loopback first.

The forwarding is lost when the cable is unplugged or the tablet restarts; the watchdog task
(every 5 minutes) sets it again, or run the `tablet` command by hand. The tablet connects over
plain HTTP from the loopback address, so the website does not redirect it to HTTPS.

## Who uses it

The feature switch **Admin → Settings → Features → "Picking products on the tablet"** starts at
*beta testers*. A scan opens the tablet for users who have *Picks products on the tablet* ticked
**and** see the feature - everyone else keeps paying the fixed card amounts. Set the switch to
*Everyone* once it has proven itself.

## The screen

The shelf has 3×3 compartments and the tablet sits in the top middle one, so the screen is the
same 3×3 grid: every product shows in its compartment (Admin → Products → a product → *Tablet* →
*Compartment*; several products may share one), products without a compartment in a row below.
The tablet's own cell holds the greeting, the total, **Correct** (the list with − / + / ✕) and
**Done**. The kiosk screen follows the tablet: it shows what was picked, the total and the open
amount, keeps the same countdown, and ends with the tablet's session.

Full screen: a page may only ask for it after a touch, and loses it when the screen goes off. So
until it is full screen the page shows a welcome screen ("Hi Daniel! Tap the screen to start")
over everything, and half a second after waking the screen the PC taps it (`sendevent`, in the
tablet's own cell, which has no button even if the page got full screen meanwhile). A tap by the
user does the same; either way the first tap hits nothing, and the page's jump when Firefox's bars
go away happens behind the welcome screen, whose text stays at the middle of the physical screen.

Pinned to the page: the updater (`start`, `tablet`, every watchdog run) pins Firefox with the page
(Android's *screen pinning*, confirm once on the tablet) - Home, app switcher and notifications do
nothing, so nobody leaves it by accident. Holding Back + app switcher together ends it on purpose;
`Snackbox.Updater tablet --unlock` unpins it and keeps the watchdog from pinning again until
`tablet --lock`.

Screen: dark between customers. A scan wakes it (see full screen above). *Done* closes the purchase - achievements earned with
it show right away, with a close button - and for 10 seconds the shelf makes way for a summary:
what was just bought, the month's last 5 purchases, the last payment, and **New purchase**, which
brings the shelf back for another purchase (the toolbar still works too); then it goes dark, as
it does when a purchase runs out. *New purchase* works only in the 10 seconds after *Done*:
reading a dialog may keep the screen on, but after that it needs a new scan, so whoever steps up
next cannot book onto the previous card. While a
purchase is open the page keeps the screen from dimming (Screen Wake Lock); between Android's own screen timeout applies (Settings → Display).

The introduction plays by itself with a progress bar (as on the kiosk) until a button is tapped;
it and the dialogs (correct, pay, purchases, payments, achievements) stop the countdown - 2 minutes
without a touch close the screen all the same. Achievements of users with the tablet feature show
on the tablet (after Done, or left over at the next scan), everyone else's on the kiosk as before;
the 🏆 button lists all of them with the date they were earned.

Who scans sees their purchase on the tablet - with the tablet feature, everyone: users who pay with
their card codes see their scanned amounts and total (taps on products are refused), tablet users
pick. The toolbar shows purchases, payments, the open amount with *Pay* (the PayPal QR) and *?*
(the introduction again). The payment reminder of the kiosk shows below it. The introduction plays
on the first scan (`user_wizard_steps` "tablet_intro").

Layout: a compartment's products share its height, so few products show big; with more than three
in one compartment, name and price go over the picture.

## Products and users

- **Price and picture**: Admin → Products → a product → *Tablet*. Only products with a price
  **and** stock on the shelf are shown. *Suggest pictures* shows pictures from Open Food Facts
  (by barcode and by name) and - with a SerpApi key in Admin → Settings - from Google Images;
  click one to use it. *Search Google Images* opens Google in a new tab: right-click a picture,
  *Copy image address*, paste it into the address field. Products created by the batch scan get
  the Open Food Facts picture automatically.
- **Users**: Admin → Users → a user → Edit → *Picks products on the tablet*. Unticking it puts
  them back on the fixed amounts printed on their card.
- Each tap is stored as a scan of the card code they scanned, at the product's price, with the
  product attached - balance, history, achievements and discounts work unchanged, and the
  purchase history shows the product names.

Unlike card scans, a tablet pick says which product was taken: each one takes an item off the
shelf (from the batch with the oldest best-before date), and − / ✕ put it back. Card scans still
leave the shelf alone; the inventory count (Admin → Inventory) corrects the shelf for those.
