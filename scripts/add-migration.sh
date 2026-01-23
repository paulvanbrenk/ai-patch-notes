#!/bin/bash
# Add a new EF Core migration for both SQLite and SQL Server
#
# Usage: ./scripts/add-migration.sh MigrationName
#
# Example: ./scripts/add-migration.sh AddUserPreferences

set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <MigrationName>"
    echo "Example: $0 AddUserPreferences"
    exit 1
fi

MIGRATION_NAME="$1"

echo "Creating SQLite migration..."
dotnet ef migrations add "$MIGRATION_NAME" \
    --context SqliteContext \
    --output-dir Migrations/Sqlite \
    --project PatchNotes.Data \
    --startup-project PatchNotes.Api

echo ""
echo "Creating SQL Server migration..."

if [ -z "$ConnectionStrings__PatchNotes" ]; then
    echo "Warning: ConnectionStrings__PatchNotes not set."
    echo "Set it to generate SQL Server migration:"
    echo "  export ConnectionStrings__PatchNotes='Server=...;Database=...;User Id=...;Password=...;'"
    exit 1
fi

dotnet ef migrations add "$MIGRATION_NAME" \
    --context SqlServerContext \
    --output-dir Migrations/SqlServer \
    --project PatchNotes.Data \
    --startup-project PatchNotes.Api

echo ""
echo "Done! Created migrations:"
echo "  - Migrations/Sqlite/*_${MIGRATION_NAME}.cs"
echo "  - Migrations/SqlServer/*_${MIGRATION_NAME}.cs"
