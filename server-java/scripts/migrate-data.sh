#!/usr/bin/env bash
# migrate-data.sh
# Imports data from a .NET EF Core-generated SQLite database into a fresh
# Java/Flyway-managed SQLite database.
#
# The .NET side uses PascalCase table/column names (e.g. Customers.ZipCode)
# while the Java side uses snake_case (e.g. customers.zip_code) as produced
# by Spring Boot's default SpringPhysicalNamingStrategy.
#
# Usage:
#   ./migrate-data.sh <source_dotnet.db> <target_java.db>
#
# Prerequisites:
#   - sqlite3 CLI installed
#   - The target database must already exist with the Flyway-managed schema
#     (run the Java Spring Boot app once, or use Flyway CLI to migrate).

set -euo pipefail

SOURCE_DB="${1:?Usage: $0 <source_dotnet.db> <target_java.db>}"
TARGET_DB="${2:?Usage: $0 <source_dotnet.db> <target_java.db>}"

if [ ! -f "$SOURCE_DB" ]; then
  echo "ERROR: Source database not found: $SOURCE_DB"
  exit 1
fi

if [ ! -f "$TARGET_DB" ]; then
  echo "ERROR: Target database not found: $TARGET_DB"
  echo "Hint: Start the Java app once to let Flyway create the schema, then re-run this script."
  exit 1
fi

echo "=== Data Migration: .NET SQLite -> Java SQLite ==="
echo "Source : $SOURCE_DB"
echo "Target : $TARGET_DB"
echo ""

# migrate_table_mapped <source_table> <target_table> <select_expr> <target_columns>
#   source_table  - table name in the .NET database (PascalCase)
#   target_table  - table name in the Java database (snake_case)
#   select_expr   - SELECT column list with aliases mapping PascalCase -> snake_case
#   target_columns - comma-separated target column names for the INSERT
migrate_table_mapped() {
  local src_table="$1"
  local tgt_table="$2"
  local select_expr="$3"
  local tgt_columns="$4"

  local count
  count=$(sqlite3 "$SOURCE_DB" "SELECT COUNT(*) FROM \"$src_table\";" 2>/dev/null || echo "0")

  if [ "$count" = "0" ]; then
    echo "  $src_table -> $tgt_table: no rows to migrate."
    return
  fi

  echo "  $src_table -> $tgt_table: migrating $count rows ..."

  # Attach source db to target and INSERT directly
  sqlite3 "$TARGET_DB" <<SQL
ATTACH DATABASE '$SOURCE_DB' AS src;
INSERT OR IGNORE INTO "$tgt_table" ($tgt_columns)
SELECT $select_expr FROM src."$src_table";
DETACH DATABASE src;
SQL

  local migrated
  migrated=$(sqlite3 "$TARGET_DB" "SELECT COUNT(*) FROM \"$tgt_table\";")
  echo "  $tgt_table: $migrated rows in target after migration."
}

echo "--- Migrating Customers ---"
migrate_table_mapped "Customers" "customers" \
  "Id, Name, Email, Phone, Address, City, State, ZipCode" \
  "id, name, email, phone, address, city, state, zip_code"

echo "--- Migrating Products ---"
migrate_table_mapped "Products" "products" \
  "Id, Name, Description, Category, Price, Sku" \
  "id, name, description, category, price, sku"

echo "--- Migrating Orders ---"
migrate_table_mapped "Orders" "orders" \
  "Id, CustomerId, OrderDate, Status, TotalAmount, ShippingAddress" \
  "id, customer_id, order_date, status, total_amount, shipping_address"

echo "--- Migrating OrderItems ---"
migrate_table_mapped "OrderItems" "order_items" \
  "Id, OrderId, ProductId, Quantity, UnitPrice" \
  "id, order_id, product_id, quantity, unit_price"

echo "--- Migrating InventoryItems ---"
migrate_table_mapped "InventoryItems" "inventory_items" \
  "Id, ProductId, QuantityOnHand, ReorderLevel, WarehouseLocation, LastRestocked" \
  "id, product_id, quantity_on_hand, reorder_level, warehouse_location, last_restocked"

echo ""
echo "=== Migration complete ==="
