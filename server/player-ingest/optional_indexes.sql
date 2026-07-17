-- Run as DB superuser / owner (client_writer cannot CREATE INDEX).
-- Prevents duplicate pid rows if two clients race.
CREATE UNIQUE INDEX IF NOT EXISTS client_data_pid_uidx ON public.client_data (pid);
