import os
import openai
import yaml
from flask import Flask, request, jsonify
import praw
import flasgger
from dotenv import load_dotenv
from models import SessionLocal, Insight, init_db


# Load environment variables
load_dotenv()

# Init Flask
app = Flask(__name__)
swagger = flasgger.Swagger(app)

init_db()

# Load config
with open("config.yaml", "r", encoding="utf-8") as f:
    config = yaml.safe_load(f)

# Load prompts
SYSTEM_PROMPT = config["prompts"]["system"]
USER_PROMPT_TEMPLATE = config["prompts"]["user"]

# OpenAI
openai.api_key = os.getenv("OPENAI_API_KEY")

# Reddit
reddit = praw.Reddit(
    client_id=os.getenv("REDDIT_CLIENT_ID"),
    client_secret=os.getenv("REDDIT_CLIENT_SECRET"),
    user_agent="gold-predictor/0.1"
)

def fetch_reddit_posts(topic: str, limit: int = 10) -> str:
    posts = reddit.subreddit("all").search(topic, limit=limit)
    combined_content = ""

    for post in posts:
        title = post.title
        body = post.selftext or ""
        combined_content += f"Title: {title}\nBody: {body}\n\n"

    return combined_content

def summarize_with_openai(topic: str, content: str) -> str:
    if not content.strip():
        return "Aucun contenu pertinent trouvé."

    user_prompt = USER_PROMPT_TEMPLATE.format(topic=topic, content=content)

    client = openai.OpenAI()

    completion = client.chat.completions.create(
        model="gpt-4o",
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": user_prompt}
        ],
        temperature=0.7,
        max_tokens=800
    )

    return completion.choices[0].message.content.strip()


def save_summary_to_db(topic: str, summary: str):
    """Enregistre un résumé dans la base PostgreSQL"""
    session = SessionLocal()
    try:
        insight = Insight(topic=topic, summary=summary)
        session.add(insight)
        session.commit()
    except Exception as e:
        session.rollback()
        raise e
    finally:
        session.close()


@app.route('/')
def home():
    return '<meta http-equiv="refresh" content="0; url=/apidocs" />'


def analyze_topic(topic: str, limit: int = 10) -> str:
    content = fetch_reddit_posts(topic, limit)
    return summarize_with_openai(topic, content)

@app.route('/analyze', methods=['GET'])
def analyze():
    """
    Analyse Reddit et génère un résumé via GPT-4o
    ---
    parameters:
      - name: topic
        in: query
        type: string
        required: false
        default: gold
        description: Sujet à analyser
      - name: limit
        in: query
        type: integer
        required: false
        default: 10
        description: Nombre de posts Reddit à analyser
    responses:
      200:
        description: Résumé généré
        examples:
          application/json:
            topic: gold
            summary: Résumé synthétique des tendances Reddit
    """
    topic = request.args.get('topic', 'gold')
    limit = int(request.args.get('limit', 10))

    try:
        summary = analyze_topic(topic, limit)
        # 👉 Enregistrement du résumé dans la base
        save_summary_to_db(topic, summary)

        return jsonify({"topic": topic, "summary": summary})
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=8000)
