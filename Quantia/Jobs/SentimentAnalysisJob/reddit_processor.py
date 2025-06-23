from typing import List, Dict
import hashlib
import pandas as pd
import spacy
from spacy.lang.en import English

# Réutilise le TitleProcessor que tu possèdes déjà
from processor import TitleProcessor


class RedditProcessor:
    def __init__(self, spacy_model):
        self.tp = TitleProcessor(spacy_model)

    def process(self, raw_texts: List[Dict]) -> List[Dict]:
        """
        raw_texts : [{'id':…, 'text':…, 'up':…, 'ts':…}, …]
        Retourne uniquement les messages filtrés,
        en conservant upvotes et timestamp.
        """
        df = pd.DataFrame(raw_texts)

        # compatibilité avec TitleProcessor
        df["title"] = df["text"]
        df["pub_time"] = pd.Timestamp.utcnow()

        df = self.tp.filter_titles(df, min_date="1900-01-01")

        # 🆕  garder aussi up et ts
        return df[["id", "text", "up", "ts"]].to_dict("records")
