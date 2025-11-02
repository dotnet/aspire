import os
import uuid
import psycopg
from flask import Flask, jsonify
from azure.identity import DefaultAzureCredential

app = Flask(__name__)

def get_connection():
    uri = os.environ['DB1_URI']
    if os.environ.get('DB1_AZURE', "false").lower() == "true":
        user = "azure_user"  # Or use os.environ['DB1_USERNAME'] if required
        credential = DefaultAzureCredential()
        password = credential.get_token("https://ossrdbms-aad.database.windows.net/.default").token
    else:
        user = os.environ['DB1_USERNAME']
        password = os.environ['DB1_PASSWORD']
    return psycopg.connect(uri, user=user, password=password)

@app.route('/')
def index():
    conn = get_connection()
    with conn.cursor() as cur:
        cur.execute("CREATE TABLE IF NOT EXISTS entries (id UUID PRIMARY KEY);")
        entry_id = str(uuid.uuid4())
        cur.execute("INSERT INTO entries (id) VALUES (%s);", (entry_id,))
        conn.commit()
        cur.execute("SELECT id FROM entries;")
        entries = [row[0] for row in cur.fetchall()]
    conn.close()
    return jsonify({'totalEntries': len(entries), 'entries': entries})
