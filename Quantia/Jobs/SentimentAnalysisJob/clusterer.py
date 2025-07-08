from typing import List, Dict
from sentence_transformers import SentenceTransformer
from sklearn.cluster import DBSCAN
import numpy as np
import re

EMB_MODEL = "sentence-transformers/all-MiniLM-L6-v2"
STOP = {"btc", "bitcoin", "crypto", "eth", "ethereum", "buy", "sell", "hodl"}
TOKEN = re.compile(r"[a-zA-Z]{3,}")

def _clean(t: str) -> str:
    return " ".join(tok for tok in TOKEN.findall(t.lower()) if tok not in STOP)

class Clusterer:
    def __init__(self, eps: float = 0.45, min_samples: int = 4):
        self.model = SentenceTransformer(EMB_MODEL)
        self.eps, self.min_samples = eps, min_samples

    def cluster(self, texts: List[str]) -> Dict[int, List[int]]:
        emb = self.model.encode([_clean(t) for t in texts],
                                batch_size=64, normalize_embeddings=True)
        labels = DBSCAN(eps=self.eps, min_samples=self.min_samples,
                        metric="cosine").fit_predict(emb)
        clusters: Dict[int, List[int]] = {}
        for i, lab in enumerate(labels):
            if lab == -1:  # bruit
                continue
            clusters.setdefault(lab, []).append(i)
        return clusters
