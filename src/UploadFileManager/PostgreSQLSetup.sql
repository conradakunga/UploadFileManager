-- Table creation
CREATE TABLE IF NOT EXISTS public.files
(
    fileid               UUID PRIMARY KEY NOT NULL,
    name                 VARCHAR(500)     NOT NULL,
    extension            VARCHAR(10)      NOT NULL,
    dateuploaded         TIMESTAMPTZ      NOT NULL,
    originalsize         INT              NOT NULL,
    persistedsize        INT              NOT NULL,
    compressionalgorithm SMALLINT         NOT NULL,
    encryptionalgorithm  SMALLINT         NOT NULL,
    hash                 BYTEA            NOT NULL,
    data                 BYTEA
);

-- Index creation
CREATE INDEX IF NOT EXISTS ix_files_metadata
    ON public.files (fileid)
    INCLUDE (name, extension, dateuploaded, originalsize, persistedsize, compressionalgorithm, encryptionalgorithm, hash);