-- supabase-policies.sql
-- Policies for development ONLY. Restrict in production.

-- Enable Row Level Security (if not already enabled)
-- ALTER TABLE public.restaurants ENABLE ROW LEVEL SECURITY;

-- Allow public SELECT
CREATE POLICY "public_select_restaurants" ON public.restaurants
  FOR SELECT
  USING (true);

-- Allow public INSERT
CREATE POLICY "public_insert_restaurants" ON public.restaurants
  FOR INSERT
  WITH CHECK (true);

-- Allow public UPDATE
CREATE POLICY "public_update_restaurants" ON public.restaurants
  FOR UPDATE
  USING (true)
  WITH CHECK (true);

-- Allow public DELETE
CREATE POLICY "public_delete_restaurants" ON public.restaurants
  FOR DELETE
  USING (true);

-- Optional: grant usage to anon role (not needed if policies cover access)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON public.restaurants TO anon;