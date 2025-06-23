from typing import List
from keybert import KeyBERT
import re

STOP = {
    "btc","bitcoin","crypto","cryptocurrency","eth","ethereum",
    "buy","sell","hodl","pump","dip","moon"
}
TOKEN = re.compile(r"[a-zA-Z]{3,}")

kw_model = KeyBERT("sentence-transformers/all-MiniLM-L6-v2")

def _clean(text: str) -> str:
    return " ".join(t for t in TOKEN.findall(text.lower()) if t not in STOP)

def top_keywords(texts: List[str]) -> str:
    """
    Retourne le 1ᵉʳ n-gramme (1-3 mots) le plus pertinent,
    ignorant le vocabulaire crypto générique.
    """
    joined = " ".join(_clean(t) for t in texts)
    # top_n=10 -> on prend le premier hors stop-list
    for kw, _ in kw_model.extract_keywords(
        joined,
        keyphrase_ngram_range=(1, 3),
        stop_words="english",
        top_n=10,
    ):
        if kw.split()[0] not in STOP:
            return kw
    return "misc"
