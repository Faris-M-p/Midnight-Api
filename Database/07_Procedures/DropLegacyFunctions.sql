-- Drop leftover Pro* functions/procedures so new signatures can be created.
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT p.oid::regprocedure AS sig, p.prokind
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'public'
          AND p.proname LIKE 'Pro%'
          AND p.prokind IN ('f', 'p')
    LOOP
        IF r.prokind = 'f' THEN
            EXECUTE format('DROP FUNCTION IF EXISTS %s CASCADE', r.sig);
        ELSE
            EXECUTE format('DROP PROCEDURE IF EXISTS %s CASCADE', r.sig);
        END IF;
    END LOOP;
END $$;
