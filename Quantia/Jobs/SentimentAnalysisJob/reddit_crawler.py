import os, time
from typing import List, Dict
from datetime import datetime, timedelta
import praw

class RedditCrawler:
    """
    Récupère posts + commentaires récents.
    • subreddits  : liste sans le préfixe r/
    • hours_back  : fenêtre glissante
    • max_total   : nombre max de messages
    • comment_limit : commentaires lus par post
    """

    def __init__(
        self,
        subreddits: List[str],
        *,
        hours_back: int = 24,
        max_total: int = 200,
        comment_limit: int = 10,
    ) -> None:
        self.subreddits = subreddits
        self.hours_back = hours_back
        self.max_total = max_total
        self.comment_limit = comment_limit

        self.reddit = praw.Reddit(
            client_id=os.getenv("REDDIT_CLIENT_ID"),
            client_secret=os.getenv("REDDIT_CLIENT_SECRET"),
            user_agent=os.getenv("USER_AGENT", "crypto_sentiment_bot"),
        )

    # -----------------------------------------------------------------
    def fetch(self) -> List[Dict]:
        texts: List[Dict] = []
        cutoff = time.time() - self.hours_back * 3600

        for sub in self.subreddits:
            subreddit = self.reddit.subreddit(sub)

            # Parcourt les posts par ordre "new"
            for post in subreddit.new(limit=None):
                if post.created_utc < cutoff:
                    break
                if len(texts) >= self.max_total:
                    return texts

                texts.append({
                    "id": post.id,
                    "text": f"{post.title} {post.selftext or ''}",
                    "up": max(post.score, 1),
                    "ts": post.created_utc  # 🆕
                })

                post.comments.replace_more(limit=0)
                for com in post.comments[: self.comment_limit]:
                    if com.created_utc < cutoff:
                        continue
                    if len(texts) >= self.max_total:
                        return texts

                    texts.append({
                        "id": f"{post.id}_{com.id}",
                        "text": com.body,
                        "up": max(com.score, 1),
                        "ts": com.created_utc  # 🆕
                    })

        return texts
