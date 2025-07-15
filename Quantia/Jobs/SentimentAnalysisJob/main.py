#!/usr/bin/env python
"""
Pipeline quotidien : 00 h UTC
  – Reddit sentiment
  – Google Trends « bitcoin »
  – Indicateurs marché (vol, momentum, volume, dominance)
  – Mix final (50 % Reddit, 25 % G‑trends, 25 % marché)
  – Sauvegarde Postgres + cache reddit_raw
"""
from __future__ import annotations
import os, logging, datetime as dt, time, numpy as np, psycopg2, spacy
from typing import Any, Dict, List
from psycopg2.extras import Json
from tqdm.auto import tqdm
from utils import amplify

# ── env ───────────────────────────────────────────────────────────────
from dotenv import load_dotenv
load_dotenv()
PG_DSN = os.environ["PG_CONN"]

# ── modules locaux ────────────────────────────────────────────────────
from logging_config   import setup_logging
from reddit_crawler   import RedditCrawler
from reddit_processor import RedditProcessor
from sentiment_scorer import score_text
from clusterer        import Clusterer
from keywords         import top_keywords
from summerizer       import summarize
from price_fetcher    import fetch_price
import tech_indicators as ti

try:
    from pytrends.request import TrendReq
except ModuleNotFoundError:
    TrendReq = None

setup_logging()
log = logging.getLogger(__name__)

# ── paramètres globaux facilement modifiables ─────────────────────────
HOURS_BACK    = 24        
MAX_TOTAL     = 20_000

MIN_REQUIRED  = 3000     
COMMENT_LIMIT = 1000
TOP_K_DISPLAY = 8
TAU_HOURS     = 6

SUBREDDITS_FILE = "subreddits.txt"
ASSET           = os.getenv("ASSET", "bitcoin").lower()

# ═════════════════════════ Google‑Trends ══════════════════════════════
def google_trend_score(keyword: str = "bitcoin") -> float:
    if TrendReq is None:
        return 0.5
    try:
        pt = TrendReq(hl="en-US", tz=0)
        pt.build_payload([keyword], timeframe="today 3-m")
        s = pt.interest_over_time()[keyword]
        s = s[s > 0]
        if s.empty:
            return 0.5
        ratio = s.iloc[-1] / (s.iloc[:-1].mean() or 1)
        return round(float(np.clip((ratio - .5) / 1.5 + .5, 0, 1)), 3)
    except Exception as exc:
        log.warning("pytrends error: %s → 0.5", exc)
        return 0.5

# ═════════════════════════ Reddit pipeline ════════════════════════════
def _subs(path: str) -> List[str]:
    with open(path, encoding="utf-8") as f:
        return [l.strip().replace("r/", "") for l in f
                if l.strip() and not l.startswith("#")]

def reddit_pipeline(ts_midnight: dt.datetime) -> Dict[str, Any]:
    log.info("▶ Reddit pipeline…")
    subs = _subs(SUBREDDITS_FILE)
    nlp  = spacy.load("en_core_web_sm", disable=["ner"])

    # -- Crawl ---------------------------------------------------------
    log.info("Crawl fenêtre %d h (max %d)…", HOURS_BACK, MAX_TOTAL)
    raw = RedditCrawler(
        subs,
        hours_back   = HOURS_BACK,
        max_total    = MAX_TOTAL,
        comment_lim  = COMMENT_LIMIT,
        force_time   = ts_midnight,
        min_required = MIN_REQUIRED, 
    ).fetch()

    # -- Filtrage ------------------------------------------------------
    clean = RedditProcessor(nlp).process(raw)
    log.info("Messages retenus : %d", len(clean))

    # -- Sentiment -----------------------------------------------------
    scores = [score_text(c["text"]) for c in
              tqdm(clean, desc="Sentiment", leave=False, dynamic_ncols=True)]

    # -- Clustering ----------------------------------------------------
    clusters = Clusterer().cluster([c["text"] for c in clean])

    # -- Agrégation ----------------------------------------------------
    upvotes = np.array([c["up"] for c in clean])
    ratios  = np.array([c.get("ratio", .5) for c in clean])
    stamps  = np.array([c["ts"] for c in clean])
    now_ts  = time.time()

    contrib = []
    for idxs in clusters.values():
        idxs = np.array(idxs)
        w   = (1 + np.log1p(upvotes[idxs]).sum()) * ratios[idxs].mean()
        avg = np.mean([scores[i] for i in idxs])
        contrib.append({"avg": avg, "w": w, "idxs": idxs})

    reddit_idx = np.average([amplify(c["avg"]) for c in contrib], weights=[c["w"] for c in contrib])

    result_clusters = []
    for c in contrib:
        idxs = c["idxs"]
        importance = (np.abs(np.array(scores)[idxs] - .5) *
                      np.log1p(upvotes[idxs]) *
                      np.exp(-(now_ts - stamps[idxs]) / 3600 / TAU_HOURS))
        top = idxs[np.argsort(importance)[::-1][:3]]
        result_clusters.append({
            "topic"   : top_keywords([clean[i]["text"] for i in idxs]),
            "avg"     : round(float(c["avg"]), 3),
            "freq"    : len(idxs),
            "delta"   : round(float((c["avg"]-.5)*c["w"]), 3),
            "summary" : summarize(" ".join(clean[i]["text"] for i in top), c["avg"]),
            "examples": [clean[i]["text"].replace("\n", " ") for i in top],
            "urls"    : [clean[i]["url"] for i in top]
        })

    return {
        "asset": ASSET,
        "reddit_index": reddit_idx,
        "clusters": sorted(result_clusters,
                           key=lambda x: abs(x["delta"]),
                           reverse=True)[:TOP_K_DISPLAY],
    }

