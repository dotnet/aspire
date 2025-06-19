import fs from 'fs';
import fetch from 'node-fetch';

const SERVICE_INDEX = 'https://api.nuget.org/v3/index.json';
const API_QUERIES = [
  'owner:aspire',
  'Aspire.Hosting.',
  'CommunityToolkit.Aspire',
];
const OUTPUT_PATH = './src/data/aspire-integrations.json';

// According to documentation, nuget.org limits:
// - 'take' parameter to 1,000
// - 'skip' parameter to 3,000 :contentReference[oaicite:5]{index=5}
const TAKE = 1000;
const MAX_SKIP = 3000;

async function discoverBase() {
  const res = await fetch(SERVICE_INDEX);
  const idx = await res.json();
  const svc = idx.resources.find(r =>
    r['@type']?.startsWith('SearchQueryService')
  );
  if (!svc) throw new Error('SearchQueryService not in service index');
  return svc['@id'];
}

async function fetchAllFromQuery(base, q) {
  const all = [];
  let skip = 0;
  let total = null;

  while (true) {
    const url = `${base}?q=${encodeURIComponent(q)}&prerelease=true&semVerLevel=2.0.0&skip=${skip}&take=${TAKE}`;
    const res = await fetch(url);
    if (!res.ok) throw new Error(`HTTP ${res.status} for ${url}`);
    const json = await res.json();

    if (total === null) total = json.totalHits;
    console.debug(`📦 "${q}" → got ${json.data.length}/${total} (skip=${skip})`);
    all.push(...json.data);

    if (skip >= MAX_SKIP) {
      console.warn(`⚠️ Skip reached limit (${skip} ≥ ${MAX_SKIP}), stopping page loop.`);
      if (total > skip + json.data.length) {
        console.warn(`⚠️ Total hits (${total}) > retrieved (${skip + json.data.length}). Some packages may be missing.`);
      }
      break;
    }

    skip += TAKE;
    if (skip >= total) break;
  }

  return all;
}

function filterAndTransform(pkgs) {
  return pkgs
    .filter(pkg => {
      const id = pkg.id.toLowerCase();
      return (
        (id.startsWith('aspire.') || id.startsWith('communitytoolkit.aspire')) &&
        pkg.verified === true &&
        (!pkg.deprecation || Object.keys(pkg.deprecation).length === 0) &&
        !['x86','x64','arm64','projecttemplates','apphost']
          .some(t => id.includes(t))
      );
    })
    .map(pkg => ({
      title: pkg.id,
      description: pkg.description,
      icon: pkg.iconUrl || 'https://www.nuget.org/Content/gallery/img/default-package-icon.svg',
      href: `https://www.nuget.org/packages/${pkg.id}`,
      tags: pkg.tags?.map(t => t.toLowerCase()) ?? [],
      downloads: pkg.totalDownloads,
      version: pkg.version,
    }));
}

(async () => {
  try {
    const base = await discoverBase();
    console.log('🔗 Using:', base);

    const results = await Promise.all(API_QUERIES.map(q => fetchAllFromQuery(base, q)));
    const merged = results.flat();
    const unique = Object.values(
      merged.reduce((acc, pkg) => (acc[pkg.id] = pkg, acc), {})
    ).sort((a,b) => a.id.localeCompare(b.id));

    const output = filterAndTransform(unique);
    fs.writeFileSync(OUTPUT_PATH, JSON.stringify(output, null, 2));
    console.log(`✅ Saved ${output.length} packages to ${OUTPUT_PATH}`);
  } catch (err) {
    console.error('❌ Error:', err);
  }
})();
