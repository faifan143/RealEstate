#!/bin/bash
set -e

# Wait for PostgreSQL to be ready
until PGPASSWORD=123 psql -h postgres -U postgres -d postgres -c '\q'; do
  >&2 echo "PostgreSQL is unavailable - sleeping"
  sleep 1
done

>&2 echo "PostgreSQL is up - executing migrations"

# Create the database if it doesn't exist
PGPASSWORD=123 psql -h postgres -U postgres -tc "SELECT 1 FROM pg_database WHERE datname = 'RealEstateDb'" | grep -q 1 || PGPASSWORD=123 psql -h postgres -U postgres -c "CREATE DATABASE \"RealEstateDb\""

# Run the migration tool
cd /app && dotnet ef database update

# Update all existing users to have PhoneNumberConfirmed = true
PGPASSWORD=123 psql -h postgres -U postgres -d RealEstateDb -c "
UPDATE \"AspNetUsers\" SET \"PhoneNumberConfirmed\" = true WHERE \"PhoneNumberConfirmed\" = false;
"

echo "Database migration completed successfully" 