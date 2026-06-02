# Split math test cases

These cases specify expected bill-splitting behavior before the production `SplitCalculator` exists. Money is `decimal` and all member shares MUST be rounded to 2 currency decimals with `Σ(member shares) == tab total` exactly.

## Shared assumptions

- Each tab has one currency only; currency is derived from the tab location. Do not split or convert mixed-currency item lists inside one tab.
- `Members` are the tab participants. Member ids are strings in these cases.
- `TabItem` fields used here: `Name`, `UnitPrice`, `Quantity`, `OrderedByUserId`.
- Item total is `UnitPrice * Quantity`, rounded to 2 decimals before allocation.
- `OrderedByUserId == null` means the item is shared equally by all members.
- Recommended deterministic remainder rule: sort member ids ordinally ascending; after flooring each equal allocation to cents, distribute one extra cent to the first `K` sorted members, where `K` is the number of leftover cents. This applies to equal tab splits and to each shared item split.

## Equal split cases

### E01 — Evenly divisible total

- Members: `u1`, `u2`
- Tab total: `$10.00`
- Expected: `u1=$5.00`, `u2=$5.00`

### E02 — Non-divisible total, one remainder cent

- Members: `u1`, `u2`, `u3`
- Tab total: `$10.00`
- Sorted ids: `u1`, `u2`, `u3`
- Base share: `$3.33`, remainder: `$0.01`
- Expected: `u1=$3.34`, `u2=$3.33`, `u3=$3.33`

### E03 — Non-divisible total, two remainder cents

- Members: `u1`, `u2`, `u3`
- Tab total: `$10.01`
- Base share: `$3.33`, remainder: `$0.02`
- Expected: `u1=$3.34`, `u2=$3.34`, `u3=$3.33`

### E04 — Deterministic id ordering for remainder

- Members input order: `u3`, `u1`, `u2`
- Tab total: `$10.00`
- Remainder is assigned by sorted id, not input order
- Expected: `u1=$3.34`, `u2=$3.33`, `u3=$3.33`

### E05 — Empty tab with members

- Members: `u1`, `u2`, `u3`
- Tab total: `$0.00`
- Expected: `u1=$0.00`, `u2=$0.00`, `u3=$0.00`

### E06 — Single member pays full amount

- Members: `u1`
- Tab total: `$12.34`
- Expected: `u1=$12.34`

### E07 — Zero-price total

- Members: `u1`, `u2`
- Tab total: `$0.00`
- Expected: `u1=$0.00`, `u2=$0.00`

### E08 — Currency rounding to 2 decimals

- Members: `u1`, `u2`
- Tab total from item math before allocation: `$10.005`
- Rounded tab total: `$10.01`
- Expected: `u1=$5.01`, `u2=$5.00`

### E09 — Exact sum invariant on large total

- Members: `u1`, `u2`, `u3`, `u4`, `u5`, `u6`, `u7`
- Tab total: `$999999.99`
- Base share: `$142857.14`, remainder: `$0.01`
- Expected: `u1=$142857.15`, `u2=$142857.14`, `u3=$142857.14`, `u4=$142857.14`, `u5=$142857.14`, `u6=$142857.14`, `u7=$142857.14`

## Itemized split cases

### I01 — Each item charged to ordering member

- Members: `u1`, `u2`
- Items:
  - Beer: `$6.00 x 1`, `OrderedByUserId=u1`
  - Wine: `$8.00 x 1`, `OrderedByUserId=u2`
- Expected: `u1=$6.00`, `u2=$8.00`

### I02 — Shared item splits evenly

- Members: `u1`, `u2`
- Items:
  - Fries: `$6.00 x 1`, `OrderedByUserId=null`
- Expected: `u1=$3.00`, `u2=$3.00`

### I03 — Shared item with one remainder cent

- Members: `u1`, `u2`, `u3`
- Items:
  - Nachos: `$10.00 x 1`, `OrderedByUserId=null`
- Expected: `u1=$3.34`, `u2=$3.33`, `u3=$3.33`

### I04 — Mixed owned and shared items

- Members: `u1`, `u2`, `u3`
- Items:
  - Burger: `$12.00 x 1`, `OrderedByUserId=u2`
  - Wings: `$9.00 x 1`, `OrderedByUserId=null`
  - Soda: `$3.00 x 1`, `OrderedByUserId=u1`
