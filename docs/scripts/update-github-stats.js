import fs from 'fs';
import fetch from 'node-fetch';

const REPOS = [
    'dotnet/aspire',
    'dotnet/aspire-samples'
];
const OUTPUT_PATH = './src/data/github-stats.json';

async function fetchRepoStats(repo) {
    const url = `https://api.github.com/repos/${repo}`;
    const res = await fetch(url, {
        headers: { 'User-Agent': 'aspire-stats-script' }
    });
    if (!res.ok) throw new Error(`Failed to fetch ${repo}: ${res.statusText}`);
    const data = await res.json();
    let licenseUrl = null;
    if (data.license?.spdx_id) {
        const base = `https://github.com/${repo}/blob/${data.default_branch}/`;
        const licenseFiles = ['LICENSE', 'LICENSE.TXT', 'LICENSE.md', 'LICENSE.txt', 'LICENSE.md'];
        for (const file of licenseFiles) {
            // Try the raw URL to see if the license file exists
            const fileUrl = `${base}${file}`;
            const res = await fetch(fileUrl, {
                method: 'HEAD',
                headers: { 'User-Agent': 'aspire-stats-script' }
            });
            if (res.ok) {
                licenseUrl = fileUrl;
                break;
            }
        }
        // Fallback to generic license url if none found
        if (!licenseUrl) {
            licenseUrl = data.license?.url || null;
        }
    } else {
        licenseUrl = data.license?.url || null;
    }

    return {
        name: data.full_name,
        stars: data.stargazers_count,
        description: data.description || null,
        license: licenseUrl,
        licenseName: data.license?.name || null,
        repo: data.html_url
    };
}

async function fetchAllStats() {
    const stats = await Promise.all(REPOS.map(fetchRepoStats));
    fs.writeFileSync(OUTPUT_PATH, JSON.stringify(stats, null, 2));
    console.log(`✅ Saved stats for ${stats.length} repos to ${OUTPUT_PATH}`);
}

fetchAllStats().catch(err => {
    console.error('❌ Failed to fetch GitHub stats', err);
});