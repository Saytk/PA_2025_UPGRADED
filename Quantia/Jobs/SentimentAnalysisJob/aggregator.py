from typing import List
import numpy as np


def sentiment_index(scores: List[float]) -> float | None:
    """Indice global = moyenne simple. None si liste vide."""
    return round(float(np.mean(scores)), 3) if scores else None

