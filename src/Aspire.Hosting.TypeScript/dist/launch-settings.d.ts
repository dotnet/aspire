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
export declare function readLaunchSettings(appHostDir?: string): LaunchSettings | null;
/**
 * Gets the environment variables from the specified launch profile or the default 'https' profile.
 * @param launchSettings The parsed launch settings
 * @param profileName The profile name to use (defaults to 'https')
 * @returns The environment variables from the profile
 */
export declare function getEnvironmentVariables(launchSettings: LaunchSettings | null, profileName?: string): Record<string, string>;
/**
 * Applies environment variables from launchSettings.json to process.env.
 * @param appHostDir The directory containing the apphost.ts file
 * @param profileName The profile name to use (defaults to 'https')
 */
export declare function applyLaunchSettings(appHostDir?: string, profileName?: string): Record<string, string>;
