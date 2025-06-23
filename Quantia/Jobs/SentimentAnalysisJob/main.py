#!/usr/bin/env python
"""
Pipeline Crypto Sentiment complet :
collecte, scoring CryptoBERT, clustering, topic, résumé GPT,
importance = sentiment extrême × popularité.
Écrit sentiment.json + affichage console/JSON.
"""

import os, sys, json, random, time
from datetime import datetime
from pathlib import Path
import numpy as np
from dotenv import load_dotenv
import spacy, tqdm

from reddit_crawler import RedditCrawler
from reddit_processor import RedditProcessor
from sentiment_scorer import score_text          # CryptoBERT
from clusterer import Clusterer
from keywords import top_keywords
from summerizer import summarize                 # GPT résumé (avec backoff)

# ---------------- Paramètres ----------------------------------------
HOURS_BACK      = 24
MAX_TOTAL       = 5_000
COMMENT_LIMIT   = 20
TOP_K_DISPLAY   = 8
SUBREDDITS_FILE = "subreddits.txt"
TAU_HOURS = 12          # constante de décroissance


OUTPUT_JSON     = "/home/jobordeau/pa/sentiment.json"
# --------------------------------------------------------------------

def load_subreddits(path: str) -> list[str]:
    with open(path) as f:
        return [l.strip().replace("r/", "") for l in f if l.strip() and not l.startswith("#")]

# --------------------------------------------------------------------
def run_pipeline() -> dict:
    load_dotenv()
    subreddits = load_subreddits(SUBREDDITS_FILE)
    nlp = spacy.load("en_core_web_sm", disable=["ner"])

    # 1. Collecte -----------------------------------------------------
    crawler = RedditCrawler(subreddits,
                            hours_back=HOURS_BACK,
                            max_total=MAX_TOTAL,
                            comment_limit=COMMENT_LIMIT)
    raw = crawler.fetch()
    print(f"[{datetime.utcnow():%H:%M}] Collectés {len(raw)} messages <{HOURS_BACK}h")

    # 2. Filtrage -----------------------------------------------------
    processor = RedditProcessor(nlp)
    clean = processor.process(raw)
    texts   = [c["text"] for c in clean]
    upvotes = [c["up"]   for c in clean]          # 👈 nouvelles métadonnées
    stamps = [c["ts"] for c in clean]  # 🆕
    now_ts = time.time()
    print(f"[{datetime.utcnow():%H:%M}] Textes après filtrage : {len(texts)}")

    # 3. Scoring CryptoBERT ------------------------------------------
    scores = [score_text(t) for t in tqdm.tqdm(texts, desc="CryptoBERT scoring")]

    # 4. Clustering ---------------------------------------------------
    clusterer = Clusterer(eps=0.45, min_samples=4)
    clusters = clusterer.cluster(texts)
    print(f"[{datetime.utcnow():%H:%M}] {len(clusters)} clusters détectés")

    # 5. Agrégation ---------------------------------------------------
    contrib = []
    for cid, idxs in clusters.items():
        freq = len(idxs)
        # Poids du cluster : somme log(upvotes)
        w = 1 + np.log1p(sum(upvotes[i] for i in idxs))
        avg = np.mean([scores[i] for i in idxs])

        contrib.append({"cid": cid, "avg": avg, "freq": freq, "w": w, "idxs": idxs})

    global_idx = round(float(np.average(
        [c["avg"] for c in contrib],
        weights=[c["w"]  for c in contrib])), 3)

    # 6. Topic + résumé + exemples -----------------------------------
    result_clusters = []
    for c in contrib:
        idxs = c["idxs"]
        texts_cluster = [texts[i] for i in idxs]

        # Importance par message
        importance = [
            abs(scores[i] - 0.5) *
            np.log1p(upvotes[i]) *
            np.exp(-(now_ts - stamps[i]) / 3600 / TAU_HOURS)  # 🆕 facteur temps
            for i in idxs
        ]
        top_idxs = [idx for idx, _ in sorted(
                        zip(idxs, importance),
                        key=lambda t: t[1],
                        reverse=True)[:3]]

        examples = [texts[i].replace("\n", " ") for i in top_idxs]

        topic = top_keywords(texts_cluster)
        summary = summarize(" ".join(texts_cluster[:5]), c["avg"])

        result_clusters.append({
            "topic":   topic,
            "avg":     round(float(c["avg"]), 3),
            "freq":    c["freq"],
            "delta":   round(float((c["avg"] - 0.5) * c["w"]), 3),
            "summary": summary,
            "examples": examples,
        })

    return {
        "global_index": global_idx,
        "clusters": sorted(result_clusters,
                           key=lambda x: abs(x["delta"]),
                           reverse=True)[:TOP_K_DISPLAY]
    }

# --------------------------------------------------------------------
def save_json(data: dict, path: str):
    Path(path).parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

# --------------------------------------------------------------------
if __name__ == "__main__":
    data = run_pipeline()
    save_json(data, OUTPUT_JSON)

    if "--json" in sys.argv:
        print(json.dumps(data, ensure_ascii=False))
    else:
        print(f"\n📊 Index : {data['global_index']}")
        for cl in data["clusters"]:
            arrow = "↑" if cl["delta"] > 0 else "↓"
            print(f"{arrow} {cl['topic']} ({cl['avg']}, {cl['freq']} msg) – {cl['summary']}")
