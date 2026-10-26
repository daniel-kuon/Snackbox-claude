# PayPal payments

People pay their Snackbox debt to an admin's PayPal account with "Snackbox" in the note (the
PayPal QR codes on the kiosk and the tablet ask for it - a paypal.me link cannot fill a note in). The API fetches the received payments
every 30 minutes, keeps the ones with "Snackbox" in the note or subject, and lists them under
**Admin → PayPal**. There you pick, once per payer, which Snackbox user they are: the payment is
booked, and every later one from that payer is booked by itself - as a *PayPal* payment to the
admin whose account received it, exactly like a payment entered by hand (deposit for the payer,
withdrawal for the admin who now holds the money).

## Setting it up

PayPal hands out API access through a REST app on developer.paypal.com, and **live** apps need
a **business account**. A private account can be upgraded under *Settings → Account → Upgrade to a
business account* (no charge for the upgrade itself; check PayPal's current fees for business
accounts receiving money, and that most business accounts cannot be turned back).

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
- When PayPal takes a payment back (reversal, chargeback, refund) the payment is marked
  *taken back*: never booked from then on, and if it was booked already it stays on the list in
  red until the booked payment is corrected by hand and the row is marked *Done*.
- *Dismiss* takes a payment off the open list (not for Snackbox after all, or already booked by
  hand); *Show booked and dismissed ones* brings it back.
- Deleting a booked payment in the user's details puts the PayPal payment back to open.
- The keyword is `PayPal:Keyword` (default `Snackbox`), the interval `PayPal:PollMinutes` (30).
- Payer names and e-mail addresses stay out of the API's HTTP body logging.

## Without a business account

The API needs one. What would work with a private account is reading PayPal's "You've received
money" e-mails from the mailbox they go to - possible, but it depends on the mail provider, so it
is not built yet.
