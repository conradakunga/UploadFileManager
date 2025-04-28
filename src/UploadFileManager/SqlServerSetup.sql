Use FileStore;

GO

-- TABLE

-- Create table if it doesn't exist
IF NOT EXISTS (SELECT *
               FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_NAME = 'Files'
                 AND TABLE_SCHEMA = 'dbo')
    BEGIN
        CREATE TABLE dbo.Files
        (
            FileID               UNIQUEIDENTIFIER PRIMARY KEY NOT NULL,
            Name                 NVARCHAR(500)                NOT NULL,
            Extension            NVARCHAR(10)                 NOT NULL,
            DateUploaded         DATETIME2                    NOT NULL,
            OriginalSize         INT                          NOT NULL,
            PersistedSize        INT                          NOT NULL,
            CompressionAlgorithm TINYINT                      NOT NULL,
            EncryptionAlgorithm  TINYINT                      NOT NULL,
            Hash                 BINARY(32)                   NOT NULL,
            Data                 VARBINARY(MAX)
        );
    END
GO

-- INDEXES

IF NOT EXISTS (SELECT 1
               FROM sys.indexes
               WHERE name = 'IX_Files_Metadata'
                 AND object_id = OBJECT_ID('dbo.Files'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_Files_Metadata
            ON dbo.Files (FileID)
            INCLUDE (
                     Name,
                     Extension,
                     DateUploaded,
                     OriginalSize,
                     PersistedSize,
                     CompressionAlgorithm,
                     EncryptionAlgorithm,
                     Hash
                );
    END
GO