import fs from 'fs';
import fetch from 'node-fetch';

const API_URL = 'https://azuresearch-usnc.nuget.org/query?q=owner:aspire&take=200';
const OUTPUT_PATH = './src/data/aspire-integrations.json';

async function fetchPackages() {
  const res = await fetch(API_URL);
  const data = await res.json();

  const packages = data.data.filter(pkg => {
    const id = pkg.id.toLowerCase();
    return (
      id.startsWith('aspire.') &&
      pkg.verified === true &&
      !pkg.deprecation &&
      !id.includes('x86') &&
      !id.includes('x64') &&
      !id.includes('arm64') &&
      !id.includes('projecttemplates') &&
      !id.includes('apphost')
    );
  });

  const transformed = packages.map(pkg => ({
    title: pkg.id,
    description: pkg.description,
    icon: pkg.iconUrl || 'https://www.nuget.org/Content/gallery/img/default-package-icon.svg',
    href: `https://www.nuget.org/packages/${pkg.id}`,
    tags: pkg.tags?.map(tag => tag.toLowerCase()) ?? [],
    downloads: pkg.totalDownloads,
    version: pkg.version,
  }));

  fs.writeFileSync(OUTPUT_PATH, JSON.stringify(transformed, null, 2));
  console.log(`✅ Saved ${transformed.length} packages to ${OUTPUT_PATH}`);
}

fetchPackages().catch(err => {
  console.error('❌ Failed to fetch NuGet packages', err);
});