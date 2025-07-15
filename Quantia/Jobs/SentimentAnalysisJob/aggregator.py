from __future__ import annotations

import datetime as dt
import json
import logging
from pathlib import Path
from typing import Any, List

import psycopg2
from psycopg2.extras import Json

LOGGER = logging.getLogger(__name__)
ARCHIVE_DAYS = 30


def archive_old(conn) -> None:
    limit = dt.datetime.utcnow() - dt.timedelta(days=ARCHIVE_DAYS)
    with conn.cursor() as cur:
        cur.execute(
            (limit,),
        )
        cur.execute("DELETE FROM sentiment_details WHERE ts_hour < %s", (limit,))
    conn.commit()
    LOGGER.info("Archivage terminé jusqu’au %s", limit.date())
