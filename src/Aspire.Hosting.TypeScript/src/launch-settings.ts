// LaunchSettings reader for TypeScript AppHosts
import * as fs from 'fs';
import * as path from 'path';

export interface LaunchProfile {
    commandName?: string;
    dotnetRunMessages?: boolean;
    launchBrowser?: boolean;
    applicationUrl?: string;
    environmentVariables?: Record<string, string>;
    commandLineArgs?: string;
}

export interface LaunchSettings {
    $schema?: string;
    profiles: Record<string, LaunchProfile>;
}

/**
 * Reads launchSettings.json from the Properties folder relative to the apphost file.
 * @param appHostDir The directory containing the apphost.ts file
 * @returns The parsed launch settings or null if not found
 */
export function readLaunchSettings(appHostDir?: string): LaunchSettings | null {
    const dir = appHostDir || process.cwd();
    const launchSettingsPath = path.join(dir, 'Properties', 'launchSettings.json');

    if (!fs.existsSync(launchSettingsPath)) {
        return null;
    }

    try {
        const content = fs.readFileSync(launchSettingsPath, 'utf-8');
        return JSON.parse(content) as LaunchSettings;
    } catch (error) {
        console.error(`Failed to read launchSettings.json: ${error}`);
        return null;
    }
}

/**
 * Gets the environment variables from the specified launch profile or the default 'https' profile.
 * @param launchSettings The parsed launch settings
 * @param profileName The profile name to use (defaults to 'https')
 * @returns The environment variables from the profile
 */
export function getEnvironmentVariables(
    launchSettings: LaunchSettings | null,
    profileName: string = 'https'
): Record<string, string> {
    if (!launchSettings) {
        return {};
    }

    const profile = launchSettings.profiles[profileName];
    if (!profile) {
        // Try the first available profile
        const firstProfileName = Object.keys(launchSettings.profiles)[0];
        if (firstProfileName) {
            const firstProfile = launchSettings.profiles[firstProfileName];
            return firstProfile.environmentVariables || {};
        }
        return {};
    }

    return profile.environmentVariables || {};
}

/**
 * Applies environment variables from launchSettings.json to process.env.
 * @param appHostDir The directory containing the apphost.ts file
 * @param profileName The profile name to use (defaults to 'https')
 */
export function applyLaunchSettings(
    appHostDir?: string,
    profileName: string = 'https'
): Record<string, string> {
    const launchSettings = readLaunchSettings(appHostDir);
    const envVars = getEnvironmentVariables(launchSettings, profileName);

    // Apply to process.env so child processes inherit them
    for (const [key, value] of Object.entries(envVars)) {
        if (!process.env[key]) {
            // Only set if not already defined (allow command-line overrides)
            process.env[key] = value;
        }
    }

    return envVars;
}
