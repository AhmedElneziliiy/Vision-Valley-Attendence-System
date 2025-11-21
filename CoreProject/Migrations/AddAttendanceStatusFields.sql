-- Migration: Add Status and MinutesLate columns to Attendances table
-- Date: 2025-11-10
-- Description: Adds timezone-aware timetable status tracking

-- Add Status column (0=Absent, 1=OnTime, 2=Late, 3=VeryLate, 4=Early)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attendances]') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[Attendances]
    ADD [Status] int NOT NULL DEFAULT 0;

    PRINT 'Column [Status] added to Attendances table';
END
ELSE
BEGIN
    PRINT 'Column [Status] already exists in Attendances table';
END
GO

-- Add MinutesLate column (nullable int - positive=late, negative=early, null=on time)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attendances]') AND name = 'MinutesLate')
BEGIN
    ALTER TABLE [dbo].[Attendances]
    ADD [MinutesLate] int NULL;

    PRINT 'Column [MinutesLate] added to Attendances table';
END
ELSE
BEGIN
    PRINT 'Column [MinutesLate] already exists in Attendances table';
END
GO

-- Verify columns were added
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Attendances]')
AND c.name IN ('Status', 'MinutesLate')
ORDER BY c.name;
GO

PRINT 'Migration completed successfully!';