- Shared Wings allocation: `u1=$3.00`, `u2=$3.00`, `u3=$3.00`
- Expected: `u1=$6.00`, `u2=$15.00`, `u3=$3.00`

### I05 — Member who ordered nothing still pays shared items

- Members: `u1`, `u2`, `u3`
- Items:
  - Pasta: `$15.00 x 1`, `OrderedByUserId=u1`
  - Bread: `$3.00 x 1`, `OrderedByUserId=null`
- Expected: `u1=$16.00`, `u2=$1.00`, `u3=$1.00`

### I06 — Member who ordered nothing and no shared items pays zero

- Members: `u1`, `u2`, `u3`
- Items:
  - Cocktail: `$7.50 x 1`, `OrderedByUserId=u1`
  - Mocktail: `$5.00 x 1`, `OrderedByUserId=u2`
- Expected: `u1=$7.50`, `u2=$5.00`, `u3=$0.00`

### I07 — Zero-price item does not alter shares

- Members: `u1`, `u2`
- Items:
  - Promo Snack: `$0.00 x 1`, `OrderedByUserId=u1`
  - Pizza: `$10.00 x 1`, `OrderedByUserId=null`
- Expected: `u1=$5.00`, `u2=$5.00`

### I08 — Large quantity item

- Members: `u1`, `u2`
- Items:
  - Oysters: `$1.25 x 1000`, `OrderedByUserId=u2`
- Item total: `$1250.00`
- Expected: `u1=$0.00`, `u2=$1250.00`

### I09 — Quantity with shared allocation and remainder

- Members: `u1`, `u2`, `u3`
- Items:
  - Sliders: `$2.50 x 5`, `OrderedByUserId=null`
- Item total: `$12.50`, base share `$4.16`, remainder `$0.02`
- Expected: `u1=$4.17`, `u2=$4.17`, `u3=$4.16`

### I10 — Multiple shared items distribute remainder per item

- Members: `u1`, `u2`, `u3`
- Items:
  - Chips: `$1.00 x 1`, `OrderedByUserId=null`
  - Salsa: `$1.00 x 1`, `OrderedByUserId=null`
- Each `$1.00` shared item allocates `u1=$0.34`, `u2=$0.33`, `u3=$0.33`
- Expected: `u1=$0.68`, `u2=$0.66`, `u3=$0.66`

### I11 — Fractional-cent item total rounds before allocation

- Members: `u1`, `u2`
- Items:
  - Discounted Tapas: `$3.335 x 3`, `OrderedByUserId=null`
- Raw item total: `$10.005`; rounded item total: `$10.01`
- Expected: `u1=$5.01`, `u2=$5.00`

### I12 — Empty item list with members

- Members: `u1`, `u2`
- Items: none
- Expected: `u1=$0.00`, `u2=$0.00`

### I13 — Single member receives full itemized and shared totals

- Members: `u1`
- Items:
  - Beer: `$6.00 x 1`, `OrderedByUserId=u1`
  - Shared Pretzel: `$4.50 x 1`, `OrderedByUserId=null`
- Expected: `u1=$10.50`

### I14 — Exact sum invariant on mixed tab

- Members: `u1`, `u2`, `u3`, `u4`
- Items:
  - Steak: `$31.99 x 1`, `OrderedByUserId=u4`
  - Apps: `$10.00 x 1`, `OrderedByUserId=null`
  - Dessert: `$7.25 x 2`, `OrderedByUserId=u2`
- Shared Apps allocation: `u1=$2.50`, `u2=$2.50`, `u3=$2.50`, `u4=$2.50`
- Expected: `u1=$2.50`, `u2=$17.00`, `u3=$2.50`, `u4=$34.49`
- Tab total: `$56.49`; share sum MUST equal `$56.49` exactly.

## Negative/validation-oriented cases to encode once API shape exists

- No members with non-empty tab: should reject split calculation rather than divide by zero.
- `OrderedByUserId` not present in members: should reject as invalid tab data.
- Negative item price or quantity: should reject unless refunds/discounts are explicitly introduced.
- Multi-currency item list in one tab: should reject; tabs are single-currency per location.
