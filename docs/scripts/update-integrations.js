import fs from 'fs';
import fetch from 'node-fetch';

const API_URLS = [
  'https://api-v2v3search-0.nuget.org/query?q=Aspire.&prerelease=true&take=300',
  'https://api-v2v3search-0.nuget.org/query?q=CommunityToolkit.Aspire&prerelease=true&take=150'
];
const OUTPUT_PATH = './src/data/aspire-integrations.json';

async function fetchPackagesFromUrl(url) {
  const res = await fetch(url);
  const data = await res.json();
  return data.data;
}

function filterAndTransform(packages) {
  return packages
    .filter(pkg => {
      const id = pkg.id.toLowerCase();
      return (
        (id.startsWith('aspire.') || id.startsWith('communitytoolkit.aspire')) &&
        pkg.verified === true &&
        !pkg.deprecation &&
        !id.includes('x86') &&
        !id.includes('x64') &&
        !id.includes('arm64') &&
        !id.includes('projecttemplates') &&
        !id.includes('apphost')
      );
    })
    .map(pkg => ({
      title: pkg.id,
      description: pkg.description,
      icon: pkg.iconUrl || 'https://www.nuget.org/Content/gallery/img/default-package-icon.svg',
      href: `https://www.nuget.org/packages/${pkg.id}`,
      tags: pkg.tags?.map(tag => tag.toLowerCase()) ?? [],
      downloads: pkg.totalDownloads,
      version: pkg.version,
    }));
}

async function fetchAllPackages() {
  const results = await Promise.all(API_URLS.map(fetchPackagesFromUrl));
  // Flatten and deduplicate by package id
  const allPackages = [...results[0], ...results[1]];
  const uniquePackages = Object.values(
    allPackages.reduce((acc, pkg) => {
      acc[pkg.id] = pkg;
      return acc;
    }, {})
  ).sort((a, b) => a.id.localeCompare(b.id));
  const transformed = filterAndTransform(uniquePackages);

  fs.writeFileSync(OUTPUT_PATH, JSON.stringify(transformed, null, 2));
  console.log(`✅ Saved ${transformed.length} packages to ${OUTPUT_PATH}`);
}

fetchAllPackages().catch(err => {
  console.error('❌ Failed to fetch NuGet packages', err);
});