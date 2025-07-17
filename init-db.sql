-- 1. Créer la base
CREATE DATABASE quantia;

-- 2. Se connecter à la base
\connect quantia;

-- 3. Créer l’extension
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- 4. Créer les tables
DROP TABLE IF EXISTS public.trades;
DROP TABLE IF EXISTS public.transactions;
DROP TABLE IF EXISTS public.users;

CREATE TABLE public.users (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL UNIQUE,
    password VARCHAR(255) NOT NULL
);
ALTER TABLE public.users OWNER TO postgres;

CREATE TABLE public.transactions (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES public.users(id),
    crypto_symbol VARCHAR(10),
    amount NUMERIC,
    price_at_purchase NUMERIC,
    "timestamp" TIMESTAMP,
    side VARCHAR(4) NOT NULL DEFAULT 'Buy',
    trade_id UUID NOT NULL DEFAULT gen_random_uuid()
);
ALTER TABLE public.transactions OWNER TO postgres;

CREATE TABLE public.trades (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    crypto_symbol VARCHAR(20) NOT NULL,
    buy_date TIMESTAMP WITH TIME ZONE NOT NULL,
    buy_price NUMERIC(20,8) NOT NULL,
    quantity NUMERIC(38,18) NOT NULL,
    sell_date TIMESTAMP WITH TIME ZONE,
    sell_price NUMERIC(20,8),
    status VARCHAR(10) NOT NULL DEFAULT 'Open'
);
ALTER TABLE public.trades OWNER TO postgres;

CREATE INDEX idx_trades_buy_date ON public.trades (buy_date);
CREATE INDEX idx_trades_symbol ON public.trades (crypto_symbol);
CREATE INDEX idx_trades_user_id ON public.trades (user_id);
