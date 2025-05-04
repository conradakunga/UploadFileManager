DO
$$
    DECLARE
        -- Change this to your desired table name
        table_name text := 'files';
    BEGIN
        EXECUTE format('
        CREATE TABLE IF NOT EXISTS public.%I (
            fileid               UUID PRIMARY KEY NOT NULL,
            name                 VARCHAR(500)     NOT NULL,
            extension            VARCHAR(10)      NOT NULL,
            dateuploaded         TIMESTAMPTZ      NOT NULL,
            originalsize         INT              NOT NULL,
            persistedsize        INT              NOT NULL,
            compressionalgorithm SMALLINT         NOT NULL,
            encryptionalgorithm  SMALLINT         NOT NULL,
            hash                 BYTEA            NOT NULL,
            data                 BYTEA            NOT NULL,
            loid                 OID              NULL 
        );
    ', table_name);

        EXECUTE format('
                   CREATE INDEX IF NOT EXISTS ix_%I_metadata
    ON public.%I (fileid)
    INCLUDE (name, extension, dateuploaded, originalsize, persistedsize, compressionalgorithm, encryptionalgorithm, hash);
                   ', table_name, table_name);

        EXECUTE format('
                   CREATE INDEX IF NOT EXISTS ix_%I_large_object_metadata
    ON public.%I (fileid)
    INCLUDE (loid);
                   ', table_name, table_name);
    END
$$;