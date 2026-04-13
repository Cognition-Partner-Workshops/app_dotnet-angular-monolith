#!/usr/bin/env bash
# migrate-data.sh
# Imports data from a .NET-generated SQLite database into a fresh
# Java/Flyway-managed SQLite database.
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

# Helper: export rows from source and import into target.
# The .NET EF Core model uses PascalCase column names that map to the same
# column names used by the Java/Hibernate entities. Both schemas share the
# same table and column names so a straight INSERT ... SELECT works.

migrate_table() {
  local table="$1"
  local columns="$2"

  local count
  count=$(sqlite3 "$SOURCE_DB" "SELECT COUNT(*) FROM $table;" 2>/dev/null || echo "0")

  if [ "$count" = "0" ]; then
    echo "  $table: no rows to migrate."
    return
  fi

  echo "  $table: migrating $count rows ..."

  # Dump INSERT statements from the source
  sqlite3 "$SOURCE_DB" <<SQL | sqlite3 "$TARGET_DB"
.mode insert $table
SELECT $columns FROM $table;
SQL

  local migrated
  migrated=$(sqlite3 "$TARGET_DB" "SELECT COUNT(*) FROM $table;")
  echo "  $table: $migrated rows in target after migration."
}

echo "--- Migrating Customers ---"
migrate_table "Customers" "id, name, email, phone, address, city, state, zipCode"

echo "--- Migrating Products ---"
migrate_table "Products" "id, name, description, category, price, sku"

echo "--- Migrating Orders ---"
migrate_table "Orders" "id, customer_id, orderDate, status, totalAmount, shippingAddress"

echo "--- Migrating OrderItems ---"
migrate_table "OrderItems" "id, order_id, product_id, quantity, unitPrice"

echo "--- Migrating InventoryItems ---"
migrate_table "InventoryItems" "id, product_id, quantityOnHand, reorderLevel, warehouseLocation, lastRestocked"

echo ""
echo "=== Migration complete ==="
