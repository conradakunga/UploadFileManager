-- Modify the table name for use
DECLARE @TableName NVARCHAR(128) = 'Files';
DECLARE @SchemaName NVARCHAR(128) = 'dbo';
DECLARE @FullTableName NVARCHAR(256) = QUOTENAME(@SchemaName) + '.' + QUOTENAME(@TableName);
DECLARE @Sql NVARCHAR(MAX);

-- Check if table exists
IF NOT EXISTS (SELECT *
               FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_NAME = @TableName
                 AND TABLE_SCHEMA = @SchemaName)
    BEGIN
        SET @Sql = '
    CREATE TABLE ' + @FullTableName + ' (
        FileID               UNIQUEIDENTIFIER PRIMARY KEY NOT NULL,
        Name                 NVARCHAR(500)                NOT NULL,
        Extension            NVARCHAR(10)                 NOT NULL,
        DateUploaded         DATETIMEOFFSET               NOT NULL,
        OriginalSize         INT                          NOT NULL,
        PersistedSize        INT                          NOT NULL,
        CompressionAlgorithm TINYINT                      NOT NULL,
        EncryptionAlgorithm  TINYINT                      NOT NULL,
        Hash                 BINARY(32)                   NOT NULL,
        Data                 VARBINARY(MAX)
    );';
        EXEC sp_executesql @Sql;
    END;

-- Check if index exists
IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = 'IX_' + @TableName + '_Metadata'
                 AND object_id = OBJECT_ID(@FullTableName))
    BEGIN
        SET @Sql = '
    CREATE NONCLUSTERED INDEX' + ' IX_' + @TableName + '_Metadata
    ON ' + @FullTableName + ' (FileID)
    INCLUDE (
             Name,
             Extension,
             DateUploaded,
             OriginalSize,
             PersistedSize,
             CompressionAlgorithm,
             EncryptionAlgorithm,
             Hash
        );';
        EXEC sp_executesql @Sql;
    END;