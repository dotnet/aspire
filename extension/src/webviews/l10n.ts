/**
 * Localization utility for webviews.
 * Localization strings are injected at build time from package.nls.json
 */

// This will be replaced by webpack with actual localization data
declare const __WEBVIEW_LOCALIZATIONS__: Record<string, string>;

let localizationCache: Record<string, string> | null = null;

/**
 * Get a localized string by key.
 * Keys correspond to package.nls.json entries (without the 'aspire-vscode.strings.' prefix).
 * 
 * @param key The localization key (e.g., 'aspireConfiguration')
 * @param defaultValue Optional default value if key not found
 * @returns The localized string
 */
export function l10n(key: string, defaultValue?: string): string {
  if (!localizationCache) {
    try {
      localizationCache = typeof __WEBVIEW_LOCALIZATIONS__ !== 'undefined' 
        ? __WEBVIEW_LOCALIZATIONS__ 
        : {};
    } catch {
      localizationCache = {};
    }
  }
  
  // Try with the aspire-vscode.strings. prefix
  const prefixedKey = `aspire-vscode.strings.${key}`;
  const value = localizationCache[prefixedKey] || localizationCache[key];
  
  return value || defaultValue || key;
}
