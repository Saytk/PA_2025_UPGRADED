# Quantia - Configuration du Projet

## 🛠️ Prérequis

### 1. Installer PostgreSQL

Téléchargez et installez PostgreSQL depuis le site officiel :

👉 [https://www.postgresql.org/download/](https://www.postgresql.org/download/)

> Lors de l'installation, définissez **`root`** comme mot de passe pour l'utilisateur `postgres`.

---

### 2. Configuration de la base de données

#### a. Ouvrir PgAdmin et se connecter

- Lancez **PgAdmin**.
- Connectez-vous au serveur avec l’utilisateur **`postgres`** et le mot de passe **`root`**.

#### b. Créer la base de données

- Ouvrez **Query Tool**.
- Exécutez la commande suivante :

```sql
CREATE DATABASE quantia;
```

#### c. Créer les tables

- Rafraîchissez l’arborescence (clic droit > Refresh).
- Sélectionnez la base **`quantia`**.
- Ouvrez à nouveau **Query Tool** et exécutez ce script SQL :

```sql
CREATE EXTENSION pgcrypto;

-- Table : users
DROP TABLE IF EXISTS public.users;
CREATE TABLE public.users (
    id          SERIAL PRIMARY KEY,
    first_name  VARCHAR(100) NOT NULL,
    last_name   VARCHAR(100) NOT NULL,
    email       VARCHAR(150) NOT NULL UNIQUE,
    password    VARCHAR(255) NOT NULL
);
ALTER TABLE public.users OWNER TO postgres;

-- Table : transactions
DROP TABLE IF EXISTS public.transactions;
CREATE TABLE public.transactions (
    id                SERIAL PRIMARY KEY,
    user_id           INTEGER REFERENCES public.users(id),
    crypto_symbol     VARCHAR(10),
    amount            NUMERIC,
    price_at_purchase NUMERIC,
    "timestamp"       TIMESTAMP,
    side              VARCHAR(4) NOT NULL DEFAULT 'Buy',
    trade_id          UUID NOT NULL DEFAULT gen_random_uuid()
);
ALTER TABLE public.transactions OWNER TO postgres;

-- Table : trades
DROP TABLE IF EXISTS public.trades;
CREATE TABLE public.trades (
    id            SERIAL PRIMARY KEY,
    user_id       INTEGER NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    crypto_symbol VARCHAR(20) NOT NULL,
    buy_date      TIMESTAMP WITH TIME ZONE NOT NULL,
    buy_price     NUMERIC(20,8)  NOT NULL,
    quantity      NUMERIC(38,18) NOT NULL,
    sell_date     TIMESTAMP WITH TIME ZONE,
    sell_price    NUMERIC(20,8),
    status        VARCHAR(10) NOT NULL DEFAULT 'Open'
);
ALTER TABLE public.trades OWNER TO postgres;

-- Indexes
CREATE INDEX idx_trades_buy_date  ON public.trades (buy_date);
CREATE INDEX idx_trades_symbol    ON public.trades (crypto_symbol);
CREATE INDEX idx_trades_user_id   ON public.trades (user_id);
```

---

## 🚀 Lancer l'API de prédiction en local

1. Clonez le dépôt suivant :

```bash
git clone https://github.com/Saytk/PA_ML
```

2. Rendez-vous dans le dossier :

```bash
cd PA_ML
```

3. Lancez le serveur avec cette commande :

```bash
uvicorn crypto_forecast_ml.predictor.serve_api:app --port 8006 --reload
```

> L’API sera disponible sur : [http://localhost:8006](http://localhost:8006)

---

## 📬 Contact

Pour toute question, n'hésitez pas à contacter l'auteur du projet.

---
