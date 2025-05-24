-- Check if IsPhoneVerified column exists
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'AspNetUsers'
    AND column_name = 'IsPhoneVerified'
  ) THEN
    -- Add the IsPhoneVerified column with default value false
    ALTER TABLE "AspNetUsers" ADD COLUMN "IsPhoneVerified" boolean NOT NULL DEFAULT false;
  END IF;
END $$;

-- Check if PhoneVerificationCode column exists
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'AspNetUsers'
    AND column_name = 'PhoneVerificationCode'
  ) THEN
    -- Add the PhoneVerificationCode column
    ALTER TABLE "AspNetUsers" ADD COLUMN "PhoneVerificationCode" text;
  END IF;
END $$;

-- Check if PhoneVerificationExpiry column exists
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'AspNetUsers'
    AND column_name = 'PhoneVerificationExpiry'
  ) THEN
    -- Add the PhoneVerificationExpiry column
    ALTER TABLE "AspNetUsers" ADD COLUMN "PhoneVerificationExpiry" timestamp with time zone;
  END IF;
END $$; 