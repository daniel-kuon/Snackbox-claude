# PayPal payments

People pay their Snackbox debt to an admin's PayPal account with "Snackbox" in the note (the
PayPal QR codes on the kiosk and the tablet ask for it - a paypal.me link cannot fill a note in). The API fetches the received payments
every 30 minutes, keeps the ones with "Snackbox" in the note or subject, and lists them under
**Admin → PayPal**. There you pick, once per payer, which Snackbox user they are: the payment is
booked, and every later one from that payer is booked by itself - as a *PayPal* payment to the
admin whose account received it, exactly like a payment entered by hand (deposit for the payer,
withdrawal for the admin who now holds the money).

## Setting it up - private PayPal account (mailbox)

PayPal's API is for business accounts only. A private account still gets a mail for every
payment ("Du hast eine Zahlung erhalten"), and Snackbox reads those from the Outlook.com /
Microsoft 365 mailbox they arrive in.

1. An app registration - the OneDrive backup's one will do (docs/ONEDRIVE-BACKUP.md, step 1:
   personal Microsoft accounts, *Allow public client flows: Yes*). The mailbox permission
   (`Mail.Read`, read-only) is asked for when signing in.
2. **Admin → PayPal → Where payments come from**: paste the client id, *Connect mailbox*, open
   the address shown on any device, enter the code and sign in **to the mailbox PayPal's mails go
   to**.
3. **Admin → Settings → PayPal payments → Account of**: the admin whose PayPal account it is,
   save.
4. **Admin → PayPal → Fetch now.** The first run looks 31 days back.

What is read: only mails in the **inbox** from a PayPal address (`...@paypal.de`, `...@paypal.com`)
that the mail server confirmed as genuine - `dmarc=pass` in the topmost *Authentication-Results*
header, the one Outlook adds itself. Anyone can write a mail that looks like a payment; a booked
one lowers somebody's debt. Payment mails moved out of the inbox by a rule are not seen. The mail
gives the payer's name but no e-mail address, so payers are told apart by name. Only German
PayPal mails are understood. The refresh token is stored with Data Protection (DPAPI) in
`%ProgramData%\Snackbox\paypal-mailbox.json`.

## Setting it up - PayPal business account (API)

With a business account PayPal's Transaction Search API gives the payments directly (and
recognises refunds and chargebacks). A connected mailbox takes precedence.

1. Log in at <https://developer.paypal.com/dashboard/applications/live> with the PayPal account
   people pay to, *Create App* (type *Merchant*).
2. In the app's settings switch on **Transaction search** and save. PayPal can take a few hours
   before the permission works ("NOT_AUTHORIZED" until then).
3. Copy *Client ID* and *Secret* into **Admin → Settings → PayPal payments**, choose under
   *Account of* the admin whose PayPal account it is, save.
4. **Admin → PayPal → Fetch now.** The first run looks 31 days back.

*Sandbox* switches to PayPal's test system (`api-m.sandbox.paypal.com`) for trying it with
sandbox accounts.

## Good to know

- PayPal lists a payment up to 3 hours after it was made; every run looks a day back, so nothing
  is missed and nothing is booked twice (the PayPal transaction id is unique).
- Only completed payments into the account count - pending ones are picked up once completed,
  money sent from the account and refunds are ignored.
- Only payments in euros are booked; one in another currency stays on the list - book it by
  hand (converted) and dismiss it.
- When PayPal takes a payment back (reversal, chargeback, refund - API only, the mailbox does not
  see those) the payment is marked
  *taken back*: never booked from then on, and if it was booked already it stays on the list in
  red until the booked payment is corrected by hand and the row is marked *Done*.
- *always* next to *Book* (on by default) remembers the payer for that user. Untick it when
  somebody pays once for someone else ("Snackbox Max" from a colleague's account): only that
  payment is booked, and the payer's next one waits for the admin again.
- *Dismiss* takes a payment off the open list (not for Snackbox after all, or already booked by
  hand); *Show booked and dismissed ones* brings it back.
- Deleting a booked payment in the user's details puts the PayPal payment back to open.
- The keyword is `PayPal:Keyword` (default `Snackbox`), the interval `PayPal:PollMinutes` (30).
- Payer names and e-mail addresses stay out of the API's HTTP body logging.
