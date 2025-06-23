"""
Scorer basé sur ElKulako/cryptobert (Bullish / Neutral / Bearish).
Renvoie un score normalisé [0,1] où 1 = très bullish, 0 = très bearish.
"""
from functools import lru_cache
from typing import Tuple

import torch
from transformers import AutoTokenizer, AutoModelForSequenceClassification

MODEL_NAME = "ElKulako/cryptobert"   # HF hub
ID_BEAR, ID_NEU, ID_BULL = 0, 1, 2   # ordre des labels dans ce modèle


@lru_cache(maxsize=1)
def _load_model() -> Tuple[AutoTokenizer, AutoModelForSequenceClassification]:
    tok = AutoTokenizer.from_pretrained(MODEL_NAME)
    mdl = AutoModelForSequenceClassification.from_pretrained(MODEL_NAME)
    mdl.eval()
    torch.set_num_threads(1)         # évite la sur-souscription CPU
    return tok, mdl


def score_text(text: str) -> float:
    tok, mdl = _load_model()
    with torch.no_grad():
        inputs = tok(text[:512], truncation=True, return_tensors="pt")
        logits = mdl(**inputs).logits
        probs = torch.softmax(logits, dim=-1).squeeze()
    bullish, bearish = probs[ID_BULL].item(), probs[ID_BEAR].item()
    # (bullish - bearish) ∈ [-1,1]  →  [0,1]
    return round((bullish - bearish + 1) / 2, 4)

