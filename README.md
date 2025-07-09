###### \# Quantia - Configuration du Projet

###### 

###### \## 🛠️ Prérequis

###### 

###### \### 1. Installer PostgreSQL

###### 

###### Téléchargez et installez PostgreSQL depuis le site officiel :

###### 

###### 👉 \[https://www.postgresql.org/download/](https://www.postgresql.org/download/)

###### 

###### > Lors de l'installation, définissez \*\*`root`\*\* comme mot de passe pour l'utilisateur `postgres`.

###### 

###### ---

###### 

###### \### 2. Configuration de la base de données

###### 

###### \#### a. Ouvrir PgAdmin et se connecter

###### 

###### \- Lancez \*\*PgAdmin\*\*.

###### \- Connectez-vous au serveur avec l’utilisateur \*\*`postgres`\*\* et le mot de passe \*\*`root`\*\*.

###### 

###### \#### b. Créer la base de données

###### 

###### \- Ouvrez \*\*Query Tool\*\*.

###### \- Exécutez la commande suivante :

###### 

###### ```sql

###### CREATE DATABASE quantia;

###### ```

###### 

###### \#### c. Créer les tables

###### 

###### \- Rafraîchissez l’arborescence (clic droit > Refresh).

###### \- Sélectionnez la base \*\*`quantia`\*\*.

###### \- Ouvrez à nouveau \*\*Query Tool\*\* et exécutez ce script SQL :

###### 

###### ```sql

###### CREATE EXTENSION pgcrypto;

###### 

###### -- Table : users

###### DROP TABLE IF EXISTS public.users;

###### CREATE TABLE public.users (

###### &nbsp;   id          SERIAL PRIMARY KEY,

###### &nbsp;   first\_name  VARCHAR(100) NOT NULL,

###### &nbsp;   last\_name   VARCHAR(100) NOT NULL,

###### &nbsp;   email       VARCHAR(150) NOT NULL UNIQUE,

###### &nbsp;   password    VARCHAR(255) NOT NULL

###### );

###### ALTER TABLE public.users OWNER TO postgres;

###### 

###### -- Table : transactions

###### DROP TABLE IF EXISTS public.transactions;

###### CREATE TABLE public.transactions (

###### &nbsp;   id                SERIAL PRIMARY KEY,

###### &nbsp;   user\_id           INTEGER REFERENCES public.users(id),

###### &nbsp;   crypto\_symbol     VARCHAR(10),

###### &nbsp;   amount            NUMERIC,

###### &nbsp;   price\_at\_purchase NUMERIC,

###### &nbsp;   "timestamp"       TIMESTAMP,

###### &nbsp;   side              VARCHAR(4) NOT NULL DEFAULT 'Buy',

###### &nbsp;   trade\_id          UUID NOT NULL DEFAULT gen\_random\_uuid()

###### );

###### ALTER TABLE public.transactions OWNER TO postgres;

###### 

###### -- Table : trades

###### DROP TABLE IF EXISTS public.trades;

###### CREATE TABLE public.trades (

###### &nbsp;   id            SERIAL PRIMARY KEY,

###### &nbsp;   user\_id       INTEGER NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,

###### &nbsp;   crypto\_symbol VARCHAR(20) NOT NULL,

###### &nbsp;   buy\_date      TIMESTAMP WITH TIME ZONE NOT NULL,

###### &nbsp;   buy\_price     NUMERIC(20,8)  NOT NULL,

###### &nbsp;   quantity      NUMERIC(38,18) NOT NULL,

###### &nbsp;   sell\_date     TIMESTAMP WITH TIME ZONE,

###### &nbsp;   sell\_price    NUMERIC(20,8),

###### &nbsp;   status        VARCHAR(10) NOT NULL DEFAULT 'Open'

###### );

###### ALTER TABLE public.trades OWNER TO postgres;

###### 

###### -- Indexes

###### CREATE INDEX idx\_trades\_buy\_date  ON public.trades (buy\_date);

###### CREATE INDEX idx\_trades\_symbol    ON public.trades (crypto\_symbol);

###### CREATE INDEX idx\_trades\_user\_id   ON public.trades (user\_id);

###### ```

###### 

###### ---

###### 

###### \## 🚀 Lancer l'API de prédiction en local

###### 

###### 1\. Clonez le dépôt suivant :

###### 

###### ```bash

###### git clone https://github.com/Saytk/PA\_ML

###### ```

###### 

###### 2\. Rendez-vous dans le dossier :

###### 

###### ```bash

###### cd PA\_ML

###### ```

###### 

###### 3\. Lancez le serveur avec cette commande :

###### 

###### ```bash

###### uvicorn crypto\_forecast\_ml.predictor.serve\_api:app --port 8006 --reload

###### ```

###### 

###### > L’API sera disponible sur : \[http://localhost:8006](http://localhost:8006)

###### 

###### ---

###### 

###### \## 📬 Contact

###### 

###### Pour toute question, n'hésitez pas à contacter l'auteur du projet.

###### 

###### ---



