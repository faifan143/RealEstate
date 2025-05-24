-- Check if IsForRent column exists, if not add it
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'Properties'
    AND column_name = 'IsForRent'
  ) THEN
    ALTER TABLE "Properties" ADD COLUMN "IsForRent" boolean NOT NULL DEFAULT false;
  END IF;
END $$;

-- Check if IsForSale column exists, if not add it
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'Properties'
    AND column_name = 'IsForSale'
  ) THEN
    ALTER TABLE "Properties" ADD COLUMN "IsForSale" boolean NOT NULL DEFAULT true;
  END IF;
END $$; 