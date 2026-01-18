# Discount System

The discount system allows administrators to create and manage discounts that are automatically applied to purchases during barcode scanning.

## Features

- **Automatic Application**: Discounts are automatically detected and applied when users scan barcodes
- **Two Discount Types**:
  - **Fixed Amount**: Reduces purchase by a fixed amount (e.g., 0.50€ off)
  - **Percentage**: Reduces purchase by a percentage (e.g., 10% off)
- **Date-Based Validity**: Discounts are only active within their ValidFrom and ValidTo date range
- **Minimum Purchase Requirement**: Discounts can require a minimum purchase amount to qualify
- **Active/Inactive Toggle**: Discounts can be temporarily disabled without deletion

## Database Schema

The `discounts` table contains:

| Column | Type | Description |
|--------|------|-------------|
| id | integer | Primary key |
| name | varchar(200) | Display name of the discount |
| valid_from | timestamp | Start date/time for discount validity |
| valid_to | timestamp | End date/time for discount validity |
| minimum_purchase_amount | decimal(10,2) | Minimum purchase amount required |
| type | integer | 0 = FixedAmount, 1 = Percentage |
| value | decimal(10,2) | Discount value (amount in euros or percentage) |
| is_active | boolean | Whether the discount is currently active |
| created_at | timestamp | When the discount was created |

## API Endpoints

All endpoints require Admin role authentication.

### List All Discounts
```
GET /api/discounts?activeOnly=true
```

**Query Parameters:**
- `activeOnly` (optional, boolean): If true, only returns discounts that are currently active and within their validity period

**Response:**
```json
[
  {
    "id": 1,
    "name": "10% Off Spring Sale",
    "validFrom": "2026-03-01T00:00:00Z",
    "validTo": "2026-03-31T23:59:59Z",
    "minimumPurchaseAmount": 5.00,
    "type": "Percentage",
    "value": 10,
    "isActive": true,
    "createdAt": "2026-02-15T10:00:00Z"
  }
]
```

### Get Single Discount
```
GET /api/discounts/{id}
```

**Response:** Same as single item in list response

### Create Discount
```
POST /api/discounts
```

**Request Body:**
```json
{
  "name": "Summer Special",
  "validFrom": "2026-06-01T00:00:00Z",
  "validTo": "2026-08-31T23:59:59Z",
  "minimumPurchaseAmount": 3.00,
  "type": "FixedAmount",
  "value": 0.50,
  "isActive": true
}
```

**Validation Rules:**
- `type` must be "FixedAmount" or "Percentage"
- `value` must be between 0-100 if type is "Percentage"
- `validFrom` must be before `validTo`

**Response:** Created discount with assigned ID and createdAt timestamp

### Update Discount
```
PUT /api/discounts/{id}
```

**Request Body:** Same as create, but must include `id` field

**Response:** 204 No Content on success

### Delete Discount
```
DELETE /api/discounts/{id}
```

**Response:** 204 No Content on success

## How Discounts are Applied

When a user scans barcodes to make a purchase:

1. The system calculates the total purchase amount
2. All active discounts are evaluated:
   - Must be within valid date range (ValidFrom ≤ now ≤ ValidTo)
   - Must be flagged as active (IsActive = true)
   - Purchase total must meet minimum requirement
3. The **best discount** (highest discount amount) is selected and applied
4. Only one discount is applied per purchase

### Discount Calculation Examples

**Example 1: Fixed Amount Discount**
- Purchase total: 8.00€
- Discount: 1.00€ off (FixedAmount)
- Result: 7.00€ (saved 1.00€)

**Example 2: Percentage Discount**
- Purchase total: 10.00€
- Discount: 15% off (Percentage)
- Result: 8.50€ (saved 1.50€)

**Example 3: Multiple Applicable Discounts**
- Purchase total: 12.00€
- Discount A: 1.00€ off (saves 1.00€)
- Discount B: 10% off (saves 1.20€)
- Result: 10.80€ (Discount B is applied as it provides higher savings)

**Example 4: Discount Exceeds Purchase**
- Purchase total: 2.00€
- Discount: 5.00€ off (FixedAmount)
- Result: 0.00€ (discount capped at purchase amount, cannot go negative)

## Scanner Response Format

When scanning barcodes, the response includes discount information:

```json
{
  "success": true,
  "userId": 1,
  "username": "john.doe",
  "totalAmount": 10.00,
  "applicableDiscounts": [
    {
      "discountId": 1,
      "name": "10% Off",
      "type": "Percentage",
      "value": 10,
      "discountAmount": 1.00
    }
  ],
  "discountedAmount": 9.00,
  "balance": -25.00,
  ...
}
```

**Key Fields:**
- `totalAmount`: Original purchase total before discount
- `applicableDiscounts`: Array of applied discounts (currently max 1)
- `discountedAmount`: Final amount after discount applied

## Business Rules

1. **One Discount Per Purchase**: Only the best (highest saving) discount is applied
2. **Admin-Only Management**: Only users with Admin role can create/edit/delete discounts
3. **Date Validation**: System uses UTC timestamps; discounts are only active within their date range
4. **Minimum Purchase**: User must meet minimum purchase threshold before discount applies
5. **Fixed Amount Cap**: Fixed amount discounts cannot exceed the purchase total
6. **Percentage Limits**: Percentage values must be between 0-100

## Future Enhancements

Potential features for future implementation:
- **Stackable Discounts**: Allow multiple discounts to be applied together
- **User-Specific Discounts**: Target discounts to specific users or groups
- **Product-Specific Discounts**: Apply discounts only to certain products
- **Usage Limits**: Limit number of times a discount can be used (per user or total)
- **Discount Codes**: Require users to enter a code to activate discount
- **Tiered Discounts**: Increase discount amount based on purchase tiers
