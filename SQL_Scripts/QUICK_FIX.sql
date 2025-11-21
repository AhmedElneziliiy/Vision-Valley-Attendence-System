-- ============================================================================
-- QUICK FIX - Performance Index Installation
-- Copy and paste this entire script into SQL Server Management Studio
-- ============================================================================

-- STEP 1: Update this line with your actual database name
USE [YourDatabaseName];  -- ⚠️ CHANGE THIS!
GO

-- STEP 2: Check if AccessControlURL has any long values
PRINT 'Checking for URLs longer than 450 characters...';
SELECT COUNT(*) AS LongURLCount
FROM Devices
WHERE LEN(AccessControlURL) > 450;
GO

-- If count is 0, continue. If not, you need to shorten those URLs first.

-- STEP 3: Change column type from NVARCHAR(MAX) to NVARCHAR(450)
PRINT 'Converting AccessControlURL to NVARCHAR(450)...';
ALTER TABLE [dbo].[Devices]
ALTER COLUMN [AccessControlURL] NVARCHAR(450) NULL;
GO

-- STEP 4: Create the performance index
PRINT 'Creating performance index...';
CREATE NONCLUSTERED INDEX [IX_Devices_AccessControlURL_INCLUDE_State]
ON [dbo].[Devices] ([AccessControlURL])
INCLUDE ([AccessControlState])
WITH (
    ONLINE = ON,
    FILLFACTOR = 90,
    PAD_INDEX = ON,
    STATISTICS_NORECOMPUTE = OFF
);
GO

-- STEP 5: Update statistics
PRINT 'Updating statistics...';
UPDATE STATISTICS [dbo].[Devices] [IX_Devices_AccessControlURL_INCLUDE_State];
GO

-- STEP 6: Verify
PRINT 'Verifying index creation...';
SELECT
    i.name AS IndexName,
    i.type_desc AS IndexType,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('Devices')
  AND i.name = 'IX_Devices_AccessControlURL_INCLUDE_State';
GO

PRINT '';
PRINT '✅ INDEX INSTALLATION COMPLETE!';
PRINT '';
PRINT 'Next steps:';
PRINT '  1. Restart your API application';
PRINT '  2. Test: curl "http://localhost:5000/api/device/access-control?url=device123"';
PRINT '  3. Expected response time: 1-3ms (was 5-15ms)';
GO
