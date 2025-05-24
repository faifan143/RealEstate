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

# Check if the AspNetUsers table has the IsPhoneVerified column, if not add it
PGPASSWORD=123 psql -h postgres -U postgres -d RealEstateDb -c "
DO \$\$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'AspNetUsers'
    AND column_name = 'IsPhoneVerified'
  ) THEN
    ALTER TABLE \"AspNetUsers\" ADD COLUMN \"IsPhoneVerified\" boolean NOT NULL DEFAULT false;
  END IF;
END \$\$;"

# Check if PhoneVerificationCode column exists
PGPASSWORD=123 psql -h postgres -U postgres -d RealEstateDb -c "
DO \$\$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'AspNetUsers'
    AND column_name = 'PhoneVerificationCode'
  ) THEN
    ALTER TABLE \"AspNetUsers\" ADD COLUMN \"PhoneVerificationCode\" text;
  END IF;
END \$\$;"

# Check if PhoneVerificationExpiry column exists
PGPASSWORD=123 psql -h postgres -U postgres -d RealEstateDb -c "
DO \$\$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'AspNetUsers'
    AND column_name = 'PhoneVerificationExpiry'
  ) THEN
    ALTER TABLE \"AspNetUsers\" ADD COLUMN \"PhoneVerificationExpiry\" timestamp with time zone;
  END IF;
END \$\$;"

echo "Database migration completed successfully" 