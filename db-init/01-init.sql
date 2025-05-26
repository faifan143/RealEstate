-- Initialize database with required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create a function to check if a column exists
CREATE OR REPLACE FUNCTION column_exists(tbl text, col text) RETURNS boolean AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = tbl
        AND column_name = col
    );
END;
$$ LANGUAGE plpgsql;

-- Create a function to add a column if it doesn't exist
CREATE OR REPLACE FUNCTION add_column_if_not_exists(
    tbl text, col text, datatype text
) RETURNS void AS $$
BEGIN
    IF NOT column_exists(tbl, col) THEN
        EXECUTE format('ALTER TABLE %I ADD COLUMN %I %s', tbl, col, datatype);
    END IF;
END;
$$ LANGUAGE plpgsql; 