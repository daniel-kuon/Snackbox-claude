# Barcode Lookup Feature

This feature allows administrators to search for product information using barcodes via the barcodelookup.com API.

## Configuration

### API Key Setup

The feature requires a barcodelookup.com API key. To set it up:

1. Sign up for an account at https://www.barcodelookup.com/
2. Obtain your API key from the dashboard
3. Configure the API key in one of the following ways:

**Option 1: appsettings.json (for production)**
```json
{
  "BarcodeLookup": {
    "ApiKey": "your-actual-api-key-here"
  }
}
```

**Option 2: appsettings.Development.json (for development)**
```json
{
  "BarcodeLookup": {
    "ApiKey": "your-actual-api-key-here"
  }
}
```

**Option 3: User Secrets (recommended for development)**
```bash
cd src/Snackbox.Api
dotnet user-secrets set "BarcodeLookup:ApiKey" "your-actual-api-key-here"
```

## Usage

### For Administrators

1. Navigate to the Products management page (`/admin/products`)
2. Click the "Lookup Barcode" button in the top-right corner
3. Enter a barcode number (UPC, EAN, or ISBN)
4. Click "Search" or press Enter
5. Review the product information returned from the API:
   - Title
   - Manufacturer (if available)
   - Brand (if available)
   - Category (if available)
6. Select a naming format for the product:
   - **Title only**: Uses just the product title
   - **Manufacturer + Title**: Combines manufacturer and title
   - **Brand + Title**: Combines brand and title
   - **Custom**: Enter your own product name
7. Set the product price (required)
8. Optionally edit or add a description
9. Click "Create Product" to save the product to the database

## API Endpoint

The backend provides a REST endpoint for barcode lookup:

```
GET /api/barcodelookup/{barcode}
```

**Authorization**: Requires Admin role

**Response**:
```json
{
  "success": true,
  "product": {
    "title": "Product Name",
    "manufacturer": "Manufacturer Name",
    "brand": "Brand Name",
    "description": "Product description",
    "category": "Product category",
    "barcode": "1234567890123"
  }
}
```

## Testing

Run the barcode lookup tests:
```bash
dotnet test tests/Snackbox.Api.Tests/Snackbox.Api.Tests.csproj --filter "FullyQualifiedName~BarcodeLookup"
```

## Technical Details

### Backend Components

- **BarcodeLookupService**: Handles HTTP communication with barcodelookup.com API
- **BarcodeLookupController**: Provides REST endpoint for frontend
- **DTOs**: Data transfer objects for API communication

### Frontend Components

- **BarcodeLookupModal.razor**: Modal component for barcode lookup interface
- Integrated into Products.razor page

### Error Handling

The service handles various error scenarios:
- Empty or invalid barcodes
- Products not found
- Network errors
- API errors (rate limits, authentication failures)

All errors are logged and user-friendly messages are displayed to administrators.
