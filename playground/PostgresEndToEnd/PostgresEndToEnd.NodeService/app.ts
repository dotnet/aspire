import express from 'express';
import { Pool } from 'pg';
import { DefaultAzureCredential } from '@azure/identity';
import { v4 as uuidv4 } from 'uuid';

const app = express();

async function getPool() {
    const uri = process.env.DB1_URI!;
    let user: string, password: string;

    if (process.env.DB1_AZURE === "true") {
        user = "azure_user"; // Or use process.env.DB1_USERNAME if required
        const credential = new DefaultAzureCredential();
        const tokenResponse = await credential.getToken("https://ossrdbms-aad.database.windows.net/.default");
        password = tokenResponse.token;
    } else {
        user = process.env.DB1_USERNAME!;
        password = process.env.DB1_PASSWORD!;
    }

    return new Pool({
        connectionString: uri,
        user,
        password,
    });
}

app.get('/', async (_req, res) => {
    const pool = await getPool();
    const client = await pool.connect();
    try {
        await client.query("CREATE TABLE IF NOT EXISTS entries (id UUID PRIMARY KEY);");
        const entryId = uuidv4();
        await client.query("INSERT INTO entries (id) VALUES ($1);", [entryId]);
        const result = await client.query("SELECT id FROM entries;");
        res.json({
            totalEntries: result.rowCount,
            entries: result.rows.map(r => r.id),
        });
    } finally {
        client.release();
        await pool.end();
    }
});

const port = process.env.PORT || 3000;
app.listen(port, () => console.log(`Node app listening on port ${port}`));
