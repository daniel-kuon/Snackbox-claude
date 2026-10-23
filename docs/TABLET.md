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
   tablet asks (tick *Always allow*).
3. The PC needs `adb.exe` (Android *platform-tools*). The updater looks on `PATH`, in
   `%LOCALAPPDATA%\Android\Sdk\platform-tools`, in `C:\platform-tools`, or wherever
   `SNACKBOX_ADB` points.
4. Run once (afterwards `start` and every watchdog run do it by themselves):
   ```powershell
   & "C:\Snackbox\tools\Snackbox.Updater\bin\Release\net10.0\Snackbox.Updater.exe" tablet
   ```
   It runs `adb reverse tcp:5186 tcp:5186`: the tablet's `localhost:5186` is now the PC's website.
5. In Chrome on the tablet open `http://localhost:5186/tablet`, then *⋮ → Add to Home screen* and
   start it from there (full screen). Keep the screen on: *Developer options → Stay awake*.

**Trust model**: like the kiosk's scan, the tablet needs no login - it can only add to the
session a card scan opened in the last minute. That is fine while the website is reachable only
on the PC itself (the default: Aspire binds it to 127.0.0.1, and the tablet comes in through
`adb reverse`). If the website is ever opened to the Wi-Fi for the phone app, anyone there could
open `/tablet` and tap onto someone's open purchase - restrict `/tablet` to loopback first.

The forwarding is lost when the cable is unplugged or the tablet restarts; the watchdog task
(every 5 minutes) sets it again, or run the `tablet` command by hand. The tablet connects over
plain HTTP from the loopback address, so the website does not redirect it to HTTPS.

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

Tablet purchases do not reduce the shelf stock (like all purchases); the inventory count
(Admin → Inventory) corrects it.
