-- Drop leftover FUNCTION-based Pro* objects so they can be created as PROCEDURE.
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT p.oid::regprocedure AS sig
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'public'
          AND p.proname LIKE 'Pro%'
          AND p.prokind = 'f'
    LOOP
        EXECUTE format('DROP FUNCTION IF EXISTS %s CASCADE', r.sig);
    END LOOP;
END $$;
