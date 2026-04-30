-- Migration: AddUniversityProperties
-- Run this script directly on your database if dotnet ef database update doesn't work

-- Step 1: Add new columns
ALTER TABLE Universities ADD Description nvarchar(max) NULL;
ALTER TABLE Universities ADD Website nvarchar(max) NULL;
ALTER TABLE Universities ADD Email nvarchar(max) NULL;
ALTER TABLE Universities ADD Phone nvarchar(max) NULL;
ALTER TABLE Universities ADD GPARequirement decimal(18,2) NOT NULL DEFAULT 0;

-- Step 2: Convert Rating from float to decimal
-- First, add a temporary column
ALTER TABLE Universities ADD RatingNew decimal(18,2) NULL;

-- Copy and convert data
UPDATE Universities SET RatingNew = CAST(Rating AS decimal(18,2));

-- Drop the old column
ALTER TABLE Universities DROP COLUMN Rating;

-- Rename the new column
EXEC sp_rename 'Universities.RatingNew', 'Rating', 'COLUMN';

-- Make it NOT NULL
ALTER TABLE Universities ALTER COLUMN Rating decimal(18,2) NOT NULL;

-- Step 3: Set GPARequirement from AverageGpa
UPDATE Universities 
SET GPARequirement = CAST(AverageGpa AS decimal(18,2))
WHERE GPARequirement = 0;

-- Step 4: Make ImageUrl nullable (if it was required before)
ALTER TABLE Universities ALTER COLUMN ImageUrl nvarchar(max) NULL;

-- Verify the changes
SELECT TOP 5 Id, Name, Rating, GPARequirement, Description, Website, Email, Phone 
FROM Universities;

