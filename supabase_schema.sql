-- Supabase Database Schema for SCIP Dashboard
-- Run this in your Supabase SQL Editor (https://app.supabase.com/project/_/sql)

-- Enable UUID extension
create extension if not exists "uuid-ossp";

-- ============================================
-- user_settings: Per-user theme and preferences
-- ============================================
create table if not exists public.user_settings (
    id uuid default uuid_generate_v4() primary key,
    user_id uuid references auth.users(id) on delete cascade not null,
    preferred_theme text default 'Light',
    enable_auto_refresh boolean default false,
    refresh_interval_minutes integer default 15,
    min_profit_margin_percent numeric default 0,
    enable_desktop_notifications boolean default false,
    default_data_source text default 'UEXCorp',
    created_at timestamptz default now(),
    updated_at timestamptz default now()
);

-- ============================================
-- user_watchlist: Per-user commodity watchlist
-- ============================================
create table if not exists public.user_watchlist (
    id uuid default uuid_generate_v4() primary key,
    user_id uuid references auth.users(id) on delete cascade not null,
    commodity_id text not null,
    commodity_name text not null,
    target_buy_price numeric not null,
    target_sell_price numeric not null,
    created_at timestamptz default now(),
    notify_on_price_change boolean default true
);

-- ============================================
-- user_routes: Per-user saved hauling routes
-- ============================================
create table if not exists public.user_routes (
    id uuid default uuid_generate_v4() primary key,
    user_id uuid references auth.users(id) on delete cascade not null,
    name text not null,
    description text default '',
    route_json jsonb not null default '{}',
    estimated_net_profit numeric default 0,
    created_at timestamptz default now(),
    updated_at timestamptz default now()
);

-- ============================================
-- user_fleet: Per-user fleet ship information
-- ============================================
create table if not exists public.user_fleet (
    id uuid default uuid_generate_v4() primary key,
    user_id uuid references auth.users(id) on delete cascade not null,
    ship_name text not null,
    total_capacity_scu integer default 0,
    current_load_scu integer default 0,
    average_speed numeric default 0,
    is_available boolean default true,
    current_location_system text default '',
    completed_runs integer default 0,
    total_profit numeric default 0,
    created_at timestamptz default now()
);

-- ============================================
-- market_snapshots: Cached market data (shared across users)
-- ============================================
create table if not exists public.market_snapshots (
    id uuid default uuid_generate_v4() primary key,
    commodity_id text not null,
    commodity_name text not null,
    location_id text not null,
    location_name text not null,
    system_name text not null,
    buy_price numeric not null,
    sell_price numeric not null,
    volume integer default 0,
    demand_volume integer default 0,
    timestamp timestamptz default now(),
    source_name text default ''
);

-- ============================================
-- Indexes for performance
-- ============================================
create index if not exists idx_user_settings_user_id on public.user_settings(user_id);
create index if not exists idx_user_watchlist_user_id on public.user_watchlist(user_id);
create index if not exists idx_user_watchlist_commodity_id on public.user_watchlist(commodity_id);
create index if not exists idx_user_routes_user_id on public.user_routes(user_id);
create index if not exists idx_user_fleet_user_id on public.user_fleet(user_id);
create index if not exists idx_market_snapshots_commodity on public.market_snapshots(commodity_id);
create index if not exists idx_market_snapshots_location on public.market_snapshots(location_id);
create index if not exists idx_market_snapshots_timestamp on public.market_snapshots(timestamp);

-- ============================================
-- Row Level Security (RLS) policies
-- ============================================
alter table public.user_settings enable row level security;
alter table public.user_watchlist enable row level security;
alter table public.user_routes enable row level security;
alter table public.user_fleet enable row level security;

-- Users can only see their own settings
create policy "Users can view their own settings"
    on public.user_settings for select
    using (auth.uid() = user_id);

create policy "Users can insert their own settings"
    on public.user_settings for insert
    with check (auth.uid() = user_id);

create policy "Users can update their own settings"
    on public.user_settings for update
    using (auth.uid() = user_id);

create policy "Users can delete their own settings"
    on public.user_settings for delete
    using (auth.uid() = user_id);

-- Users can only see their own watchlist
create policy "Users can view their own watchlist"
    on public.user_watchlist for select
    using (auth.uid() = user_id);

create policy "Users can insert their own watchlist"
    on public.user_watchlist for insert
    with check (auth.uid() = user_id);

create policy "Users can update their own watchlist"
    on public.user_watchlist for update
    using (auth.uid() = user_id);

create policy "Users can delete their own watchlist"
    on public.user_watchlist for delete
    using (auth.uid() = user_id);

-- Users can only see their own routes
create policy "Users can view their own routes"
    on public.user_routes for select
    using (auth.uid() = user_id);

create policy "Users can insert their own routes"
    on public.user_routes for insert
    with check (auth.uid() = user_id);

create policy "Users can update their own routes"
    on public.user_routes for update
    using (auth.uid() = user_id);

create policy "Users can delete their own routes"
    on public.user_routes for delete
    using (auth.uid() = user_id);

-- Users can only see their own fleet
create policy "Users can view their own fleet"
    on public.user_fleet for select
    using (auth.uid() = user_id);

create policy "Users can insert their own fleet"
    on public.user_fleet for insert
    with check (auth.uid() = user_id);

create policy "Users can update their own fleet"
    on public.user_fleet for update
    using (auth.uid() = user_id);

create policy "Users can delete their own fleet"
    on public.user_fleet for delete
    using (auth.uid() = user_id);

-- Market snapshots are readable by all authenticated users
create policy "Authenticated users can view market snapshots"
    on public.market_snapshots for select
    using (auth.role() = 'authenticated');

create policy "Authenticated users can insert market snapshots"
    on public.market_snapshots for insert
    with check (auth.role() = 'authenticated');

-- ============================================
-- Realtime subscriptions (optional, for live updates)
-- ============================================
-- Uncomment these lines if you want real-time updates:
-- alter publication supabase_realtime add table public.user_settings;
-- alter publication supabase_realtime add table public.user_watchlist;
-- alter publication supabase_realtime add table public.user_routes;
-- alter publication supabase_realtime add table public.user_fleet;
-- alter publication supabase_realtime add table public.market_snapshots;
