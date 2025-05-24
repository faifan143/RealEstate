-- Script to add missing columns to the Properties table

-- Check if RentalDurationMonths column exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Properties' AND column_name = 'RentalDurationMonths'
    ) THEN
        ALTER TABLE "Properties" ADD COLUMN "RentalDurationMonths" integer NULL;
    END IF;
END $$;

-- Check if RentalEndDate column exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Properties' AND column_name = 'RentalEndDate'
    ) THEN
        ALTER TABLE "Properties" ADD COLUMN "RentalEndDate" timestamp with time zone NULL;
    END IF;
END $$;

-- Check if IsForRent column exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'Properties'
        AND column_name = 'IsForRent'
    ) THEN
        ALTER TABLE "Properties" ADD COLUMN "IsForRent" boolean NOT NULL DEFAULT false;
        RAISE NOTICE 'IsForRent column added';
    ELSE
        RAISE NOTICE 'IsForRent column already exists';
    END IF;
END $$;

-- Check if IsForSale column exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'Properties'
        AND column_name = 'IsForSale'
    ) THEN
        ALTER TABLE "Properties" ADD COLUMN "IsForSale" boolean NOT NULL DEFAULT true;
        RAISE NOTICE 'IsForSale column added';
    ELSE
        RAISE NOTICE 'IsForSale column already exists';
    END IF;
END $$;

-- Update IsForRent and IsForSale based on RentalDurationMonths and RentalEndDate
UPDATE "Properties" 
SET "IsForRent" = CASE 
    WHEN "RentalDurationMonths" IS NOT NULL OR "RentalEndDate" IS NOT NULL THEN true 
    ELSE false 
END,
"IsForSale" = CASE 
    WHEN "RentalDurationMonths" IS NULL AND "RentalEndDate" IS NULL THEN true 
    ELSE false 
END;

-- Check if UserUploadedImages table exists and create it if it doesn't
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_name = 'UserUploadedImages'
    ) THEN
        CREATE TABLE "UserUploadedImages" (
            "Id" uuid NOT NULL,
            "UserId" text NOT NULL,
            "PropertyId" uuid NULL,
            "ImageUrl" text NOT NULL,
            "FileName" text NOT NULL,
            "Description" text NOT NULL,
            "UploadedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
            "IsApproved" boolean NOT NULL DEFAULT false,
            CONSTRAINT "PK_UserUploadedImages" PRIMARY KEY ("Id"),
            CONSTRAINT "FK_UserUploadedImages_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE,
            CONSTRAINT "FK_UserUploadedImages_Properties_PropertyId" FOREIGN KEY ("PropertyId") REFERENCES "Properties" ("Id") ON DELETE SET NULL
        );
        
        -- Create index for faster lookups
        CREATE INDEX "IX_UserUploadedImages_UserId" ON "UserUploadedImages" ("UserId");
        CREATE INDEX "IX_UserUploadedImages_PropertyId" ON "UserUploadedImages" ("PropertyId");
        
        RAISE NOTICE 'UserUploadedImages table created';
    ELSE
        RAISE NOTICE 'UserUploadedImages table already exists';
    END IF;
END $$;

-- Check if IsDirectBooking column exists in Bookings table
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Bookings' AND column_name = 'IsDirectBooking'
    ) THEN
        ALTER TABLE "Bookings" ADD COLUMN "IsDirectBooking" boolean NOT NULL DEFAULT true;
        RAISE NOTICE 'IsDirectBooking column added to Bookings table';
    ELSE
        RAISE NOTICE 'IsDirectBooking column already exists in Bookings table';
    END IF;
END $$;

-- Check if VisitDateTime column exists in Bookings table
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'Bookings' AND column_name = 'VisitDateTime'
    ) THEN
        ALTER TABLE "Bookings" ADD COLUMN "VisitDateTime" timestamp with time zone NULL;
        RAISE NOTICE 'VisitDateTime column added to Bookings table';
    ELSE
        RAISE NOTICE 'VisitDateTime column already exists in Bookings table';
    END IF;
END $$;

-- Add record to EF migration history if needed
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM "__EFMigrationsHistory"
        WHERE "MigrationId" = '20250524_AddMissingColumns'
    ) THEN
        INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20250524_AddMissingColumns', '8.0.0');
    END IF;
END $$; 