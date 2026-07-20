-- Run as postgres/superuser on clientdb:
--   sudo -u postgres psql -d clientdb -f fix-presence-trigger.sql
--
-- 1) Writers that omit last_seen_at still refresh it (anti-ghost).
-- 2) Session timer survives only SHORT offline blips (~3 min).
--    Lobby hops while still IN GAME keep the same session (not a logout).

CREATE OR REPLACE FUNCTION public.fusion_sync_status_timestamps()
RETURNS trigger
LANGUAGE plpgsql
SET search_path TO 'pg_catalog', 'public'
AS $function$
DECLARE
    changed_at TIMESTAMPTZ := clock_timestamp();
    new_status TEXT;
    old_status TEXT;
    old_offline BOOLEAN;
    new_offline BOOLEAN;
    presence_changed BOOLEAN;
    last_seen_untouched BOOLEAN;
    -- One scrape miss / short EOS lag only — NOT multi-hour lobby hopping.
    session_resume INTERVAL := interval '3 minutes';
BEGIN
    new_status := UPPER(BTRIM(COALESCE(NEW.status::TEXT, 'OFFLINE')));
    new_offline := new_status = 'OFFLINE';

    IF TG_OP = 'INSERT' THEN
        NEW.status_changed_at := COALESCE(NEW.status_changed_at, changed_at);
        NEW.last_seen_at := COALESCE(NEW.last_seen_at, changed_at);
        RETURN NEW;
    END IF;

    old_status := UPPER(BTRIM(COALESCE(OLD.status::TEXT, 'OFFLINE')));
    old_offline := old_status = 'OFFLINE';

    presence_changed :=
        (NEW.status IS DISTINCT FROM OLD.status)
        OR (NEW.server IS DISTINCT FROM OLD.server)
        OR (NEW.server_map IS DISTINCT FROM OLD.server_map)
        OR (NEW.lobby_code IS DISTINCT FROM OLD.lobby_code);

    last_seen_untouched := NEW.last_seen_at IS NOT DISTINCT FROM OLD.last_seen_at;

    -- Writer changed presence but omitted last_seen → refresh it (kills the ghost factory).
    IF (NOT new_offline) AND presence_changed AND (NEW.last_seen_at IS NULL OR last_seen_untouched) THEN
        NEW.last_seen_at := changed_at;
    END IF;

    IF new_status IS NOT DISTINCT FROM old_status THEN
        RETURN NEW;
    END IF;

    IF new_offline THEN
        -- Keep session start; offline duration uses last_seen_at in the API.
        NEW.status_changed_at := COALESCE(OLD.status_changed_at, changed_at);
        NEW.last_seen_at := COALESCE(NEW.last_seen_at, OLD.last_seen_at, changed_at);
        RETURN NEW;
    END IF;

    IF old_offline THEN
        -- Brief scraper/ghost blackout → resume same session timer.
        IF OLD.last_seen_at IS NOT NULL
           AND (changed_at - OLD.last_seen_at) <= session_resume
           AND OLD.status_changed_at IS NOT NULL THEN
            NEW.status_changed_at := OLD.status_changed_at;
        ELSE
            NEW.status_changed_at := changed_at;
        END IF;
        NEW.last_seen_at := COALESCE(NEW.last_seen_at, changed_at);
        RETURN NEW;
    END IF;

    -- Active → active (LOADING ↔ IN GAME): keep session start timer.
    NEW.status_changed_at := OLD.status_changed_at;
    RETURN NEW;
END;
$function$;

DROP TRIGGER IF EXISTS fusion_sync_status_timestamps ON public.client_data;
CREATE TRIGGER fusion_sync_status_timestamps
    BEFORE INSERT OR UPDATE OF status ON public.client_data
    FOR EACH ROW
    EXECUTE FUNCTION public.fusion_sync_status_timestamps();
