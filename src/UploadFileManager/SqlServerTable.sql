-- Database setup

CREATE DATABASE FileStore;

GO

Use FileStore;

-- TABLE

create table Files
(
    FileID               uniqueidentifier primary key not null,
    Name                 nvarchar(500)                not null,
    Extension            nvarchar(10)                 not null,
    DateUploaded         DATETIME2                    not null,
    OriginalSize         int                          not null,
    PersistedSize        int                          not null,
    CompressionAlgorithm tinyint                      not null,
    EncryptionAlgorithm  tinyint                      not null,
    Hash                 binary(32)               not null,
    Data                 varbinary(MAX)
)

-- INDEXES

CREATE NONCLUSTERED INDEX IX_Files_Metadata
    ON Files (FileID)
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