-- Run as table owner / superuser (client_writer cannot CREATE INDEX).
CREATE UNIQUE INDEX IF NOT EXISTS client_data_pid_uidx ON public.client_data (pid);
CREATE INDEX IF NOT EXISTS client_data_status_idx ON public.client_data (status);
