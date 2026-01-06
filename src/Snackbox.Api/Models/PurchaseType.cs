namespace Snackbox.Api.Models;

public enum PurchaseType
{
    Normal,        // Regular barcode-scanned purchase
    Manual,        // Manually entered purchase by admin
    Correction     // Purchase correction referencing another purchase
}