# ═════════════════════════ Mix & save ════════════════════════════════
def mix_scores(r: float, g: float, m: float) -> float:
    return round(0.5*r + 0.5*m, 3)

def save_score(idx: float, price_btc: float | None, price_eth: float | None,
               payload: Dict[str, Any], ts_midnight: dt.datetime) -> None:
    with psycopg2.connect(PG_DSN) as conn, conn.cursor() as cur:
        cur.execute("""
            INSERT INTO sentiment_scores (ts, ts_hour, score, price_btc, price_eth)
            VALUES (%s,%s,%s,%s,%s)
            ON CONFLICT (ts_hour) DO UPDATE
            SET score = EXCLUDED.score,
                price_btc = EXCLUDED.price_btc,
                price_eth = EXCLUDED.price_eth;
        """, (ts_midnight, ts_midnight, idx, price_btc, price_eth))
        cur.execute("""
            INSERT INTO sentiment_details (ts_hour, json_payload)
            VALUES (%s,%s)
            ON CONFLICT (ts_hour) DO UPDATE
            SET json_payload = EXCLUDED.json_payload;
        """, (ts_midnight, Json(payload)))
        conn.commit()
    log.info("✓ Save Postgres")

# ═════════════════════════ Orchestrateur ═════════════════════════════
def run_day(ts_midnight: dt.datetime) -> None:
    log.info("🗓️ %s", ts_midnight.date())
    reddit  = reddit_pipeline(ts_midnight)
    gtrend  = google_trend_score()
    market  = ti.market_composite(ts_midnight)
    global_ = mix_scores(reddit["reddit_index"], gtrend, market)

    price_btc = fetch_price("bitcoin",  ts_midnight)
    price_eth = fetch_price("ethereum", ts_midnight)

    payload = reddit | {
        "google_trend":  gtrend,
        "market_index":  market,
        "global_index":  global_,
        "price_btc":     price_btc,
        "price_eth":     price_eth,
    }
    save_score(global_, price_btc, price_eth, payload, ts_midnight)

# --------------------------------------------------------------------
def missing_days(from_date: dt.datetime) -> List[dt.datetime]:
    from_date = from_date.replace(hour=0, minute=0, second=0,
                                  microsecond=0, tzinfo=dt.timezone.utc)
    today = dt.datetime.utcnow().replace(hour=0, minute=0, second=0,
                                         microsecond=0, tzinfo=dt.timezone.utc)
    with psycopg2.connect(PG_DSN) as conn, conn.cursor() as cur:
        cur.execute("""
            SELECT date_trunc('day', ts_hour)
            FROM sentiment_scores
            WHERE ts_hour >= %s
        """, (from_date,))
        existing = {r[0].date() for r in cur.fetchall()}

    return [
        dt.datetime.combine(from_date.date() + dt.timedelta(d),
                            dt.time.min,
                            tzinfo=dt.timezone.utc)
        for d in range((today.date() - from_date.date()).days + 1)
        if (from_date.date() + dt.timedelta(d)) not in existing
    ]

# --------------------------------------------------------------------
def main() -> None:
    try:
        START = dt.datetime(2025, 6, 14, tzinfo=dt.timezone.utc)
        days  = missing_days(START)
        log.info("⏳ Jours manquants : %d", len(days))

        for day in tqdm(days, desc="Back‑fill"):
            run_day(day)

        if not days:  # exécution pour le jour courant
            run_day(dt.datetime.utcnow().replace(hour=0, minute=0, second=0,
                                                 microsecond=0,
                                                 tzinfo=dt.timezone.utc))
        log.info("🎉 Pipeline terminé")

    except Exception:
        log.exception("💥 Pipeline failed")

if __name__ == "__main__":
    main()
